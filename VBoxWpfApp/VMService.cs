using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtualBox;

namespace VBoxWpfApp
{
    public static class VMService
    {
        private static readonly IVirtualBox _virtualBox;
        private static readonly string LogFilePath = "log.txt";
        private static readonly string HistoryFilePath = "history.json";

        static VMService()
        {
            try
            {
                _virtualBox = new VirtualBox.VirtualBox();
            }
            catch (Exception ex)
            {
                WriteLog($"Ошибка инициализации VirtualBox: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получение списка всех виртуальных машин.
        /// </summary>
        public static List<VmModel> GetAllMachines()
        {
            var list = new List<VmModel>();
            try
            {
                foreach (IMachine machine in _virtualBox.Machines ?? new IMachine[0])
                {
                    list.Add(VmModel.FromIMachine(machine).Result);
                }
                WriteLog("Загружен список виртуальных машин.");
            }
            catch (Exception ex)
            {
                WriteLog($"Ошибка получения списка машин: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Запуск виртуальной машины.
        /// </summary>
        public static async Task StartVM(string name)
        {
            await ExecuteAndLog(name, "запущена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) throw new Exception($"Машина '{name}' не найдена.");

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Write);
                    IProgress progress = machine.LaunchVMProcess((Session)session, "gui", "".ToCharArray());
                    await WaitForProgress(progress);
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        /// <summary>
        /// Остановка виртуальной машины.
        /// </summary>
        public static async Task StopVM(string name)
        {
            await ExecuteAndLog(name, "остановлена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) throw new Exception($"Машина '{name}' не найдена.");

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    IProgress progress = console.PowerDown();
                    await WaitForProgress(progress);
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        /// <summary>
        /// Приостановка виртуальной машины.
        /// </summary>
        public static async Task PauseVM(string name)
        {
            await ExecuteAndLog(name, "приостановлена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) throw new Exception($"Машина '{name}' не найдена.");

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.Pause();
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        /// <summary>
        /// Возобновление работы виртуальной машины.
        /// </summary>
        public static async Task ResumeVM(string name)
        {
            await ExecuteAndLog(name, "возобновила работу", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) throw new Exception($"Машина '{name}' не найдена.");

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.Resume();
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        /// <summary>
        /// Получение метрик производительности виртуальной машины.
        /// </summary>
        public static async Task<Dictionary<string, object>> GetPerformanceMetrics(string machineName, string[] metricNames, int period = 1, int sampleCount = 10)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var machine = _virtualBox.FindMachine(machineName);
                    if (machine == null) return null;

                    var collector = _virtualBox.PerformanceCollector;
                    var metrics = new Dictionary<string, object>();
                    collector.SetupMetrics(metricNames, new[] { machine }, period, (uint)sampleCount);

                    var data = collector.QueryMetricsData(metricNames, new[] { machine });
                    for (int i = 0; i < data[1].Length; i++)
                    {
                        var metricData = new
                        {
                            Name = data[1][i], // Имя метрики
                            Object = data[2][i], // Объект
                            Unit = data[3][i], // Единица измерения
                            Scale = data[4][i], // Масштаб
                            Values = data[0].Skip(data[5][i]).Take(data[7][i]).ToArray() // Значения
                        };
                        metrics[metricData.Name] = metricData;
                    }

                    return metrics;
                }
                catch (Exception ex)
                {
                    WriteLog($"Ошибка получения метрик для '{machineName}': {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Создание новой виртуальной машины с заданными параметрами.
        /// </summary>
        public static async Task CreateVM(VmConfig config)
        {
            await Task.Run(() =>
            {
                try
                {
                    IMachine machine = _virtualBox.CreateMachine(
                        settingsFile: "",
                        name: config.Name,
                        osTypeId: config.OSTypeId,
                        id: "",
                        forceOverwrite: false
                    );

                    // Настройка параметров
                    machine.CPUCount = (uint)config.CPUs;
                    machine.MemorySize = (uint)config.MemorySizeMB;
                    machine.SetCPUProperty(CPUPropertyType.CPUPropertyType_PAE, config.HasPAE ? 1 : 0);
                    machine.SetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_VPID, config.HasVtX ? 1 : 0);
                    machine.SetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_NestedPaging, config.HasPAG ? 1 : 0);

                    // Настройка жёсткого диска
                    if (config.HDDSizeGB > 0)
                    {
                        IMedium medium = _virtualBox.CreateMedium(
                            format: "VDI",
                            location: Path.Combine(_virtualBox.DefaultMachineFolder, config.Name, $"{config.Name}.vdi"),
                            accessMode: AccessMode.AccessMode_ReadWrite,
                            aDeviceType: DeviceType.DeviceType_HardDisk
                        );

                        IProgress progress = medium.CreateBaseStorage((long)config.HDDSizeGB * 1_000_000_000, new[] { MediumVariant.MediumVariant_Standard });
                        progress.WaitForCompletion(-1);

                        IStorageController controller = machine.AddStorageController("SATA Controller", StorageBus.StorageBus_SATA);
                        machine.AttachDevice("SATA Controller", 0, 0, DeviceType.DeviceType_HardDisk, medium);
                    }

                    // Настройка сети
                    for (uint i = 0; i < (uint)config.EthernetAdapterCount; i++)
                    {
                        INetworkAdapter adapter = machine.GetNetworkAdapter(i);
                        adapter.Enabled = 1;
                        adapter.AttachmentType = NetworkAttachmentType.NetworkAttachmentType_NAT;
                    }

                    machine.SaveSettings();
                    _virtualBox.RegisterMachine(machine);

                    WriteLog($"Виртуальная машина '{config.Name}' создана.");
                }
                catch (Exception ex)
                {
                    WriteLog($"Ошибка создания ВМ '{config.Name}': {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Удаление виртуальной машины.
        /// </summary>
        public static async Task DeleteVM(string name)
        {
            await Task.Run(() =>
            {
                try
                {
                    IMachine machine = _virtualBox.FindMachine(name);
                    if (machine == null) throw new Exception($"Машина '{name}' не найдена.");

                    var media = machine.Unregister(CleanupMode.CleanupMode_Full);
                    foreach (IMedium medium in media)
                    {
                        IProgress progress = medium.DeleteStorage();
                        progress.WaitForCompletion(-1);
                    }

                    WriteLog($"Виртуальная машина '{name}' удалена.");
                }
                catch (Exception ex)
                {
                    WriteLog($"Ошибка удаления ВМ '{name}': {ex.Message}");
                    throw;
                }
            });
        }

        private static async Task ExecuteAndLog(string name, string action, Func<Task> action)
        {
            try
            {
                await Task.Run(async () =>
                {
                    await action();
                    string msg = $"ВМ '{name}' {action}";
                    WriteLog(msg);
                    AppendHistory(msg);
                    ToastHelper.ShowToast(msg);
                });
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка для ВМ '{name}' ({action}): {ex.Message}";
                WriteLog(errorMsg);
                ToastHelper.ShowToast(errorMsg);
            }
        }

        private static async Task WaitForProgress(IProgress progress)
        {
            await Task.Run(() =>
            {
                progress.WaitForCompletion(-1);
                if (progress.ResultCode != 0)
                {
                    throw new Exception($"Ошибка выполнения операции: {progress.ErrorInfo.Text}");
                }
            });
        }

        private static void AppendHistory(string message)
        {
            var history = new List<string>();
            if (File.Exists(HistoryFilePath))
            {
                try
                {
                    history = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(HistoryFilePath)) ?? [];
                }
                catch { }
            }

            history.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
            File.WriteAllText(HistoryFilePath, JsonConvert.SerializeObject(history, Formatting.Indented));
        }


        private static void WriteLog(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            try
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch { }
        }

        public static string GetMachineStateDescription(MachineState state)
        {
            switch (state)
            {
                case MachineState.MachineState_Null: return "Неизвестное состояние";

                case MachineState.MachineState_PoweredOff: return "Выключена";
                case MachineState.MachineState_Saved: return "Сохранена";
                case MachineState.MachineState_Teleported: return "Телепортирована";
                case MachineState.MachineState_Aborted: return "Аварийно завершена";
                case MachineState.MachineState_AbortedSaved: return "Аварийно завершена (сохранена)";

                // Running и FirstOnline имеют одинаковое значение (6)
                case MachineState.MachineState_Running:
                    return "Запущена / Онлайн (первое состояние)";

                case MachineState.MachineState_Paused: return "Приостановлена";
                case MachineState.MachineState_Stuck: return "Зависла (Guru Meditation)";

                // Teleporting и FirstTransient имеют значение 9
                case MachineState.MachineState_Teleporting:
                    return "Телепортируется / Первое переходное состояние";

                case MachineState.MachineState_LiveSnapshotting: return "Создание Live-снимка";
                case MachineState.MachineState_Starting: return "Запускается";
                case MachineState.MachineState_Stopping: return "Останавливается";
                case MachineState.MachineState_Saving: return "Сохраняется";
                case MachineState.MachineState_Restoring: return "Восстанавливается";

                case MachineState.MachineState_TeleportingPausedVM: return "Телепортируется (пауза)";
                case MachineState.MachineState_TeleportingIn: return "Входящая телепортация";

                case MachineState.MachineState_DeletingSnapshotOnline: return "Удаление снимка (в процессе работы)";
                case MachineState.MachineState_DeletingSnapshotPaused: return "Удаление снимка (на паузе)";

                // OnlineSnapshotting и LastOnline имеют значение 19
                case MachineState.MachineState_OnlineSnapshotting:
                    return "Создание снимка онлайн / Последнее онлайн-состояние";

                case MachineState.MachineState_RestoringSnapshot: return "Восстановление снимка";
                case MachineState.MachineState_DeletingSnapshot: return "Удаление снимка";
                case MachineState.MachineState_SettingUp: return "Настройка";

                // Snapshotting и LastTransient имеют значение 23
                case MachineState.MachineState_Snapshotting:
                    return "Создание снимка / Последнее переходное состояние";

                default:
                    return $"Неизвестное состояние ({state})";
            }
        }


    }

    /// <summary>
    /// Модель конфигурации для создания новой виртуальной машины.
    /// </summary>
    public class VmConfig
    {
        public string Name { get; set; }
        public string OSTypeId { get; set; }
        public int CPUs { get; set; }
        public int MemorySizeMB { get; set; }
        public int HDDSizeGB { get; set; }
        public bool HasPAE { get; set; }
        public bool HasVtX { get; set; }
        public bool HasPAG { get; set; }
        public int EthernetAdapterCount { get; set; }
    }
}
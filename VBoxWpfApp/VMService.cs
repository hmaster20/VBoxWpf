using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    IProgress progress = machine.LaunchVMProcess((Session)session, "gui", "".ToArray());
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
        public static async Task<Dictionary<string, object>> GetPerformanceMetrics(string machineName, string[] metricNames, int period = 1, uint sampleCount = 10)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var machine = _virtualBox.FindMachine(machineName);
                    if (machine == null) return null;

                    var collector = _virtualBox.PerformanceCollector;
                    var metrics = new Dictionary<string, object>();
                    collector.SetupMetrics(metricNames, new[] { machine }, (uint)period, sampleCount);

                    Array returnMetricNames, returnObjects, returnUnits, returnScales, returnSequenceNumbers, returnDataIndices, returnDataLengths;
                    var returnData = collector.QueryMetricsData(
                        metricNames,
                        new[] { machine },
                        out returnMetricNames,
                        out returnObjects,
                        out returnUnits,
                        out returnScales,
                        out returnSequenceNumbers,
                        out returnDataIndices,
                        out returnDataLengths
                    );

                    for (int i = 0; i < returnMetricNames.Length; i++)
                    {
                        var metricData = new
                        {
                            Name = (string)returnMetricNames.GetValue(i),
                            Object = returnObjects.GetValue(i)?.ToString(),
                            Unit = (string)returnUnits.GetValue(i),
                            Scale = Convert.ToInt32(returnScales.GetValue(i)),
                            Values = returnData.Cast<int>().Skip(Convert.ToInt32(returnDataIndices.GetValue(i))).Take(Convert.ToInt32(returnDataLengths.GetValue(i))).ToArray()
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
                        aSettingsFile: "",
                        aName: config.Name,
                        aGroups: new string[] { "" },
                        aOSTypeId: config.OSTypeId,
                        aFlags: "",
                        aCipher: "",
                        aPasswordId: "",
                        aPassword: ""
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
                        string machineFolder = Path.Combine(_virtualBox.SystemProperties.DefaultMachineFolder, config.Name);
                        Directory.CreateDirectory(machineFolder);

                        IMedium medium = _virtualBox.CreateMedium(
                            "VDI",
                            Path.Combine(machineFolder, $"{config.Name}.vdi"),
                            AccessMode.AccessMode_ReadWrite,
                            DeviceType.DeviceType_HardDisk
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
                        adapter.AttachmentType = config.NetworkType;
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

        /// <summary>
        /// Импорт виртуальной машины из OVA/OVF файла.
        /// </summary>
        public static async Task ImportVM(string ovaPath, string vmName)
        {
            await Task.Run(() =>
            {
                try
                {
                    IAppliance appliance = _virtualBox.CreateAppliance();
                    appliance.Read(ovaPath);
                    IProgress progress = appliance.ImportMachines(new[] { ImportOptions.ImportOptions_KeepAllMACs });
                    progress.WaitForCompletion(-1);
                    WriteLog($"ВМ '{vmName}' импортирована из {ovaPath}.");
                }
                catch (Exception ex)
                {
                    WriteLog($"Ошибка импорта ВМ '{vmName}': {ex.Message}");
                    throw;
                }
            });
        }

        private static async Task ExecuteAndLog(string name, string actionMessage, Func<Task> actionFunc)
        {
            try
            {
                await Task.Run(async () =>
                {
                    await actionFunc();
                    string msg = $"ВМ '{name}' {actionMessage}";
                    WriteLog(msg);
                    AppendHistory(msg);
                    ToastHelper.ShowToast(msg);
                });
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка для ВМ '{name}' ({actionMessage}): {ex.Message}";
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
                    history = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(HistoryFilePath));
                }
                catch { }
            }

            history.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
            File.WriteAllText(HistoryFilePath, JsonConvert.SerializeObject(history, Formatting.Indented));
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
                case MachineState.MachineState_Running: return "Запущена / Онлайн (первое состояние)";
                case MachineState.MachineState_Paused: return "Приостановлена";
                case MachineState.MachineState_Stuck: return "Зависла (Guru Meditation)";
                case MachineState.MachineState_Teleporting: return "Телепортируется / Первое переходное состояние";
                case MachineState.MachineState_LiveSnapshotting: return "Создание Live-снимка";
                case MachineState.MachineState_Starting: return "Запускается";
                case MachineState.MachineState_Stopping: return "Останавливается";
                case MachineState.MachineState_Saving: return "Сохраняется";
                case MachineState.MachineState_Restoring: return "Восстанавливается";
                case MachineState.MachineState_TeleportingPausedVM: return "Телепортируется (пауза)";
                case MachineState.MachineState_TeleportingIn: return "Входящая телепортация";
                case MachineState.MachineState_DeletingSnapshotOnline: return "Удаление снимка (в процессе работы)";
                case MachineState.MachineState_DeletingSnapshotPaused: return "Удаление снимка (на паузе)";
                case MachineState.MachineState_OnlineSnapshotting: return "Создание снимка онлайн / Последнее онлайн-состояние";
                case MachineState.MachineState_RestoringSnapshot: return "Восстановление снимка";
                case MachineState.MachineState_DeletingSnapshot: return "Удаление снимка";
                case MachineState.MachineState_SettingUp: return "Настройка";
                case MachineState.MachineState_Snapshotting: return "Создание снимка / Последнее переходное состояние";
                default: return $"Неизвестное состояние ({state})";
            }
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
        public NetworkAttachmentType NetworkType { get; set; } = NetworkAttachmentType.NetworkAttachmentType_NAT;
    }
}
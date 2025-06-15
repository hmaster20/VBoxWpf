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
        private static readonly IVirtualBox _virtualBox = new VirtualBox.VirtualBox();
        private static readonly string LogFilePath = "log.txt";
        private static readonly string HistoryFilePath = "history.json";

        public static List<VmModel> GetAllMachines()
        {
            var list = new List<VmModel>();
            foreach (IMachine machine in _virtualBox.Machines ?? new IMachine[0])
            {
                list.Add(VmModel.FromIMachine(machine));
            }
            WriteLog("Загружен список виртуальных машин.");
            return list;
        }

        public static async Task StartVM(string name)
        {
            await ExecuteAndLog(name, "запущена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Write);
                    IConsole console = session.Console;
                    console.PowerUp();
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        public static async Task StopVM(string name)
        {
            await ExecuteAndLog(name, "остановлена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.PowerDown();
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        public static async Task PauseVM(string name)
        {
            await ExecuteAndLog(name, "приостановлена", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

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

        public static async Task ResumeVM(string name)
        {
            await ExecuteAndLog(name, "возобновила работу", async () =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

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

        private static async Task ExecuteAndLog(string name, string action, Func<Task> func)
        {
            await Task.Run(() =>
            {
                func();
                string msg = $"ВМ '{name}' {action}";
                WriteLog(msg);
                SaveHistory(msg);
                ToastHelper.ShowToast(msg);
            });
        }

        private static void SaveHistory(string message)
        {
            var history = new List<string>();
            if (File.Exists(HistoryFilePath))
            {
                history = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(HistoryFilePath)) ?? new List<string>();
            }

            history.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");
            File.WriteAllText(HistoryFilePath, JsonConvert.SerializeObject(history, Formatting.Indented));
        }

        public static string GetMachineStateDescription(MachineState state)
        {
            switch (state)
            {
                case MachineState.MachineState_PoweredOff: return "Выключена";
                case MachineState.MachineState_Running: return "Запущена";
                case MachineState.MachineState_Paused: return "Приостановлена";
                default: return $"Неизвестное состояние ({state})";
            }
        }

        private static void WriteLog(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }
    }
}
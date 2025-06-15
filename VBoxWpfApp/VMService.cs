using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VirtualBox;

namespace VBoxWpfApp
{
    public static class VMService
    {
        private static readonly string LogFilePath = "log.txt";
        private static IVirtualBox _virtualBox = new VirtualBox.VirtualBox();

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
            await Task.Run(() =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Write);
                    IConsole console = session.Console;
                    console.PowerUp();
                    WriteLog($"ВМ '{name}' запущена");
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        public static async Task StopVM(string name)
        {
            await Task.Run(() =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.PowerDown();
                    WriteLog($"ВМ '{name}' остановлена");
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        public static async Task PauseVM(string name)
        {
            await Task.Run(() =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.Pause();
                    WriteLog($"ВМ '{name}' приостановлена");
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
        }

        public static async Task ResumeVM(string name)
        {
            await Task.Run(() =>
            {
                IMachine machine = _virtualBox.FindMachine(name);
                if (machine == null) return;

                ISession session = new Session();
                try
                {
                    machine.LockMachine((Session)session, LockType.LockType_Shared);
                    IConsole console = session.Console;
                    console.Resume();
                    WriteLog($"ВМ '{name}' возобновила работу");
                }
                finally
                {
                    session.UnlockMachine();
                }
            });
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
using System;
using VirtualBox;
using System.Linq;
using VirtualBox;

namespace VBoxWpfApp
{
    public class VmModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string OSTypeId { get; set; }
        public int CPUs { get; set; }

        //VirtualBox COM API напрямую нет метода
        //public string CPUUsagePercent => $"{CPUs * 50}%"; // Условное значение | => $"{new Random().Next(0, 100)}%"; // Заглушка
        public string CPUModes => $"PAE: {(HasPAE ? "Да" : "Нет")}, VT-x: {(HasVtX ? "Да" : "Нет")}, NestedPaging: {(HasPAG ? "Да" : "Нет")}"; // Пример
        public long MemorySizeMB { get; set; }
        public string MemorySize => $"{MemorySizeMB} MB";
        public string HDDSizeGB => $"{(HDDSizeBytes / 1_000_000_000)} GB";
        public long HDDSizeBytes { get; set; }
        public bool HasPAE { get; set; }
        public bool HasVtX { get; set; }
        public bool HasPAG { get; set; }
        public bool HasPARAv { get; set; }
        public string EthernetCount => $"{EthernetAdapterCount} адаптеров";
        public int EthernetAdapterCount { get; set; }
        public string StateDescription => VMService.GetMachineStateDescription(State);
        public MachineState State { get; set; }

        public static VmModel FromIMachine(IMachine machine)
        {
            // Получаем размер дисков
            ulong totalSize = 0;

            foreach (IMediumAttachment attachment in machine.MediumAttachments)
            {
                if (attachment?.Medium != null && attachment.Medium.DeviceType == DeviceType.DeviceType_HardDisk)
                {
                    totalSize += (ulong)attachment.Medium.LogicalSize;
                }
            }



            // Подсчёт сетевых адаптеров
            int networkCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if (machine.GetNetworkAdapter((uint)i)?.Enabled == 1)
                    networkCount++;
            }

            return new VmModel
            {
                Name = machine.Name,
                Description = machine.Description,
                OSTypeId = machine.OSTypeId,
                CPUs = (int)machine.CPUCount,
                //CPUUsagePercent = 
                MemorySizeMB = (int)machine.MemorySize,
                HDDSizeBytes = (long)totalSize,
                HasPAE = machine.GetCPUProperty(CPUPropertyType.CPUPropertyType_PAE) == 1,
                HasVtX = machine.GetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_VPID) == 1,
                HasPAG = machine.GetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_NestedPaging) == 1,
                HasPARAv = machine.GetEffectiveParavirtProvider() == ParavirtProvider.ParavirtProvider_Default,
                EthernetAdapterCount = networkCount,
                State = machine.State

                //https://pythonhosted.org/pyvbox/virtualbox/library.html
            };

        }
    }
}
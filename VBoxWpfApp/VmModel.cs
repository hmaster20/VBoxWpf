using System.Threading.Tasks;
using VirtualBox;

namespace VBoxWpfApp
{
    public class VmModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string OSTypeId { get; set; }
        public int CPUs { get; set; }
        public string CPUUsagePercent { get; set; } // Реальное значение
        public string CPUModes => $"PAE: {(HasPAE ? "Да" : "Нет")}, VT-x: {(HasVtX ? "Да" : "Нет")}, NestedPaging: {(HasPAG ? "Да" : "Нет")}";
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

        public static async Task<VmModel> FromIMachine(IMachine machine)
        {
            ulong totalSize = 0;
            foreach (IMediumAttachment attachment in machine.MediumAttachments)
            {
                if (attachment?.Medium != null && attachment.Medium.DeviceType == DeviceType.DeviceType_HardDisk)
                {
                    totalSize += (ulong)attachment.Medium.LogicalSize;
                }
            }

            int networkCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if (machine.GetNetworkAdapter((uint)i)?.Enabled == 1)
                    networkCount++;
            }

            var model = new VmModel
            {
                Name = machine.Name,
                Description = machine.Description,
                OSTypeId = machine.OSTypeId,
                CPUs = (int)machine.CPUCount,
                MemorySizeMB = (int)machine.MemorySize,
                HDDSizeBytes = (long)totalSize,
                HasPAE = machine.GetCPUProperty(CPUPropertyType.CPUPropertyType_PAE) == 1,
                HasVtX = machine.GetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_VPID) == 1,
                HasPAG = machine.GetHWVirtExProperty(HWVirtExPropertyType.HWVirtExPropertyType_NestedPaging) == 1,
                HasPARAv = machine.GetEffectiveParavirtProvider() == ParavirtProvider.ParavirtProvider_Default,
                EthernetAdapterCount = networkCount,
                State = machine.State
            };

            // Получение метрик производительности
            var metrics = await VMService.GetPerformanceMetrics(machine.Name, new[] { "CPU/Load/User", "CPU/Load/Kernel" });
            if (metrics != null && metrics.ContainsKey("CPU/Load/User"))
            {
                var userCpu = (dynamic)metrics["CPU/Load/User"];
                var kernelCpu = (dynamic)metrics["CPU/Load/Kernel"];
                var totalCpu = userCpu.Values.LastOrDefault() + kernelCpu.Values.LastOrDefault();
                model.CPUUsagePercent = $"{totalCpu / userCpu.Scale:F2}%";
            }
            else
            {
                model.CPUUsagePercent = "N/A";
            }

            return model;
        }
    }
}
using VirtualBox;

namespace VBoxWpfApp
{
    public class VmModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string StateDescription => VMService.GetMachineStateDescription(State);
        public MachineState State { get; set; }
        public string OSTypeId { get; set; }

        public static VmModel FromIMachine(IMachine machine)
        {
            return new VmModel
            {
                Name = machine.Name,
                Description = machine.Description,
                State = machine.State,
                OSTypeId = machine.OSTypeId
            };
        }
    }
}
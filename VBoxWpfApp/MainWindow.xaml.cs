using System.Collections.Generic;
using System.Windows;

namespace VBoxWpfApp
{
    public partial class MainWindow : Window
    {
        private List<VmModel> _vmList = new List<VmModel>();

        public MainWindow()
        {
            InitializeComponent();
            LoadMachines();
        }

        private void LoadMachines()
        {
            _vmList = VMService.GetAllMachines();
            VmList.ItemsSource = _vmList;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (VmList.SelectedItem is VmModel vm)
            {
                await VMService.StartVM(vm.Name);
                Log($"[INFO] Машина '{vm.Name}' запущена");
            }
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (VmList.SelectedItem is VmModel vm)
            {
                await VMService.StopVM(vm.Name);
                Log($"[INFO] Машина '{vm.Name}' остановлена");
            }
        }

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (VmList.SelectedItem is VmModel vm)
            {
                await VMService.PauseVM(vm.Name);
                Log($"[INFO] Машина '{vm.Name}' приостановлена");
            }
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (VmList.SelectedItem is VmModel vm)
            {
                await VMService.ResumeVM(vm.Name);
                Log($"[INFO] Машина '{vm.Name}' возобновила работу");
            }
        }

        private void Log(string message)
        {
            LogOutput.AppendText(message + "\n");
            LogOutput.ScrollToEnd();
        }
    }
}
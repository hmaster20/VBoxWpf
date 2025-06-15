using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace VBoxWpfApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<VmModel> _vmList = new List<VmModel>();
        private VmModel _selectedVm;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadMachines();
        }

        public List<VmModel> VmList
        {
            get => _vmList;
            set
            {
                _vmList = value;
                OnPropertyChanged();
            }
        }

        public VmModel SelectedVm
        {
            get => _selectedVm;
            set
            {
                _selectedVm = value;
                OnPropertyChanged();
            }
        }

        private void LoadMachines()
        {
            VmList = VMService.GetAllMachines();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);
            if (SelectedVm != null)
            {
                await VMService.StartVM(SelectedVm.Name);
                Log($"[INFO] Машина '{SelectedVm.Name}' запущена");
            }
            ShowProgress(false);
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);
            if (SelectedVm != null)
            {
                await VMService.StopVM(SelectedVm.Name);
                Log($"[INFO] Машина '{SelectedVm.Name}' остановлена");
            }
            ShowProgress(false);
        }

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);
            if (SelectedVm != null)
            {
                await VMService.PauseVM(SelectedVm.Name);
                Log($"[INFO] Машина '{SelectedVm.Name}' приостановлена");
            }
            ShowProgress(false);
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress(true);
            if (SelectedVm != null)
            {
                await VMService.ResumeVM(SelectedVm.Name);
                Log($"[INFO] Машина '{SelectedVm.Name}' возобновила работу");
            }
            ShowProgress(false);
        }

        private void Log(string message)
        {
            LogOutput.AppendText(message + "\n");
            LogOutput.ScrollToEnd();
        }

        private void ShowProgress(bool show)
        {
            ProgressIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
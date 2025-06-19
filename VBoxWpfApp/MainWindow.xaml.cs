using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
            ThemeManager.LoadSavedTheme();
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

        private async void LoadMachines()
        {
            var machines = await Task.Run(() => VMService.GetAllMachines());
            VmList = new List<VmModel>(machines);
            MachineList.ItemsSource = VmList;
        }

        private void MachineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MachineList.SelectedItem is VmModel selected)
            {
                SelectedVm = selected;
                OnPropertyChanged(nameof(SelectedVm));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void FilterName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (FilterName.Text == "Фильтр по имени...")
                FilterName.Text = "";
        }

        private void FilterName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilterName.Text))
                FilterName.Text = "Фильтр по имени...";
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            var filterName = FilterName.Text == "Фильтр по имени..." ? "" : FilterName.Text;
            var filterState = FilterState.SelectedItem as ComboBoxItem;

            var filtered = VMService.GetAllMachines().AsEnumerable();
            if (!string.IsNullOrEmpty(filterName))
            {
                filtered = filtered.Where(vm => vm.Name.IndexOf(filterName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (filterState?.Content.ToString() != "Все")
            {
                filtered = filtered.Where(vm => vm.StateDescription == filterState.Content.ToString());
            }

            VmList = new List<VmModel>(filtered);
            MachineList.ItemsSource = VmList;
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVm != null)
            {
                ProgressIndicator.Visibility = Visibility.Visible;
                await VMService.StartVM(SelectedVm.Name);
                ProgressIndicator.Visibility = Visibility.Collapsed;
                LoadMachines();
            }
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVm != null)
            {
                ProgressIndicator.Visibility = Visibility.Visible;
                await VMService.StopVM(SelectedVm.Name);
                ProgressIndicator.Visibility = Visibility.Collapsed;
                LoadMachines();
            }
        }

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVm != null)
            {
                ProgressIndicator.Visibility = Visibility.Visible;
                await VMService.PauseVM(SelectedVm.Name);
                ProgressIndicator.Visibility = Visibility.Collapsed;
                LoadMachines();
            }
        }

        private async void Resume_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVm != null)
            {
                ProgressIndicator.Visibility = Visibility.Visible;
                await VMService.ResumeVM(SelectedVm.Name);
                ProgressIndicator.Visibility = Visibility.Collapsed;
                LoadMachines();
            }
        }

        private void CreateVm_Click(object sender, RoutedEventArgs e)
        {
            var createVmWindow = new CreateVmWindow();
            if (createVmWindow.ShowDialog() == true)
            {
                LoadMachines();
            }
        }

        private async void ImportVm_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "OVA/OVF Files (*.ova;*.ovf)|*.ova;*.ovf",
                Title = "Выберите файл для импорта"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressIndicator.Visibility = Visibility.Visible;
                var vmName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                try
                {
                    await VMService.ImportVM(openFileDialog.FileName, vmName);
                    ToastHelper.ShowToast($"ВМ '{vmName}' успешно импортирована.");
                }
                catch (Exception ex)
                {
                    ToastHelper.ShowToast($"Ошибка импорта: {ex.Message}");
                }
                finally
                {
                    ProgressIndicator.Visibility = Visibility.Collapsed;
                    LoadMachines();
                }
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
            ThemeManager.SaveCurrentTheme();
        }
    }
}
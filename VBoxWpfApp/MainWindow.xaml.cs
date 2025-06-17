// Убедитесь, что все эти using присутствуют
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading; // Для DispatcherTimer
using System.Linq; // Для AsEnumerable()
using System;

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

            // Установите начальную тему
            ThemeManager.LoadSavedTheme(); // Загружаем сохранённую тему

            LoadMachines();
            //ApplySavedTheme();

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

        private void ApplySavedTheme()
        {
            string savedTheme = Properties.Settings.Default.Theme ?? "DarkTheme";
            ThemeManager.ApplyTheme(savedTheme);
            //ThemeManager.IsDarkTheme = savedTheme == "DarkTheme";
        }

   private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.ToggleTheme(); // Переключаем тему
        ThemeManager.SaveCurrentTheme(); // Сохраняем выбор
    }

        private void LoadMachines()
        {
            var machines = VMService.GetAllMachines();
            VmList = new List<VmModel>(machines);
            MachineList.ItemsSource = VmList;
        }

        private void MachineList_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (MachineList.SelectedItem is VmModel selected)
            {
                SelectedVm = selected;
                OnPropertyChanged(nameof(SelectedVm)); // Явное обновление
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
            // Здесь реализуйте логику фильтрации
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            // Реализация запуска ВМ
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            // Реализация остановки ВМ
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            // Реализация паузы ВМ
        }

        private void Resume_Click(object sender, RoutedEventArgs e)
        {
            // Реализация возобновления ВМ
        }


    }
}
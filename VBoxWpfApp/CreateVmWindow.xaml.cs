using System;
using System.Windows;
using System.Windows.Controls;
using VirtualBox;

namespace VBoxWpfApp
{
    public partial class CreateVmWindow : Window
    {
        public CreateVmWindow()
        {
            InitializeComponent();
            NetworkType.SelectedIndex = 0; // NAT по умолчанию
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = new VmConfig
                {
                    Name = VmName.Text,
                    OSTypeId = (OsType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    CPUs = int.Parse(CpuCount.Text),
                    MemorySizeMB = int.Parse(MemorySize.Text),
                    HDDSizeGB = int.Parse(HddSize.Text),
                    HasPAE = HasPAE.IsChecked ?? false,
                    HasVtX = HasVtX.IsChecked ?? false,
                    HasPAG = HasPAG.IsChecked ?? false,
                    EthernetAdapterCount = int.Parse(EthernetCount.Text)
                };

                string networkTypeTag = (NetworkType.SelectedItem as ComboBoxItem)?.Tag.ToString();
                switch (networkTypeTag)
                {
                    case "Bridged":
                        config.NetworkType = NetworkAttachmentType.NetworkAttachmentType_Bridged;
                        break;
                    case "HostOnly":
                        config.NetworkType = NetworkAttachmentType.NetworkAttachmentType_HostOnly;
                        break;
                    default:
                        config.NetworkType = NetworkAttachmentType.NetworkAttachmentType_NAT;
                        break;
                }

                if (string.IsNullOrEmpty(config.Name) || config.CPUs <= 0 || config.MemorySizeMB <= 0 || 
                    config.HDDSizeGB <= 0 || config.EthernetAdapterCount < 0)
                {
                    MessageBox.Show("Заполните все поля корректно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await VMService.CreateVM(config);
                ToastHelper.ShowToast($"ВМ '{config.Name}' создана.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ToastHelper.ShowToast($"Ошибка создания ВМ: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
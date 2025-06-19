using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VBoxWpfApp
{
    public static class ToastHelper
    {
        public static void ShowToast(string message)
        {
            var window = Application.Current.MainWindow;
            if (window == null) return;

            var toast = new Border
            {
                Background = Brushes.Gray,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var text = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14
            };

            toast.Child = text;

            if (window.Content is Grid grid)
            {
                grid.Children.Add(toast);
                System.Timers.Timer timer = new System.Timers.Timer(3000);
                timer.Elapsed += (s, e) =>
                {
                    window.Dispatcher.Invoke(() => grid.Children.Remove(toast));
                    timer.Dispose();
                };
                timer.Start();
            }
        }
    }
}
using System;
using System.IO;
using System.Windows;

namespace VBoxWpfApp
{
    public static class ThemeManager
    {
        private static bool _isDarkTheme = true;

        public static bool IsDarkTheme => _isDarkTheme;

        public static void ApplyTheme(string themeName)
        {
            try
            {
                // Очистка текущих словарей ресурсов
                Application.Current.Resources.MergedDictionaries.Clear();

                // Формируем путь к файлу темы
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string themePath = Path.Combine(baseDir, "Themes", $"{themeName}.xaml");

                // Проверяем существование файла
                if (!File.Exists(themePath))
                {
                    MessageBox.Show($"Файл темы не найден: {themePath}");
                    return;
                }

                // Загружаем словарь ресурсов
                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Absolute)
                };

                Application.Current.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить тему '{themeName}': {ex.Message}");
            }
        }

        public static void ToggleTheme()
        {
            string nextTheme = _isDarkTheme ? "LightTheme" : "DarkTheme";
            ApplyTheme(nextTheme);
            _isDarkTheme = !_isDarkTheme;
        }

        public static void SaveCurrentTheme()
        {
            Properties.Settings.Default.Theme = _isDarkTheme ? "DarkTheme" : "LightTheme";
            Properties.Settings.Default.Save();
        }

        public static void LoadSavedTheme()
        {
            string savedTheme = Properties.Settings.Default.Theme ?? "DarkTheme";
            ApplyTheme(savedTheme);
            _isDarkTheme = savedTheme == "DarkTheme";
        }
    }
}
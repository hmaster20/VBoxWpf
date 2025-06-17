using System;
using System.IO;
using System.Windows;

namespace VBoxWpfApp
{
    public static class ThemeManager
    {
        private static bool _isDarkTheme = true;

        public static bool IsDarkTheme => _isDarkTheme;

        /// <summary>
        /// Применяет указанную тему
        /// </summary>
        public static void ApplyTheme(string themeName)
        {
            try
            {
                // Очистка текущих стилей
                // Application.Current.Resources.Clear();
                Application.Current.Resources.MergedDictionaries.Clear();


                //string themePath = $"Themes/{themeName}.xaml";
                //Console.WriteLine("Загружаем тему по пути: " + themePath);
                //MessageBox.Show("Загружаем тему по пути: " + themePath);

                //string path = $"Themes/{themeName}.xaml";
                //Uri uri = new Uri(path, UriKind.Relative);

                //bool fileExists = System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
                //if (!fileExists)
                //{
                //    MessageBox.Show($"Файл темы не найден: {path}");
                //    return;
                //}


                //string themesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes");
                //if (Directory.Exists(themesDir))
                //{
                //    foreach (var file in Directory.GetFiles(themesDir, "*.xaml"))
                //    {
                //        MessageBox.Show("Найден файл темы: " + file);
                //    }
                //}
                //else
                //{
                //    MessageBox.Show("Папка Themes не найдена!");
                //}

                //string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                //Console.WriteLine("Базовый каталог: " + baseDir);
                //string testPath = Path.Combine(baseDir, "Themes\\DarkTheme.xaml");
                //Console.WriteLine("Проверяемый путь: " + testPath);
                //Console.WriteLine("Существует файл: " + File.Exists(testPath));


                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string FullPath = Path.Combine(baseDir, $"Themes\\{themeName}.xaml");

                var themeDict = new ResourceDictionary
                {
                    //Source = new Uri($"{themeName}.xaml", UriKind.Relative)
                    //Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative)
                    Source = new Uri(FullPath)
                };

                Application.Current.Resources.MergedDictionaries.Add(themeDict);



            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить тему '{themeName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Переключает текущую тему (светлая/тёмная)
        /// </summary>
        public static void ToggleTheme()
        {
            string nextTheme = _isDarkTheme ? "LightTheme" : "DarkTheme";
            ApplyTheme(nextTheme);
            _isDarkTheme = !_isDarkTheme;
        }

        /// <summary>
        /// Сохраняет текущую тему в настройках
        /// </summary>
        public static void SaveCurrentTheme()
        {
            Properties.Settings.Default.Theme = _isDarkTheme ? "DarkTheme" : "LightTheme";
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Загружает последнюю сохранённую тему
        /// </summary>
        public static void LoadSavedTheme()
        {
            string savedTheme = Properties.Settings.Default.Theme ?? "DarkTheme";
            ApplyTheme(savedTheme);
            _isDarkTheme = savedTheme == "DarkTheme";
        }
    }
}
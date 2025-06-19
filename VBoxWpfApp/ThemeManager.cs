using System.Windows;

namespace VBoxWpfApp
{
    public static class ThemeManager
    {
        private const string DarkThemeResource = "DarkTheme.xaml";
        private const string LightThemeResource = "LightTheme.xaml";
        private static bool _isDarkTheme = true;

        public static bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
                ApplyTheme(_isDarkTheme ? "DarkTheme" : "LightTheme");
            }
        }

        public static void LoadSavedTheme()
        {
            string savedTheme = Properties.Settings.Default.Theme ?? "DarkTheme";
            _isDarkTheme = savedTheme == "DarkTheme";
            ApplyTheme(savedTheme);
        }

        public static void SaveCurrentTheme()
        {
            Properties.Settings.Default.Theme = _isDarkTheme ? "DarkTheme" : "LightTheme";
            Properties.Settings.Default.Save();
        }

        public static void ToggleTheme()
        {
            IsDarkTheme = !_isDarkTheme;
        }

        public static void ApplyTheme(string themeName)
        {
            var dict = new ResourceDictionary
            {
                Source = new System.Uri($"/Themes/{(themeName == "DarkTheme" ? DarkThemeResource : LightThemeResource)}", System.UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
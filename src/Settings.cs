using System;
using System.Globalization;
using System.IO;
using IniParser;
using IniParser.Model;

namespace NativeMDView
{
    public static class Settings
    {
        private static string ConfigPath => Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "settings.ini");

        public static string Theme { get; set; } = "dark";
        public static bool ShowToolbar { get; set; } = true;
        public static double Zoom { get; set; } = 1.0;
        public static double WindowWidth { get; set; } = 1000;
        public static double WindowHeight { get; set; } = 700;
        public static double WindowLeft { get; set; } = 100;
        public static double WindowTop { get; set; } = 100;
        public static bool WindowMaximized { get; set; } = true;
        public static string ViewMode { get; set; } = "preview";

        public static void Load()
        {
            if (!File.Exists(ConfigPath)) return;
            try
            {
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(ConfigPath);
                if (data.Sections.ContainsSection("General"))
                {
                    Theme = data["General"]["Theme"] ?? "dark";
                    ShowToolbar = bool.TryParse(data["General"]["ShowToolbar"], out var st) ? st : true;
                    Zoom = double.TryParse(data["General"]["Zoom"], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z : 1.0;
                }
                if (data.Sections.ContainsSection("Window"))
                {
                    WindowWidth = double.TryParse(data["Window"]["Width"], NumberStyles.Float, CultureInfo.InvariantCulture, out var ww) ? ww : 1000;
                    WindowHeight = double.TryParse(data["Window"]["Height"], NumberStyles.Float, CultureInfo.InvariantCulture, out var wh) ? wh : 700;
                    WindowLeft = double.TryParse(data["Window"]["Left"], NumberStyles.Float, CultureInfo.InvariantCulture, out var wl) ? wl : 100;
                    WindowTop = double.TryParse(data["Window"]["Top"], NumberStyles.Float, CultureInfo.InvariantCulture, out var wt) ? wt : 100;
                    WindowMaximized = bool.TryParse(data["Window"]["Maximized"], out var wm) ? wm : true;
                }
                if (data.Sections.ContainsSection("View"))
                {
                    var vm = data["View"]["Mode"];
                    if (!string.IsNullOrEmpty(vm)) ViewMode = vm;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var parser = new FileIniDataParser();
                var data = new IniData();
                data.Sections.AddSection("General");
                data["General"]["Theme"] = Theme;
                data["General"]["ShowToolbar"] = ShowToolbar.ToString();
                data["General"]["Zoom"] = Zoom.ToString(CultureInfo.InvariantCulture);
                data.Sections.AddSection("Window");
                data["Window"]["Width"] = WindowWidth.ToString(CultureInfo.InvariantCulture);
                data["Window"]["Height"] = WindowHeight.ToString(CultureInfo.InvariantCulture);
                data["Window"]["Left"] = WindowLeft.ToString(CultureInfo.InvariantCulture);
                data["Window"]["Top"] = WindowTop.ToString(CultureInfo.InvariantCulture);
                data["Window"]["Maximized"] = WindowMaximized.ToString();
                data.Sections.AddSection("View");
                data["View"]["Mode"] = ViewMode;
                parser.WriteFile(ConfigPath, data);
            }
            catch { }
        }
    }
}

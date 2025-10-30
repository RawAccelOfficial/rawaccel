using grapher.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace grapher.Models.Theming.IO
{
    public class ThemeFileOperations
    {
        public string ThemePath { get; set; }

        public IEnumerable<ColorScheme> LoadThemes()
        {
            ThemePath = Path.Combine(Helper.GetCfgPath(), "themes");

            var pathFound = Directory.Exists(ThemePath);

            if (!pathFound)
            {
                Directory.CreateDirectory(ThemePath);
                CreateDefaultThemes();
            }


            var files = Directory.GetFiles(ThemePath);
            return files.Select(XDocument.Load)
                .Select(ColorSchemeManager.FromXml);
        }

        private void CreateDefaultThemes()
        {
            // Save all the "preset" themes
            var lightTheme = ColorScheme.LightTheme;
            var lightStreamerTheme = ColorScheme.LightStreamerTheme;
            var darkTheme = ColorScheme.DarkTheme;
            var darkStreamerTheme = ColorScheme.DarkStreamerTheme;
            var accentedDarkTheme = ColorScheme.AccentedDarkTheme;

            SaveScheme(lightTheme, "LightTheme");
            SaveScheme(lightStreamerTheme, "LightStreamerTheme");
            SaveScheme(darkTheme, "DarkTheme");
            SaveScheme(darkStreamerTheme, "DarkStreamerTheme");
            SaveScheme(accentedDarkTheme, "AccentedDarkTheme");
        }

        public bool SaveScheme(ColorScheme scheme, string filename)
        {
            var xml = ColorSchemeManager.ToXml(scheme);

            var xmlDoc = new XmlDocument
            {
                PreserveWhitespace = true
            };

            using (var reader = xml.CreateReader())
            {
                xmlDoc.Load(reader);
            }

            var fullPath = Path.Combine(ThemePath, $"{filename}.xml");

            xmlDoc.Save(fullPath);

            return true;
        }
    }
}
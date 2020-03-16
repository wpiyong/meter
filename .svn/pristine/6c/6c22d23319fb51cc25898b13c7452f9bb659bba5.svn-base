using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace gColor.Model
{
    public static class AppSettings
    {
        static XDocument _settings;
        static string settingsFileName = "gColor.exe.config";

        static AppSettings()
        {
            _settings = XDocument.Load(settingsFileName);
        }

        public static void Load()
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                var t = Properties.Settings.Default[currentProperty.Name].GetType();
                Properties.Settings.Default[currentProperty.Name] = 
                    Convert.ChangeType(Get(currentProperty.Name), t, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        static object Get(string name)
        {
            object res = null;

            var field = _settings.Descendants("setting")
                                    .Where(x => (string)x.Attribute("name") == name)
                                    .FirstOrDefault();

            if (field != null)
            {
                res = field.Element("value").Value;
            }
            else
                throw new Exception("Property not found in AppSettings");

            return res;
        }

        static void Set(string name, object value)
        {
            var field = _settings.Descendants("setting")
                                    .Where(x => (string)x.Attribute("name") == name)
                                    .FirstOrDefault();

            if (field != null)
            {
                field.Element("value").Value = value.ToString();
            }
            else
                throw new Exception("Property not found in AppSettings");
        }

        public static void Save()
        {
            try
            {
                foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
                {
                    Set(currentProperty.Name, Properties.Settings.Default[currentProperty.Name]);
                }
                _settings.Save(settingsFileName);
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Settings not saved");
            }
        }
    }
}

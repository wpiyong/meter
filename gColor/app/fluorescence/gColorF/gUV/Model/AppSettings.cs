using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace gUV.Model
{
    public class AppSettings : Settings
    {
        public AppSettings() : base("gUV.exe.config")
        {

        }

        public override bool Load()
        {
            bool result = false;

            try
            {
                _settings = XDocument.Load(settingsFileName);

                foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
                {
                    var t = Properties.Settings.Default[currentProperty.Name].GetType();
                    Properties.Settings.Default[currentProperty.Name] =
                        Convert.ChangeType(Get(currentProperty.Name), t, System.Globalization.CultureInfo.InvariantCulture);
                }
                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        

        public override void Save()
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

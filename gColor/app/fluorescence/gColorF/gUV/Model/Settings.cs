using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace gUV.Model
{
    public abstract class Settings
    {
        protected XDocument _settings;
        protected string settingsFileName = "";

        public Settings(string fileName)
        {
            settingsFileName = fileName;
        }

        public virtual bool Load()
        {
            bool result = false;
            try
            {
                _settings = XDocument.Load(settingsFileName);
                foreach (var prop in this.GetType().GetProperties())
                {
                    prop.SetValue(this, Convert.ChangeType(Get(prop.Name), prop.PropertyType, 
                        System.Globalization.CultureInfo.InvariantCulture));
                    
                }

                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        protected object Get(string name)
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
                throw new Exception("Property not found in Settings");

            return res;
        }

        protected void Set(string name, object value)
        {
            var field = _settings.Descendants("setting")
                                    .Where(x => (string)x.Attribute("name") == name)
                                    .FirstOrDefault();

            if (field != null)
            {
                field.Element("value").Value = value.ToString();
            }
            else
                throw new Exception("Property not found in Settings");
        }

        public virtual void Save()
        {
            try
            {
                foreach (var prop in this.GetType().GetProperties())
                {
                    Set(prop.Name, prop.GetValue(this));
                }
                _settings.Save(settingsFileName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Settings not saved");
            }
        }
    }
}

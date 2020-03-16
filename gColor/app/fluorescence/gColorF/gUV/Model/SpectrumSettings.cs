using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gUV.Model
{
    public class SpectrumSettings : Settings
    {
        public SpectrumSettings()
            : base("spectrumSettings.config")
        {

        }

        public string RootUrl { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace gUV.Model
{
    public class FluorescenceSettings : Settings
    {
        public FluorescenceSettings()
            : base("fluorescenceSettings.config")
        {

        }

        public bool FluorescenceMeasure { get; set; }
        public double Gain { get; set; }
        public double FShutterTime { get; set; }
        public double FShutterTimeDiff { get; set; }
        public uint FluorescenceSetCurrent { get; set; }
        public double LowGain { get; set; }
        public double LowFShutterTime { get; set; }
        public double FLThreshold { get; set; }
        public bool ExtractFluorescenceDataToTextFile { get; set; }
        public string FluorescenceTextFilePath { get; set; }
        public uint MainLightSetCurrent { get; set; }
        public int UVWarningThreshold { get; set; }
        public int UVWarningThresholdHigh { get; set; }
        public bool EnableWBAdjustment { get; set; }
        public bool ShowMultiColorComment { get; set; }
        public double MultiColorThresholdPercent { get; set; }

    }
}

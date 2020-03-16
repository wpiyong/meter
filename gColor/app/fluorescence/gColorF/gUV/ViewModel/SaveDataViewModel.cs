using gUV.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace gUV.ViewModel
{
    class SaveDataViewModel : ViewModelBase
    {
        List<Stone> _stoneMeasurements;
        int _uvIntensity;
        string _spectrumUserName;
        string _spectrumFileName, _deviceName, _spectrumMessage;
        

        public string SpectrumUser { get { return _spectrumUserName; } }
        public string SpectrumMessage { get { return _spectrumMessage; } }

        public SaveDataViewModel(Stone stone, List<Stone> stoneMeasurements, int uvIntensity,
            string specFileName, string devName, ref string userName)
        {
            base.DisplayName = "SaveDataViewModel";

            CommandCancel = new RelayCommand(param => this.Close(param));
            CommandSave = new RelayCommand(param => this.Save(param), cc => { return Cassette.GoodColorResult; });

            Cassette = stone;
            _stoneMeasurements = stoneMeasurements;
            _uvIntensity = uvIntensity;
            _spectrumFileName = specFileName;
            _spectrumUserName = userName;
            _deviceName = devName;

        }

        public RelayCommand CommandCancel { get; set; }
        public RelayCommand CommandSave { get; set; }

        public Stone Cassette { get; set; }
        public double LValue { get { return Math.Round(Cassette.L, 3); } }
        public double CValue { get { return Math.Round(Cassette.C, 3); } }
        public double HValue { get { return Math.Round(Cassette.H, 3); } }

        public double LFValue { get { return Math.Round(Cassette.LF, 3); } }
        public double CFValue { get { return Math.Round(Cassette.CF, 3); } }
        public double HFValue { get { return Math.Round(Cassette.HF, 3); } }

        public Visibility ShowFLuorescenceResults
        {
            get 
            {
                if (GlobalVariables.fluorescenceSettings.FluorescenceMeasure)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        private bool IsValid(DependencyObject obj)
        {
            Cassette.ControlNumber = Cassette.ControlNumber;

            // The dependency object is valid if it has no errors, 
            //and all of its children (that are dependency objects) are error-free.
            return !Validation.GetHasError(obj) &&
                LogicalTreeHelper.GetChildren(obj)
                .OfType<DependencyObject>()
                .All(IsValid);
        }

        public bool DisableControlNumber
        {
            get
            {
                return (Cassette != null && Cassette.ControlNumber != null &&
                    Cassette.ControlNumber.Length > 0);
            }
        }

        void Save(object param)
        {
            if (!GlobalVariables.IsAdmin)
            {
                if (IsValid(param as DependencyObject) && UpdateSpectrumData(out _spectrumMessage))
                    ((Window)param).DialogResult = true;
                else
                    return;
            }
            else
            {
                if (Cassette != null && Cassette.ControlNumber != null && Cassette.ControlNumber.Length > 0 
                    && UpdateSpectrumData(out _spectrumMessage))
                {
                    ((Window)param).DialogResult = true;
                }
                else
                    Close(param);
            }
        }

        void Close(object param)
        {
            ((Window)param).Close();
        }

        bool UpdateSpectrumData(out string message)
        {
            bool result = false;
            Stone st = _stoneMeasurements[0];
            message = "";

            try
            {
                if (Properties.Settings.Default.ExtractToTextFile)
                {
                    //st.Save(Properties.Settings.Default.TextFilePath, spectrumFileName);
                    //st.Save(Properties.Settings.Default.MetrologyFilePath, deviceName + "ColorimeterMetrology.csv");
                    if (Properties.Settings.Default.TextFilePath.Length > 0)
                    {
                        Stone.SaveFluorescenceData(Properties.Settings.Default.TextFilePath,
                                                        _spectrumFileName, _stoneMeasurements, _uvIntensity);
                    }
                    if (Properties.Settings.Default.MetrologyFilePath.Length > 0)
                    {
                        Stone.SaveFluorescenceData(Properties.Settings.Default.MetrologyFilePath,
                                                        _deviceName + "FluorescenceMetrology.csv", _stoneMeasurements, _uvIntensity);
                    }
                    result = true;
                }

                if (GlobalVariables.spectrumSettings.RootUrl.Length > 0)
                {
                    string code = null;
                    result = false;
                    var httpStatusCode = System.Net.HttpStatusCode.BadRequest;

                    for (int retries = 0; retries < 3; retries++)
                    {
                        if (Stone.SaveFluorescenceDataSpectrum(_stoneMeasurements, _uvIntensity,
                                                        _spectrumUserName, out code, out message, out httpStatusCode))
                        {
                            result = true;
                            break;
                        }
                        else
                        {
                            if (httpStatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                var spectrumLogin = new gUV.View.SpectrumLogin();
                                bool? dialogResult = spectrumLogin.ShowDialog();
                                if (dialogResult == true)
                                {
                                    _spectrumUserName = "";
                                    for (int authRetries = 0; authRetries < 3; authRetries++)
                                    {
                                        if (GiaSpectrum.MapUser(GlobalVariables.endpoints.map_user_url, spectrumLogin.SpectrumUser, spectrumLogin.SpectrumDat,
                                ClassOpenCV.ImageProcessing.getDevicename(), out code, out message))
                                        {
                                            _spectrumUserName = spectrumLogin.SpectrumUser;
                                            break;
                                        }
                                    }
                                    if (_spectrumUserName.Length == 0)
                                        throw new Exception("Spectrum login failure:\n" + message);
                                }
                                else
                                    throw new Exception("Cancelled spectrum login");
                            }
                            else
                                throw new Exception("Spectrum Update failed:\n" + message);
                        }

                    }

                }
                
            }
            catch (Exception ex)
            {
                App.LogEntry.AddEntry(ex.Message, true);
            }

            return result;
        }
    }
}

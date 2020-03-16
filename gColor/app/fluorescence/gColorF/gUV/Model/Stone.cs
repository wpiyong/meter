﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace gUV.Model
{
    [Serializable]
    class Stone : IDataErrorInfo, INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;


        Dictionary<string, string> validationErrors = new Dictionary<string, string>();
        string _controlNumber;
        string _comment1, _comment2, _comment3;
        double _l, _a, _b, _c, _h;
        double _mask_L, _mask_A;
        string _lDesc, _cDesc, _hDesc;
        string _lFDesc, _cFDesc, _hFDesc;
        double _lf, _cf, _hf;
        string _fcomment;
        string _warningMessage;
        string _instruction;
        string _version;

        public object Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, this);
                    stream.Position = 0;
                    return bf.Deserialize(stream);
                }
            }
            return null;
        }

        public Stone()
        {
            validationErrors["ControlNumber"] = String.Empty;
        }

        public string ControlNumber 
        {
            get
            {
                return _controlNumber;
            }
            set
            {
                _controlNumber = value;
                if (!Validate(_controlNumber))
                {
                    validationErrors["ControlNumber"] = "Invalid Control Number";
                }
                else
                {
                    validationErrors["ControlNumber"] = String.Empty;
                }
                OnPropertyChanged("ControlNumber");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
        
        public string Description 
        {
            get
            {
                return CDesc + " " + LDesc + " " + HDesc;
            }     
        }

        public string CDesc
        {
            get
            {
                return _cDesc;
            }
            set
            {
                _cDesc = value;
                OnPropertyChanged("CDesc");
            }
        }
        public string LDesc
        {
            get
            {
                return _lDesc;
            }
            set
            {
                _lDesc = value;
                OnPropertyChanged("LDesc");
            }
        }
        public string HDesc
        {
            get
            {
                return _hDesc;
            }
            set
            {
                _hDesc = value;
                OnPropertyChanged("HDesc");
            }
        }

        public string FDescription
        {
            get
            {
                return CFDesc + " " + LFDesc + " " + HFDesc;
            }
        }

        public string CFDesc
        {
            get
            {
                return _cFDesc;
            }
            set
            {
                _cFDesc = value;
                OnPropertyChanged("CFDesc");
            }
        }
        public string LFDesc
        {
            get
            {
                return _lFDesc;
            }
            set
            {
                _lFDesc = value;
                OnPropertyChanged("LFDesc");
            }
        }
        public string HFDesc
        {
            get
            {
                return _hFDesc;
            }
            set
            {
                _hFDesc = value;
                OnPropertyChanged("HFDesc");
            }
        }

        public double L
        {
            get
            {
                return _l;
            }
            set
            {
                _l = value;
                OnPropertyChanged("L");
            }
        }
        public double A
        {
            get
            {
                return _a;
            }
            set
            {
                _a = value;
                OnPropertyChanged("A");
            }
        }
        public double B
        {
            get
            {
                return _b;
            }
            set
            {
                _b = value;
                OnPropertyChanged("B");
            }
        }
        public double C
        {
            get
            {
                return _c;
            }
            set
            {
                _c = value;
                OnPropertyChanged("C");
            }
        }
        public double H
        {
            get
            {
                return _h;
            }
            set
            {
                _h = value;
                OnPropertyChanged("H");
            }
        }

        public double LF
        {
            get
            {
                return _lf;
            }
            set
            {
                _lf = value;
                OnPropertyChanged("LF");
            }
        }
        public double CF
        {
            get
            {
                return _cf;
            }
            set
            {
                _cf = value;
                OnPropertyChanged("CF");
            }
        }
        public double HF
        {
            get
            {
                return _hf;
            }
            set
            {
                _hf = value;
                OnPropertyChanged("HF");
            }
        }

        public double Mask_L
        {
            get
            {
                return _mask_L;
            }
            set
            {
                _mask_L = value;
                OnPropertyChanged("Mask_L");
            }
        }

        public double Mask_A
        {
            get
            {
                return _mask_A;
            }
            set
            {
                _mask_A = value;
                OnPropertyChanged("Mask_A");
            }
        }

        public string Comment1
        {
            get
            {
                return _comment1;
            }
            set
            {
                _comment1 = value;
                OnPropertyChanged("Comment1");
            }
        }
        public string Comment2
        {
            get
            {
                return _comment2;
            }
            set
            {
                _comment2 = value;
                OnPropertyChanged("Comment2");
            }
        }
        public string Comment3
        {
            get
            {
                return _comment3;
            }
            set
            {
                _comment3 = value;
                OnPropertyChanged("Comment3");
            }
        }

        public string FComment
        {
            get
            {
                return _fcomment;
            }
            set
            {
                _fcomment = value;
                OnPropertyChanged("FComment");
            }
        }

        public string WarningMessage
        {
            get
            {
                return _warningMessage;
            }
            set
            {
                _warningMessage = value;
                OnPropertyChanged("WarningMessage");
            }
        }

        public string Instruction
        {
            get
            {
                return _instruction;
            }
            set
            {
                _instruction = value;
                OnPropertyChanged("Instruction");
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                OnPropertyChanged("Version");
            }
        }

        public Visibility ShowMultiColorComment
        {
            get
            {
                Visibility visible = Visibility.Collapsed;
                if (GlobalVariables.fluorescenceSettings.ShowMultiColorComment && WarningMessage != "")
                    visible = Visibility.Visible;

                return visible;
            }
        }

        bool _goodColorResult;
        public bool GoodColorResult
        {
            get
            {
#if DEBUG
                return true;
#else
                if (!GlobalVariables.IsAdmin)
                    return _goodColorResult;
                else
                    return true;
#endif
            }
            set
            {
                _goodColorResult = value;
                OnPropertyChanged("GoodColorResult");
            }
        }


        public string Error
        {
            get { return String.Empty; }
        }

        public string this[string columnname]
        {
            get
            {
                string result = string.Empty;
                switch (columnname)
                {
                    case "ControlNumber":
                        result = validationErrors["ControlNumber"];
                        break;
                };
               return result;
           }
        }

        bool Validate(object value)
        {
            Regex regex = new Regex(@"^[0-9]+$");

            bool result = false;
            string inputString = (value ?? string.Empty).ToString();
            if (inputString.Length == 12 && regex.IsMatch(inputString))
            {
                result = true;
            }
            return result;
        }


        public void Save(string filePath, string file)
        {
            try
            {
                if (ControlNumber != null && ControlNumber.Length > 0)
                {
                    var fileName = filePath + @"\" + file;
                    Directory.CreateDirectory(filePath);

                    var firstWord = ControlNumber;
                    var secondWord = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt") ;
                    var thirdWord = String.Empty;
                    var csv = new StringBuilder();

                    if (Comment1 != null && Comment1.Length > 0)
                    {
                        thirdWord = Comment1;
                        var newLine = string.Format("{0},{1},{2}{3}", firstWord, secondWord, thirdWord, Environment.NewLine);
                        csv.Append(newLine);
                        
                    }
                    if (Comment2 != null && Comment2.Length > 0)
                    {
                        thirdWord = Comment2;
                        var newLine = string.Format("{0},{1},{2}{3}", firstWord, secondWord, thirdWord, Environment.NewLine);
                        csv.Append(newLine);
                    }
                                        

                    //note using UNIX epoch of 1970-01-01
                    //.Net epoch would be DateTime.MinValue
                    //TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    //int secondsSinceEpoch = (int)t.TotalSeconds;
                    //var fileName = filePath + @"\Colorimeter" + DateTime.Now.ToUniversalTime().ToString("yyyyMMddhhmmss") +
                    //                "_" + secondsSinceEpoch + ".txt";



                    // This text is added only once to the file.
                    if (!File.Exists(fileName))
                    {
                        // Create a file to write to.
                        string createText = "control_number, date, device, volume, L_diamond, a_diamond," +
                                            " b_diamond, L_background, a_background, b_background, L, a, b, C," +
                                            " H, L_description, C_description, H_description, version, masklength, maskarea," +
                                            " maskheight, maskpvheight, maxmin_widthratio, min_aspectratio, diamond_proportion, comment," +
                                            " temp_measurement, temp_background, shutter, blue_gain, red_gain";

                        createText += Environment.NewLine;
                        File.WriteAllText(fileName, createText);
                    }




                    File.AppendAllText(fileName, csv.ToString());
                    
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to save text file");
            }
        }


        public static void SaveFluorescenceData(string filePath, string file, List<Stone> stoneList, int uvIntensity)
        {
            var fileName = filePath + @"\" + file;
            try
            {
                Directory.CreateDirectory(filePath);

                // This text is added only once to the file.
                if (!File.Exists(fileName))
                {
                    // Create a file to write to.
                    string createText = "control_number, date, device, L_pl, a_pl, b_pl, C_pl, H_pl, L_desc_pl, C_desc_pl," +
                                        "H_desc_pl, " +
                                        "masklength_pl, maskarea_pl, " +
                                        "gain, shutter, temperature, blue_gain, red_gain, " +
                                        "control_number, date, device, L_pl, a_pl, b_pl, C_pl, H_pl, L_desc_pl, C_desc_pl," +
                                        "H_desc_pl, " +
                                        "masklength_pl, maskarea_pl, " +
                                        "gain, shutter, temperature, blue_gain, red_gain,  " +
                                        "control_number, date, device, L_pl, a_pl, b_pl, C_pl, H_pl, L_desc_pl, C_desc_pl," +
                                        "H_desc_pl, " +
                                        "masklength_pl, maskarea_pl, " +
                                        "gain, shutter, temperature, blue_gain, red_gain, uv_sensor_reading, " +
                                        "Boundary version, Comment, Comment2";

                    createText += Environment.NewLine;
                    File.WriteAllText(fileName, createText);
                }

                string tempFComment;

                for (int i = 0; i < 2; i++ )
                {
                    Stone s;

                    if (i == 1 && stoneList.Count < 2)
                    {
                        s = stoneList[0];
                        tempFComment = ",,,,,,,,,,,,,,";
                    }
                    else
                    {
                        s = stoneList[i];
                        tempFComment = stoneList[i].FComment;
                    }

                    if (s.ControlNumber != null && s.ControlNumber.Length > 0)
                    {
                        var firstWord = s.ControlNumber;
                        var secondWord = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt");
                        var thirdWord = ClassOpenCV.ImageProcessing.getDevicename();
                        var csv = new StringBuilder();

                        var newLine = string.Format("{0},{1},{2},{3},", firstWord, secondWord, thirdWord, tempFComment);
                        csv.Append(newLine);

                        File.AppendAllText(fileName, csv.ToString());
                    }
                    else
                        throw new Exception("Bad Control Number");

                }

                string combinedFL = stoneList[0].FComment;
                string[] vals = combinedFL.Split(',');
                string outputComment = stoneList[0].WarningMessage;
                string instruction = stoneList[0].Instruction;
                if (stoneList.Count > 1)
                {
                    string[] vals2 = stoneList[1].FComment.Split(',');
                    if (vals.Length != 15 || vals2.Length != 15)
                        throw new Exception("Bad FComment");

                    for (int j = 1; j < 15; j++)
                        vals[j] = vals2[j];

                    outputComment = stoneList[1].WarningMessage;
                    instruction = stoneList[1].Instruction;
                }
                vals[10] = "combined";
                combinedFL = String.Join(",", vals);

                var csv2 = new StringBuilder();
                var newLine2 = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}{8}", stoneList[0].ControlNumber,
                        DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt"), 
                        ClassOpenCV.ImageProcessing.getDevicename(), combinedFL, 
                        uvIntensity, stoneList[0].Version, outputComment, instruction, Environment.NewLine);
                csv2.Append(newLine2);
                File.AppendAllText(fileName, csv2.ToString());

            }
            catch (Exception e)
            {
                File.AppendAllText(fileName, Environment.NewLine);
                MessageBox.Show(e.Message, "Failed to save text file");
            }
        }

        public static bool SaveFluorescenceDataSpectrum(List<Stone> stoneList, int uvIntensity, 
            string userName, out string code, out string message, out System.Net.HttpStatusCode httpStatusCode)
        {
            bool result = false;
            code = null;
            message = "";
            httpStatusCode = System.Net.HttpStatusCode.BadRequest;

            try
            {

                string tempFComment;

                for (int i = 0; i < 2; i++)
                {
                    Stone s;

                    if (i == 1 && stoneList.Count < 2)
                    {
                        s = stoneList[0];
                        tempFComment = ",,,,,,,,,,,,,,";
                    }
                    else
                    {
                        s = stoneList[i];
                        tempFComment = stoneList[i].FComment;
                    }

                    if (s.ControlNumber != null && s.ControlNumber.Length > 0)
                    {
                        var firstWord = s.ControlNumber;
                        var secondWord = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt");
                        var thirdWord = ClassOpenCV.ImageProcessing.getDevicename();
                        var csv = new StringBuilder();

                        var newLine = string.Format("{0},{1},{2},{3},", firstWord, secondWord, thirdWord, tempFComment);
                        csv.Append(newLine);

                    }
                    else
                        throw new Exception("Bad Control Number");

                }

                string combinedFL = stoneList[0].FComment;
                string[] vals = combinedFL.Split(',');
                string outputComment = stoneList[0].WarningMessage;
                string instruction = stoneList[0].Instruction;
                if (stoneList.Count > 1)
                {
                    string[] vals2 = stoneList[1].FComment.Split(',');
                    if (vals.Length != 15 || vals2.Length != 15)
                        throw new Exception("Bad FComment");

                    for (int j = 1; j < 15; j++)
                        vals[j] = vals2[j];

                    outputComment = stoneList[1].WarningMessage;
                    instruction = stoneList[1].Instruction;
                }
                vals[10] = "combined";
                combinedFL = String.Join(",", vals);

                var csv2 = new StringBuilder();
                var newLine2 = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}{8}", stoneList[0].ControlNumber,
                        DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy hh:mm:ss tt"),
                        ClassOpenCV.ImageProcessing.getDevicename(), combinedFL,
                        uvIntensity, stoneList[0].Version, outputComment, instruction, Environment.NewLine);
                csv2.Append(newLine2);

                #region SpectrumUpload

                string[] values = combinedFL.Split(',');

                UploadRequest upload = new UploadRequest()
                {
                    control_number = stoneList[0].ControlNumber,
                    upload_date = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy HH:mm"),
                    device_name = ClassOpenCV.ImageProcessing.getDevicename(),
                    intensity = values[5],
                    color = values[7],
                    user_name = userName,
                    l_pl = values[0],
                    a_pl = values[1],
                    b_pl = values[2],
                    c_pl = values[3],
                    h_pl = values[4],
                    c_verbal_pl = null,
                    masklength_pl = values[8],
                    maskarea_pl = values[9],
                    gain = values[10],
                    shutter = values[11],
                    temperature = values[12],
                    blue_gain = values[13],
                    red_gain = values[14],
                    uv_sensor_reading = uvIntensity.ToString(),
                    boundary_version = stoneList[0].Version,
                    comment = outputComment,
                    comment2 = instruction,
                    
                };

                string body = JsonConvert.SerializeObject(upload);
                result = GiaSpectrum.Upload(GlobalVariables.endpoints.upload_url, body, out code, out message, out httpStatusCode);

                #endregion

            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }
    }
}

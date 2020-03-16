using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;

namespace gColor.Model
{
    public class Stone : IDataErrorInfo, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Dictionary<string, string> kinesisConfigDict;

        Dictionary<string, string> validationErrors = new Dictionary<string, string>();
        string _controlNumber;
        string _comment1, _comment2, _comment3;
        double _l, _a, _b, _c, _h;
        double _mask_L, _mask_A;
        string _lDesc, _cDesc, _hDesc;

        public Stone()
        {
            validationErrors["ControlNumber"] = String.Empty;

            kinesisConfigDict = LoadKinesisConfig();
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


        public void Save(string filePath, string file, bool toAws = false)
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
                                            " temp_measurement, temp_background, shutter, blue_gain, red_gain" + Environment.NewLine;
                        File.WriteAllText(fileName, createText);
                    }




                    File.AppendAllText(fileName, csv.ToString());
                    if (toAws)
                    {
                        WriteToKinesis(csv.ToString());
                    }
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Failed to save text file");
            }
        }

        public Dictionary<string, string> LoadKinesisConfig()
        {
            Dictionary<string, string> kinesisConfigDict = new Dictionary<string, string>();

            try
            {
                /*
                var path = Directory.GetCurrentDirectory();
                //var path = System.AppDomain.CurrentDomain.BaseDirectory;
                //MessageBox.Show(path, "Path");
                */

                //using (var reader = new StreamReader(Path.Combine(path, "KinesisConfig.txt")))
                using (var reader = new StreamReader("KinesisConfig.txt"))
                {
                    // Load Dictionary from text file
                    while (!reader.EndOfStream)
                    {
                        string kinesisConfigLine = reader.ReadLine();

                        if (!kinesisConfigLine.StartsWith("#"))
                        {
                            string[] kinesisConfigLineArray = kinesisConfigLine.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                            char[] trimChars = { '"', '\'', ' ' };
                            string key = kinesisConfigLineArray[0];
                            string val = kinesisConfigLineArray[1].TrimStart(trimChars).TrimEnd(trimChars);

                            kinesisConfigDict.Add(key, val);
                        }
                    }



                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, "Exception Reading KinesisConfig.txt");
            }
            return kinesisConfigDict;
        }

        public void WriteToKinesis(string data)
        {
            //Moved to Constructor
            //Dictionary<string, string> kinesisConfigDict = LoadKinesisConfig();

            int maxattempts = 5;
            for (int attempts = 1; attempts <= maxattempts; attempts++)
            {

                try
                {
                    //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Kinesis/TKinesisConfig.html
                    var kinesisConfig = new AmazonKinesisConfig();

                    // Use Dictionary to initialize kinesisConfig
                    // https://www.tutorialsteacher.com/csharp/csharp-dictionary

                    string resultTimeout;
                    if (kinesisConfigDict.TryGetValue("Timeout", out resultTimeout))
                    {
                        var timeout = new TimeSpan(0, 0, 5);
                        kinesisConfig.Timeout = timeout;
                    }
                    else
                    {
                        // This key does not exist
                    }

                    string resultRegionEndpoint;
                    if (kinesisConfigDict.TryGetValue("RegionEndpoint", out resultRegionEndpoint))
                    {
                        kinesisConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(resultRegionEndpoint);
                    }
                    else
                    {
                        // This key does not exist
                    }

                    string resultMaxErrorRetry;
                    if (kinesisConfigDict.TryGetValue("MaxErrorRetry", out resultMaxErrorRetry))
                    {
                        kinesisConfig.MaxErrorRetry = Int32.Parse(resultMaxErrorRetry);
                    }
                    else
                    {
                        // This key does not exist
                    }

                    string resultMaxIdleTimeMS;
                    if (kinesisConfigDict.TryGetValue("MaxIdleTime", out resultMaxIdleTimeMS))
                    {
                        kinesisConfig.MaxIdleTime = Int32.Parse(resultMaxIdleTimeMS);
                    }
                    else
                    {
                        // This key does not exist
                    }

                    // Use Dictionary to initialize awsCredentials

                    string resultAccessKey;
                    if (kinesisConfigDict.TryGetValue("AccessKey", out resultAccessKey))
                    {
                        // This key does exist
                    }
                    else
                    {
                        // This key does not exist
                    }

                    string resultSecretKey;
                    if (kinesisConfigDict.TryGetValue("SecretKey", out resultSecretKey))
                    {
                        // This key does exist
                    }
                    else
                    {
                        // This key does not exist
                    }


                    var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(resultAccessKey, resultSecretKey);

                    //https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Kinesis/TKinesisClient.html
                    //create client that pulls creds from web.config and takes in Kinesis config
                    var kinesisClient = new AmazonKinesisClient(awsCredentials, kinesisConfig);


                    // Use Dictionary get StreamName and PartitionKey for request

                    string resultStreamName;
                    if (kinesisConfigDict.TryGetValue("StreamName", out resultStreamName))
                    {
                        // This key does exist
                    }
                    else
                    {
                        // This key does not exist
                        MessageBox.Show("StreamName is required", "FATAL Kinesis Config Error");
                        System.Windows.Forms.Application.Exit();
                    }

                    string resultPartitionKey;
                    if (kinesisConfigDict.TryGetValue("PartitionKey", out resultPartitionKey))
                    {
                        // This key does exist
                    }
                    else
                    {
                        // This key does not exist
                        System.Diagnostics.Debug.WriteLine("Kinesis Config Error:" + "Kinesis PartitionKey is required" + "Setting Kinesis PartitionKey to default value GIADiamonds");
                        //MessageBox.Show("Kinesis Partition Key Required", "Kinesis Config Error");
                        resultPartitionKey = "GIADiamonds";
                    }

                    //https://csharp.hotexamples.com/examples/Amazon.Kinesis.Model/PutRecordRequest/-/php-putrecordrequest-class-examples.html
                    var request = new PutRecordRequest();
                    request.StreamName = resultStreamName;
                    request.Data = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(data));
                    request.PartitionKey = resultPartitionKey;

                    var kinesisResponse = kinesisClient.PutRecord(request);
                    // PutRecord returns the shard ID of where the data record was placed and the sequence number that was assigned to the data record.
                    // Sequence numbers generally increase over time. To guarantee strictly increasing ordering, use the SequenceNumberForOrdering parameter. For more information, see the Amazon Kinesis Developer Guide .
                    // If a PutRecord request cannot be processed because of insufficient provisioned throughput on the shard involved in the request, PutRecord throws ProvisionedThroughputExceededException .
                    // Data records are accessible for only 24 hours from the time that they are added to an Amazon Kinesis stream.

                    System.Diagnostics.Debug.WriteLine("---vvv--- kinesisResponse Response ---vvv---:");
                    System.Diagnostics.Debug.WriteLine(kinesisResponse.HttpStatusCode.ToString());
                    System.Diagnostics.Debug.WriteLine(kinesisResponse.ResponseMetadata.ToString());
                    System.Diagnostics.Debug.WriteLine(kinesisResponse.SequenceNumber.ToString());
                    System.Diagnostics.Debug.WriteLine(kinesisResponse.ShardId.ToString());
                    System.Diagnostics.Debug.WriteLine("");

                    break;
                }
                catch (ProvisionedThroughputExceededException ptee)
                {
                    if (attempts < maxattempts)
                    {
                        System.Diagnostics.Debug.WriteLine(ptee.Message);
                        //MessageBox.Show(ptee.Message, "Kinesis Provisioned Throughput Exceeded Exception");
                        Thread.Sleep(1000 * attempts * attempts);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(ptee.Message);
                        System.Diagnostics.Debug.WriteLine("Max Retries Exceeded - Exiting Retry Loop");
                        //MessageBox.Show(ptee.Message, "Kinesis Provisioned Throughput Exceeded Exception");
                        //MessageBox.Show("Max Retries Exceeded - Exiting Retry Loop", "Kinesis Provisioned Throughput Exceeded Exception");
                        break;
                    }
                }
                catch (AmazonKinesisException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    //MessageBox.Show(e.Message, "Kinesis Exception");
                    break;
                }
            }

        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace gColor.ViewModel
{
    class DailyMonitorViewModel : ViewModelBase
    {
        DataTable dtNoShift, dtNewShift, dtOldShift, dtTempShift;

        double _newLconv, _newAconv, _newBconv;
        double _newLshift, _newAshift, _newBshift;

        public DailyMonitorViewModel(DataTable dt)
        {
            base.DisplayName = "DailyMonitorViewModel";

            CommandCancel = new RelayCommand(param => this.Close(param));
            CommandUpdate = new RelayCommand(param => this.Update(param));

            dtNoShift = dt.Copy();
            dtNewShift = dt.Copy();
            dtOldShift = dt.Copy();

            _newLconv = 1.0;/*(Convert.ToDouble(dt.Rows[3]["L"]) - Convert.ToDouble(dt.Rows[0]["L"])) /
                                (Convert.ToDouble(dt.Rows[4]["L"]) - Convert.ToDouble(dt.Rows[1]["L"]));*/
            _newAconv = (Convert.ToDouble(dt.Rows[3]["a"]) - Convert.ToDouble(dt.Rows[0]["a"])) /
                                (Convert.ToDouble(dt.Rows[4]["a"]) - Convert.ToDouble(dt.Rows[1]["a"]));
            _newBconv = (Convert.ToDouble(dt.Rows[3]["b"]) - Convert.ToDouble(dt.Rows[0]["b"])) /
                                (Convert.ToDouble(dt.Rows[4]["b"]) - Convert.ToDouble(dt.Rows[1]["b"]));
            _newLshift = Convert.ToDouble(dt.Rows[1]["L"]) - (Convert.ToDouble(dt.Rows[0]["L"]) / _newLconv);
            _newAshift = Convert.ToDouble(dt.Rows[1]["a"]) - (Convert.ToDouble(dt.Rows[0]["a"]) / _newAconv);
            _newBshift = Convert.ToDouble(dt.Rows[1]["b"]) - (Convert.ToDouble(dt.Rows[0]["b"]) / _newBconv);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows[i]["Description"] != DBNull.Value && (string)(dt.Rows[i]["Description"]) == "Measure")
                {
                    dtOldShift.Rows[i]["L"] = Math.Round(Properties.Settings.Default.LConv * 
                        (Convert.ToDouble(dtOldShift.Rows[i]["L"]) - Properties.Settings.Default.Lshift), 3);
                    double a = Math.Round(Properties.Settings.Default.AConv *
                        (Convert.ToDouble(dtOldShift.Rows[i]["a"]) - Properties.Settings.Default.Ashift), 3);
                    dtOldShift.Rows[i]["a"] = a;
                    double b = Math.Round(Properties.Settings.Default.BConv *
                        (Convert.ToDouble(dtOldShift.Rows[i]["b"]) - Properties.Settings.Default.Bshift), 3);
                    dtOldShift.Rows[i]["b"] = b;
                    dtOldShift.Rows[i]["C"] = Math.Round(ClassOpenCV.ImageProcessing.calc_C(ref a, ref b), 3);
                    dtOldShift.Rows[i]["H"] = Math.Round(ClassOpenCV.ImageProcessing.calc_H(ref a, ref b), 3);

                    //diffs
                    dtOldShift.Rows[i + 1]["L"] = Math.Round(Convert.ToDouble(dtOldShift.Rows[i]["L"]) -
                                                    Convert.ToDouble(dtOldShift.Rows[i - 1]["L"]), 3);
                    dtOldShift.Rows[i + 1]["a"] = Math.Round(Convert.ToDouble(dtOldShift.Rows[i]["a"]) -
                                                    Convert.ToDouble(dtOldShift.Rows[i - 1]["a"]), 3);
                    dtOldShift.Rows[i + 1]["b"] = Math.Round(Convert.ToDouble(dtOldShift.Rows[i]["b"]) -
                                                    Convert.ToDouble(dtOldShift.Rows[i - 1]["b"]), 3);
                    dtOldShift.Rows[i + 1]["C"] = Math.Round(Convert.ToDouble(dtOldShift.Rows[i]["C"]) -
                                                    Convert.ToDouble(dtOldShift.Rows[i - 1]["C"]), 3);
                    dtOldShift.Rows[i + 1]["H"] = Math.Round(Convert.ToDouble(dtOldShift.Rows[i]["H"]) -
                                                    Convert.ToDouble(dtOldShift.Rows[i - 1]["H"]), 3);


                    dtNewShift.Rows[i]["L"] = Math.Round(_newLconv *
                        (Convert.ToDouble(dtNewShift.Rows[i]["L"]) - _newLshift), 3);
                    a = Math.Round(_newAconv *
                        (Convert.ToDouble(dtNewShift.Rows[i]["a"]) - _newAshift), 3);
                    dtNewShift.Rows[i]["a"] = a;
                    b = Math.Round(_newBconv *
                        (Convert.ToDouble(dtNewShift.Rows[i]["b"]) - _newBshift), 3);
                    dtNewShift.Rows[i]["b"] = b;
                    dtNewShift.Rows[i]["C"] = Math.Round(ClassOpenCV.ImageProcessing.calc_C(ref a, ref b), 3);
                    dtNewShift.Rows[i]["H"] = Math.Round(ClassOpenCV.ImageProcessing.calc_H(ref a, ref b), 3);

                    //diffs
                    dtNewShift.Rows[i + 1]["L"] = Math.Round(Convert.ToDouble(dtNewShift.Rows[i]["L"]) -
                                                    Convert.ToDouble(dtNewShift.Rows[i - 1]["L"]), 3);
                    dtNewShift.Rows[i + 1]["a"] = Math.Round(Convert.ToDouble(dtNewShift.Rows[i]["a"]) -
                                                    Convert.ToDouble(dtNewShift.Rows[i - 1]["a"]), 3);
                    dtNewShift.Rows[i + 1]["b"] = Math.Round(Convert.ToDouble(dtNewShift.Rows[i]["b"]) -
                                                    Convert.ToDouble(dtNewShift.Rows[i - 1]["b"]), 3);
                    dtNewShift.Rows[i + 1]["C"] = Math.Round(Convert.ToDouble(dtNewShift.Rows[i]["C"]) -
                                                    Convert.ToDouble(dtNewShift.Rows[i - 1]["C"]), 3);
                    dtNewShift.Rows[i + 1]["H"] = Math.Round(Convert.ToDouble(dtNewShift.Rows[i]["H"]) -
                                                    Convert.ToDouble(dtNewShift.Rows[i - 1]["H"]), 3); 

                }
                else if ( dt.Rows[i]["ReferenceStone"] != DBNull.Value && (string)dt.Rows[i]["ReferenceStone"] == "Average")
                {
                    var averageRow = dtOldShift.Select("ReferenceStone = 'Average'");
                    var diffRows = dtOldShift.Select("Description = 'Diff'");

                    double sumL = 0, suma = 0, sumb = 0, sumC = 0, sumH = 0;
                    int count = diffRows.Length - 1;
                    for (int j = 0; j < count; j++)
                    {
                        sumL += Convert.ToDouble(diffRows[j]["L"]);
                        suma += Convert.ToDouble(diffRows[j]["a"]);
                        sumb += Convert.ToDouble(diffRows[j]["b"]);
                        sumC += Convert.ToDouble(diffRows[j]["C"]);
                        sumH += Convert.ToDouble(diffRows[j]["H"]);
                    }

                    averageRow[0]["L"] = Math.Round(sumL / count, 3);
                    averageRow[0]["a"] = Math.Round(suma / count, 3);
                    averageRow[0]["b"] = Math.Round(sumb / count, 3);
                    averageRow[0]["C"] = Math.Round(sumC / count, 3);
                    averageRow[0]["H"] = Math.Round(sumH / count, 3);


                    averageRow = dtNewShift.Select("ReferenceStone = 'Average'");
                    diffRows = dtNewShift.Select("Description = 'Diff'");

                    sumL = 0; suma = 0; sumb = 0; sumC = 0; sumH = 0;
                    count = diffRows.Length - 1;
                    for (int j = 0; j < count; j++)
                    {
                        sumL += Convert.ToDouble(diffRows[j]["L"]);
                        suma += Convert.ToDouble(diffRows[j]["a"]);
                        sumb += Convert.ToDouble(diffRows[j]["b"]);
                        sumC += Convert.ToDouble(diffRows[j]["C"]);
                        sumH += Convert.ToDouble(diffRows[j]["H"]);
                    }

                    averageRow[0]["L"] = Math.Round(sumL / count, 3);
                    averageRow[0]["a"] = Math.Round(suma / count, 3);
                    averageRow[0]["b"] = Math.Round(sumb / count, 3);
                    averageRow[0]["C"] = Math.Round(sumC / count, 3);
                    averageRow[0]["H"] = Math.Round(sumH / count, 3);
                }
            }

            dtTempShift = dtNoShift.Copy();
            UseOldShiftConv = true;
        }

        public RelayCommand CommandCancel { get; set; }
        public RelayCommand CommandUpdate { get; set; }


        bool _newShift, _oldShift, _noShift;
        public bool UseNewShiftConv
        {
            get
            {
                return _newShift;
            }
            set
            {
                _newShift = value;
                if (_newShift)
                    Adjust();
                OnPropertyChanged("UseNewShiftConv");
            }
        }
        public bool UseOldShiftConv
        {
            get
            {
                return _oldShift;
            }
            set
            {
                _oldShift = value;
                if (_oldShift)
                    Adjust();
                OnPropertyChanged("UseOldShiftConv");
            }
        }
        public bool UseNoShiftConv
        {
            get
            {
                return _noShift;
            }
            set
            {
                _noShift = value;
                if (_noShift)
                    Adjust();
                OnPropertyChanged("UseNoShiftConv");
            }
        }

        DataTable _dtResults;
        public DataTable Results
        {
            get { return _dtResults; }
            set
            {
                _dtResults = value;
                OnPropertyChanged("Results");
            }
        }

        double _lshift, _ashift, _bshift;
        public double Lshift
        {
            get
            {
                return _lshift;
            }
            set
            {
                _lshift = Math.Round(value, 3);
                ManualChange();
                OnPropertyChanged("Lshift");
            }
        }
        public double Ashift
        {
            get
            {
                return _ashift;
            }
            set
            {
                _ashift = Math.Round(value, 3);
                ManualChange();
                OnPropertyChanged("Ashift");
            }
        }
        public double Bshift
        {
            get
            {
                return _bshift;
            }
            set
            {
                _bshift = Math.Round(value, 3);
                ManualChange();
                OnPropertyChanged("Bshift");
            }
        }


        double _lconv, _aconv, _bconv;
        public double Lconv
        {
            get
            {
                return _lconv;
            }
            set
            {
                _lconv = Math.Round(value, 3); 
                ManualChange();
                OnPropertyChanged("Lconv");
            }
        }
        public double Aconv
        {
            get
            {
                return _aconv;
            }
            set
            {
                _aconv = Math.Round(value, 3);
                ManualChange();
                OnPropertyChanged("Aconv");
            }
        }
        public double Bconv
        {
            get
            {
                return _bconv;
            }
            set
            {
                _bconv = Math.Round(value, 3);
                ManualChange();
                OnPropertyChanged("Bconv");
            }
        }

        
        void Adjust()
        {
            if (UseNewShiftConv)
            {
                dtTempShift = dtNoShift.Copy();
                Lshift = _newLshift;
                Ashift = _newAshift;
                Bshift = _newBshift;
                Lconv = _newLconv;
                Aconv = _newAconv;
                Bconv = _newBconv;
                Results = dtNewShift;
            }
            else if (UseOldShiftConv)
            {
                Lshift = Properties.Settings.Default.Lshift;
                Ashift = Properties.Settings.Default.Ashift;
                Bshift = Properties.Settings.Default.Bshift;
                Lconv = Properties.Settings.Default.LConv;
                Aconv = Properties.Settings.Default.AConv;
                Bconv = Properties.Settings.Default.BConv;
                Results = dtOldShift;
            }
            else
            {
                Lshift = Ashift = Bshift = 0;
                Lconv = Aconv = Bconv = 1.0;
                Results = dtNoShift;
            }

        }

        void ManualChange()
        {
            if (UseNewShiftConv)
            {
                for (int i = 0; i < dtTempShift.Rows.Count; i++)
                {
                    if (dtTempShift.Rows[i]["Description"] != DBNull.Value && (string)(dtTempShift.Rows[i]["Description"]) == "Measure")
                    {
                        dtTempShift.Rows[i]["L"] = Math.Round(Lconv * (Convert.ToDouble(dtNoShift.Rows[i]["L"]) - Lshift), 3);
                        double a = Math.Round(Aconv * (Convert.ToDouble(dtNoShift.Rows[i]["a"]) - Ashift), 3);
                        dtTempShift.Rows[i]["a"] = a;
                        double b = Math.Round(Bconv * (Convert.ToDouble(dtNoShift.Rows[i]["b"]) - Bshift), 3);
                        dtTempShift.Rows[i]["b"] = b;
                        dtTempShift.Rows[i]["C"] = Math.Round(ClassOpenCV.ImageProcessing.calc_C(ref a, ref b), 3);
                        dtTempShift.Rows[i]["H"] = Math.Round(ClassOpenCV.ImageProcessing.calc_H(ref a, ref b), 3);

                        //diffs
                        dtTempShift.Rows[i + 1]["L"] = Math.Round(Convert.ToDouble(dtTempShift.Rows[i]["L"]) -
                                                        Convert.ToDouble(dtTempShift.Rows[i - 1]["L"]), 3);
                        dtTempShift.Rows[i + 1]["a"] = Math.Round(Convert.ToDouble(dtTempShift.Rows[i]["a"]) -
                                                        Convert.ToDouble(dtTempShift.Rows[i - 1]["a"]), 3);
                        dtTempShift.Rows[i + 1]["b"] = Math.Round(Convert.ToDouble(dtTempShift.Rows[i]["b"]) -
                                                        Convert.ToDouble(dtTempShift.Rows[i - 1]["b"]), 3);
                        dtTempShift.Rows[i + 1]["C"] = Math.Round(Convert.ToDouble(dtTempShift.Rows[i]["C"]) -
                                                        Convert.ToDouble(dtTempShift.Rows[i - 1]["C"]), 3);
                        dtTempShift.Rows[i + 1]["H"] = Math.Round(Convert.ToDouble(dtTempShift.Rows[i]["H"]) -
                                                        Convert.ToDouble(dtTempShift.Rows[i - 1]["H"]), 3);

                    }
                    else if (dtTempShift.Rows[i]["ReferenceStone"] != DBNull.Value && (string)dtTempShift.Rows[i]["ReferenceStone"] == "Average")
                    {
                        var averageRow = dtTempShift.Select("ReferenceStone = 'Average'");
                        var diffRows = dtTempShift.Select("Description = 'Diff'");

                        double sumL = 0, suma = 0, sumb = 0, sumC = 0, sumH = 0;
                        int count = diffRows.Length - 1;
                        for (int j = 0; j < count; j++)
                        {
                            sumL += Convert.ToDouble(diffRows[j]["L"]);
                            suma += Convert.ToDouble(diffRows[j]["a"]);
                            sumb += Convert.ToDouble(diffRows[j]["b"]);
                            sumC += Convert.ToDouble(diffRows[j]["C"]);
                            sumH += Convert.ToDouble(diffRows[j]["H"]);
                        }

                        averageRow[0]["L"] = Math.Round(sumL / count, 3);
                        averageRow[0]["a"] = Math.Round(suma / count, 3);
                        averageRow[0]["b"] = Math.Round(sumb / count, 3);
                        averageRow[0]["C"] = Math.Round(sumC / count, 3);
                        averageRow[0]["H"] = Math.Round(sumH / count, 3);
                    }
                }

                Results = dtTempShift;
            }
        }


        void Update(object param)
        {
            try
            {
                Properties.Settings.Default.Lshift = Lshift;
                Properties.Settings.Default.Ashift = Ashift;
                Properties.Settings.Default.Bshift = Bshift;
                Properties.Settings.Default.LConv = Lconv;
                Properties.Settings.Default.AConv = Aconv;
                Properties.Settings.Default.BConv = Bconv;

                gColor.Model.AppSettings.Save();

                ClassOpenCV.ImageProcessing.setLabAdjustment(Lconv, Aconv, Bconv, Lshift, Ashift, Bshift);

                ((Window)param).Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Update failed");
            }
        }

        void Close(object param)
        {
            ((Window)param).Close();
        }
    }



    public class RadioButtonExtended : RadioButton
    {
        static bool m_bIsChanging = false;

        public RadioButtonExtended()
        {
            this.Checked += new RoutedEventHandler(RadioButtonExtended_Checked);
            this.Unchecked += new RoutedEventHandler(RadioButtonExtended_Unchecked);
        }

        void RadioButtonExtended_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsChanging)
                this.IsCheckedReal = false;
        }

        void RadioButtonExtended_Checked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsChanging)
                this.IsCheckedReal = true;
        }

        public bool? IsCheckedReal
        {
            get { return (bool?)GetValue(IsCheckedRealProperty); }
            set
            {
                SetValue(IsCheckedRealProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for IsCheckedReal. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedRealProperty =
        DependencyProperty.Register("IsCheckedReal", typeof(bool?), typeof(RadioButtonExtended),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Journal |
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        IsCheckedRealChanged));

        public static void IsCheckedRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            m_bIsChanging = true;
            ((RadioButtonExtended)d).IsChecked = (bool)e.NewValue;
            m_bIsChanging = false;
        }
    }
}

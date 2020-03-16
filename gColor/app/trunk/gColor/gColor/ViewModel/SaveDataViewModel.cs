using gColor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace gColor.ViewModel
{
    class SaveDataViewModel : ViewModelBase
    {
        public SaveDataViewModel(Stone stone)
        {
            base.DisplayName = "SaveDataViewModel";

            CommandCancel = new RelayCommand(param => this.Close(param));
            CommandSave = new RelayCommand(param => this.Save(param), cc => { return Cassette.GoodColorResult; });

            Cassette = stone;
        }

        public RelayCommand CommandCancel { get; set; }
        public RelayCommand CommandSave { get; set; }

        public Stone Cassette { get; set; }
        public double LValue { get { return Math.Round(Cassette.L, 3); } }
        public double CValue { get { return Math.Round(Cassette.C, 3); } }
        public double HValue { get { return Math.Round(Cassette.H, 3); } }

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
                if (IsValid(param as DependencyObject))
                    ((Window)param).DialogResult = true;
                else
                    return;
            }
            else
            {
                if (Cassette != null && Cassette.ControlNumber != null && Cassette.ControlNumber.Length > 0)
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
    }
}

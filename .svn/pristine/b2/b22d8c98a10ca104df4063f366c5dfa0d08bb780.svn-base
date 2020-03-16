using gColor.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace gColor.ViewModel
{
    class StoneDataViewModel : ViewModelBase
    {
        public StoneDataViewModel()
        {
            base.DisplayName = "StoneDataViewModel";

            CommandCancel = new RelayCommand(param => this.Close(param));
            CommandOK = new RelayCommand(param => this.Continue(param));
            Cassette = new Stone();
        }

        public RelayCommand CommandCancel { get; set; }
        public RelayCommand CommandOK { get; set; }

        public Stone Cassette { get; private set; }

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

        void Continue(object param)
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

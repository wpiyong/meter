using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace gColor.View
{
    /// <summary>
    /// Interaction logic for LogIn.xaml
    /// </summary>
    public partial class LogIn : Window
    {
        const int retryLimit = 3;
        int retryCount;
        private string key = "aadF8347-9e01-497d-AE21-38a88Ed4e8e4";

        bool _correct = false;
        public bool Correct
        {
            get { return _correct; }
            private set { _correct = value; }
        }

        public LogIn()
        {
            InitializeComponent();
            retryCount = 0;
            txtPassword.Focus();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Password == key)
            {
                Correct = true;
                this.Close();
            }
            else
            {
                lblHint.Visibility = Visibility.Visible;
                if (++retryCount >= 3)
                    this.Close();
            }
            txtPassword.Focus();
            txtPassword.SelectAll();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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

namespace gUV.View
{
    /// <summary>
    /// Interaction logic for SpectrumLogin.xaml
    /// </summary>
    public partial class SpectrumLogin : Window
    {
        public string SpectrumUser
        {
            get { return spectrumUser.Text; }
        }

        public SecureString SpectrumDat
        {
            get { return spectrumDat.SecurePassword; }
        }

        public SpectrumLogin()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}

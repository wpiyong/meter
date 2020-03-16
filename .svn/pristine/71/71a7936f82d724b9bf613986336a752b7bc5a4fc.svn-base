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
using System.Reflection;

namespace gColor.View
{
    /// <summary>
    /// Interaction logic for HelpAbout.xaml
    /// </summary>
    public partial class HelpAbout : Window
    {
        public HelpAbout()
        {
            InitializeComponent();
            txtAbout.Text = "gColor Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\r\n";
            txtAbout.Text += "ClassOpenCV Version " + typeof(ClassOpenCV.ImageProcessing).Assembly.GetName().Version.ToString() + "\r\n";
            //txtAbout.Text += "FlyCap2CameraControl_v100 Version " + typeof(FlyCapture2Managed.Gui.CameraSelectionDialog).Assembly.GetName().Version.ToString() + "\r\n";
            //txtAbout.Text += "FlyCapture2Managed_100 Version " + typeof(FlyCapture2Managed.ManagedCamera).Assembly.GetName().Version.ToString() + "\r\n";
            txtAbout.Text += "\r\n";
            txtAbout.Text += "\r\n";
            txtAbout.Text += "\r\n";
            txtAbout.Text += "\u00a9" + " Gemological Institute of America 2013";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    
}

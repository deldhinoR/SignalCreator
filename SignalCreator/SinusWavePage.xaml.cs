using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
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

namespace SignalCreator
{
    /// <summary>
    /// Interaction logic for SinusWaveWindow.xaml
    /// </summary>
    public partial class SinusWavePage : Page
    {
        private SerialPort serial;
        public SinusWavePage()
        {
            InitializeComponent();
            try
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                serial = mainWindow.Serial;
                if (serial != null && serial.IsOpen)
                {
                    serial.WriteLine("<@C>");
                    serial.BaseStream.Flush();
                }
                else
                {
                    MessageBox.Show("Serial port is not open. Please connect first.");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("COM portu açılamadı: " + ex.Message);
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var culture = CultureInfo.InvariantCulture;

                string amplitude = F(AmplitudeBox.Text);
                string frequency = F(FrequencyBox.Text);

                string cmd = $"<{amplitude},{frequency}>\n";
                serial.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderilemedi: " + ex.Message);
            }
        }
        private static string F(string text)
        {
            return double.Parse(text, CultureInfo.InvariantCulture)
                         .ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}

using System;
using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SignalCreator
{
    public partial class PhaseA : Page
    {
        private SerialPort serial;

        public PhaseA()
        {
            InitializeComponent();
            try
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                serial = mainWindow.Serial;
                if (serial != null && serial.IsOpen)
                {
                    serial.WriteLine("<@A>");
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

                string amplitude = double.Parse(AmplitudeBox.Text, culture).ToString(culture);
                string frequency = double.Parse(FrequencyBox.Text, culture).ToString(culture);
                string duty = double.Parse(PulseWidthBox.Text, culture).ToString(culture);

                string cmd = $"<{amplitude},{frequency},{duty}>\n";
                serial.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri gönderilemedi: " + ex.Message);
            }
        }


    }
}
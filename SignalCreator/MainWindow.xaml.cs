using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SignalCreator
{
    public partial class MainWindow : Window
    {
        public SerialPort Serial { get; private set; }
        private readonly string expectedVersion = "1.1.0";
        private StringBuilder serialBuffer = new StringBuilder();
        private CancellationTokenSource versionCheckCts;

        public enum ArduinoStatus
        {
            Disconnected,
            SketchMismatch,
            Connected
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadComPorts();
            UpdateSerialToggleButtonContent();
            UpdateStatus(ArduinoStatus.Disconnected);
        }

        private void PhaseA_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PhaseA());
        }

        private void PhaseB_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PhaseB());
        }
        private void SinusWaveWindow_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SinusWavePage());
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoForward)
                MainFrame.GoForward();
        }

        private void ConnectArduino_Click(object sender, RoutedEventArgs e)
        {
            var page = new ConnectArduinoPage();
            MainFrame.Navigate(page);
        }

        private async void SerialToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialToggleButton.IsEnabled = false;

                string selectedPort = ComPortComboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedPort))
                {
                    MessageBox.Show("Please select a COM port before opening serial.");
                    return;
                }

                if (Serial == null || Serial.PortName != selectedPort)
                {
                    CloseSerialPort();

                    Serial = new SerialPort(selectedPort, 9600)
                    {
                        NewLine = "\n",
                        Encoding = System.Text.Encoding.ASCII
                    };
                    Serial.DataReceived += Serial_DataReceived;
                }

                if (Serial.IsOpen)
                {
                    CloseSerialPort();
                    MessageBox.Show("Serial port closed.");
                    UpdateStatus(ArduinoStatus.Disconnected);
                }
                else
                {
                    serialBuffer.Clear();
                    versionCheckCts?.Cancel();
                    versionCheckCts = new CancellationTokenSource();

                    Serial.Open();

                    await Task.Delay(100);

                    Serial.WriteLine("<VERSION?>");

                    bool versionReceived = await WaitForVersionResponse(versionCheckCts.Token);

                    if (!versionReceived)
                    {
                        MessageBox.Show("No response from Arduino. Please upload the correct sketch and try again.");
                        CloseSerialPort();
                        UpdateStatus(ArduinoStatus.SketchMismatch);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Serial port error: " + ex.Message);
                CloseSerialPort();
                UpdateStatus(ArduinoStatus.Disconnected);
            }
            finally
            {
                UpdateSerialToggleButtonContent();
                SerialToggleButton.IsEnabled = true;
            }
        }


        private async Task<bool> WaitForVersionResponse(CancellationToken token)
        {
            int timeoutMs = 3000;
            int elapsed = 0;
            int delayStep = 50;

            while (!token.IsCancellationRequested && elapsed < timeoutMs)
            {
                string bufferString = serialBuffer.ToString();
                int start = bufferString.IndexOf('<');
                int end = bufferString.IndexOf('>');

                if (start != -1 && end != -1 && end > start)
                {
                    string message = bufferString.Substring(start + 1, end - start - 1);
                    serialBuffer.Remove(0, end + 1);

                    if (message.StartsWith("VERSION:"))
                    {
                        string versionReceived = message.Substring("VERSION:".Length).Trim();

                        if (versionReceived == expectedVersion)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Arduino sketch version matched: {versionReceived}. You can now use the functions.");
                                UpdateStatus(ArduinoStatus.Connected);
                            });
                            return true;
                        }
                        else
                        {
                            Dispatcher.Invoke(() => UpdateStatus(ArduinoStatus.SketchMismatch));
                            return false;
                        }
                    }
                }

                await Task.Delay(delayStep);
                elapsed += delayStep;
            }
            return false;
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = Serial.ReadExisting();
                serialBuffer.Append(data);
            }
            catch
            {
                // Ignore read errors
            }
        }

        private void CloseSerialPort()
        {
            try
            {
                if (Serial != null && Serial.IsOpen)
                {
                    Serial.Close();
                }
            }
            catch
            {
                // Ignore close errors
            }
        }

        private void UpdateSerialToggleButtonContent()
        {
            if (Serial != null && Serial.IsOpen)
            {
                SerialToggleButton.Content = "Close Serial";
            }
            else
            {
                SerialToggleButton.Content = "Open Serial";
            }
        }

        public void UpdateStatus(ArduinoStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                var blinkStoryboard = (Storyboard)FindResource("BlinkGreen");

                switch (status)
                {
                    case ArduinoStatus.Disconnected:
                        blinkStoryboard.Stop(this);
                        StatusIndicatorFill.Color = Colors.Red;
                        StatusLabel.Text = "Disconnected";
                        break;

                    case ArduinoStatus.SketchMismatch:
                        blinkStoryboard.Stop(this);
                        StatusIndicatorFill.Color = Colors.Orange;
                        StatusLabel.Text = "Sketch Mismatch";
                        break;

                    case ArduinoStatus.Connected:
                        StatusLabel.Text = $"Connected {expectedVersion} \nConnect output pin to DAC0";
                        StatusIndicatorFill.Color = Colors.Green;
                        blinkStoryboard.Begin(this, true);
                        break;
                }
            });
        }
        private void LoadComPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
                ComPortComboBox.ItemsSource = ports;
                if (ports.Any())
                    ComPortComboBox.SelectedIndex = 0;
                else
                    MessageBox.Show("No COM ports detected. Please connect your Arduino.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading COM ports: " + ex.Message);
            }
        }

    }
}

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SignalCreator
{
    public partial class ConnectArduinoPage : Page
    {
        private readonly string arduinoCliPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "ArduinoCLI", "arduino-cli.exe");
        private readonly string sketchPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "ArduinoCLI", "sketch");

        private string selectedComport;
        public ConnectArduinoPage()
        {
            InitializeComponent();
            LoadSerialPorts();
        }

        private void LoadSerialPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
                PortComboBox.ItemsSource = ports;
                if (ports.Any())
                {
                    PortComboBox.SelectedIndex = 0;
                }
                else
                {
                    StatusText.Text = "No COM ports detected. Please connect your Arduino.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading COM ports: " + ex.Message;
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            string comPort = PortComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(comPort))
            {
                StatusText.Text = "Please select a COM port.";
                return;
            }

            StatusText.Text = "Starting compile and upload...\n";

            await CompileAndUploadSketchAsync(comPort);
        }

        private async Task CompileAndUploadSketchAsync(string comPort)
        {
            try
            {
                // Compile
                int compileExitCode = await Task.Run(() =>
                    RunProcess(arduinoCliPath, $"compile --fqbn arduino:sam:arduino_due_x_dbg \"{sketchPath}\""));

                if (compileExitCode != 0)
                {
                    AppendStatus("\nCompilation failed. Aborting upload.");
                    return;
                }

                AppendStatus("\nCompilation succeeded. Starting upload...\n");

                // Upload
                int uploadExitCode = await Task.Run(() =>
                    RunProcess(arduinoCliPath, $"upload -p {comPort} --fqbn arduino:sam:arduino_due_x_dbg \"{sketchPath}\""));

                if (uploadExitCode == 0)
                {
                    AppendStatus("\nUpload successful!");
                    MessageBox.Show("Upload completed. Please open serial to use phases.");
                }
                else
                {
                    AppendStatus("\nUpload failed.");
                }
            }
            catch (Exception ex)
            {
                AppendStatus("\nError: " + ex.Message);
            }
        }

        private int RunProcess(string fileName, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Dispatcher.Invoke(() => AppendStatus(e.Data));
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Dispatcher.Invoke(() => AppendStatus("ERR: " + e.Data));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode;
            }
        }

        private void AppendStatus(string text)
        {
            StatusText.Text += text + "\n";
        }
    }
}

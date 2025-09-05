using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;  // Needed for FontFamily

namespace SignalCreator
{
    public partial class PhaseB : Page
    {
        private SerialPort serial;

        public PhaseB()
        {
            InitializeComponent();

            try
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                serial = mainWindow.Serial;

                if (serial != null && serial.IsOpen)
                {
                    serial.WriteLine("<@B>");
                    serial.BaseStream.Flush();
                }
                else
                {
                    MessageBox.Show("Serial port is not open. Please connect first.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("COM port couldn't be accessed: " + ex.Message);
            }

            PulseCountSelector_SelectionChanged(PulseCountSelector, null);
        }

        private static string F(string text)
        {
            return double.Parse(text, CultureInfo.InvariantCulture)
                         .ToString("F3", CultureInfo.InvariantCulture);
        }

        private void PulseCountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PulseCountSelector.SelectedItem == null || PulseInputsGrid == null)
                return;

            var existingValues = new Dictionary<string, string>();

            for (int i = 0; i < PulseInputsGrid.Children.Count; i++)
            {
                StackPanel panel = (StackPanel)PulseInputsGrid.Children[i];

                string amp = GetTextBoxValue(panel, $"Amplitude{i + 1}Box");
                string freq = GetTextBoxValue(panel, $"Frequency{i + 1}Box");
                string duty = GetTextBoxValue(panel, $"PulseWidth{i + 1}Box");
                string lag = GetTextBoxValue(panel, $"LeadLag{i + 1}Box");
                string angle = GetTextBoxValue(panel, $"Angle{i + 1}Box");

                existingValues[$"Amplitude{i + 1}Box"] = amp;
                existingValues[$"Frequency{i + 1}Box"] = freq;
                existingValues[$"PulseWidth{i + 1}Box"] = duty;
                existingValues[$"LeadLag{i + 1}Box"] = lag;
                existingValues[$"Angle{i + 1}Box"] = angle;
            }

            // Clear and rebuild the input grid
            PulseInputsGrid.Children.Clear();

            int selectedCount = int.Parse(((ComboBoxItem)PulseCountSelector.SelectedItem).Content.ToString());

            for (int i = 0; i < selectedCount; i++)
            {
                int row = i < 4 ? 0 : 1;
                int col = i % 4;

                StackPanel panel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Name = $"PulsePanel{i + 1}"
                };

                // Title for each pulse
                panel.Children.Add(new TextBlock
                {
                    Text = $"Pulse {i + 1}",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 6)
                });

                // Helper function to create Label with font
                Label CreateLabel(string content)
                {
                    return new Label
                    {
                        Content = content,
                        FontFamily = new FontFamily("Bahnschrift"),
                        FontSize = 14,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                }

                // Amplitude
                panel.Children.Add(CreateLabel("Amplitude (V):"));
                panel.Children.Add(new TextBox
                {
                    Name = $"Amplitude{i + 1}Box",
                    Width = 100,
                    Text = existingValues.TryGetValue($"Amplitude{i + 1}Box", out var amp) ? amp : "2.0",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontSize = 14
                });

                // Frequency
                panel.Children.Add(CreateLabel("Frequency (Hz):"));
                panel.Children.Add(new TextBox
                {
                    Name = $"Frequency{i + 1}Box",
                    Width = 100,
                    Text = existingValues.TryGetValue($"Frequency{i + 1}Box", out var freq) ? freq : "20.0",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontSize = 14
                });

                // Pulse Width
                panel.Children.Add(CreateLabel("Pulse Width (%):"));
                panel.Children.Add(new TextBox
                {
                    Name = $"PulseWidth{i + 1}Box",
                    Width = 100,
                    Text = existingValues.TryGetValue($"PulseWidth{i + 1}Box", out var duty) ? duty : "40",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontSize = 14
                });

                // Lead/Lag
                panel.Children.Add(CreateLabel("Lead/Lag (-0.5–0.5):"));
                panel.Children.Add(new TextBox
                {
                    Name = $"LeadLag{i + 1}Box",
                    Width = 100,
                    Text = existingValues.TryGetValue($"LeadLag{i + 1}Box", out var lag) ? lag : "0",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontSize = 14
                });

                // Cap Angle
                panel.Children.Add(CreateLabel("Cap Angle (0°–90°):"));
                panel.Children.Add(new TextBox
                {
                    Name = $"Angle{i + 1}Box",
                    Width = 100,
                    Text = existingValues.TryGetValue($"Angle{i + 1}Box", out var angle) ? angle : "10",
                    FontFamily = new FontFamily("Bahnschrift"),
                    FontSize = 14
                });

                Grid.SetRow(panel, row);
                Grid.SetColumn(panel, col);

                PulseInputsGrid.Children.Add(panel);
            }

            if (SameFreqCheck.IsChecked == true)
            {
                SetFrequencyReadOnly(true);
            }
        }


        private void Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serial == null || !serial.IsOpen)
                {
                    MessageBox.Show("Serial port is not open. Please connect first.");
                    return;
                }

                int pulseCount = PulseInputsGrid.Children.Count;
                if (pulseCount == 0)
                {
                    MessageBox.Show("Please select number of pulses and enter values.");
                    return;
                }

                string[] pulses = new string[pulseCount];

                for (int i = 0; i < pulseCount; i++)
                {
                    StackPanel panel = (StackPanel)PulseInputsGrid.Children[i];

                    string amp = GetTextBoxValue(panel, $"Amplitude{i + 1}Box");
                    string freq = GetTextBoxValue(panel, $"Frequency{i + 1}Box");
                    string duty = GetTextBoxValue(panel, $"PulseWidth{i + 1}Box");
                    string lag = GetTextBoxValue(panel, $"LeadLag{i + 1}Box");
                    string angle = GetTextBoxValue(panel, $"Angle{i + 1}Box");

                    if (string.IsNullOrWhiteSpace(amp) || string.IsNullOrWhiteSpace(freq) ||
                        string.IsNullOrWhiteSpace(duty) || string.IsNullOrWhiteSpace(lag) || string.IsNullOrWhiteSpace(angle))
                    {
                        MessageBox.Show($"Please fill all fields for Pulse {i + 1}.");
                        return;
                    }

                    pulses[i] = $"{F(amp)},{F(freq)},{F(duty)},{F(lag)},{F(angle)}";
                }

                string cmd = "<" + string.Join(";", pulses) + ">\n";
                serial.WriteLine(cmd);
                serial.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to send data: " + ex.Message);
            }
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !(Keyboard.FocusedElement is ComboBox))
            {
                Send_Click(null, null);
            }
        }

        private string GetTextBoxValue(Panel parent, string name)
        {
            foreach (var child in parent.Children)
            {
                if (child is TextBox tb && tb.Name == name)
                    return tb.Text;
            }
            return "";
        }

        private void SameFreqCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (SameFreqCheck.IsChecked == true)
            {
                SharedFreqPanel.Visibility = Visibility.Visible;

                if (PulseInputsGrid.Children.Count > 0 &&
                    GetTextBoxValue((Panel)PulseInputsGrid.Children[0], "Frequency1Box") is string text &&
                    double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double firstFreq))
                {
                    SharedFrequencyBox.Text = (firstFreq / 4.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    SharedFrequencyBox.Text = "1.50";
                }

                SetFrequencyReadOnly(true);
            }
            else
            {
                SharedFreqPanel.Visibility = Visibility.Collapsed;
                SetFrequencyReadOnly(false);
            }
        }

        private void SharedFrequencyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SameFreqCheck.IsChecked == true &&
                double.TryParse(SharedFrequencyBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double trainFreq))
            {
                double pulseFreq = trainFreq * 4.0;
                string freqStr = pulseFreq.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                for (int i = 0; i < PulseInputsGrid.Children.Count; i++)
                {
                    SetTextBoxValue((Panel)PulseInputsGrid.Children[i], $"Frequency{i + 1}Box", freqStr);
                }
            }
        }

        private void SetFrequencyReadOnly(bool isReadOnly)
        {
            foreach (StackPanel panel in PulseInputsGrid.Children)
            {
                foreach (var child in panel.Children)
                {
                    if (child is TextBox tb && tb.Name.Contains("Frequency"))
                    {
                        tb.IsReadOnly = isReadOnly;
                    }
                }
            }
        }

        private void SetTextBoxValue(Panel parent, string name, string value)
        {
            foreach (var child in parent.Children)
            {
                if (child is TextBox tb && tb.Name == name)
                {
                    tb.Text = value;
                    break;
                }
            }
        }

        private void InvertPulse_Checked(object sender, RoutedEventArgs e)
        {
            serial?.WriteLine("<Invert:ON>");
            serial?.BaseStream.Flush();
        }

        private void InvertPulse_UnChecked(object sender, RoutedEventArgs e)
        {
            serial?.WriteLine("<Invert:OFF>");
            serial?.BaseStream.Flush();
        }
    }
}

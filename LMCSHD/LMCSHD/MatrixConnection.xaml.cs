using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MatrixConnection.xaml
    /// </summary>
    public partial class MatrixConnection : INotifyPropertyChanged
    {
        public MatrixConnection()
        {
            InitializeComponent();
            RefreshSerialPorts();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void RefreshSerialPorts()
        {
            string[] ports = SerialManager.GetPortNames();
            SSerialPortList.ItemsSource = ports;
            if (ports.Length > 0)
            {
                SSerialPortList.SelectedIndex = 0;
                SSerialConnect.IsEnabled = true;
            }
            else
            {
                SSerialConnect.IsEnabled = false;
            }
        }
        private void SSerialConnect_Click(object sender, RoutedEventArgs e)
        {
            switch (SSColorModeList.SelectedIndex)
            {
                case 0: SerialManager.ColorMode = SerialManager.CMode.BPP24; break;
                case 1: SerialManager.ColorMode = SerialManager.CMode.BPP16; break;
                case 2: SerialManager.ColorMode = SerialManager.CMode.BPP8; break;
            }
            int[] matrixDef = null;

                matrixDef = SerialManager.Connect(SSerialPortList.SelectedValue.ToString(), int.Parse(SBaudRate.Text));

            if (matrixDef != null)
            {
                ((MainWindow)Application.Current.MainWindow).SetMatrixDimensions(matrixDef[0], matrixDef[1]);
                Close();
            }
            else
            {
                MessageBox.Show("Cannot establish connection on: " + SSerialPortList.SelectedValue.ToString());
            }
        }
        private void SSerialRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshSerialPorts();
        }

        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void SBaudRate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        private void SBaudRate_TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = (String)e.DataObject.GetData(typeof(String));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MatrixConnection.xaml
    /// </summary>
    public partial class MatrixConnection : Window
    {
        public MatrixConnection()
        {
            InitializeComponent();
            RefreshSerialPorts();
        }
        void RefreshSerialPorts()
        {
            string[] ports = SerialManager.GetPortNames();
            SSerialPortList.ItemsSource = ports;
            if (ports.Length > 0)
                SSerialPortList.SelectedIndex = 0;
        }
        private void SSerialConnect_Click(object sender, RoutedEventArgs e)
        {
            int[] matrixDef = SerialManager.Connect(SSerialPortList.SelectedValue.ToString(), int.Parse(SBaudRate.Text));
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

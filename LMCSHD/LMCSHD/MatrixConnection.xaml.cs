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
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for MatrixConnection.xaml
    /// </summary>
    public partial class MatrixConnection : Window
    {
        private SerialManager sm;
        private MainWindow m;
        public MatrixConnection(SerialManager serialManager, MainWindow main)
        {
            InitializeComponent();
            sm = serialManager;
            m = main;
            RefreshSerialPorts();
        }
        void RefreshSerialPorts()
        {
            string[] ports = sm.GetPortNames();
            SSerialPortList.ItemsSource = ports;
            if (ports.Length > 0)
                SSerialPortList.SelectedIndex = 0;
        }
        private void SSerialConnect_Click(object sender, RoutedEventArgs e)
        {
            int[] matrixDef = sm.Connect(SSerialPortList.SelectedValue.ToString(), int.Parse(SBaudRate.Text));
            if (matrixDef != null)
            {
                m.SetupFrameObject(matrixDef[0], matrixDef[1]);
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

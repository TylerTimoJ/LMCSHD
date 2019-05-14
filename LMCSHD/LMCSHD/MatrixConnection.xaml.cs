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
            m.SetupFrameObject(matrixDef[0], matrixDef[1]);
            Close();
        }
        private void SSerialRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshSerialPorts();
        }
    }
}

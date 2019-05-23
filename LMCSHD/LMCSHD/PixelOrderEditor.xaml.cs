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
//using static LMCSHD.PixelOrder;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for PixelOrderEditor.xaml
    /// </summary>
    public partial class PixelOrderEditor : Window
    {

        private MainWindow w;
        private SerialManager sm;
        private PixelOrder pixelOrder;

        public PixelOrderEditor(MainWindow window, SerialManager serialM)
        {
            sm = serialM;
            w = window;
            InitializeComponent();
            pixelOrder = new PixelOrder();
            pixelOrder.newLine = sm.pixelOrder.newLine;
            pixelOrder.orientation = sm.pixelOrder.orientation;
            pixelOrder.startCorner = sm.pixelOrder.startCorner;
            UIOrientation.SelectedIndex = (int)pixelOrder.orientation;
            UIStartingCorner.SelectedIndex = (int)pixelOrder.startCorner;
            UINewLine.SelectedIndex = (int)pixelOrder.newLine;
        }

        private void Direction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pixelOrder.orientation = (PixelOrder.Orientation)UIOrientation.SelectedIndex;
            OrderUpdated();
        }

        private void StartingCorner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pixelOrder.startCorner = (PixelOrder.StartCorner)UIStartingCorner.SelectedIndex;
            OrderUpdated();
        }

        private void NewLine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pixelOrder.newLine = (PixelOrder.NewLine)UINewLine.SelectedIndex;
            OrderUpdated();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sm.pixelOrder = pixelOrder;
            Close();
        }
        public void OrderUpdated()
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri("/LMCSHD;component/Images/" + pixelOrder.orientation.ToString() + "_" + pixelOrder.newLine.ToString() + "_" + pixelOrder.startCorner.ToString() + ".png", UriKind.Relative));
            HelperImage.Source = bitmapImage;
        }
    }
}

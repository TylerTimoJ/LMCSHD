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
        public PixelOrderEditor()
        {
            InitializeComponent();
            PixelOrder.newLine = PixelOrder.newLine;
            PixelOrder.orientation = PixelOrder.orientation;
            PixelOrder.startCorner = PixelOrder.startCorner;
            UIOrientation.SelectedIndex = (int)PixelOrder.orientation;
            UIStartingCorner.SelectedIndex = (int)PixelOrder.startCorner;
            UINewLine.SelectedIndex = (int)PixelOrder.newLine;
        }

        private void Direction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PixelOrder.orientation = (PixelOrder.Orientation)UIOrientation.SelectedIndex;
            OrderUpdated();
        }

        private void StartingCorner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PixelOrder.startCorner = (PixelOrder.StartCorner)UIStartingCorner.SelectedIndex;
            OrderUpdated();
        }

        private void NewLine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PixelOrder.newLine = (PixelOrder.NewLine)UINewLine.SelectedIndex;
            OrderUpdated();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        public void OrderUpdated()
        {
            BitmapImage bitmapImage = new BitmapImage(new Uri("/LMCSHD;component/Images/" + PixelOrder.orientation.ToString() + "_" + PixelOrder.newLine.ToString() + "_" + PixelOrder.startCorner.ToString() + ".png", UriKind.Relative));
            HelperImage.Source = bitmapImage;
        }
    }
}

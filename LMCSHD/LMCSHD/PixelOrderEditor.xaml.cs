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

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for PixelOrderEditor.xaml
    /// </summary>
    public partial class PixelOrderEditor : Window
    {
        private MatrixFrame f;
        private MainWindow w;
        public PixelOrderEditor(MatrixFrame frame, MainWindow window)
        {
            f = frame;
            w = window;
            InitializeComponent();

            
        }

        private void Direction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            o = (Orientation)UIOrientation.SelectedIndex;
            OrderUpdated();
        }

        private void StartingCorner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            s = (StartCorner)UIStartingCorner.SelectedIndex;
            OrderUpdated();
        }

        private void NewLine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            n = (NewLine)UINewLine.SelectedIndex;
            OrderUpdated();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private Orientation o { get; set; }
        private StartCorner s { get; set; }
        private NewLine n { get; set; }
        public enum Orientation { Horizontal, Vertical }
        public enum StartCorner { TopLeft, TopRight, BottomLeft, BottomRight }
        public enum NewLine { Scan, Snake }
        public Order pxOrder { get; set; }
        public enum Order
        {
            HZ_TL_SC,
            HZ_TL_SN,
            HZ_TR_SC,
            HZ_TR_SN,
            HZ_BL_SC,
            HZ_BL_SN,
            HZ_BR_SC,
            HZ_BR_SN,
            VT_TL_SC,
            VT_TL_SN,
            VT_TR_SC,
            VT_TR_SN,
            VT_BL_SC,
            VT_BL_SN,
            VT_BR_SC,
            VT_BR_SN
        }

        public void OrderUpdated()
        {
            if (o == Orientation.Horizontal)
            {
                if (s == StartCorner.TopLeft)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.HZ_TL_SC;
                    }
                    else
                    {
                        pxOrder = Order.HZ_TL_SN;
                    }
                }
                else if (s == StartCorner.TopRight)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.HZ_TR_SC;
                    }
                    else
                    {
                        pxOrder = Order.HZ_TR_SN;
                    }
                }
                else if (s == StartCorner.BottomLeft)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.HZ_BL_SC;
                    }
                    else
                    {
                        pxOrder = Order.HZ_BL_SN;
                    }
                }
                else if (s == StartCorner.BottomRight)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.HZ_BR_SC;
                    }
                    else
                    {
                        pxOrder = Order.HZ_BR_SN;
                    }
                }
            }
            else
            {
                if (s == StartCorner.TopLeft)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.VT_TL_SC;
                    }
                    else
                    {
                        pxOrder = Order.VT_TL_SN;
                    }
                }
                else if (s == StartCorner.TopRight)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.VT_TR_SC;
                    }
                    else
                    {
                        pxOrder = Order.VT_TR_SN;
                    }
                }
                else if (s == StartCorner.BottomLeft)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.VT_BL_SC;
                    }
                    else
                    {
                        pxOrder = Order.VT_BL_SN;
                    }
                }
                else if (s == StartCorner.BottomRight)
                {
                    if (n == NewLine.Scan)
                    {
                        pxOrder = Order.VT_BR_SC;
                    }
                    else
                    {
                        pxOrder = Order.VT_BR_SN;
                    }
                }
            }
            /*
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri("/Images/HZ_BL_SN.png", UriKind.Relative);
            img.EndInit();
            */
            try
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri("/LMCSHD;component/Images/" + pxOrder.ToString() + ".png", UriKind.Relative));
                HelperImage.Source = bitmapImage;
            }
            catch (Exception)
            {

            }
                //  Uri pic = new Uri(@"pack://application:,,,/Images/HZ_BL_SN.png", UriKind.Absolute);


         //   new BitmapImage(new Uri("LMCSHD_WPF;component/Images/HZ_BL_SN.png")); 
         //   MessageBox.Show(pxOrder.ToString());
        }

    }
}

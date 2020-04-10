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
using System.Windows.Forms;
using System.IO;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        public ExportWindow()
        {
            InitializeComponent();
        }

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WriteFile(saveFileDialog.FileName);
                //   
            }
        }

        private void WriteFile(string path)
        {
            //System.Windows.Forms.MessageBox.Show(path);
            string[] formattedData = new string[MatrixFrame.Height];
            /*
            for (int i = 0; i < MatrixFrame.Height; i++)
            {
                for(int e = 0; e < MatrixFrame.Width; e++)
                {
                    formattedData[i] += "0x" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].R, 16) + ",";
                    formattedData[i] += "0x" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].G, 16) + ",";
                    formattedData[i] += "0x" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].B, 16) + ", ";
                }
                formattedData[i] += "\n";
            }

                        for (int i = 0; i < MatrixFrame.Height; i++)
            {
                for (int e = 0; e < MatrixFrame.Width; e++)
                {
                    formattedData[i] += "B" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].R, 2) + ",";
                    formattedData[i] += "B" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].G, 2) + ",";
                    formattedData[i] += "B" + Convert.ToString(MatrixFrame.Frame[i * MatrixFrame.Width + e].B, 2) + ", ";
                }
                formattedData[i] += "\n";
            }
            */

            string[] d = { BitConverter.ToString(MatrixFrame.GetOrderedSerialFrame()) };

            for (int i = 0; i < MatrixFrame.Height; i++)
            {
                for (int e = 0; e < MatrixFrame.Width; e++)
                {
                    formattedData[i] += "0x" + MatrixFrame.Frame[i * MatrixFrame.Width + e].R.ToString("X2") + ",";
                    formattedData[i] += "0x" + MatrixFrame.Frame[i * MatrixFrame.Width + e].G.ToString("X2") + ",";
                    formattedData[i] += "0x" + MatrixFrame.Frame[i * MatrixFrame.Width + e].B.ToString("X2") + ",";
                }
                formattedData[i] += "\n";
            }




            System.IO.File.WriteAllLines(path, formattedData);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
        }
    }
}

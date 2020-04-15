using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LMCSHD
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MatrixDimensions : INotifyPropertyChanged
    {
        public MatrixDimensions()
        {
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _width =  MatrixFrame.Width;
        private int _height = MatrixFrame.Height;

        public int MatrixWidth
        {
            get { return _width; }
            set
            {
                if(value != _width)
                {
                    _width = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MatrixHeight
        {
            get { return _height; }
            set
            {
                if(value != _height)
                {
                    _height = value;
                    OnPropertyChanged();
                }
            }
        }

        private void Button_Accept_Click(object sender, RoutedEventArgs e)
        {
            MatrixFrame.SetDimensions(MatrixWidth, MatrixHeight);
            this.Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

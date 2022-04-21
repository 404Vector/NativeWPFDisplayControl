using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NativeWPFDisplayControl.Example.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(0.1);
            var bitmapImage = new BitmapImage(new Uri(@"..\..\..\Grid.png", UriKind.Relative));
            dt.Tick += (s, e) =>
            {
                testImage.ImageContext = new WriteableBitmap(bitmapImage);
            };
            dt.Start();
        }
    }
}

using System;
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
            dt.Interval = TimeSpan.FromSeconds(0.01);
            var bitmapImage = new BitmapImage(new Uri(@"C:\Users\kim.hs\Pictures\Grid.png", UriKind.Absolute));
            dt.Tick += (s, e) =>
            {
                testImage.ImageContext = new WriteableBitmap(bitmapImage);
            };
            dt.Start();
        }
    }
}

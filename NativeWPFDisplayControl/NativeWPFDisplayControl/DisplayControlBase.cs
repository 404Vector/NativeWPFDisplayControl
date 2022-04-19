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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NativeWPFDisplayControl
{
    public class DisplayControlBase : UserControl
    {
        #region DependencyProperty

        public Matrix RenderMatrix
        {
            get { return (Matrix)GetValue(RenderMatrixProperty); }
            set { SetValue(RenderMatrixProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RenderMatrixProperty =
            DependencyProperty.Register("RenderMatrix", typeof(Matrix), typeof(DisplayControlBase), new PropertyMetadata(Matrix.Identity));


        #endregion

        #region Event Callback

        protected void ImageLayoutUpdated(object sender, EventArgs e)
        { }

        protected void ImageMouseWheel() 
        { }

        protected void ImageMouseMove() 
        { }

        protected void ImageMouseLeftDown()
        { }

        protected void ImageMouseRightDown()
        { }

        protected void ImageMouseLeftUp() 
        { }

        #endregion

        #region Field

        #endregion

        #region Property

        #endregion

        #region Method

        #endregion
    }
}

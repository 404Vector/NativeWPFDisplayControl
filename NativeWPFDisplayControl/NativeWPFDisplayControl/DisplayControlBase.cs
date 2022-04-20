using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NativeWPFDisplayControl
{
    public abstract class DisplayControlBase : Control
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

        #region Field

        protected Canvas canvasControl = new Canvas();
        protected Image imageControl = new Image();
        protected Point capturedOffset;
        protected Point capturedMousePosition;
        protected Matrix renderTransformMatrix = new Matrix();
        protected double scaleRatio = 1.0;
        protected double scaleMax = 64.0;
        protected double scaleMin = 1.0 / 64.0;
        protected double scaleDelta = 2.0;

        #endregion

        #region Property

        #endregion

        #region Method

        public override void OnApplyTemplate()
        {
            canvasControl = (Canvas)GetTemplateChild("PART_canvas");
            imageControl = (Image)GetTemplateChild("PART_image");
            if (canvasControl != null && imageControl != null)
            {
                canvasControl.MouseMove += OnMouseMove; ;
                canvasControl.MouseDown += OnMouseDown;
                canvasControl.MouseUp += OnMouseUp;
                canvasControl.MouseWheel += OnMouseWheel;
                imageControl.Source = new BitmapImage(new System.Uri(@"C:\Users\kim.hs\Pictures\Grid.png"));
            }
            
            base.OnApplyTemplate();
        }

        private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Image image = imageControl;
            Point pointTarget;
            pointTarget = e.MouseDevice.GetPosition(image);

            double delta = (e.Delta > 0) ? scaleDelta : 1.0 / scaleDelta;
            ScaleAtPrependRenderMatrix(ref renderTransformMatrix, pointTarget, delta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Image image = imageControl;
            Point targetPosition;

            if (!image.IsMouseCaptured) { return; }

            Point currentMousePosition = e.MouseDevice.GetPosition((UIElement)image.Parent);
            targetPosition = new Point()
            {
                X = capturedOffset.X + (currentMousePosition.X - capturedMousePosition.X),
                Y = capturedOffset.Y + (currentMousePosition.Y - capturedMousePosition.Y)
            };
            TranslateRenderMatrix(ref renderTransformMatrix, targetPosition);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image image = imageControl;
            image.ReleaseMouseCapture();
        }

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image image = imageControl;
            image.CaptureMouse();

            capturedMousePosition = e.GetPosition((UIElement)image.Parent);
            capturedOffset.X = image.RenderTransform.Value.OffsetX;
            capturedOffset.Y = image.RenderTransform.Value.OffsetY;
        }

        protected void ApplyRenderMatrix(ref Matrix matrix)
        {
            imageControl.RenderTransform = new MatrixTransform(matrix);
        }

        protected void TranslateRenderMatrix(ref Matrix matrix, Point point)
        {
            matrix.OffsetX = point.X;
            matrix.OffsetY = point.Y;
            return ;
        }

        protected void ScaleRenderMatrix(ref Matrix matrix, double scaleDelta)
        {
            bool isNotInRange = (scaleDelta < 1) ? (scaleMin > scaleRatio * scaleDelta) : (scaleMax < scaleRatio * scaleDelta);
            if (isNotInRange) { return; }
            
            matrix.Scale(scaleDelta, scaleDelta);
            scaleRatio *= scaleDelta;
        }

        protected void ScaleAtPrependRenderMatrix(ref Matrix matrix, Point point, double scaleDelta)
        {
            bool isNotInRange = (scaleDelta < 1) ? (scaleMin > scaleRatio * scaleDelta) : (scaleMax < scaleRatio * scaleDelta);
            if (isNotInRange) { return; }

            matrix.ScaleAtPrepend(scaleDelta, scaleDelta, point.X, point.Y);
            scaleRatio *= scaleDelta;
        }

        #endregion
    }
}

using NativeWPFDisplayControl.Command;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NativeWPFDisplayControl
{
    public abstract class DisplayControlBase : Control
    {
        #region DependencyProperty

        public WriteableBitmap ImageContext
        {
            get { return (WriteableBitmap)GetValue(ImageContextProperty); }
            set { SetValue(ImageContextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageContext.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageContextProperty =
            DependencyProperty.Register("ImageContext", typeof(WriteableBitmap), typeof(DisplayControlBase), new PropertyMetadata(DEFAULT_NULL_IMAGE, OnImageContextChanged));

        #endregion

        #region Field

        protected static readonly WriteableBitmap DEFAULT_NULL_IMAGE = new WriteableBitmap(1, 1, 1, 1, PixelFormats.Bgr24, BitmapPalettes.Gray256);
        protected Canvas canvasControl = new Canvas();
        protected Image imageControl = new Image();
        protected Button scaleRawBtn = new Button();
        protected Button scaleFitBtn = new Button();
        protected Button zoomInBtn = new Button();
        protected Button zoomOutBtn = new Button();

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

        protected static void OnImageContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null || d is not DisplayControlBase) { return; }

            DisplayControlBase displayControlBase = (DisplayControlBase)d;
            Image imageControl = displayControlBase.imageControl;
            WriteableBitmap newBitmap = (WriteableBitmap)(e.NewValue);

            if (newBitmap == null)
            {
                displayControlBase.imageControl.Source = DEFAULT_NULL_IMAGE;
                return;
            }

            if (imageControl.Source is not WriteableBitmap)
            {
                imageControl.Source = newBitmap;
                return;
            }

            WriteableBitmap currentBitmap = (WriteableBitmap)imageControl.Source;
            if(!IsSameDemension(newBitmap, currentBitmap)) 
            {
                imageControl.Source = newBitmap.Clone(); 
            }
            else
            {
                var cloneArea = new Int32Rect(0, 0, newBitmap.PixelWidth, newBitmap.PixelHeight);
                var bufferSize = newBitmap.BackBufferStride * newBitmap.PixelHeight * (newBitmap.Format.BitsPerPixel/8);

                newBitmap.CopyPixels(cloneArea, currentBitmap.BackBuffer, bufferSize, currentBitmap.BackBufferStride);
            }
        }

        protected static bool IsSameDemension(WriteableBitmap bitmapA, WriteableBitmap bitmapB)
        {
            if(bitmapA.Width != bitmapB.Width || bitmapA.Height != bitmapB.Height) { return false; }
            else if(bitmapA.PixelWidth != bitmapB.PixelWidth || bitmapA.PixelHeight != bitmapB.PixelHeight) { return false; }
            else if(bitmapA.Format != bitmapB.Format) { return false; }

            return true;
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
            canvasControl.UpdateLayout();
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

        protected void OnScaleToFitClick(object sender, RoutedEventArgs e)
        {
            if(imageControl.Source == null) { return; }

            double viewWidth = imageControl.ActualWidth * scaleRatio;
            double viewHeight = imageControl.ActualHeight * scaleRatio;
            double viewRatio = viewWidth / viewHeight;
            double canvasWidth = canvasControl.ActualWidth;
            double canvasHeight = canvasControl.ActualHeight;
            double canvasRatio = canvasWidth / canvasHeight;
            bool isFitToCanvasWith = canvasRatio < viewRatio;
            double fitScaleRatio = isFitToCanvasWith ? canvasWidth / viewWidth : canvasHeight / viewHeight;
            Point offset = isFitToCanvasWith ? new Point(0, (canvasHeight - imageControl.ActualHeight * scaleRatio) /2.0) : new Point((canvasWidth - imageControl.ActualWidth * scaleRatio) / 2.0, 0);

            ScaleRenderMatrix(ref renderTransformMatrix, fitScaleRatio);
            ApplyRenderMatrix(ref renderTransformMatrix);
            TranslateRenderMatrix(ref renderTransformMatrix, offset);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        protected void OnScaleToRawClick(object sender, RoutedEventArgs e)
        {
            if (imageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, 1.0 / scaleRatio);
            ApplyRenderMatrix(ref renderTransformMatrix);
            TranslateRenderMatrix(ref renderTransformMatrix, new Point(0, 0));
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        protected void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            if (imageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, scaleDelta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        protected void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (imageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, 1.0 / scaleDelta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        public override void OnApplyTemplate()
        {
            canvasControl = (Canvas)GetTemplateChild("PART_canvas");
            canvasControl.MouseMove += OnMouseMove; ;
            canvasControl.MouseDown += OnMouseDown;
            canvasControl.MouseUp += OnMouseUp;
            canvasControl.MouseWheel += OnMouseWheel;

            imageControl = (Image)GetTemplateChild("PART_image");

            scaleFitBtn = (Button)GetTemplateChild("PART_scaleFitBtn");
            scaleFitBtn.Click += OnScaleToFitClick;

            scaleRawBtn = (Button)GetTemplateChild("PART_scaleRawBtn");
            scaleRawBtn.Click += OnScaleToRawClick;

            zoomInBtn = (Button)GetTemplateChild("PART_zoomInBtn");
            zoomInBtn.Click += OnZoomInClick;

            zoomOutBtn = (Button)GetTemplateChild("PART_zoomOutBtn");
            zoomOutBtn.Click += OnZoomOutClick;

            base.OnApplyTemplate();
        }


        #endregion

        protected DisplayControlBase() : base()
        {
        }
    }
}

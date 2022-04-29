using NativeWPFDisplayControl.Command;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NativeWPFDisplayControl
{
    [TemplatePart(Name = "PART_canvas")]
    [TemplatePart(Name = "PART_image")]
    [TemplatePart(Name = "PART_scaleFitBtn")]
    [TemplatePart(Name = "PART_scaleRawBtn")]
    [TemplatePart(Name = "PART_zoomInBtn")]
    [TemplatePart(Name = "PART_zoomOutBtn")]
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

        public double ScaleRatio
        {
            get { return (double)GetValue(ScaleRatioProperty); }
            set { SetValue(ScaleRatioProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleRatio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleRatioProperty =
            DependencyProperty.Register("ScaleRatio", typeof(double), typeof(DisplayControlBase), new PropertyMetadata(1.0));

        public double ScaleRange
        {
            get { return (double)GetValue(ScaleRangeProperty); }
            set { SetValue(ScaleRangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleRange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleRangeProperty =
            DependencyProperty.Register("ScaleRange", typeof(double), typeof(DisplayControlBase), new PropertyMetadata(64.0));



        public double ScaleDelta
        {
            get { return (double)GetValue(ScaleDeltaProperty); }
            set { SetValue(ScaleDeltaProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleDelta.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleDeltaProperty =
            DependencyProperty.Register("ScaleDelta", typeof(double), typeof(DisplayControlBase), new PropertyMetadata(2.0));




        #endregion

        #region Field

        protected static readonly WriteableBitmap DEFAULT_NULL_IMAGE = new WriteableBitmap(1, 1, 1, 1, PixelFormats.Bgr24, BitmapPalettes.Gray256);
        protected static readonly Point ZERO_POINT = new Point(0, 0);
        private Matrix renderTransformMatrix = new Matrix();
        private Point capturedOffset = ZERO_POINT;
        private Point capturedMousePosition = ZERO_POINT;

        #endregion

        #region Property

        protected Canvas CanvasControl { get; private set; }
        protected Image ImageControl { get; private set; }
        protected Button ScaleRawBtn { get; private set; }
        protected Button ScaleFitBtn { get; private set; }
        protected Button ZoomInBtn { get; private set; }
        protected Button ZoomOutBtn { get; private set; }
        protected Point CapturedOffset { get => capturedOffset; private set => capturedOffset = value; }
        protected Point CapturedMousePosition { get => capturedMousePosition; private set => capturedMousePosition = value; }
        protected Matrix RenderTransformMatrix { get => renderTransformMatrix; private set => renderTransformMatrix = value; }
        protected double ScaleMax => ScaleRange;
        protected double ScaleMin => 1.0 / ScaleRange;

        #endregion

        #region Method

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            CanvasControl = GetTemplateChild("PART_canvas") as Canvas;
            if (CanvasControl == null) { throw new InvalidProgramException("PART_canvas"); }
            CanvasControl.MouseMove += OnMouseMove; ;
            CanvasControl.MouseDown += OnMouseDown;
            CanvasControl.MouseUp += OnMouseUp;
            CanvasControl.MouseWheel += OnMouseWheel;

            ImageControl = GetTemplateChild("PART_image") as Image;

            ScaleFitBtn = (Button)GetTemplateChild("PART_scaleFitBtn");
            ScaleFitBtn.Click += OnScaleToFitClick;

            ScaleRawBtn = (Button)GetTemplateChild("PART_scaleRawBtn");
            ScaleRawBtn.Click += OnScaleToRawClick;

            ZoomInBtn = (Button)GetTemplateChild("PART_zoomInBtn");
            ZoomInBtn.Click += OnZoomInClick;

            ZoomOutBtn = (Button)GetTemplateChild("PART_zoomOutBtn");
            ZoomOutBtn.Click += OnZoomOutClick;

        }

        protected static bool IsSameDemension(WriteableBitmap bitmapA, WriteableBitmap bitmapB)
        {
            if (bitmapA.Width != bitmapB.Width || bitmapA.Height != bitmapB.Height) { return false; }
            else if (bitmapA.PixelWidth != bitmapB.PixelWidth || bitmapA.PixelHeight != bitmapB.PixelHeight) { return false; }
            else if (bitmapA.Format != bitmapB.Format) { return false; }

            return true;
        }

        protected static void OnImageContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null || d is not DisplayControlBase) { return; }

            DisplayControlBase displayControlBase = (DisplayControlBase)d;
            Image imageControl = displayControlBase.ImageControl;
            WriteableBitmap newBitmap = (WriteableBitmap)(e.NewValue);

            if (newBitmap == null)
            {
                displayControlBase.ImageControl.Source = DEFAULT_NULL_IMAGE;
                return;
            }

            if (imageControl.Source is not WriteableBitmap)
            {
                imageControl.Source = newBitmap;
                return;
            }

            WriteableBitmap currentBitmap = (WriteableBitmap)imageControl.Source;
            if (!IsSameDemension(newBitmap, currentBitmap))
            {
                imageControl.Source = newBitmap.Clone();
            }
            else
            {
                var cloneArea = new Int32Rect(0, 0, newBitmap.PixelWidth, newBitmap.PixelHeight);
                var bufferSize = newBitmap.BackBufferStride * newBitmap.PixelHeight * (newBitmap.Format.BitsPerPixel / 8);

                newBitmap.CopyPixels(cloneArea, currentBitmap.BackBuffer, bufferSize, currentBitmap.BackBufferStride);
            }
        }

        protected void ApplyRenderMatrix(ref Matrix matrix)
        {
            ImageControl.RenderTransform = new MatrixTransform(matrix);
            CanvasControl.UpdateLayout();
        }

        protected void TranslateRenderMatrix(ref Matrix matrix, Point point)
        {
            matrix.OffsetX = point.X;
            matrix.OffsetY = point.Y;
            return;
        }

        protected void ScaleRenderMatrix(ref Matrix matrix, double scaleDelta)
        {
            bool isNotInRange = (scaleDelta < 1) ? (ScaleMin > ScaleRatio * scaleDelta) : (ScaleMax < ScaleRatio * scaleDelta);
            if (isNotInRange) { return; }

            matrix.Scale(scaleDelta, scaleDelta);
            ScaleRatio *= scaleDelta;
        }

        protected void ScaleAtPrependRenderMatrix(ref Matrix matrix, Point point, double scaleDelta)
        {
            bool isNotInRange = (scaleDelta < 1) ? (ScaleMin > ScaleRatio * scaleDelta) : (ScaleMax < ScaleRatio * scaleDelta);
            if (isNotInRange) { return; }

            matrix.ScaleAtPrepend(scaleDelta, scaleDelta, point.X, point.Y);
            ScaleRatio *= scaleDelta;
        }

        private void OnMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Image image = ImageControl;
            Point pointTarget;
            pointTarget = e.MouseDevice.GetPosition(image);

            double delta = (e.Delta > 0) ? ScaleDelta : 1.0 / ScaleDelta;
            ScaleAtPrependRenderMatrix(ref renderTransformMatrix, pointTarget, delta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Image image = ImageControl;
            Point targetPosition;

            if (!image.IsMouseCaptured) { return; }

            Point currentMousePosition = e.MouseDevice.GetPosition((UIElement)image.Parent);
            targetPosition = new Point()
            {
                X = CapturedOffset.X + (currentMousePosition.X - CapturedMousePosition.X),
                Y = CapturedOffset.Y + (currentMousePosition.Y - CapturedMousePosition.Y)
            };
            TranslateRenderMatrix(ref renderTransformMatrix, targetPosition);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image image = ImageControl;
            image.ReleaseMouseCapture();
        }

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image image = ImageControl;
            image.CaptureMouse();

            CapturedMousePosition = e.GetPosition((UIElement)image.Parent);
            capturedOffset.X = image.RenderTransform.Value.OffsetX;
            capturedOffset.Y = image.RenderTransform.Value.OffsetY;
        }

        private void OnScaleToFitClick(object sender, RoutedEventArgs e)
        {
            if (ImageControl.Source == null) { return; }

            double viewWidth = ImageControl.ActualWidth * ScaleRatio;
            double viewHeight = ImageControl.ActualHeight * ScaleRatio;
            double viewRatio = viewWidth / viewHeight;
            double canvasWidth = CanvasControl.ActualWidth;
            double canvasHeight = CanvasControl.ActualHeight;
            double canvasRatio = canvasWidth / canvasHeight;
            bool isFitToCanvasWith = canvasRatio < viewRatio;
            double fitScaleRatio = isFitToCanvasWith ? canvasWidth / viewWidth : canvasHeight / viewHeight;
            Point offset = isFitToCanvasWith ? new Point(0, (canvasHeight - ImageControl.ActualHeight * ScaleRatio) / 2.0) : new Point((canvasWidth - ImageControl.ActualWidth * ScaleRatio) / 2.0, 0);

            ScaleRenderMatrix(ref renderTransformMatrix, fitScaleRatio);
            ApplyRenderMatrix(ref renderTransformMatrix);
            TranslateRenderMatrix(ref renderTransformMatrix, offset);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnScaleToRawClick(object sender, RoutedEventArgs e)
        {
            if (ImageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, 1.0 / ScaleRatio);
            ApplyRenderMatrix(ref renderTransformMatrix);
            TranslateRenderMatrix(ref renderTransformMatrix, new Point(0, 0));
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            if (ImageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, ScaleDelta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (ImageControl.Source == null) { return; }

            ScaleRenderMatrix(ref renderTransformMatrix, 1.0 / ScaleDelta);
            ApplyRenderMatrix(ref renderTransformMatrix);
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
using WPFAdorner.Models;
using WPFAdorner.Views.Adorners;

namespace WPFAdorner.Views.UserControls
{
    /// <summary>
    /// ImageUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ImageUserControl : UserControl
    {
        private WriteableBitmap _writeableBitmap;
        private int _rectWidth = 24;
        private Point? _mouseStartPosition=null;
        private Point _mouseContainerPoint;
        private Point _distance;

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            nameof(ImageSource), typeof(WriteableBitmap), typeof(ImageUserControl), new PropertyMetadata(default(WriteableBitmap)));


        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale), typeof(double), typeof(ImageUserControl), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty CurrentPointProperty = DependencyProperty.Register(
            nameof(CurrentPoint), typeof(Point), typeof(ImageUserControl), new PropertyMetadata(default(Point)));

        public static readonly DependencyProperty AdornersProperty = DependencyProperty.Register(
            nameof(Adorners), typeof(ObservableCollection<MarkingShape>), typeof(ImageUserControl), new PropertyMetadata(default(ObservableCollection<MarkingShape>), OnAdornersPropertyChanged));

        private static void OnAdornersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageUserControl imageUserControl)
            {
                imageUserControl.Adorners.CollectionChanged += imageUserControl.OnAdornersCollectionChanged;
            }
        }

        private Dictionary<MarkingShape, Adorner> _adorners = new Dictionary<MarkingShape, Adorner>();
        public void OnAdornersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    if (eNewItem is MarkingRect markingRect)
                    {
                        Rect rect;
                        if (markingRect.IsNew)
                        {
                            // 获取图片的尺寸
                            double imageWidth = ImageSource.Width;
                            double imageHeight = ImageSource.Height;

                            // 创建默认大小的标注框（图片大小的20%）
                            double annotationWidth = imageWidth * 0.2;
                            double annotationHeight = imageHeight * 0.15;

                            // 计算标注框的位置（图片中心）
                            double left = (imageWidth - annotationWidth) / 2;
                            double top = (imageHeight - annotationHeight) / 2;
                            rect = new Rect(left, top, annotationWidth, annotationHeight);
                            markingRect.Width = annotationWidth;
                            markingRect.Height = annotationHeight;
                        }
                        else
                        {
                            rect = new Rect(markingRect.CenterX - markingRect.Width / 2,
                                markingRect.CenterY - markingRect.Height / 2, markingRect.Width, markingRect.Height);
                        }

                        // 创建并添加Adorner
                        var adorner = new RectAdorner(Part_Image, rect, "矩形",markingRect);
                        var layer = AdornerLayer.GetAdornerLayer(Part_Image);

                        if (layer != null)
                        {
                            layer.Add(adorner);
                        }
                        _adorners.Add(markingRect,adorner);
                    }
                    else if (eNewItem is MarkingEllipse markingEllipse)
                    {
                        // 创建并添加Adorner
                        var adorner = new EllipseAdorner(Part_Image,markingEllipse);
                        var layer = AdornerLayer.GetAdornerLayer(Part_Image);

                        if (layer != null)
                        {
                            layer.Add(adorner);
                        }
                        _adorners.Add(markingEllipse, adorner);
                    }
                    else if (eNewItem is MarkingFreeForm markingFreeForm)
                    {
                        // 创建并添加Adorner
                        var adorner = new FreeFormAdorner(Part_Image, markingFreeForm);
                        var layer = AdornerLayer.GetAdornerLayer(Part_Image);

                        if (layer != null)
                        {
                            layer.Add(adorner);
                        }
                        _adorners.Add(markingFreeForm, adorner);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    if (eOldItem is MarkingShape markingShape)
                    {
                        if (_adorners.ContainsKey(markingShape))
                        {
                            var adorner = _adorners[markingShape];
                            _adorners.Remove(markingShape);
                            var layer = AdornerLayer.GetAdornerLayer(Part_Image);
                            if (layer != null)
                            {
                                layer.Remove(adorner);
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<MarkingShape> Adorners
        {
            get => (ObservableCollection<MarkingShape>)GetValue(AdornersProperty);
            set => SetValue(AdornersProperty, value);
        }

        public Point CurrentPoint
        {
            get => (Point)GetValue(CurrentPointProperty);
            set => SetValue(CurrentPointProperty, value);
        }
        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public WriteableBitmap ImageSource
        {
            get => (WriteableBitmap)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }
        public ImageUserControl()
        {
            InitializeComponent();
        }

        private void Part_ImageContainer_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var point = e.GetPosition(Part_Image);
            DoScale(point, e.Delta<0?-0.1:0.1);
        }


        private void ImageUserControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Part_Background.Source = null;
            _writeableBitmap = new WriteableBitmap((int)e.NewSize.Width, (int)e.NewSize.Height, 96, 96,
                PixelFormats.Bgr32, null);
            var data = GenerateBackGroundImageData(_writeableBitmap);
            _writeableBitmap.Lock();
            _writeableBitmap.WritePixels(new Int32Rect(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height), data,
                _writeableBitmap.BackBufferStride, 0);


            _writeableBitmap.Unlock();

            Part_Background.Source = _writeableBitmap;
        }

        private byte[] GenerateBackGroundImageData(WriteableBitmap bitmap)
        {
            int length = bitmap.BackBufferStride * bitmap.PixelHeight;
            byte[] data = new byte[length];
            for (int row = 0; row < bitmap.PixelHeight; row++)
            {
                for (int col = 0; col < bitmap.BackBufferStride; col += 4)
                {
                    if ((row / _rectWidth) % 2 == 0)
                    {
                        if ((col / (_rectWidth * (bitmap.BackBufferStride / bitmap.PixelWidth))) % 2 == 0)
                        {
                            data[row * bitmap.BackBufferStride + col] = 70;
                            data[row * bitmap.BackBufferStride + col + 1] = 70;
                            data[row * bitmap.BackBufferStride + col + 2] = 70;
                            data[row * bitmap.BackBufferStride + col + 3] = 255;
                        }
                        else
                        {
                            data[row * bitmap.BackBufferStride + col] = 30;
                            data[row * bitmap.BackBufferStride + col + 1] = 30;
                            data[row * bitmap.BackBufferStride + col + 2] = 30;
                            data[row * bitmap.BackBufferStride + col + 3] = 255;
                        }
                    }
                    else
                    {
                        if ((col / (_rectWidth * (bitmap.BackBufferStride / bitmap.PixelWidth))) % 2 == 0)
                        {
                            data[row * bitmap.BackBufferStride + col] = 30;
                            data[row * bitmap.BackBufferStride + col + 1] = 30;
                            data[row * bitmap.BackBufferStride + col + 2] = 30;
                            data[row * bitmap.BackBufferStride + col + 3] = 255;
                        }
                        else
                        {
                            data[row * bitmap.BackBufferStride + col] = 70;
                            data[row * bitmap.BackBufferStride + col + 1] = 70;
                            data[row * bitmap.BackBufferStride + col + 2] = 70;
                            data[row * bitmap.BackBufferStride + col + 3] = 255;
                        }
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// 缩放图片。最小为0.1倍，最大为30倍
        /// </summary>
        /// <param name="point">相对于图片的点，以此点为中心缩放</param>
        /// <param name="delta">缩放的倍数增量</param>
        private void DoScale(Point point, double delta)
        {
            // 限制最大、最小缩放倍数
            if (scaler.ScaleX + delta < 0.1 || scaler.ScaleX + delta > 30) return;

 
            scaler.ScaleX += delta;
            scaler.ScaleY += delta;

            Scale = scaler.ScaleX;

            transer.X -= point.X * delta;
            transer.Y -= point.Y * delta;
        }


        private void Part_Image_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scale = scaler.ScaleX;
        }

        private void Part_ImageContainer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(Part_Image);
            _distance = Part_Image.TranslatePoint(new Point(), Part_ImageContainer);
            _mouseStartPosition = point;
            _mouseContainerPoint = e.GetPosition(Part_ImageContainer);
        }

        private void Part_ImageContainer_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void ImageUserControl_OnMouseMove(object sender, MouseEventArgs e)
        {

            var point = e.GetPosition(Part_Image);

            //var point=Part_Image.TranslatePoint(new Point(), Part_ImageContainer);
            CurrentPoint = point;
        }

        private void Part_ImageContainer_OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(Part_Image);
            var containerPoint = e.GetPosition(Part_ImageContainer);

            //鼠标左键按下移动视为拖拽
            if (e.LeftButton == MouseButtonState.Pressed&&_mouseStartPosition!=null)
            {
                Part_ImageContainer.Cursor = Cursors.SizeAll;
                
                var xMoveDistance=containerPoint.X- _mouseContainerPoint.X;
                var yMoveDistance=containerPoint.Y- _mouseContainerPoint.Y;
                var xOffset = point.X - _mouseStartPosition.Value.X;
                var yOffset = point.Y - _mouseStartPosition.Value.Y;

                Debug.WriteLine($"----------  {_distance.X + Part_Image.ActualWidth * scaler.ScaleX + xMoveDistance}------------   {_distance.X + xMoveDistance}-----------");
                if ((_distance.X + Part_Image.ActualWidth * scaler.ScaleX + xMoveDistance) >=
                    Part_ImageContainer.ActualWidth && _distance.X + xMoveDistance <= 0)
                {
                    transer.X += xOffset;
                }

                if ((_distance.Y + Part_Image.ActualHeight * scaler.ScaleY + yMoveDistance) >=
                    Part_ImageContainer.ActualHeight && _distance.Y + yMoveDistance <= 0)
                {
                    transer.Y += yOffset;
                }

            }
            else
            {
                Part_ImageContainer.Cursor = Cursors.Arrow;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var adorner=Adorners.FirstOrDefault(P => P.IsSelected);

            if (adorner != null)
            {
                Adorners.Remove(adorner);
            }
        }
    }
}

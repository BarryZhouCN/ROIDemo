using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFAdorner.Views.Adorners
{
    public class EfficientAnnotationAdorner : Adorner
    {
        private Rect _annotationRect;
        private string _annotationType;
        private Pen _outlinePen;
        private Brush _handleBrush;
        private Brush _rotateHandleBrush;

        // 交互状态
        private bool _isDragging;
        private bool _isRotating;
        private bool _isResizing;
        private Point _startPoint;
        private Point _transformOrigin;
        private double _startAngle;
        private Rect _initialRect;

        // 手柄大小
        private const double HandleSize = 8;
        private const double RotateHandleOffset = 25;

        // 旋转中心
        private Point RotateCenter => new Point(
            _annotationRect.Left + _annotationRect.Width / 2,
            _annotationRect.Top + _annotationRect.Height / 2);

        public EfficientAnnotationAdorner(UIElement adornedElement, Rect initialRect, string annotationType)
            : base(adornedElement)
        {
            _annotationRect = initialRect;
            _annotationType = annotationType;

            // 设置样式
            _outlinePen = new Pen(Brushes.Red, 2);
            _handleBrush = Brushes.Red;
            _rotateHandleBrush = Brushes.Blue;

            // 启用鼠标交互
            IsHitTestVisible = true;
        }

        // 重写OnRender方法进行高效绘制
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 绘制标注形状
            DrawAnnotationShape(drawingContext);

            // 绘制手柄（只有在选中状态时才显示）
            DrawHandles(drawingContext);
        }

        private void DrawAnnotationShape(DrawingContext drawingContext)
        {
            switch (_annotationType)
            {
                case "矩形":
                    drawingContext.DrawRectangle(null, _outlinePen, _annotationRect);
                    break;

                case "圆形":
                    double radiusX = _annotationRect.Width / 2;
                    double radiusY = _annotationRect.Height / 2;
                    Point center = new Point(
                        _annotationRect.Left + radiusX,
                        _annotationRect.Top + radiusY);
                    drawingContext.DrawEllipse(null, _outlinePen, center, radiusX, radiusY);
                    break;

                case "箭头":
                    DrawArrow(drawingContext);
                    break;
            }
        }

        private void DrawArrow(DrawingContext drawingContext)
        {
            Point start = new Point(_annotationRect.Left, _annotationRect.Top + _annotationRect.Height / 2);
            Point end = new Point(_annotationRect.Right, _annotationRect.Top + _annotationRect.Height / 2);

            // 绘制箭头线
            drawingContext.DrawLine(_outlinePen, start, end);

            // 绘制箭头头部
            double arrowLength = 10;
            double arrowAngle = Math.PI / 6; // 30度

            Vector direction = (end - start);
            direction.Normalize();

            Point arrowPoint1 = end - arrowLength * (direction * Math.Cos(arrowAngle) +
                                                    new Vector(0, 1) * Math.Sin(arrowAngle));
            Point arrowPoint2 = end - arrowLength * (direction * Math.Cos(arrowAngle) -
                                                    new Vector(0, 1) * Math.Sin(arrowAngle));

            drawingContext.DrawLine(_outlinePen, end, arrowPoint1);
            drawingContext.DrawLine(_outlinePen, end, arrowPoint2);
        }

        private void DrawHandles(DrawingContext drawingContext)
        {
            // 绘制四个角的手柄
            DrawHandle(drawingContext, new Point(_annotationRect.Left, _annotationRect.Top));
            DrawHandle(drawingContext, new Point(_annotationRect.Right, _annotationRect.Top));
            DrawHandle(drawingContext, new Point(_annotationRect.Left, _annotationRect.Bottom));
            DrawHandle(drawingContext, new Point(_annotationRect.Right, _annotationRect.Bottom));

            // 绘制旋转手柄
            Point rotateHandlePos = new Point(
                _annotationRect.Left + _annotationRect.Width / 2,
                _annotationRect.Top - RotateHandleOffset);

            drawingContext.DrawEllipse(_rotateHandleBrush, null, rotateHandlePos, HandleSize, HandleSize);

            // 绘制连接线
            drawingContext.DrawLine(
                new Pen(Brushes.Blue, 1),
                new Point(rotateHandlePos.X, _annotationRect.Top),
                rotateHandlePos);
        }

        private void DrawHandle(DrawingContext drawingContext, Point center)
        {
            drawingContext.DrawEllipse(_handleBrush, null, center, HandleSize, HandleSize);
        }

        // 鼠标事件处理
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point position = e.GetPosition(this);
            _startPoint = position;
            _initialRect = _annotationRect;

            // 确定操作类型
            if (IsOverRotateHandle(position))
            {
                _isRotating = true;
                _transformOrigin = RotateCenter;

                Vector v = position - _transformOrigin;
                _startAngle = Math.Atan2(v.Y, v.X);
            }
            else if (IsOverResizeHandle(position))
            {
                _isResizing = true;
            }
            else if (_annotationRect.Contains(position))
            {
                _isDragging = true;
            }

            if (_isDragging || _isResizing || _isRotating)
            {
                CaptureMouse();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!IsMouseCaptured) return;

            Point currentPoint = e.GetPosition(this);

            if (_isDragging)
            {
                // 移动标注
                Vector delta = currentPoint - _startPoint;
                _annotationRect.X = _initialRect.X + delta.X;
                _annotationRect.Y = _initialRect.Y + delta.Y;

                // 限制在图片范围内
                var adornedElement = AdornedElement as FrameworkElement;
                if (adornedElement != null)
                {
                    _annotationRect.X = Math.Max(0, Math.Min(_annotationRect.X, adornedElement.ActualWidth - _annotationRect.Width));
                    _annotationRect.Y = Math.Max(0, Math.Min(_annotationRect.Y, adornedElement.ActualHeight - _annotationRect.Height));
                }
            }
            else if (_isResizing)
            {
                // 调整大小
                ResizeAnnotation(currentPoint);
            }
            else if (_isRotating)
            {
                // 旋转标注
                RotateAnnotation(currentPoint);
            }

            // 请求重绘
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
                _isDragging = false;
                _isResizing = false;
                _isRotating = false;
            }
        }

        private bool IsOverRotateHandle(Point point)
        {
            Point rotateHandlePos = new Point(
                _annotationRect.Left + _annotationRect.Width / 2,
                _annotationRect.Top - RotateHandleOffset);

            return (point - rotateHandlePos).Length <= HandleSize * 2;
        }

        private bool IsOverResizeHandle(Point point)
        {
            // 检查四个角的手柄
            return (point - new Point(_annotationRect.Left, _annotationRect.Top)).Length <= HandleSize * 2 ||
                   (point - new Point(_annotationRect.Right, _annotationRect.Top)).Length <= HandleSize * 2 ||
                   (point - new Point(_annotationRect.Left, _annotationRect.Bottom)).Length <= HandleSize * 2 ||
                   (point - new Point(_annotationRect.Right, _annotationRect.Bottom)).Length <= HandleSize * 2;
        }

        private void ResizeAnnotation(Point currentPoint)
        {
            // 计算缩放比例
            double scaleX = (currentPoint.X - _annotationRect.Left) / _initialRect.Width;
            double scaleY = (currentPoint.Y - _annotationRect.Top) / _initialRect.Height;

            // 应用缩放，保持最小尺寸
            _annotationRect.Width = Math.Max(10, _initialRect.Width * scaleX);
            _annotationRect.Height = Math.Max(10, _initialRect.Height * scaleY);
        }

        private void RotateAnnotation(Point currentPoint)
        {
            // 计算旋转角度
            Vector v = currentPoint - _transformOrigin;
            double currentAngle = Math.Atan2(v.Y, v.X);
            double angleDelta = currentAngle - _startAngle;

            // 应用旋转变换
            var transform = new RotateTransform(angleDelta * 180 / Math.PI,
                                              _transformOrigin.X, _transformOrigin.Y);

            // 更新标注形状（这里简化处理，实际应用中可能需要更复杂的变换）
            // 注意：这个示例中旋转只是视觉上的，实际存储的矩形数据没有旋转
            // 如果需要保存旋转状态，需要修改数据结构
        }

        // 获取标注框的位置和大小（相对于图片）
        public Rect GetAnnotationRect()
        {
            return _annotationRect;
        }

        // 设置标注框的位置和大小
        public void SetAnnotationRect(Rect rect)
        {
            _annotationRect = rect;
            InvalidateVisual();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WPFAdorner.Helpers;
using WPFAdorner.Models;

namespace WPFAdorner.Views.Adorners
{
    public class RectAdorner:GeometryAnnotationAdorner
    {
        #region 属性

        private Point _center;
        // 旋转中心
        private Point _start;
       

        private MarkingRect _markingRect;

        #endregion

        #region 构造函数


        public RectAdorner(UIElement adornedElement, Rect initialRect, string annotationType, MarkingRect rect)
            : base(adornedElement,rect)
        {
            _annotationRect = initialRect;
            _annotationType = annotationType;
            _adornedElement = adornedElement;
            // 自由绘制初始化
            _freeformPoints = new List<Point>();
            _markingRect = rect;
           
            _center = new Point(
                _annotationRect.Left + _annotationRect.Width / 2,
                _annotationRect.Top + _annotationRect.Height / 2);
            _start = _center;
            _markingRect.CenterX = _center.X;
            _markingRect.CenterY = _center.Y;
            _transformGroup = new TransformGroup();
            _rotateTransform = new RotateTransform
            {
                CenterX = _start.X,
                CenterY = _start.Y
            };
            _translateTransform = new TranslateTransform();
            _markingRect.Width = _annotationRect.Width;
            _markingRect.Height = _annotationRect.Height;
            _scaleTransform = new ScaleTransform()
            {
                CenterX = _start.X,
                CenterY = _start.Y
            };

            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_rotateTransform);
            _transformGroup.Children.Add(_translateTransform);

            // 初始创建几何图形
            UpdateAnnotationGeometry();
        }

        #endregion

        #region 几何图形更新

        protected override void UpdateGeometries()
        {
            // 更新手柄几何图形
            UpdateHandleGeometries();

            // 请求重绘
            InvalidateVisual();
        }

        private void UpdateAnnotationGeometry()
        {
            switch (_annotationType)
            {
                case "矩形":
                    _annotationGeometry = new RectangleGeometry(_annotationRect);
                    break;

                case "圆形":
                    double radiusX = _annotationRect.Width / 2;
                    double radiusY = _annotationRect.Height / 2;
                    Point center = new Point(
                        _annotationRect.Left + radiusX,
                        _annotationRect.Top + radiusY);
                    _annotationGeometry = new EllipseGeometry(center, radiusX, radiusY);
                    break;

                case "箭头":
                    _annotationGeometry = CreateArrowGeometry();
                    break;

                case "自由形状":
                    _annotationGeometry = CreateFreeformGeometry();
                    break;
            }

            _annotationGeometry.Transform = _transformGroup;
        }

        private Geometry CreateArrowGeometry()
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            Point start = new Point(_annotationRect.Left, _annotationRect.Top + _annotationRect.Height / 2);
            Point end = new Point(_annotationRect.Right, _annotationRect.Top + _annotationRect.Height / 2);

            figure.StartPoint = start;
            figure.Segments.Add(new LineSegment(end, true));

            // 添加箭头头部
            double arrowLength = 10;
            double arrowAngle = Math.PI / 6; // 30度

            Vector direction = (end - start);
            direction.Normalize();

            Point arrowPoint1 = end - arrowLength * (direction * Math.Cos(arrowAngle) +
                                                    new Vector(0, 1) * Math.Sin(arrowAngle));
            Point arrowPoint2 = end - arrowLength * (direction * Math.Cos(arrowAngle) -
                                                    new Vector(0, 1) * Math.Sin(arrowAngle));

            figure.Segments.Add(new LineSegment(arrowPoint1, true));
            figure.Segments.Add(new LineSegment(end, true));
            figure.Segments.Add(new LineSegment(arrowPoint2, true));

            geometry.Figures.Add(figure);
            return geometry;
        }

        private Geometry CreateFreeformGeometry()
        {
            if (_freeformPoints.Count < 2)
                return null;

            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            figure.StartPoint = _freeformPoints[0];

            for (int i = 1; i < _freeformPoints.Count; i++)
            {
                figure.Segments.Add(new LineSegment(_freeformPoints[i], true));
            }

            // 如果是封闭形状，连接首尾点
            if (_freeformPoints.Count > 2)
            {
                figure.IsClosed = true;
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        private void UpdateHandleGeometries()
        {

            if (IsSelected)
            {
                //计算4个顶点的位置
                var results = PositionCalHelper.CalculateVertices(_markingRect.Width, _markingRect.Height, new Point(_markingRect.CenterX, _markingRect.CenterY), _markingRect.Angle);
                // 更新四个角的手柄
                _topLeftHandle = new EllipseGeometry(results[0], HandleSize, HandleSize);
                _topRightHandle = new EllipseGeometry(results[1], HandleSize, HandleSize);
                _bottomLeftHandle = new EllipseGeometry(results[3], HandleSize, HandleSize);
                _bottomRightHandle = new EllipseGeometry(results[2], HandleSize, HandleSize);

                // 更新旋转手柄
                _rotateHandle = new EllipseGeometry(results[4], HandleSize, HandleSize);
            }
            else
            {
                _topLeftHandle = null;
                _topRightHandle = null;
                _bottomLeftHandle = null;
                _bottomRightHandle = null;
                _rotateHandle = null;
            }
        
        

        }

       

        private void UpdateRotateConnection()
        {
            Point rotateHandleCenter = new Point(
                _annotationRect.Left + _annotationRect.Width / 2,
                _annotationRect.Top - RotateHandleOffset);

            Point topCenter = new Point(
                _annotationRect.Left + _annotationRect.Width / 2,
                _annotationRect.Top);

            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            figure.StartPoint = rotateHandleCenter;
            figure.Segments.Add(new LineSegment(topCenter, true));

            geometry.Figures.Add(figure);
            _rotateConnection = geometry;
        }

        #endregion

        #region 渲染

       
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 绘制标注形状
            if (_annotationGeometry != null)
            {
                drawingContext.DrawGeometry(null, _outlinePen, _annotationGeometry);
                //drawingContext.DrawGeometry(null, _outlinePen, new RectangleGeometry(_annotationRect));
            }

            if (IsSelected)
            {
                // 绘制手柄
                DrawHandles(drawingContext);
            }


        }

        private void DrawHandles(DrawingContext drawingContext)
        {
            // 绘制四个角的手柄
            drawingContext.DrawGeometry(_handleBrush, null, _topLeftHandle);
            drawingContext.DrawGeometry(_handleBrush, null, _topRightHandle);
            drawingContext.DrawGeometry(_handleBrush, null, _bottomLeftHandle);
            drawingContext.DrawGeometry(_handleBrush, null, _bottomRightHandle);

            // 绘制旋转手柄
            drawingContext.DrawGeometry(_rotateHandleBrush, null, _rotateHandle);
        }

        #endregion

        #region 鼠标交互

        private Point _rotateStartPoint;
        private double _rotateStartAngle;

        private Point _scaleStartPoint;
        private double _scaleX;
        private double _scaleY;
        private double _scaleWidth;
        private double _scaleHeight;

        private Point _dragStartPoint;
        private double _offsetX;
        private double _offsetY;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsSelected = true;
            base.OnMouseLeftButtonDown(e);

            Point position = e.GetPosition(_adornedElement);
            _startPoint = position;
            _initialRect = _annotationRect;

            // 自由绘制模式
            if (_annotationType == "自由形状")
            {
                _isDrawingFreeform = true;
                _freeformPoints.Add(position);
                UpdateGeometries();
                return;
            }

            // 确定操作类型
            if (IsOverGeometry(position, _rotateHandle))
            {
                _isRotating = true;
                _rotateStartPoint = position;
                _rotateStartAngle = _rotateTransform.Angle;
                Vector v = position - _start;
                _startAngle = Math.Atan2(v.Y, v.X);
            }
            else if (IsOverResizeHandle(position))
            {
                _scaleStartPoint = position;
                _scaleX = _scaleTransform.ScaleX;
                _scaleY = _scaleTransform.ScaleY;
                _scaleWidth = _markingRect.Width;
                _scaleHeight = _markingRect.Height;
                _isResizing = true;
            }
            else if (_annotationGeometry != null && _annotationGeometry.FillContains(position))
            {
                _isDragging = true;
                _dragStartPoint = position;
                _center = new Point(_markingRect.CenterX, _markingRect.CenterY);
                _offsetX = _translateTransform.X;
                _offsetY = _translateTransform.Y;
            }

            if (_isDragging || _isResizing || _isRotating)
            {
                CaptureMouse();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point currentPoint = e.GetPosition(_adornedElement);


            if (!IsMouseCaptured)
            {
                if (IsOverGeometry(currentPoint, _topLeftHandle) || IsOverGeometry(currentPoint, _bottomRightHandle))
                {
                    this.Cursor = Cursors.SizeNWSE;
                }
                else if (IsOverGeometry(currentPoint, _topRightHandle) || IsOverGeometry(currentPoint, _bottomLeftHandle))
                {
                    this.Cursor = Cursors.SizeNESW;
                }
                else if (IsOverGeometry(currentPoint, _rotateHandle))
                {
                    this.Cursor = new Cursor(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RotateCursor.cur"));
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                if (IsOverGeometry(currentPoint, _annotationGeometry))
                {
                    this.Cursor = Cursors.SizeAll;
                }
            }

            // 自由绘制模式
            if (_isDrawingFreeform)
            {
                _freeformPoints.Add(currentPoint);
                UpdateGeometries();
                return;
            }

            if (!IsMouseCaptured) return;



            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                MoveAnnotation(currentPoint);
                // 更新手柄几何图形
                UpdateHandleGeometries();

                // 更新旋转连接线
                UpdateRotateConnection();

                // 请求重绘
                InvalidateVisual();
            }
            else if (_isResizing)
            {
                // 调整大小
                ResizeAnnotation(currentPoint);
                // 更新手柄几何图形
                UpdateHandleGeometries();

                // 更新旋转连接线
                UpdateRotateConnection();

                // 请求重绘
                InvalidateVisual();
            }
            else if (_isRotating)
            {
                this.Cursor = new Cursor(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RotateCursor.cur"));
                // 旋转标注
                RotateAnnotation(currentPoint);
                // 更新手柄几何图形
                UpdateHandleGeometries();

                // 更新旋转连接线
                UpdateRotateConnection();

                // 请求重绘
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isDrawingFreeform)
            {
                _isDrawingFreeform = false;
                return;
            }

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
                _isDragging = false;
                _isResizing = false;
                _isRotating = false;
            }

        }

        private void ResizeAnnotation(Point currentPoint)
        {
            // 计算缩放比例
            double scaleX = (currentPoint.X - _markingRect.CenterX) / (_scaleStartPoint.X - _markingRect.CenterX);
            double scaleY = (currentPoint.Y - _markingRect.CenterY) / (_scaleStartPoint.Y - _markingRect.CenterY);

            _markingRect.Width = Math.Max(10, _scaleWidth * scaleX);
            _markingRect.Height = Math.Max(10, _scaleHeight * scaleY);

            //Debug.WriteLine($"before:{_markingRect.Width},{_markingRect.Height};After:{_annotationGeometry.Bounds.Width},{_annotationGeometry.Bounds.Height}");
            //Debug.WriteLine($"before:{_scaleTransform.ScaleX},{_scaleTransform.ScaleY};After:{scaleX},{scaleY}");
            //_scaleTransform.CenterX = _markingRect.Width/2;
            //_scaleTransform.CenterY =_markingRect.Height/2;
            _scaleTransform.ScaleX = _scaleX * scaleX;
            _scaleTransform.ScaleY = _scaleY * scaleY;

        }

        private void RotateAnnotation(Point currentPoint)
        {
            // 计算旋转角度
            double currentAngle = PositionCalHelper.CalculateAngle(_rotateStartPoint, currentPoint,
                new Point(_markingRect.CenterX, _markingRect.CenterY));

            //Debug.WriteLine($"Rotate Angle{_rotateStartPoint},Current Point{currentPoint},Origin Point{_rotateStartAngle},Angle{angle}");

            _rotateTransform.Angle = _rotateStartAngle + currentAngle;
            _markingRect.Angle = _rotateTransform.Angle;
        }

  


        private void MoveAnnotation(Point currentPoint)
        {
            // 移动标注
            Vector delta = currentPoint - _dragStartPoint;
            //_annotationRect.X = _initialRect.X + delta.X;
            //_annotationRect.Y = _initialRect.Y + delta.Y;
            _markingRect.CenterX = _center.X + (delta.X);
            _markingRect.CenterY = _center.Y + (delta.Y);
            // 限制在图片范围内
            //var adornedElement = AdornedElement as FrameworkElement;
            //if (adornedElement != null)
            //{
            //    _annotationRect.X = Math.Max(0, Math.Min(_annotationRect.X, adornedElement.ActualWidth - _annotationRect.Width));
            //    _annotationRect.Y = Math.Max(0, Math.Min(_annotationRect.Y, adornedElement.ActualHeight - _annotationRect.Height));
            //}
            _translateTransform.X = _offsetX + delta.X;
            _translateTransform.Y = _offsetY + delta.Y;
        }

        #endregion

        #region 公共方法

        public Rect GetAnnotationRect()
        {
            return _annotationRect;
        }

        public void SetAnnotationRect(Rect rect)
        {
            _annotationRect = rect;
            UpdateGeometries();
        }

        public string GetAnnotationType()
        {
            return _annotationType;
        }

        public void SetAnnotationType(string type)
        {
            _annotationType = type;
            UpdateGeometries();
        }

        #endregion
    }
}

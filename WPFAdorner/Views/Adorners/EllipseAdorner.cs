using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFAdorner.Helpers;
using WPFAdorner.Models;

namespace WPFAdorner.Views.Adorners
{
    public class EllipseAdorner:GeometryAnnotationAdorner
    {
        private MarkingEllipse _ellipse;
        private Point _center;
        public EllipseAdorner(UIElement adornedElement,MarkingEllipse ellipse) : base(adornedElement,ellipse)
        {
            _ellipse=ellipse;
            Init();
        }

        private void Init()
        {
            if (_adornedElement is FrameworkElement adornedElement)
            {
                if (_ellipse.IsNew)
                {
                    _ellipse.CenterX = adornedElement.ActualWidth / 2;
                    _ellipse.CenterY= adornedElement.ActualHeight / 2;

                    _ellipse.RadiusX = adornedElement.ActualHeight * 0.2;
                    _ellipse.RadiusY = _ellipse.RadiusX;
                }

                _rotateTransform = new RotateTransform
                {
                    CenterX = _ellipse.CenterX,
                    CenterY = _ellipse.CenterY
                };

                _annotationGeometry = new EllipseGeometry(new Point(_ellipse.CenterX, _ellipse.CenterY), _ellipse.RadiusX,
                    _ellipse.RadiusY);
                _annotationGeometry.Transform= _rotateTransform;

                _isSelectedGeometry = new RectangleGeometry(new Rect(_ellipse.CenterX- _ellipse.RadiusX, _ellipse.CenterY- _ellipse.RadiusY,
                    _ellipse.RadiusX * 2, _ellipse.RadiusY * 2));
                _isSelectedGeometry.Transform= _rotateTransform;
                InvalidateVisual();
            }

           
        }


        protected override void UpdateGeometries()
        {
            // 更新手柄几何图形
            UpdateHandleGeometries();

            // 请求重绘
            InvalidateVisual();
        }


        private void UpdateHandleGeometries()
        {
            _rotateTransform.Angle = _ellipse.Angle;
            _rotateTransform.CenterX = _ellipse.CenterX;
            _rotateTransform.CenterY = _ellipse.CenterY;
            _annotationGeometry = new EllipseGeometry(new Point(_ellipse.CenterX, _ellipse.CenterY), _ellipse.RadiusX,
                _ellipse.RadiusY);
            _annotationGeometry.Transform = _rotateTransform;
            _isSelectedGeometry = new RectangleGeometry(new Rect(_ellipse.CenterX - _ellipse.RadiusX, _ellipse.CenterY - _ellipse.RadiusY,
                _ellipse.RadiusX * 2, _ellipse.RadiusY * 2));
            _isSelectedGeometry.Transform = _rotateTransform;
            if (IsSelected)
            {
                //计算4个顶点的位置
                var results = PositionCalHelper.CalculateVertices(_ellipse.RadiusX * 2, _ellipse.RadiusY * 2, new Point(_ellipse.CenterX, _ellipse.CenterY), _ellipse.Angle);
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


        #region 渲染

        private Pen _isSelectedPen=new Pen(Brushes.LightBlue,2){DashStyle = DashStyles.Dash};
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 绘制标注形状
            if (_annotationGeometry != null)
            {
                drawingContext.DrawGeometry(null, _outlinePen, _annotationGeometry);
            }

            if (IsSelected)
            {
                if (_isSelectedGeometry != null)
                {
                    drawingContext.DrawGeometry(null, _isSelectedPen, _isSelectedGeometry);
                }
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

            // 确定操作类型
            if (IsOverGeometry(position, _rotateHandle))
            {
                _isRotating = true;
                _rotateStartPoint = position;
                _rotateStartAngle = _rotateTransform.Angle;
            }
            else if (IsOverResizeHandle(position))
            {
                _scaleStartPoint = position;
                _scaleWidth = _ellipse.RadiusX;
                _scaleHeight = _ellipse.RadiusY;
                _isResizing = true;
            }
            else if (_annotationGeometry != null && _annotationGeometry.FillContains(position))
            {
                _isDragging = true;
                _dragStartPoint = position;
                _center = new Point(_ellipse.CenterX, _ellipse.CenterY);
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
                else if (IsOverGeometry(currentPoint, _rotateHandle) ||_isRotating)
                {
                    this.Cursor = new Cursor(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RotateCursor.cur"));
                }
                else
                {
                    this.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                if (IsOverGeometry(currentPoint, _annotationGeometry) || _isDragging)
                {
                    this.Cursor = Cursors.SizeAll;
                }
            }


            if (!IsMouseCaptured) return;



            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                MoveAnnotation(currentPoint);
                UpdateGeometries();
            }
            else if (_isResizing)
            {
                // 调整大小
                ResizeAnnotation(currentPoint);
                UpdateGeometries();
            }
            else if (_isRotating)
            {
                this.Cursor = new Cursor(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RotateCursor.cur"));
                // 旋转标注
                RotateAnnotation(currentPoint);
                UpdateGeometries();
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
            double scaleX = (currentPoint.X - _ellipse.CenterX) / (_scaleStartPoint.X - _ellipse.CenterX);
            double scaleY = (currentPoint.Y - _ellipse.CenterY) / (_scaleStartPoint.Y - _ellipse.CenterY);

            _ellipse.RadiusX = Math.Max(10, _scaleWidth * scaleX);
            _ellipse.RadiusY = Math.Max(10, _scaleHeight * scaleY);

        }

        private void RotateAnnotation(Point currentPoint)
        {
            // 计算旋转角度
            double currentAngle = PositionCalHelper.CalculateAngle(_rotateStartPoint, currentPoint,
                new Point(_ellipse.CenterX, _ellipse.CenterY));
            _ellipse.Angle = _rotateStartAngle + currentAngle;
        }

        private void MoveAnnotation(Point currentPoint)
        {
            // 移动标注
            Vector delta = currentPoint - _dragStartPoint;
            _ellipse.CenterX = _center.X + (delta.X);
            _ellipse.CenterY = _center.Y + (delta.Y);
 
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using WPFAdorner.Helpers;
using WPFAdorner.Models;

namespace WPFAdorner.Views.Adorners
{
    public class FreeFormAdorner : GeometryAnnotationAdorner
    {
        private MarkingFreeForm _markingFreeForm;
        private List<Geometry> _handles;
        private bool _isDrawing;
        private FrameworkElement _element;
        public FreeFormAdorner(UIElement adornedElement, MarkingFreeForm markingFreeForm) : base(adornedElement,markingFreeForm)
        {
            _markingFreeForm = markingFreeForm;
            _element=adornedElement as FrameworkElement;
            Init();
        }

        private void Init()
        {
            _handles = new List<Geometry>();
            _isDrawing = _markingFreeForm.Points.Count == 0;
            UpdateGeometries();
        }

        private void CreateFreeFormGeometry(bool isFigureClose=false)
        {
            if (_markingFreeForm.Points.Count < 1)
                return;

            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();

            figure.StartPoint = _markingFreeForm.Points[0];

            for (int i = 1; i < _markingFreeForm.Points.Count; i++)
            {
                figure.Segments.Add(new LineSegment(_markingFreeForm.Points[i], true));
            }

            //// 如果是封闭形状，连接首尾点
            //if (_markingFreeForm.Points.Count > 2)
            //{
            figure.IsClosed = isFigureClose;
            //}

            geometry.Figures.Add(figure);
            _annotationGeometry = geometry;
        }

        protected override void UpdateGeometries()
        {

            CreateFreeFormGeometry();
            UpdateHandleGeometries();
            InvalidateVisual();
        }

        protected void UpdateGeometries(bool isFigureClose)
        {

            CreateFreeFormGeometry(isFigureClose);
            UpdateHandleGeometries();
            InvalidateVisual();
        }

        private void UpdateHandleGeometries()
        {
            _handles.Clear();
            for (int i = 0; i < _markingFreeForm.Points.Count; i++)
            {
                if (i == 0)
                {
                    if (_canClose && _isDrawing)
                    {
                        _handles.Add(new EllipseGeometry(_markingFreeForm.Points[i], HandleSize+10, HandleSize+10));
                        continue;
                    }
                }
                _handles.Add(new EllipseGeometry(_markingFreeForm.Points[i], HandleSize, HandleSize));
            }

        }

        #region 渲染
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 绘制标注形状
            if (_annotationGeometry != null)
            {
                drawingContext.DrawGeometry(null, _outlinePen, _annotationGeometry);
            }

            // 绘制手柄
            DrawHandles(drawingContext);


        }

        private void DrawHandles(DrawingContext drawingContext)
        {

            for (int i = 0; i < _handles.Count; i++)
            {
                drawingContext.DrawGeometry(_handleBrush, null, _handles[i]);
            }
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
        private List<Point> _startPoints;
        private int _resizeIndex;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point position = e.GetPosition(_adornedElement);
            _startPoint = position;

            if (_isDrawing)
            {
                if (_canClose)
                {
                    _isDrawing = false;
                    UpdateGeometries(true);
                }
                else
                {
                    _markingFreeForm.Points.Add(position);
                    UpdateGeometries();
                }
                
            }
            else
            {
                var temp = _handles.FirstOrDefault(p => IsOverGeometry(position, p));
                if (temp!=null)
                {
                    _isResizing = true;
                    _resizeIndex = _handles.IndexOf(temp);
                }
                else if (_annotationGeometry != null && _annotationGeometry.FillContains(position))
                {
                    _isDragging = true;
                    _dragStartPoint = position;
                    _startPoints = new List<Point>(_markingFreeForm.Points);
                }

                if (_isDragging || _isResizing || _isRotating)
                {
                    CaptureMouse();
                }
            }

           
        }

        private bool _canClose;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point currentPoint = e.GetPosition(_adornedElement);


            if (_isDrawing)
            {
                var temp = _markingFreeForm.Points.Count > 2 && IsOverGeometry(currentPoint, _handles[0]);
                if (_canClose!=temp)
                {
                    _canClose = temp;
                    UpdateGeometries();
                }
                
               
            }
            else
            {

                if (!IsMouseCaptured)
                {
            
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
                    UpdateGeometries(true);
                }
                else if (_isResizing)
                {
                    // 调整大小
                    ResizeAnnotation(currentPoint);
                    UpdateGeometries(true);
                }
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

            IsSelected = true;
        }

        private void ResizeAnnotation(Point currentPoint)
        {
            _markingFreeForm.Points[_resizeIndex] = currentPoint;

        }

      
        private void MoveAnnotation(Point currentPoint)
        {
            // 移动标注
            Vector delta = currentPoint - _dragStartPoint;

            for (var i = 0; i < _markingFreeForm.Points.Count; i++)
            {
                _markingFreeForm.Points[i] =_startPoints[i]+ delta;
            }
        }

        #endregion


        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point point = hitTestParameters.HitPoint;

            if (_isDrawing)
            {
                return new PointHitTestResult(this, point);
            }
            // 检查是否在标注几何图形上
            if (_annotationGeometry != null && _annotationGeometry.FillContains(point))
                return new PointHitTestResult(this, point);

            for (int i = 0; i < _handles.Count; i++)
            {
                // 检查是否在手柄上
                if (IsOverGeometry(point, _handles[i]))
                    return new PointHitTestResult(this, point);
            }


            return null;
        }
    }
}

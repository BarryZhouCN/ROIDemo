using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFAdorner.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace WPFAdorner.Views.Adorners
{
    public class GeometryAnnotationAdorner : Adorner
    {
        protected UIElement _adornedElement;
        private bool _isSelected;
        private MarkingShape _markingShape;

        #region 几何图形定义

        // 标注几何图形
        protected Geometry _annotationGeometry;

        // 手柄几何图形
        protected Geometry _topLeftHandle;
        protected Geometry _topRightHandle;
        protected Geometry _bottomLeftHandle;
        protected Geometry _bottomRightHandle;
        protected Geometry _rotateHandle;

        // 连接线几何图形
        protected Geometry _rotateConnection;
        protected Geometry _isSelectedGeometry;

        protected TransformGroup _transformGroup;
        protected TranslateTransform _translateTransform;
        protected RotateTransform _rotateTransform;
        protected ScaleTransform _scaleTransform;

        #endregion

        #region 样式定义

        protected Pen _outlinePen;
        protected Brush _handleBrush;
        protected Brush _rotateHandleBrush;

        #endregion

        #region 状态变量

        protected Rect _annotationRect;
        protected string _annotationType;
        protected bool _isDragging;
        protected bool _isRotating;
        protected bool _isResizing;
        protected Point _startPoint;
        protected Point _transformOrigin;
        protected double _startAngle;
        protected Rect _initialRect;

        // 自由绘制相关
        protected bool _isDrawingFreeform;
        protected List<Point> _freeformPoints;

        // 手柄大小
        protected const double HandleSize = 8;
        protected const double RotateHandleOffset = 25;

        #endregion

        #region 公共属性

        public bool IsSelected
        {
            get => _markingShape.IsSelected;
            set
            {
                if (_markingShape.IsSelected != value)
                {
                    _markingShape.IsSelected = value;
                }
            }
        }

        #endregion


        public GeometryAnnotationAdorner(UIElement adornedElement, MarkingShape markingShape) : base(adornedElement)
        {
            _adornedElement = adornedElement;
            _markingShape = markingShape;
            _markingShape.IsSelectedChanged += IsMarkingShapeIsSelectedChanged;
            // 初始化样式
            _outlinePen = new Pen(Brushes.Red, 2);
            _handleBrush = Brushes.Red;
            _rotateHandleBrush = Brushes.Blue;
            // 启用鼠标交互
            IsHitTestVisible = true;
        }

        private void IsMarkingShapeIsSelectedChanged(object sender, EventArgs e)
        {
            UpdateGeometries();
        }

        protected  virtual void UpdateGeometries()
        {

        }

        #region 几何图形辅助方法

        protected bool IsOverResizeHandle(Point point)
        {
            return IsOverGeometry(point, _topLeftHandle) ||
                   IsOverGeometry(point, _topRightHandle) ||
                   IsOverGeometry(point, _bottomLeftHandle) ||
                   IsOverGeometry(point, _bottomRightHandle);
        }

        protected bool IsOverGeometry(Point point, Geometry geometry)
        {
            return geometry != null && geometry.FillContains(point);
        }

        // 合并多个几何图形用于命中测试
        private Geometry CombineGeometries(params Geometry[] geometries)
        {
            if (geometries == null || geometries.Length == 0)
                return null;

            GeometryGroup group = new GeometryGroup();
            foreach (var geometry in geometries)
            {
                if (geometry != null)
                    group.Children.Add(geometry);
            }

            return group;
        }

        // 检查点是否在任何几何图形上
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            Point point = hitTestParameters.HitPoint;

            // 检查是否在标注几何图形上
            if (_annotationGeometry != null && _annotationGeometry.FillContains(point))
                return new PointHitTestResult(this, point);

            // 检查是否在手柄上
            if (IsOverResizeHandle(point) || IsOverGeometry(point, _rotateHandle))
                return new PointHitTestResult(this, point);

            return null;
        }

        #endregion

        public void Dispose()
        {
            _markingShape.IsSelectedChanged -= IsMarkingShapeIsSelectedChanged;
        }
    }
}
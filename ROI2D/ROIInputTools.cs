using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Input;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI输入工具基类
    /// </summary>
    public abstract class ROIInputTool : IDisposable
    {
        protected readonly ROI2DOverlay _overlay;
        protected bool _isDrawing;
        protected Point2D _startPoint;
        protected Point2D _currentPoint;
        protected ROIShape _currentShape;
        
        public ROIInputTool(ROI2DOverlay overlay)
        {
            _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        }
        
        /// <summary>
        /// 渲染预览
        /// </summary>
        public virtual void RenderPreview(ROI2DRenderer renderer)
        {
            // 默认实现：如果有当前形状，渲染它
            if (_currentShape != null && _isDrawing)
            {
                renderer.RenderROIShape(_currentShape);
            }
        }
        
        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        public virtual void OnMouseDown(Point2D point, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _startPoint = point;
                _currentPoint = point;
                _isDrawing = true;
                StartDrawing(point);
            }
            else if (button == MouseButton.Right)
            {
                OnRightMouseDown(point);
            }
        }
        
        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        public virtual void OnMouseMove(Point2D point)
        {
            _currentPoint = point;
            if (_isDrawing)
            {
                UpdateDrawing(point);
            }
        }
        
        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        public virtual void OnMouseUp(Point2D point, MouseButton button)
        {
            if (button == MouseButton.Left && _isDrawing)
            {
                _currentPoint = point;
                FinishDrawing(point);
                _isDrawing = false;
            }
            else if (button == MouseButton.Right)
            {
                OnRightMouseUp(point);
            }
        }
        
        /// <summary>
        /// 处理右键按下事件
        /// </summary>
        protected virtual void OnRightMouseDown(Point2D point) { }
        
        /// <summary>
        /// 处理右键释放事件
        /// </summary>
        protected virtual void OnRightMouseUp(Point2D point) { }
        
        /// <summary>
        /// 开始绘制
        /// </summary>
        protected abstract void StartDrawing(Point2D point);
        
        /// <summary>
        /// 更新绘制
        /// </summary>
        protected abstract void UpdateDrawing(Point2D point);
        
        /// <summary>
        /// 完成绘制
        /// </summary>
        protected abstract void FinishDrawing(Point2D point);
        
        /// <summary>
        /// 取消当前操作
        /// </summary>
        public virtual void Cancel()
        {
            _isDrawing = false;
            _currentShape = null;
        }
        
        public virtual void Dispose()
        {
            _currentShape = null;
        }
    }
    
    /// <summary>
    /// 指针工具 - 用于选择和编辑
    /// </summary>
    public class PointerTool : ROIInputTool
    {
        private ROIShape _selectedShape;
        private bool _isDragging;
        private Point2D _dragStartPoint;
        
        public PointerTool(ROI2DOverlay overlay) : base(overlay) { }
        
        protected override void StartDrawing(Point2D point)
        {
            // 查找点击位置的形状
            _selectedShape = _overlay.FindShapeAt(point);
            if (_selectedShape != null)
            {
                _overlay.SelectShape(_selectedShape);
                _isDragging = true;
                _dragStartPoint = point;
            }
            else
            {
                _overlay.ClearSelection();
            }
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            if (_isDragging && _selectedShape != null)
            {
                var delta = new Point2D(point.X - _dragStartPoint.X, point.Y - _dragStartPoint.Y);
                _overlay.MoveShape(_selectedShape, delta);
                _dragStartPoint = point;
            }
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            _isDragging = false;
        }
    }
    
    /// <summary>
    /// 点绘制工具
    /// </summary>
    public class PointTool : ROIInputTool
    {
        public PointTool(ROI2DOverlay overlay) : base(overlay) { }
        
        protected override void StartDrawing(Point2D point)
        {
            _currentShape = new PointROI(point);
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            // 点不需要更新
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            if (_currentShape != null)
            {
                _overlay.AddShape(_currentShape);
                _currentShape = null;
            }
        }
    }
    
    /// <summary>
    /// 线绘制工具
    /// </summary>
    public class LineTool : ROIInputTool
    {
        public LineTool(ROI2DOverlay overlay) : base(overlay) { }
        
        protected override void StartDrawing(Point2D point)
        {
            _currentShape = new LineROI(new List<Point2D> { point, point });
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            if (_currentShape is LineROI line && line.Points.Count >= 2)
            {
                line.Points[1] = point;
            }
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            if (_currentShape is LineROI line && line.Points.Count >= 2)
            {
                line.Points[1] = point;
                _overlay.AddShape(_currentShape);
                _currentShape = null;
            }
        }
    }
    
    /// <summary>
    /// 矩形绘制工具
    /// </summary>
    public class RectangleTool : ROIInputTool
    {
        public RectangleTool(ROI2DOverlay overlay) : base(overlay) { }
        
        protected override void StartDrawing(Point2D point)
        {
            _currentShape = new RectangleROI(point, 0, 0);
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            if (_currentShape is RectangleROI rect)
            {
                var width = Math.Abs(point.X - _startPoint.X);
                var height = Math.Abs(point.Y - _startPoint.Y);
                var centerX = (_startPoint.X + point.X) / 2;
                var centerY = (_startPoint.Y + point.Y) / 2;
                
                rect.Center = new Point2D(centerX, centerY);
                rect.Width = width;
                rect.Height = height;
            }
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            if (_currentShape != null)
            {
                UpdateDrawing(point);
                _overlay.AddShape(_currentShape);
                _currentShape = null;
            }
        }
    }
    
    /// <summary>
    /// 圆形绘制工具
    /// </summary>
    public class CircleTool : ROIInputTool
    {
        public CircleTool(ROI2DOverlay overlay) : base(overlay) { }
        
        protected override void StartDrawing(Point2D point)
        {
            _currentShape = new CircleROI(point, 0);
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            if (_currentShape is CircleROI circle)
            {
                var radius = _startPoint.Distance(point);
                circle.Radius = radius;
            }
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            if (_currentShape != null)
            {
                UpdateDrawing(point);
                _overlay.AddShape(_currentShape);
                _currentShape = null;
            }
        }
    }
    
    /// <summary>
    /// 多边形绘制工具
    /// </summary>
    public class PolygonTool : ROIInputTool
    {
        private List<Point2D> _vertices = new List<Point2D>();
        private bool _isCompleted;
        
        public PolygonTool(ROI2DOverlay overlay) : base(overlay) { }
        
        public override void OnMouseDown(Point2D point, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (!_isDrawing)
                {
                    // 开始新的多边形
                    _vertices.Clear();
                    _vertices.Add(point);
                    _isDrawing = true;
                    _currentShape = new PolygonROI(_vertices);
                }
                else
                {
                    // 添加新顶点
                    _vertices.Add(point);
                    if (_currentShape is PolygonROI polygon)
                    {
                        polygon.Vertices = new List<Point2D>(_vertices);
                    }
                }
            }
            else if (button == MouseButton.Right && _isDrawing)
            {
                // 完成多边形绘制
                FinishDrawing(point);
            }
        }
        
        protected override void StartDrawing(Point2D point)
        {
            // 在OnMouseDown中处理
        }
        
        protected override void UpdateDrawing(Point2D point)
        {
            // 实时更新最后一个顶点位置
            if (_currentShape is PolygonROI polygon && _vertices.Count > 0)
            {
                var tempVertices = new List<Point2D>(_vertices) { point };
                polygon.Vertices = tempVertices;
            }
        }
        
        protected override void FinishDrawing(Point2D point)
        {
            if (_currentShape != null && _vertices.Count >= 3)
            {
                _overlay.AddShape(_currentShape);
            }
            
            _currentShape = null;
            _vertices.Clear();
            _isDrawing = false;
            _isCompleted = false;
        }
        
        public override void Cancel()
        {
            base.Cancel();
            _vertices.Clear();
            _isCompleted = false;
        }
    }
}
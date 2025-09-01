using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using OpenTK.Mathematics;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 2D ROI覆盖层，管理ROI绘制、编辑和交互
    /// </summary>
    public class ROI2DOverlay
    {
        private readonly CoordinateMapper _coordinateMapper;
        private readonly ROI2DRenderer _renderer;
        private readonly HistoryManager _historyManager;
        private readonly List<ROILayer> _layers;
        private ROILayer _currentLayer;
        private ROIInputTool _currentTool;
        private ROIInputMode _inputMode;
        private ROISelectionMode _selectionMode;
        private readonly List<ROIShape> _selectedShapes;
        
        // 鼠标状态
        private SysVector2 _lastMousePos;
        private SysVector2 _startMousePos;
        private SysVector2 _currentMousePos;
        private bool _isDragging;
        private bool _isResizing;
        
        // 3D平面参数
        private Vector3 _planePoint;
        private Vector3 _planeNormal;
        private bool _planeEnabled;
        
        // 事件
        public event EventHandler<ROIChangedEventArgs> ROIChanged;
        public event EventHandler<ROISelectedEventArgs> ROISelected;
        public event EventHandler<PointGeneratedEventArgs> PointsGenerated;
        public event EventHandler OverlayInvalidated;

        // 公共属性
        public ROILayer CurrentLayer
        {
            get => _currentLayer;
            set
            {
                _currentLayer = value;
                if (value != null && !_layers.Contains(value))
                {
                    _layers.Add(value);
                }
            }
        }
        
        public ROIInputMode InputMode
        {
            get => _inputMode;
            set
            {
                _inputMode = value;
                UpdateCurrentTool();
            }
        }
        
        public HistoryManager HistoryManager => _historyManager;
        
        public ROISelectionMode SelectionMode
        {
            get => _selectionMode;
            set => _selectionMode = value;
        }
        
        public Point2D LastMousePos
        {
            get => new Point2D(_lastMousePos.X, _lastMousePos.Y);
            set => _lastMousePos = new SysVector2((float)value.X, (float)value.Y);
        }
        
        public Point2D StartMousePos
        {
            get => new Point2D(_startMousePos.X, _startMousePos.Y);
            set => _startMousePos = new SysVector2((float)value.X, (float)value.Y);
        }
        
        public Point2D Delta
        {
            get
            {
                var delta = _currentMousePos - _lastMousePos;
                return new Point2D(delta.X, delta.Y);
            }
        }
        
        public bool IsDragging
        {
            get => _isDragging;
            set => _isDragging = value;
        }
        
        public bool IsResizing
        {
            get => _isResizing;
            set => _isResizing = value;
        }
        
        public double Kx { get; set; } = 1.0;
        public double Ky { get; set; } = 1.0;
        public string Unit { get; set; } = "mm";
        
        /// <summary>
        /// 是否启用覆盖层
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        public IReadOnlyList<ROIShape> SelectedShapes => _selectedShapes.AsReadOnly();
        public IReadOnlyList<ROILayer> Layers => _layers.AsReadOnly();
        
        public void Invalidate()
        {
            OverlayInvalidated?.Invoke(this, EventArgs.Empty);
        }

        public ROI2DOverlay(CoordinateMapper coordinateMapper, ROI2DRenderer renderer)
        {
            _coordinateMapper = coordinateMapper ?? throw new ArgumentNullException(nameof(coordinateMapper));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _historyManager = new HistoryManager();
            _layers = new List<ROILayer>();
            _currentLayer = new ROILayer("Default Layer");
            _layers.Add(_currentLayer);
            _selectedShapes = new List<ROIShape>();
            
            _inputMode = ROIInputMode.Pointer;
            _selectionMode = ROISelectionMode.Single;
            
            // 默认平面设置（XY平面，Z=0）
            _planePoint = Vector3.Zero;
            _planeNormal = Vector3.UnitZ;
            _planeEnabled = true;
            
            UpdateCurrentTool();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ROI2DOverlay()
        {
            _renderer = new ROI2DRenderer();
            _historyManager = new HistoryManager();
            _layers = new List<ROILayer>();
            _currentLayer = new ROILayer("Default Layer");
            _layers.Add(_currentLayer);
            _selectedShapes = new List<ROIShape>();
            
            _inputMode = ROIInputMode.Pointer;
            _selectionMode = ROISelectionMode.Single;
            
            // 默认平面设置（XY平面，Z=0）
            _planePoint = Vector3.Zero;
            _planeNormal = Vector3.UnitZ;
            _planeEnabled = true;
            
            UpdateCurrentTool();
        }

        /// <summary>
        /// 设置3D投影平面
        /// </summary>
        /// <param name="planePoint">平面上的一点</param>
        /// <param name="planeNormal">平面法向量</param>
        public void SetProjectionPlane(Vector3 planePoint, Vector3 planeNormal)
        {
            _planePoint = planePoint;
            _planeNormal = Vector3.Normalize(planeNormal);
            _planeEnabled = true;
        }

        /// <summary>
        /// 禁用3D投影，直接在屏幕空间操作
        /// </summary>
        public void DisableProjection()
        {
            _planeEnabled = false;
        }

        /// <summary>
        /// 更新当前输入工具
        /// </summary>
        private void UpdateCurrentTool()
        {
            _currentTool?.Dispose();
            
            _currentTool = _inputMode switch
            {
                ROIInputMode.Pointer => new PointerTool(this),
                ROIInputMode.Point => new PointTool(this),
                ROIInputMode.Line => new LineTool(this),
                ROIInputMode.Rectangle => new RectangleTool(this),
                ROIInputMode.Circle => new CircleTool(this),
                ROIInputMode.Polygon => new PolygonTool(this),
                ROIInputMode.Triangle => new PolygonTool(this), // 使用多边形工具创建三角形
                _ => new PointerTool(this)
            };
        }

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        public void OnMouseDown(SysVector2 screenPos, MouseButton button)
        {
            _startMousePos = screenPos;
            _lastMousePos = screenPos;
            _currentMousePos = screenPos;
            
            // 转换屏幕坐标
            var drawingPoint = ScreenToDrawingPoint(screenPos);
            if (drawingPoint.HasValue)
            {
                _currentTool?.OnMouseDown(drawingPoint.Value, button);
            }
        }

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        public void OnMouseMove(SysVector2 screenPos)
        {
            _lastMousePos = _currentMousePos;
            _currentMousePos = screenPos;
            
            var drawingPoint = ScreenToDrawingPoint(screenPos);
            if (drawingPoint.HasValue)
            {
                _currentTool?.OnMouseMove(drawingPoint.Value);
            }
        }

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        public void OnMouseUp(SysVector2 screenPos, MouseButton button)
        {
            _currentMousePos = screenPos;
            
            var drawingPoint = ScreenToDrawingPoint(screenPos);
            if (drawingPoint.HasValue)
            {
                _currentTool?.OnMouseUp(drawingPoint.Value, button);
            }
            
            _isDragging = false;
            _isResizing = false;
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        public void OnKeyDown(Avalonia.Input.Key key)
        {
            switch (key)
            {
                case Avalonia.Input.Key.Delete:
                    DeleteSelectedShapes();
                    break;
                case Avalonia.Input.Key.Escape:
                    ClearSelection();
                    _currentTool?.Cancel();
                    break;
                case Avalonia.Input.Key.Z when IsCtrlPressed():
                    _historyManager.Undo();
                    Invalidate();
                    break;
                case Avalonia.Input.Key.Y when IsCtrlPressed():
                    _historyManager.Redo();
                    Invalidate();
                    break;
                case Avalonia.Input.Key.A when IsCtrlPressed():
                    SelectAllShapes();
                    break;
            }
        }

        /// <summary>
        /// 屏幕坐标转换为绘图坐标
        /// </summary>
        private Point2D? ScreenToDrawingPoint(SysVector2 screenPos)
        {
            if (_planeEnabled)
            {
                // 投影到3D平面
                var screenVector2 = new Vector2(screenPos.X, screenPos.Y);
                var worldPos = _coordinateMapper.ScreenToPlane(screenVector2, _planePoint, _planeNormal);
                if (worldPos.HasValue)
                {
                    // 将3D坐标投影到2D绘图坐标
                    var worldVector3 = new SysVector3(worldPos.Value.X, worldPos.Value.Y, worldPos.Value.Z);
                    var projected = ProjectWorldToDrawing(worldVector3);
                    return new Point2D(projected.X, projected.Y);
                }
                return null;
            }
            else
            {
                // 直接使用屏幕坐标
                return new Point2D(screenPos.X, screenPos.Y);
            }
        }

        /// <summary>
        /// 将3D世界坐标投影到2D绘图坐标
        /// </summary>
        private SysVector2 ProjectWorldToDrawing(SysVector3 worldPos)
        {
            // 这里可以根据具体需求实现不同的投影方式
            // 简单实现：使用XY坐标
            return new SysVector2(worldPos.X, worldPos.Y);
        }

        /// <summary>
        /// 删除选中的图形
        /// </summary>
        public void DeleteSelectedShapes()
        {
            var selectedShapes = GetSelectedShapes();
            if (selectedShapes.Any())
            {
                foreach (var shape in selectedShapes)
                {
                    _currentLayer.RemoveShape(shape);
                }
                
                _selectedShapes.Clear();
                ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Deleted, selectedShapes));
                Invalidate();
            }
        }

        /// <summary>
        /// 设置输入模式
        /// </summary>
        public void SetInputMode(ROIInputMode mode)
        {
            if (_inputMode != mode)
            {
                _inputMode = mode;
                UpdateCurrentTool();
            }
        }
        
        /// <summary>
        /// 设置选择模式
        /// </summary>
        public void SetSelectionMode(ROISelectionMode mode)
        {
            _selectionMode = mode;
        }
        
        /// <summary>
        /// 获取当前输入模式
        /// </summary>
        public ROIInputMode GetInputMode() => _inputMode;
        
        /// <summary>
        /// 获取当前选择模式
        /// </summary>
        public ROISelectionMode GetSelectionMode() => _selectionMode;

        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            _selectedShapes.Clear();
            foreach (var layer in _layers)
            {
                foreach (var shape in layer.Shapes)
                {
                    shape.IsSelected = false;
                }
            }
            ROISelected?.Invoke(this, new ROISelectedEventArgs(new List<ROIShape>()));
            Invalidate();
        }

        /// <summary>
        /// 选择所有图形
        /// </summary>
        public void SelectAllShapes()
        {
            _selectedShapes.Clear();
            foreach (var shape in _currentLayer.Shapes)
            {
                shape.IsSelected = true;
                _selectedShapes.Add(shape);
            }
            ROISelected?.Invoke(this, new ROISelectedEventArgs(_selectedShapes.ToList()));
            Invalidate();
        }

        /// <summary>
        /// 获取选中的图形
        /// </summary>
        public List<ROIShape> GetSelectedShapes()
        {
            return _selectedShapes.ToList();
        }

        /// <summary>
        /// 检查Ctrl键是否按下
        /// </summary>
        private bool IsCtrlPressed()
        {
            // 这里需要根据具体的输入系统实现
            // 暂时返回false，实际使用时需要检查键盘状态
            return false;
        }

        /// <summary>
        /// 渲染ROI覆盖层
        /// </summary>
        public void Render()
        {
            _renderer.BeginRender();
            
            // 渲染所有图层
            foreach (var layer in _layers)
            {
                if (layer != null)
                {
                    RenderLayer(layer);
                }
            }
            
            // 渲染当前工具的预览
            _currentTool?.RenderPreview(_renderer);
            
            _renderer.EndRender();
        }

        /// <summary>
        /// 渲染所有ROI形状
        /// </summary>
        public void RenderShapes()
        {
            if (_renderer == null) return;
            
            foreach (var layer in _layers)
            {
                foreach (var shape in layer.Shapes)
                {
                    _renderer.RenderROIShape(shape);
                }
            }
        }

        /// <summary>
        /// 渲染单个图层
        /// </summary>
        private void RenderLayer(ROILayer layer)
        {
            foreach (var shape in layer.Shapes)
            {
                if (shape != null)
                {
                    RenderShape(shape);
                }
            }
        }

        /// <summary>
        /// 渲染单个图形
        /// </summary>
        private void RenderShape(ROIShape shape)
        {
            // 根据图形类型进行渲染
            switch (shape.ShapeType)
            {
                case ROIShapeType.Point:
                    var point = new Vector2((float)shape.Points[0].X, (float)shape.Points[0].Y);
                    _renderer.RenderPoint(point, shape.IsSelected);
                    break;
                case ROIShapeType.Line:
                    var start = new Vector2((float)shape.Points[0].X, (float)shape.Points[0].Y);
                    var end = new Vector2((float)shape.Points[1].X, (float)shape.Points[1].Y);
                    _renderer.RenderLine((start, end), shape.IsSelected);
                    break;
                case ROIShapeType.Rectangle:
                    var rectCenter = new Vector2((float)shape.Points[0].X, (float)shape.Points[0].Y);
                    var size = new Vector2((float)shape.Width, (float)shape.Height);
                    _renderer.RenderRectangle((rectCenter, size, (float)shape.Rotation), shape.IsSelected);
                    break;
                case ROIShapeType.Circle:
                    if (shape.Points.Count > 0)
                    {
                        var circleCenter = new Vector2((float)shape.Points[0].X, (float)shape.Points[0].Y);
                        _renderer.RenderCircle((circleCenter, (float)shape.Radius), shape.IsSelected);
                    }
                    break;
                case ROIShapeType.Polygon:
                    var points = shape.Points.Select(p => new Vector2((float)p.X, (float)p.Y)).ToArray();
                    _renderer.RenderPolygon(points, shape.IsSelected);
                    break;
            }
        }

        /// <summary>
        /// 添加ROI形状到当前图层
        /// </summary>
        public void AddShape(ROIShape shape)
        {
            _currentLayer.AddShape(shape);
            ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Added, new List<ROIShape> { shape }));
            Invalidate();
        }
        
        /// <summary>
        /// 选择形状
        /// </summary>
        public void SelectShape(ROIShape shape, bool addToSelection = false)
        {
            if (!addToSelection)
            {
                ClearSelection();
            }
            
            if (!_selectedShapes.Contains(shape))
            {
                shape.IsSelected = true;
                _selectedShapes.Add(shape);
                ROISelected?.Invoke(this, new ROISelectedEventArgs(_selectedShapes.ToList()));
                Invalidate();
            }
        }
        
        /// <summary>
        /// 取消选择形状
        /// </summary>
        public void DeselectShape(ROIShape shape)
        {
            if (_selectedShapes.Contains(shape))
            {
                shape.IsSelected = false;
                _selectedShapes.Remove(shape);
                ROISelected?.Invoke(this, new ROISelectedEventArgs(_selectedShapes.ToList()));
                Invalidate();
            }
        }
        
        /// <summary>
        /// 获取指定位置的形状
        /// </summary>
        public ROIShape GetShapeAt(Point2D point)
        {
            // 从后往前遍历（最后绘制的在最上层）
            for (int i = _currentLayer.Shapes.Count - 1; i >= 0; i--)
            {
                var shape = _currentLayer.Shapes[i];
                if (shape.Contains(point))
                {
                    return shape;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 查找指定位置的形状
        /// </summary>
        public ROIShape FindShapeAt(Point2D point)
        {
            // 从后往前查找（最后绘制的在最上层）
            for (int i = _currentLayer.Shapes.Count - 1; i >= 0; i--)
            {
                var shape = _currentLayer.Shapes[i];
                if (shape.Contains(point))
                {
                    return shape;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 移动形状
        /// </summary>
        public void MoveShape(ROIShape shape, Point2D delta)
        {
            if (shape == null) return;
            
            shape.Move(delta);
            OnROIChanged(new ROIChangedEventArgs(ROIChangeType.Modified, new List<ROIShape> { shape }));
            Invalidate();
        }
        
        /// <summary>
        /// 触发ROI变化事件
        /// </summary>
        protected virtual void OnROIChanged(ROIChangedEventArgs e)
        {
            ROIChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 设置渲染器的投影和视图矩阵
        /// </summary>
        public void SetRenderMatrices(OpenTK.Mathematics.Matrix4 projection, OpenTK.Mathematics.Matrix4 view)
        {
            _renderer?.SetMatrices(projection, view);
        }
        
        /// <summary>
        /// 更新视口信息
        /// </summary>
        /// <param name="width">视口宽度</param>
        /// <param name="height">视口高度</param>
        public void UpdateViewport(int width, int height)
        {
            // 更新视口尺寸
            // 这里可以根据需要更新相关的视口信息
        }
        
        #region 几何运算
        
        /// <summary>
        /// 对选中的形状执行几何运算
        /// </summary>
        public ROIShape PerformGeometryOperation(GeometryOperation operation)
        {
            if (_selectedShapes.Count < 2)
                throw new InvalidOperationException("几何运算需要至少选择两个形状");
            
            var shape1 = _selectedShapes[0];
            var shape2 = _selectedShapes[1];
            
            try
            {
                var result = GeometryOperations.PerformOperation(shape1, shape2, operation);
                
                if (result != null)
                {
                    // 添加结果形状到当前图层
                    AddShape(result);
                    
                    // 可选：移除原始形状
                    if (operation == GeometryOperation.Union || operation == GeometryOperation.XOR)
                    {
                        _currentLayer.RemoveShape(shape1);
                _currentLayer.RemoveShape(shape2);
                    }
                    
                    // 选中新形状
                    ClearSelection();
                    SelectShape(result);
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                // 记录错误或抛出自定义异常
                throw new InvalidOperationException($"几何运算失败: {ex.Message}", ex);
            }
            
            return null;
        }
        
        /// <summary>
        /// 执行并集运算
        /// </summary>
        public ROIShape Union()
        {
            return PerformGeometryOperation(GeometryOperation.Union);
        }
        
        /// <summary>
        /// 执行交集运算
        /// </summary>
        public ROIShape Intersection()
        {
            return PerformGeometryOperation(GeometryOperation.Intersection);
        }
        
        /// <summary>
        /// 执行差集运算
        /// </summary>
        public ROIShape Difference()
        {
            return PerformGeometryOperation(GeometryOperation.Difference);
        }
        
        /// <summary>
        /// 执行异或运算
        /// </summary>
        public ROIShape XOR()
        {
            return PerformGeometryOperation(GeometryOperation.XOR);
        }
        
        /// <summary>
        /// 裁剪形状到指定区域
        /// </summary>
        public List<ROIShape> ClipShapes(ROIShape clipRegion)
        {
            var clippedShapes = new List<ROIShape>();
            
            foreach (var shape in _currentLayer.Shapes.ToList())
            {
                try
                {
                    var clipped = GeometryOperations.PerformOperation(shape, clipRegion, GeometryOperation.Intersection);
                    if (clipped != null)
                    {
                        clippedShapes.Add(clipped);
                        
                        // 替换原形状
                        _currentLayer.RemoveShape(shape);
                        AddShape(clipped);
                    }
                    else
                    {
                        // 形状完全在裁剪区域外，移除
                        _currentLayer.RemoveShape(shape);
                    }
                }
                catch
                {
                    // 裁剪失败，保留原形状
                    continue;
                }
            }
            
            return clippedShapes;
        }
        
        #endregion
         
         #region 智能布点
         
         /// <summary>
         /// 在指定形状内生成点
         /// </summary>
         public List<PointROI> GeneratePointsInShape(ROIShape shape, PointGenerationParameters parameters)
         {
             if (shape == null)
                 throw new ArgumentNullException(nameof(shape));
             
             if (parameters == null)
                 throw new ArgumentNullException(nameof(parameters));
             
             try
             {
                 var points = PointGeneration.GeneratePoints(shape, parameters);
                 var pointROIs = PointGeneration.CreatePointROIs(points, 2.0);
                 
                 // 添加到当前图层
                 foreach (var pointROI in pointROIs)
                 {
                     AddShape(pointROI);
                 }
                 
                 return pointROIs;
             }
             catch (Exception ex)
             {
                 throw new InvalidOperationException($"布点生成失败: {ex.Message}", ex);
             }
         }
         
         /// <summary>
         /// 在选中形状内生成网格点
         /// </summary>
         public List<PointROI> GenerateGridPoints(double spacing = 10.0, double angleOffset = 0.0)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.Grid,
                 Spacing = spacing,
                 AngleOffset = angleOffset
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成随机密度点
         /// </summary>
         public List<PointROI> GenerateRandomPoints(double density = 0.1, int seed = 42)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.RandomDensity,
                 Density = density,
                 RandomSeed = seed
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成圆形网格点
         /// </summary>
         public List<PointROI> GenerateCircularGridPoints(double spacing = 10.0)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.CircularGrid,
                 Spacing = spacing
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成六边形网格点
         /// </summary>
         public List<PointROI> GenerateHexagonalGridPoints(double spacing = 10.0)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.HexagonalGrid,
                 Spacing = spacing
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成泊松分布点
         /// </summary>
         public List<PointROI> GeneratePoissonPoints(double minDistance = 5.0, int maxAttempts = 30, int seed = 42)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.Poisson,
                 MinDistance = minDistance,
                 MaxAttempts = maxAttempts,
                 RandomSeed = seed
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成螺旋线点
         /// </summary>
         public List<PointROI> GenerateSpiralPoints(double spacing = 5.0)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new PointGenerationParameters
             {
                 Mode = PointGenerationMode.Spiral,
                 Spacing = spacing
             };
             
             return GeneratePointsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 清除所有生成的点
         /// </summary>
         public void ClearGeneratedPoints()
         {
             var pointsToRemove = _currentLayer.Shapes.OfType<PointROI>().ToList();
             foreach (var point in pointsToRemove)
             {
                 _currentLayer.RemoveShape(point);
             }
         }
         
         #endregion
         
         #region 面片生成
         
         /// <summary>
         /// 在指定形状内生成三角面片
         /// </summary>
         public List<Triangle> GenerateTrianglesInShape(ROIShape shape, MeshGenerationParameters parameters)
         {
             if (shape == null)
                 throw new ArgumentNullException(nameof(shape));
             
             if (parameters == null)
                 throw new ArgumentNullException(nameof(parameters));
             
             try
             {
                 return MeshGeneration.GenerateTriangles(shape, parameters);
             }
             catch (Exception ex)
             {
                 throw new InvalidOperationException($"三角面片生成失败: {ex.Message}", ex);
             }
         }
         
         /// <summary>
         /// 在指定形状内生成四边形面片
         /// </summary>
         public List<Quadrilateral> GenerateQuadrilateralsInShape(ROIShape shape, MeshGenerationParameters parameters)
         {
             if (shape == null)
                 throw new ArgumentNullException(nameof(shape));
             
             if (parameters == null)
                 throw new ArgumentNullException(nameof(parameters));
             
             try
             {
                 return MeshGeneration.GenerateQuadrilaterals(shape, parameters);
             }
             catch (Exception ex)
             {
                 throw new InvalidOperationException($"四边形面片生成失败: {ex.Message}", ex);
             }
         }
         
         /// <summary>
         /// 在选中形状内生成规则三角网格
         /// </summary>
         public List<Triangle> GenerateTriangularMesh(double density = 0.1)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new MeshGenerationParameters
             {
                 Mode = MeshGenerationMode.Triangular,
                 Density = density
             };
             
             return GenerateTrianglesInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成四边形网格
         /// </summary>
         public List<Quadrilateral> GenerateQuadrilateralMesh(double density = 0.1)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new MeshGenerationParameters
             {
                 Mode = MeshGenerationMode.Quadrilateral,
                 Density = density
             };
             
             return GenerateQuadrilateralsInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成德劳内三角剖分
         /// </summary>
         public List<Triangle> GenerateDelaunayMesh(double density = 0.1, double qualityThreshold = 0.7)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new MeshGenerationParameters
             {
                 Mode = MeshGenerationMode.Delaunay,
                 Density = density,
                 QualityThreshold = qualityThreshold
             };
             
             return GenerateTrianglesInShape(shape, parameters);
         }
         
         /// <summary>
         /// 在选中形状内生成自适应三角网格
         /// </summary>
         public List<Triangle> GenerateAdaptiveMesh(double density = 0.1, double qualityThreshold = 0.8)
         {
             if (_selectedShapes.Count == 0)
                 throw new InvalidOperationException("请先选择一个形状");
             
             var shape = _selectedShapes[0];
             var parameters = new MeshGenerationParameters
             {
                 Mode = MeshGenerationMode.Adaptive,
                 Density = density,
                 QualityThreshold = qualityThreshold
             };
             
             return GenerateTrianglesInShape(shape, parameters);
         }
         
         /// <summary>
         /// 将三角面片转换为多边形ROI并添加到图层
         /// </summary>
         public void AddTrianglesToLayer(List<Triangle> triangles)
         {
             foreach (var triangle in triangles)
             {
                 var vertices = new List<Point2D> { triangle.A, triangle.B, triangle.C };
                 var polygonROI = new PolygonROI(vertices);
                 AddShape(polygonROI);
             }
         }
         
         /// <summary>
         /// 将四边形面片转换为多边形ROI并添加到图层
         /// </summary>
         public void AddQuadrilateralsToLayer(List<Quadrilateral> quadrilaterals)
         {
             foreach (var quad in quadrilaterals)
             {
                 var vertices = new List<Point2D> { quad.A, quad.B, quad.C, quad.D };
                 var polygonROI = new PolygonROI(vertices);
                 AddShape(polygonROI);
             }
         }
         
         #endregion
         
         /// <summary>
         /// 清空所有形状
         /// </summary>
         public void ClearShapes()
         {
             foreach (var layer in _layers)
             {
                 layer.Shapes.Clear();
             }
             _selectedShapes.Clear();
             ROIChanged?.Invoke(this, new ROIChangedEventArgs(ROIChangeType.Deleted, new List<ROIShape>()));
             Invalidate();
         }
         
         /// <summary>
         /// 释放资源
         /// </summary>
        public void Dispose()
        {
            _renderer?.Dispose();
            _selectedShapes.Clear();
            _layers.Clear();
            _currentTool?.Dispose();
        }
    }

    #region Event Args
    
    public enum ROIChangeType
    {
        Added,
        Modified,
        Deleted
    }
    
    public class ROIChangedEventArgs : EventArgs
    {
        public ROIChangeType ChangeType { get; }
        public List<ROIShape> Shapes { get; }
        
        public ROIChangedEventArgs(ROIChangeType changeType, List<ROIShape> shapes)
        {
            ChangeType = changeType;
            Shapes = shapes ?? new List<ROIShape>();
        }
    }
    
    public class ROISelectedEventArgs : EventArgs
    {
        public List<ROIShape> SelectedShapes { get; }
        
        public ROISelectedEventArgs(List<ROIShape> selectedShapes)
        {
            SelectedShapes = selectedShapes ?? new List<ROIShape>();
        }
    }
    
    public class PointGeneratedEventArgs : EventArgs
    {
        public List<SysVector2> Points { get; }
        public string GenerationMethod { get; }
        
        public PointGeneratedEventArgs(List<SysVector2> points, string method)
        {
            Points = points ?? new List<SysVector2>();
            GenerationMethod = method;
        }
    }
    
    #endregion
}
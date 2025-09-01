using System;
using System.Collections.Generic;
using Avalonia.Input;
using OpenTK.Mathematics;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.ROI2D;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI2D与OpenGL3DControl的集成接口
    /// 负责将2D ROI功能集成到3D控件中
    /// </summary>
    public class ROI2DIntegration
    {
        #region 私有字段
        private readonly OpenGL3DControl _control3D;
        private ROI2DOverlay? _roiOverlay;
        private CoordinateMapper _coordinateMapper;
        private bool _isEnabled;
        private bool _isInitialized;
        #endregion

        #region 事件
        /// <summary>
        /// ROI操作事件
        /// </summary>
        public event EventHandler<ROIMouseEventArgs>? ROIMouseDown;
        public event EventHandler<ROIMouseEventArgs>? ROIMouseMove;
        public event EventHandler<ROIMouseEventArgs>? ROIMouseUp;
        public event EventHandler<ROISelectionEventArgs>? ROISelectionChanged;
        public event EventHandler<ROICreatedEventArgs>? ROICreated;
        public event EventHandler<ROIModifiedEventArgs>? ROIModified;
        public event EventHandler<ROIDeletedEventArgs>? ROIDeleted;
        public event EventHandler? ROIChanged;
        public event EventHandler? SelectionChanged;
        #endregion

        #region 属性
        /// <summary>
        /// 当前输入模式
        /// </summary>
        public ROIInputMode CurrentInputMode { get; private set; } = ROIInputMode.Pointer;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化ROI2D集成
        /// </summary>
        /// <param name="control3D">3D控件实例</param>
        public ROI2DIntegration(OpenGL3DControl control3D)
        {
            _control3D = control3D ?? throw new ArgumentNullException(nameof(control3D));
            _coordinateMapper = new CoordinateMapper();
            _isEnabled = false;
            _isInitialized = false;
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 是否启用ROI2D功能
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                    {
                        EnableROI2D();
                    }
                    else
                    {
                        DisableROI2D();
                    }
                }
            }
        }

        /// <summary>
        /// ROI覆盖层
        /// </summary>
        public ROI2DOverlay? ROIOverlay => _roiOverlay;

        /// <summary>
        /// 坐标映射器
        /// </summary>
        public CoordinateMapper CoordinateMapper => _coordinateMapper;
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化ROI2D系统
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                // 创建ROI渲染器和覆盖层
                var renderer = new ROI2DRenderer();
                _roiOverlay = new ROI2DOverlay(_coordinateMapper, renderer);
                
                // 订阅ROI事件
                SubscribeROIEvents();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ROI2D初始化失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            DisableROI2D();
            UnsubscribeROIEvents();
            _roiOverlay?.Dispose();
            _roiOverlay = null;
            _isInitialized = false;
        }
        #endregion

        #region ROI控制方法
        /// <summary>
        /// 启用ROI2D功能
        /// </summary>
        private void EnableROI2D()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_roiOverlay != null)
            {
                _roiOverlay.IsEnabled = true;
                
                // 订阅3D控件的鼠标事件
                Subscribe3DControlEvents();
                
                // 请求重新渲染
                _control3D.RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// 禁用ROI2D功能
        /// </summary>
        private void DisableROI2D()
        {
            if (_roiOverlay != null)
            {
                _roiOverlay.IsEnabled = false;
                
                // 取消订阅3D控件的鼠标事件
                Unsubscribe3DControlEvents();
                
                // 请求重新渲染
                _control3D.RequestNextFrameRendering();
            }
        }
        #endregion

        #region 坐标更新方法
        /// <summary>
        /// 更新坐标映射器
        /// 应在每帧渲染前调用
        /// </summary>
        public void UpdateCoordinateMapping()
        {
            if (!_isEnabled || !_isInitialized || _roiOverlay == null)
                return;

            try
            {
                var camera = _control3D.Scene.Camera;
                var bounds = _control3D.Bounds;
                
                // 计算视口参数
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(_control3D);
                var renderScaling = topLevel?.RenderScaling ?? 1.0;
                
                var pixelWidth = (float)(bounds.Width * renderScaling);
                var pixelHeight = (float)(bounds.Height * renderScaling);
                
                Vector4 viewport = new Vector4(0, 0, pixelWidth, pixelHeight);
                
                // 更新坐标映射器
                _coordinateMapper.UpdateFromCamera(camera, Matrix4.Identity, viewport);
                
                // 更新ROI覆盖层的视口信息
                _roiOverlay.UpdateViewport((int)pixelWidth, (int)pixelHeight);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，避免影响渲染
                System.Diagnostics.Debug.WriteLine($"更新坐标映射失败: {ex.Message}");
            }
        }
        #endregion

        #region 渲染方法
        /// <summary>
        /// 渲染ROI2D覆盖层
        /// 应在3D场景渲染完成后调用
        /// </summary>
        public void RenderROI2D()
        {
            if (!_isEnabled || !_isInitialized || _roiOverlay == null)
                return;

            try
            {
                // 更新坐标映射
                UpdateCoordinateMapping();
                
                // 渲染ROI覆盖层
                _roiOverlay.RenderShapes();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ROI2D渲染失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置渲染矩阵
        /// </summary>
        public void SetRenderMatrices(OpenTK.Mathematics.Matrix4 projection, OpenTK.Mathematics.Matrix4 view)
        {
            _roiOverlay?.SetRenderMatrices(projection, view);
        }
        #endregion
        
        #region 几何运算接口
        
        /// <summary>
        /// 执行并集运算
        /// </summary>
        public ROIShape Union()
        {
            return _roiOverlay?.Union();
        }
        
        /// <summary>
        /// 执行交集运算
        /// </summary>
        public ROIShape Intersection()
        {
            return _roiOverlay?.Intersection();
        }
        
        /// <summary>
        /// 执行差集运算
        /// </summary>
        public ROIShape Difference()
        {
            return _roiOverlay?.Difference();
        }
        
        /// <summary>
        /// 执行异或运算
        /// </summary>
        public ROIShape XOR()
        {
            return _roiOverlay?.XOR();
        }
        
        /// <summary>
        /// 裁剪形状到指定区域
        /// </summary>
        public List<ROIShape> ClipShapes(ROIShape clipRegion)
        {
            return _roiOverlay?.ClipShapes(clipRegion) ?? new List<ROIShape>();
        }
        
        /// <summary>
        /// 对选中的形状执行几何运算
        /// </summary>
        public ROIShape PerformGeometryOperation(GeometryOperation operation)
        {
            return _roiOverlay?.PerformGeometryOperation(operation);
        }
        
        #endregion
        
        #region 智能布点接口
        
        /// <summary>
        /// 在指定形状内生成点
        /// </summary>
        public List<PointROI> GeneratePointsInShape(ROIShape shape, PointGenerationParameters parameters)
        {
            return _roiOverlay.GeneratePointsInShape(shape, parameters);
        }
        
        /// <summary>
        /// 在选中形状内生成网格点
        /// </summary>
        public List<PointROI> GenerateGridPoints(double spacing = 10.0, double angleOffset = 0.0)
        {
            return _roiOverlay.GenerateGridPoints(spacing, angleOffset);
        }
        
        /// <summary>
        /// 在选中形状内生成随机密度点
        /// </summary>
        public List<PointROI> GenerateRandomPoints(double density = 0.1, int seed = 42)
        {
            return _roiOverlay.GenerateRandomPoints(density, seed);
        }
        
        /// <summary>
        /// 在选中形状内生成圆形网格点
        /// </summary>
        public List<PointROI> GenerateCircularGridPoints(double spacing = 10.0)
        {
            return _roiOverlay.GenerateCircularGridPoints(spacing);
        }
        
        /// <summary>
        /// 在选中形状内生成六边形网格点
        /// </summary>
        public List<PointROI> GenerateHexagonalGridPoints(double spacing = 10.0)
        {
            return _roiOverlay.GenerateHexagonalGridPoints(spacing);
        }
        
        /// <summary>
        /// 在选中形状内生成泊松分布点
        /// </summary>
        public List<PointROI> GeneratePoissonPoints(double minDistance = 5.0, int maxAttempts = 30, int seed = 42)
        {
            return _roiOverlay.GeneratePoissonPoints(minDistance, maxAttempts, seed);
        }
        
        /// <summary>
        /// 在选中形状内生成螺旋线点
        /// </summary>
        public List<PointROI> GenerateSpiralPoints(double spacing = 5.0)
        {
            return _roiOverlay.GenerateSpiralPoints(spacing);
        }
        
        /// <summary>
        /// 清除所有生成的点
        /// </summary>
        public void ClearGeneratedPoints()
        {
            _roiOverlay.ClearGeneratedPoints();
        }
        
        #endregion
        
        #region 面片生成接口
        
        /// <summary>
        /// 在指定形状内生成三角面片
        /// </summary>
        public List<Triangle> GenerateTrianglesInShape(ROIShape shape, MeshGenerationParameters parameters)
        {
            return _roiOverlay.GenerateTrianglesInShape(shape, parameters);
        }
        
        /// <summary>
        /// 在指定形状内生成四边形面片
        /// </summary>
        public List<Quadrilateral> GenerateQuadrilateralsInShape(ROIShape shape, MeshGenerationParameters parameters)
        {
            return _roiOverlay.GenerateQuadrilateralsInShape(shape, parameters);
        }
        
        /// <summary>
        /// 在选中形状内生成规则三角网格
        /// </summary>
        public List<Triangle> GenerateTriangularMesh(double density = 0.1)
        {
            return _roiOverlay.GenerateTriangularMesh(density);
        }
        
        /// <summary>
        /// 在选中形状内生成四边形网格
        /// </summary>
        public List<Quadrilateral> GenerateQuadrilateralMesh(double density = 0.1)
        {
            return _roiOverlay.GenerateQuadrilateralMesh(density);
        }
        
        /// <summary>
        /// 在选中形状内生成德劳内三角剖分
        /// </summary>
        public List<Triangle> GenerateDelaunayMesh(double density = 0.1, double qualityThreshold = 0.7)
        {
            return _roiOverlay.GenerateDelaunayMesh(density, qualityThreshold);
        }
        
        /// <summary>
        /// 在选中形状内生成自适应三角网格
        /// </summary>
        public List<Triangle> GenerateAdaptiveMesh(double density = 0.1, double qualityThreshold = 0.8)
        {
            return _roiOverlay.GenerateAdaptiveMesh(density, qualityThreshold);
        }
        
        /// <summary>
        /// 将三角面片转换为多边形ROI并添加到图层
        /// </summary>
        public void AddTrianglesToLayer(List<Triangle> triangles)
        {
            _roiOverlay.AddTrianglesToLayer(triangles);
        }
        
        /// <summary>
        /// 将四边形面片转换为多边形ROI并添加到图层
        /// </summary>
        public void AddQuadrilateralsToLayer(List<Quadrilateral> quadrilaterals)
        {
            _roiOverlay.AddQuadrilateralsToLayer(quadrilaterals);
        }
        
        #endregion
        
        #region 事件处理
        /// <summary>
        /// 订阅ROI事件
        /// </summary>
        private void SubscribeROIEvents()
        {
            if (_roiOverlay == null) return;

            _roiOverlay.ROIChanged += OnROIChanged;
            _roiOverlay.ROISelected += OnROISelected;
        }

        /// <summary>
        /// 取消订阅ROI事件
        /// </summary>
        private void UnsubscribeROIEvents()
        {
            if (_roiOverlay == null) return;

            _roiOverlay.ROIChanged -= OnROIChanged;
            _roiOverlay.ROISelected -= OnROISelected;
        }

        /// <summary>
        /// 订阅3D控件事件
        /// </summary>
        private void Subscribe3DControlEvents()
        {
            // 注意：这里需要拦截鼠标事件，但不能直接订阅
            // 需要在3D控件的事件处理中调用ROI的处理方法
        }

        /// <summary>
        /// 取消订阅3D控件事件
        /// </summary>
        private void Unsubscribe3DControlEvents()
        {
            // 对应的取消订阅逻辑
        }

        // ROI事件转发方法
        private void OnROIChanged(object? sender, ROIChangedEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ROIChangeType.Added:
                    if (e.Shapes.Count > 0)
                    {
                        var args = new ROICreatedEventArgs
                        {
                            CreatedROI = e.Shapes[0],
                            ROIType = GetROIType(e.Shapes[0])
                        };
                        ROICreated?.Invoke(this, args);
                    }
                    break;
                case ROIChangeType.Modified:
                    if (e.Shapes.Count > 0)
                    {
                        var args = new ROIModifiedEventArgs
                        {
                            ModifiedROI = e.Shapes[0],
                            OriginalROI = e.Shapes[0], // 简化处理
                            ModificationType = ModificationType.Property
                        };
                        ROIModified?.Invoke(this, args);
                    }
                    break;
                case ROIChangeType.Deleted:
                    var deleteArgs = new ROIDeletedEventArgs
                    {
                        DeletedROIs = e.Shapes,
                        Reason = DeleteReason.UserAction
                    };
                    ROIDeleted?.Invoke(this, deleteArgs);
                    break;
            }
        }
        
        private void OnROISelected(object? sender, ROISelectedEventArgs e)
        {
            var args = new ROISelectionEventArgs
            {
                SelectedROIs = e.SelectedShapes,
                SelectionType = SelectionType.Multiple
            };
            ROISelectionChanged?.Invoke(this, args);
        }
        
        private ROIType GetROIType(ROIShape shape)
        {
            return shape.ShapeType switch
            {
                ROIShapeType.Point => ROIType.Point,
                ROIShapeType.Line => ROIType.Line,
                ROIShapeType.Rectangle => ROIType.Rectangle,
                ROIShapeType.Circle => ROIType.Circle,
                ROIShapeType.Polygon => ROIType.Polygon,
                _ => ROIType.Custom
            };
        }
        #endregion

        #region 公共接口方法
        /// <summary>
        /// 处理鼠标按下事件
        /// 由3D控件调用
        /// </summary>
        public bool HandleMouseDown(PointerPressedEventArgs e)
        {
            if (!_isEnabled || _roiOverlay == null)
                return false;

            var position = e.GetPosition(_control3D);
            var screenPos = new System.Numerics.Vector2((float)position.X, (float)position.Y);
            var button = e.GetCurrentPoint(_control3D).Properties.IsLeftButtonPressed ? MouseButton.Left : MouseButton.Right;
            
            _roiOverlay.OnMouseDown(screenPos, button);
            return true; // ROI处理了事件
        }

        /// <summary>
        /// 处理鼠标移动事件
        /// 由3D控件调用
        /// </summary>
        public bool HandleMouseMove(PointerEventArgs e)
        {
            if (!_isEnabled || _roiOverlay == null)
                return false;

            var position = e.GetPosition(_control3D);
            var screenPos = new System.Numerics.Vector2((float)position.X, (float)position.Y);
            
            _roiOverlay.OnMouseMove(screenPos);
            return true; // ROI处理了事件
        }

        /// <summary>
        /// 处理鼠标抬起事件
        /// 由3D控件调用
        /// </summary>
        public bool HandleMouseUp(PointerReleasedEventArgs e)
        {
            if (!_isEnabled || _roiOverlay == null)
                return false;

            var position = e.GetPosition(_control3D);
            var screenPos = new System.Numerics.Vector2((float)position.X, (float)position.Y);
            var button = e.GetCurrentPoint(_control3D).Properties.IsLeftButtonPressed ? MouseButton.Left : MouseButton.Right;
            
            _roiOverlay.OnMouseUp(screenPos, button);
            return true; // ROI处理了事件
        }

        /// <summary>
        /// 处理键盘事件
        /// 由3D控件调用
        /// </summary>
        public bool HandleKeyDown(KeyEventArgs e)
        {
            if (!_isEnabled || _roiOverlay == null)
                return false;

            _roiOverlay.OnKeyDown(e.Key);
            return true; // ROI处理了事件
        }
        
        /// <summary>
        /// 获取当前选中的形状
        /// </summary>
        public List<ROIShape>? GetSelectedShapes()
        {
            return _roiOverlay?.GetSelectedShapes();
        }
        
        /// <summary>
        /// 删除选中的形状
        /// </summary>
        public void DeleteSelected()
        {
            _roiOverlay?.DeleteSelectedShapes();
        }
        
        /// <summary>
        /// 清除选择
        /// </summary>
        public void ClearSelection()
        {
            _roiOverlay?.ClearSelection();
        }
        
        /// <summary>
        /// 全选所有形状
        /// </summary>
        public void SelectAll()
        {
            _roiOverlay?.SelectAllShapes();
        }
        
        /// <summary>
        /// 清空当前图层
        /// </summary>
        public void ClearCurrentLayer()
        {
            _roiOverlay?.ClearShapes();
        }
        
        /// <summary>
        /// 设置输入模式
        /// </summary>
        /// <param name="mode">输入模式</param>
        public void SetInputMode(ROIInputMode mode)
        {
            CurrentInputMode = mode;
            _roiOverlay?.SetInputMode(mode);
        }
        
        /// <summary>
        /// 添加新图层
        /// </summary>
        /// <param name="layerName">图层名称</param>
        public void AddLayer(string layerName)
        {
            // 目前简单实现，可以扩展为多图层管理
            // 这里可以添加图层管理逻辑
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Platform.Storage;

namespace Avalonia3DControl.ROI2D
{
    public partial class ROIDemoWindow : Window
    {
        private ROI2DIntegration? _roiIntegration;
        private OpenGL3DControl? _viewport3D;
        
        // UI控件引用
        private Button? _pointerToolButton;
        private Button? _pointToolButton;
        private Button? _lineToolButton;
        private Button? _rectangleToolButton;
        private Button? _circleToolButton;
        private Button? _polygonToolButton;
        
        private NumericUpDown? _gridSpacingInput;
        private NumericUpDown? _randomDensityInput;
        private NumericUpDown? _meshDensityInput;
        
        private Button? _generateGridButton;
        private Button? _generateRandomButton;
        private Button? _generateCircularButton;
        private Button? _generateHexagonalButton;
        private Button? _generatePoissonButton;
        private Button? _generateSpiralButton;
        private Button? _clearPointsButton;
        
        private Button? _generateTriangularMeshButton;
        private Button? _generateQuadMeshButton;
        private Button? _generateDelaunayButton;
        private Button? _generateAdaptiveButton;
        
        private Button? _unionButton;
        private Button? _intersectionButton;
        private Button? _differenceButton;
        private Button? _xorButton;
        
        private Button? _addLayerButton;
        private Button? _removeLayerButton;
        private Button? _clearLayerButton;
        private ListBox? _layerListBox;
        
        private Button? _selectAllButton;
        private Button? _clearSelectionButton;
        private Button? _deleteSelectedButton;
        private TextBlock? _selectionInfoText;
        
        private Button? _zoomFitButton;
        private Button? _zoomInButton;
        private Button? _zoomOutButton;
        private Button? _resetViewButton;
        
        private Button? _saveROIButton;
        private Button? _loadROIButton;
        private Button? _exportImageButton;
        
        private Button? _loadBackgroundButton;
        private Button? _clearBackgroundButton;
        private TextBlock? _backgroundInfoText;
        
        private ContentControl? _viewportContainer;
        private TextBlock? _statusText;
        private TextBlock? _coordinateText;
        private TextBlock? _zoomText;
        
        public ROIDemoWindow()
        {
            InitializeComponent();
            InitializeControls();
            InitializeROI();
            SetupEventHandlers();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void InitializeControls()
        {
            // 获取UI控件引用
            _pointerToolButton = this.FindControl<Button>("PointerToolButton");
            _pointToolButton = this.FindControl<Button>("PointToolButton");
            _lineToolButton = this.FindControl<Button>("LineToolButton");
            _rectangleToolButton = this.FindControl<Button>("RectangleToolButton");
            _circleToolButton = this.FindControl<Button>("CircleToolButton");
            _polygonToolButton = this.FindControl<Button>("PolygonToolButton");
            
            _gridSpacingInput = this.FindControl<NumericUpDown>("GridSpacingInput");
            _randomDensityInput = this.FindControl<NumericUpDown>("RandomDensityInput");
            _meshDensityInput = this.FindControl<NumericUpDown>("MeshDensityInput");
            
            _generateGridButton = this.FindControl<Button>("GenerateGridButton");
            _generateRandomButton = this.FindControl<Button>("GenerateRandomButton");
            _generateCircularButton = this.FindControl<Button>("GenerateCircularButton");
            _generateHexagonalButton = this.FindControl<Button>("GenerateHexagonalButton");
            _generatePoissonButton = this.FindControl<Button>("GeneratePoissonButton");
            _generateSpiralButton = this.FindControl<Button>("GenerateSpiralButton");
            _clearPointsButton = this.FindControl<Button>("ClearPointsButton");
            
            _generateTriangularMeshButton = this.FindControl<Button>("GenerateTriangularMeshButton");
            _generateQuadMeshButton = this.FindControl<Button>("GenerateQuadMeshButton");
            _generateDelaunayButton = this.FindControl<Button>("GenerateDelaunayButton");
            _generateAdaptiveButton = this.FindControl<Button>("GenerateAdaptiveButton");
            
            _unionButton = this.FindControl<Button>("UnionButton");
            _intersectionButton = this.FindControl<Button>("IntersectionButton");
            _differenceButton = this.FindControl<Button>("DifferenceButton");
            _xorButton = this.FindControl<Button>("XorButton");
            
            _addLayerButton = this.FindControl<Button>("AddLayerButton");
            _removeLayerButton = this.FindControl<Button>("RemoveLayerButton");
            _clearLayerButton = this.FindControl<Button>("ClearLayerButton");
            _layerListBox = this.FindControl<ListBox>("LayerListBox");
            
            _selectAllButton = this.FindControl<Button>("SelectAllButton");
            _clearSelectionButton = this.FindControl<Button>("ClearSelectionButton");
            _deleteSelectedButton = this.FindControl<Button>("DeleteSelectedButton");
            _selectionInfoText = this.FindControl<TextBlock>("SelectionInfoText");
            
            _zoomFitButton = this.FindControl<Button>("ZoomFitButton");
            _zoomInButton = this.FindControl<Button>("ZoomInButton");
            _zoomOutButton = this.FindControl<Button>("ZoomOutButton");
            _resetViewButton = this.FindControl<Button>("ResetViewButton");
            
            _saveROIButton = this.FindControl<Button>("SaveROIButton");
            _loadROIButton = this.FindControl<Button>("LoadROIButton");
            _exportImageButton = this.FindControl<Button>("ExportImageButton");
            
            _loadBackgroundButton = this.FindControl<Button>("LoadBackgroundButton");
            _clearBackgroundButton = this.FindControl<Button>("ClearBackgroundButton");
            _backgroundInfoText = this.FindControl<TextBlock>("BackgroundInfoText");
            
            _viewportContainer = this.FindControl<ContentControl>("ViewportContainer");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _coordinateText = this.FindControl<TextBlock>("CoordinateText");
            _zoomText = this.FindControl<TextBlock>("ZoomText");
        }
        
        private void InitializeROI()
        {
            try
            {
                // 创建3D视口控件
                _viewport3D = new OpenGL3DControl();
                
                // 创建ROI集成组件
                _roiIntegration = new ROI2DIntegration(_viewport3D);
                
                // 将3D控件添加到容器中
                if (_viewportContainer != null)
                {
                    _viewportContainer.Content = _viewport3D;
                }
                
                // 订阅ROI事件
                _roiIntegration.ROIChanged += OnROIChanged;
                _roiIntegration.SelectionChanged += OnSelectionChanged;
                
                // 初始化背景管理器并加载默认背景
                InitializeBackground();
                
                UpdateStatus("ROI系统初始化完成");
            }
            catch (Exception ex)
            {
                UpdateStatus($"ROI系统初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化背景管理器
        /// </summary>
        private void InitializeBackground()
        {
            try
            {
                // 加载默认的测试背景图片
                if (_roiIntegration?.BackgroundManager != null)
                {
                    // 使用空路径会生成默认的渐变背景
                    _roiIntegration.BackgroundManager.LoadImage("");
                    UpdateStatus("背景图片加载完成");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"背景图片加载失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 加载自定义背景图片
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        public void LoadBackgroundImage(string imagePath)
        {
            try
            {
                if (_roiIntegration?.BackgroundManager != null)
                {
                    bool success = _roiIntegration.BackgroundManager.LoadImage(imagePath);
                    if (success)
                    {
                        UpdateStatus($"背景图片加载成功: {imagePath}");
                    }
                    else
                    {
                        UpdateStatus($"背景图片加载失败: {imagePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载背景图片时发生错误: {ex.Message}");
            }
        }
        
        private void SetupEventHandlers()
        {
            // 绘制工具事件
            if (_pointerToolButton != null) _pointerToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Pointer);
            if (_pointToolButton != null) _pointToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Point);
            if (_lineToolButton != null) _lineToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Line);
            if (_rectangleToolButton != null) _rectangleToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Rectangle);
            if (_circleToolButton != null) _circleToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Circle);
            if (_polygonToolButton != null) _polygonToolButton.Click += (s, e) => SetInputTool(ROIInputMode.Polygon);
            
            // 智能布点事件
            if (_generateGridButton != null) _generateGridButton.Click += OnGenerateGrid;
            if (_generateRandomButton != null) _generateRandomButton.Click += OnGenerateRandom;
            if (_generateCircularButton != null) _generateCircularButton.Click += OnGenerateCircular;
            if (_generateHexagonalButton != null) _generateHexagonalButton.Click += OnGenerateHexagonal;
            if (_generatePoissonButton != null) _generatePoissonButton.Click += OnGeneratePoisson;
            if (_generateSpiralButton != null) _generateSpiralButton.Click += OnGenerateSpiral;
            if (_clearPointsButton != null) _clearPointsButton.Click += OnClearPoints;
            
            // 面片生成事件
            if (_generateTriangularMeshButton != null) _generateTriangularMeshButton.Click += OnGenerateTriangularMesh;
            if (_generateQuadMeshButton != null) _generateQuadMeshButton.Click += OnGenerateQuadMesh;
            if (_generateDelaunayButton != null) _generateDelaunayButton.Click += OnGenerateDelaunay;
            if (_generateAdaptiveButton != null) _generateAdaptiveButton.Click += OnGenerateAdaptive;
            
            // 几何运算事件
            if (_unionButton != null) _unionButton.Click += OnUnion;
            if (_intersectionButton != null) _intersectionButton.Click += OnIntersection;
            if (_differenceButton != null) _differenceButton.Click += OnDifference;
            if (_xorButton != null) _xorButton.Click += OnXor;
            
            // 图层管理事件
            if (_addLayerButton != null) _addLayerButton.Click += OnAddLayer;
            if (_removeLayerButton != null) _removeLayerButton.Click += OnRemoveLayer;
            if (_clearLayerButton != null) _clearLayerButton.Click += OnClearLayer;
            
            // 选择操作事件
            if (_selectAllButton != null) _selectAllButton.Click += OnSelectAll;
            if (_clearSelectionButton != null) _clearSelectionButton.Click += OnClearSelection;
            if (_deleteSelectedButton != null) _deleteSelectedButton.Click += OnDeleteSelected;
            
            // 视图控制事件
            if (_zoomFitButton != null) _zoomFitButton.Click += OnZoomFit;
            if (_zoomInButton != null) _zoomInButton.Click += OnZoomIn;
            if (_zoomOutButton != null) _zoomOutButton.Click += OnZoomOut;
            if (_resetViewButton != null) _resetViewButton.Click += OnResetView;
            
            // 背景设置事件
            if (_loadBackgroundButton != null) _loadBackgroundButton.Click += OnLoadBackground;
            if (_clearBackgroundButton != null) _clearBackgroundButton.Click += OnClearBackground;
            
            // 导入导出事件
            if (_saveROIButton != null) _saveROIButton.Click += OnSaveROI;
            if (_loadROIButton != null) _loadROIButton.Click += OnLoadROI;
            if (_exportImageButton != null) _exportImageButton.Click += OnExportImage;
        }
        
        #region 绘制工具
        
        private void SetInputTool(ROIInputMode mode)
        {
            try
            {
                _roiIntegration?.SetInputMode(mode);
                UpdateStatus($"切换到{GetModeDisplayName(mode)}工具");
            }
            catch (Exception ex)
            {
                UpdateStatus($"切换工具失败: {ex.Message}");
            }
        }
        
        private string GetModeDisplayName(ROIInputMode mode)
        {
            return mode switch
            {
                ROIInputMode.Pointer => "指针",
                ROIInputMode.Point => "点",
                ROIInputMode.Line => "线",
                ROIInputMode.Rectangle => "矩形",
                ROIInputMode.Circle => "圆形",
                ROIInputMode.Polygon => "多边形",
                _ => "未知"
            };
        }
        
        #endregion
        
        #region 智能布点
        
        private void OnGenerateGrid(object? sender, RoutedEventArgs e)
        {
            try
            {
                var spacing = (double)(_gridSpacingInput?.Value ?? 10.0m);
                var points = _roiIntegration?.GenerateGridPoints(spacing);
                UpdateStatus($"生成了{points?.Count ?? 0}个网格点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成网格点失败: {ex.Message}");
            }
        }
        
        private void OnGenerateRandom(object? sender, RoutedEventArgs e)
        {
            try
            {
                var density = (double)(_randomDensityInput?.Value ?? 0.1m);
                var points = _roiIntegration?.GenerateRandomPoints(density);
                UpdateStatus($"生成了{points?.Count ?? 0}个随机点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成随机点失败: {ex.Message}");
            }
        }
        
        private void OnGenerateCircular(object? sender, RoutedEventArgs e)
        {
            try
            {
                var spacing = (double)(_gridSpacingInput?.Value ?? 10.0m);
                var points = _roiIntegration?.GenerateCircularGridPoints(spacing);
                UpdateStatus($"生成了{points?.Count ?? 0}个圆形网格点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成圆形网格点失败: {ex.Message}");
            }
        }
        
        private void OnGenerateHexagonal(object? sender, RoutedEventArgs e)
        {
            try
            {
                var spacing = (double)(_gridSpacingInput?.Value ?? 10.0m);
                var points = _roiIntegration?.GenerateHexagonalGridPoints(spacing);
                UpdateStatus($"生成了{points?.Count ?? 0}个六边形网格点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成六边形网格点失败: {ex.Message}");
            }
        }
        
        private void OnGeneratePoisson(object? sender, RoutedEventArgs e)
        {
            try
            {
                var points = _roiIntegration?.GeneratePoissonPoints();
                UpdateStatus($"生成了{points?.Count ?? 0}个泊松分布点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成泊松分布点失败: {ex.Message}");
            }
        }
        
        private void OnGenerateSpiral(object? sender, RoutedEventArgs e)
        {
            try
            {
                var points = _roiIntegration?.GenerateSpiralPoints();
                UpdateStatus($"生成了{points?.Count ?? 0}个螺旋线点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成螺旋线点失败: {ex.Message}");
            }
        }
        
        private void OnClearPoints(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.ClearGeneratedPoints();
                UpdateStatus("已清除所有生成的点");
            }
            catch (Exception ex)
            {
                UpdateStatus($"清除点失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 面片生成
        
        private void OnGenerateTriangularMesh(object? sender, RoutedEventArgs e)
        {
            try
            {
                var density = (double)(_meshDensityInput?.Value ?? 0.1m);
                var triangles = _roiIntegration?.GenerateTriangularMesh(density);
                _roiIntegration?.AddTrianglesToLayer(triangles ?? new List<Triangle>());
                UpdateStatus($"生成了{triangles?.Count ?? 0}个三角面片");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成三角面片失败: {ex.Message}");
            }
        }
        
        private void OnGenerateQuadMesh(object? sender, RoutedEventArgs e)
        {
            try
            {
                var density = (double)(_meshDensityInput?.Value ?? 0.1m);
                var quads = _roiIntegration?.GenerateQuadrilateralMesh(density);
                _roiIntegration?.AddQuadrilateralsToLayer(quads ?? new List<Quadrilateral>());
                UpdateStatus($"生成了{quads?.Count ?? 0}个四边形面片");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成四边形面片失败: {ex.Message}");
            }
        }
        
        private void OnGenerateDelaunay(object? sender, RoutedEventArgs e)
        {
            try
            {
                var density = (double)(_meshDensityInput?.Value ?? 0.1m);
                var triangles = _roiIntegration?.GenerateDelaunayMesh(density);
                _roiIntegration?.AddTrianglesToLayer(triangles ?? new List<Triangle>());
                UpdateStatus($"生成了{triangles?.Count ?? 0}个德劳内三角面片");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成德劳内三角面片失败: {ex.Message}");
            }
        }
        
        private void OnGenerateAdaptive(object? sender, RoutedEventArgs e)
        {
            try
            {
                var density = (double)(_meshDensityInput?.Value ?? 0.1m);
                var triangles = _roiIntegration?.GenerateAdaptiveMesh(density);
                _roiIntegration?.AddTrianglesToLayer(triangles ?? new List<Triangle>());
                UpdateStatus($"生成了{triangles?.Count ?? 0}个自适应三角面片");
            }
            catch (Exception ex)
            {
                UpdateStatus($"生成自适应三角面片失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 几何运算
        
        private void OnUnion(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.Union();
                UpdateStatus("执行并集运算");
            }
            catch (Exception ex)
            {
                UpdateStatus($"并集运算失败: {ex.Message}");
            }
        }
        
        private void OnIntersection(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.Intersection();
                UpdateStatus("执行交集运算");
            }
            catch (Exception ex)
            {
                UpdateStatus($"交集运算失败: {ex.Message}");
            }
        }
        
        private void OnDifference(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.Difference();
                UpdateStatus("执行差集运算");
            }
            catch (Exception ex)
            {
                UpdateStatus($"差集运算失败: {ex.Message}");
            }
        }
        
        private void OnXor(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.XOR();
                UpdateStatus("执行异或运算");
            }
            catch (Exception ex)
            {
                UpdateStatus($"异或运算失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 图层管理
        
        private void OnAddLayer(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.AddLayer($"图层 {DateTime.Now:HH:mm:ss}");
                UpdateLayerList();
                UpdateStatus("添加新图层");
            }
            catch (Exception ex)
            {
                UpdateStatus($"添加图层失败: {ex.Message}");
            }
        }
        
        private void OnRemoveLayer(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现移除选中图层的逻辑
                UpdateStatus("移除图层功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"移除图层失败: {ex.Message}");
            }
        }
        
        private void OnClearLayer(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.ClearCurrentLayer();
                UpdateStatus("清空当前图层");
            }
            catch (Exception ex)
            {
                UpdateStatus($"清空图层失败: {ex.Message}");
            }
        }
        
        private void UpdateLayerList()
        {
            // 更新图层列表显示
            if (_layerListBox != null && _roiIntegration != null)
            {
                // 这里需要实现获取图层列表的逻辑
            }
        }
        
        #endregion
        
        #region 选择操作
        
        private void OnSelectAll(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.SelectAll();
                UpdateStatus("全选所有形状");
            }
            catch (Exception ex)
            {
                UpdateStatus($"全选失败: {ex.Message}");
            }
        }
        
        private void OnClearSelection(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.ClearSelection();
                UpdateStatus("清除选择");
            }
            catch (Exception ex)
            {
                UpdateStatus($"清除选择失败: {ex.Message}");
            }
        }
        
        private void OnDeleteSelected(object? sender, RoutedEventArgs e)
        {
            try
            {
                _roiIntegration?.DeleteSelected();
                UpdateStatus("删除选中的形状");
            }
            catch (Exception ex)
            {
                UpdateStatus($"删除选中形状失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 视图控制
        
        private void OnZoomFit(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现适应窗口的逻辑
                UpdateStatus("适应窗口功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"适应窗口失败: {ex.Message}");
            }
        }
        
        private void OnZoomIn(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现放大的逻辑
                UpdateStatus("放大功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"放大失败: {ex.Message}");
            }
        }
        
        private void OnZoomOut(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现缩小的逻辑
                UpdateStatus("缩小功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"缩小失败: {ex.Message}");
            }
        }
        
        private void OnResetView(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现重置视图的逻辑
                UpdateStatus("重置视图功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"重置视图失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 导入导出
        
        private void OnSaveROI(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现保存ROI的逻辑
                UpdateStatus("保存ROI功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"保存ROI失败: {ex.Message}");
            }
        }
        
        private void OnLoadROI(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现加载ROI的逻辑
                UpdateStatus("加载ROI功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载ROI失败: {ex.Message}");
            }
        }
        
        private void OnExportImage(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 这里需要实现导出图像的逻辑
                UpdateStatus("导出图像功能待实现");
            }
            catch (Exception ex)
            {
                UpdateStatus($"导出图像失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 背景设置
        
        private async void OnLoadBackground(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 使用Avalonia的StorageProvider API
                var storageProvider = StorageProvider;
                if (storageProvider == null)
                {
                    UpdateStatus("无法访问文件系统");
                    return;
                }
                
                var fileTypes = new List<FilePickerFileType>
                {
                    new("图片文件")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" }
                    },
                    FilePickerFileTypes.All
                };
                
                var options = new FilePickerOpenOptions
                {
                    Title = "选择背景图片",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                };
                
                var result = await storageProvider.OpenFilePickerAsync(options);
                if (result != null && result.Count > 0)
                {
                    string imagePath = result[0].Path.LocalPath;
                    LoadBackgroundImage(imagePath);
                    
                    // 更新背景信息显示
                    if (_backgroundInfoText != null)
                    {
                        var fileName = System.IO.Path.GetFileName(imagePath);
                        _backgroundInfoText.Text = $"当前: {fileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"选择背景图片失败: {ex.Message}");
            }
        }
        
        private void OnClearBackground(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 清除背景，恢复默认渐变背景
                if (_roiIntegration?.BackgroundManager != null)
                {
                    _roiIntegration.BackgroundManager.LoadImage(""); // 空路径加载默认背景
                    
                    // 更新背景信息显示
                    if (_backgroundInfoText != null)
                    {
                        _backgroundInfoText.Text = "当前: 默认渐变背景";
                    }
                    
                    UpdateStatus("已恢复默认背景");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"清除背景失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 事件处理
        
        private void OnROIChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatus("ROI已更改");
            });
        }
        
        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateSelectionInfo();
            });
        }
        
        #endregion
        
        #region UI更新
        
        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.Text = message;
            }
        }
        
        private void UpdateSelectionInfo()
        {
            if (_selectionInfoText != null && _roiIntegration != null)
            {
                var selectedCount = _roiIntegration.GetSelectedShapes()?.Count ?? 0;
                _selectionInfoText.Text = selectedCount > 0 ? $"已选择 {selectedCount} 个形状" : "未选择";
            }
        }
        
        private void UpdateCoordinate(double x, double y)
        {
            if (_coordinateText != null)
            {
                _coordinateText.Text = $"坐标: ({x:F1}, {y:F1})";
            }
        }
        
        private void UpdateZoom(double zoom)
        {
            if (_zoomText != null)
            {
                _zoomText.Text = $"缩放: {zoom:P0}";
            }
        }
        
        #endregion
        
        protected override void OnClosed(EventArgs e)
        {
            // 清理资源
            _roiIntegration?.Dispose();
            
            base.OnClosed(e);
        }
    }
}
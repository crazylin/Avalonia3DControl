using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia3DControl.Core;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.Core.Lighting;
using Avalonia3DControl.Core.Input;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Rendering;
using Avalonia3DControl.Core.ErrorHandling;
using Avalonia3DControl.Rendering.OpenGL;
using Avalonia3DControl.Geometry.Factories;
using Avalonia3DControl.UI;
using Avalonia3DControl.Core.Animation;
using Avalonia3DControl.ROI2D;

namespace Avalonia3DControl
{
    /// <summary>
    /// OpenGL 3D控件，提供3D场景渲染功能
    /// 支持模型加载、相机控制、光照、材质、动画等完整的3D渲染管线
    /// </summary>
    /// <remarks>
    /// 主要功能包括：
    /// - 3D模型渲染和显示
    /// - 相机视角控制（旋转、缩放、平移）
    /// - 多种光照模式（方向光、点光源）
    /// - 材质系统（漫反射、镜面反射、纹理）
    /// - 模态动画播放
    /// - 坐标轴显示
    /// - 渐变色条显示
    /// </remarks>
    public class OpenGL3DControl : OpenGlControlBase, ICustomHitTest
    {
        #region 私有字段
        // 核心组件
        private OpenGLRenderer? _renderer;
        private CameraController? _cameraController;
        private InputHandler? _inputHandler;
        
        // ROI2D集成
        private ROI2DIntegration? _roi2DIntegration;
        
        // 渲染状态
        private ShadingMode _currentShadingMode = ShadingMode.Vertex;
        private RenderMode _currentRenderMode = RenderMode.Fill;
        private bool _isOpenGLInitialized = false;
        #endregion

        #region 公共属性
        /// <summary>
        /// 3D场景管理器
        /// </summary>
        public Scene3D Scene { get; private set; } = new Scene3D();
        
        /// <summary>
        /// ROI2D集成接口
        /// </summary>
        public ROI2DIntegration ROI2D => _roi2DIntegration ??= new ROI2DIntegration(this);
        #endregion
        
        #region 构造函数
        public OpenGL3DControl()
        {
            InitializeComponent();
            InitializeScene();
        }

        private void InitializeComponent()
        {
            Scene = new Scene3D();
            
            // 确保控件可以接收焦点和输入事件
            Focusable = true;
            IsHitTestVisible = true;
            
            // 设置控件布局属性，确保填充整个容器
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        }

        private void InitializeScene()
        {
            // 场景初始化为空，模型将按需动态加载
        }
        #endregion

        #region OpenGL初始化和清理
        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            
            // 初始化渲染器
            _renderer = new OpenGLRenderer();
            _renderer.Initialize(gl);
            
            // 初始化相机控制器
            _cameraController = new CameraController(Scene);
            
            // 初始化输入处理器
            _inputHandler = new InputHandler(_cameraController);
            _inputHandler.RenderRequested += () => RequestNextFrameRendering();
            _inputHandler.FocusRequested += () => Focus();
            
            _isOpenGLInitialized = true;
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            _roi2DIntegration?.Dispose();
            _inputHandler?.Dispose();
            _renderer?.Dispose();
            base.OnOpenGlDeinit(gl);
        }
        #endregion

        #region 渲染方法
        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (!_isOpenGLInitialized || _renderer == null)
                return;

            try
            {
                // 设置视口，考虑DPI缩放
                var bounds = Bounds;
                var topLevel = TopLevel.GetTopLevel(this);
                var renderScaling = topLevel?.RenderScaling ?? 1.0;
                
                var pixelWidth = Math.Max(1, (int)(bounds.Width * renderScaling));
                var pixelHeight = Math.Max(1, (int)(bounds.Height * renderScaling));
                
                // 检查视口参数是否有效
                if (pixelWidth <= 0 || pixelHeight <= 0)
                {
                    Debug.WriteLine($"无效的视口尺寸: {pixelWidth}x{pixelHeight}");
                    return;
                }
                
                GL.Viewport(0, 0, pixelWidth, pixelHeight);
                
                // 检查视口设置后的OpenGL错误
                ErrorCode error = GL.GetError();
                if (error != ErrorCode.NoError)
                {
                    Debug.WriteLine($"设置视口后的OpenGL错误: {error}");
                }

                // 更新相机参数
                UpdateCamera((float)bounds.Width / (float)bounds.Height);

                // 渲染场景（包含坐标轴）
                var coordinateAxes = Scene.ShowCoordinateAxes ? Scene.CoordinateAxes.AxesModel : null;
                _renderer?.RenderSceneWithAxes(Scene.Camera, Scene.Models, Scene.Lights, Scene.BackgroundColor, _currentShadingMode, _currentRenderMode, coordinateAxes, Scene.MiniAxes, renderScaling);
                
                // 渲染ROI2D覆盖层
                _roi2DIntegration?.RenderROI2D();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleRenderingException(ex, "OnOpenGlRender");
            }
        }



        private Matrix4 CreateProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45.0f),
                aspectRatio,
                0.1f,
                100.0f
            );
        }

        private void UpdateCamera(float aspectRatio)
        {
            if (_cameraController == null) return;
            
            // 使用相机控制器更新相机状态
            bool needsContinuousRendering = _cameraController.UpdateCamera(aspectRatio);
            
            // 如果需要继续渲染（平滑动画），请求下一帧
            if (needsContinuousRendering)
            {
                RequestNextFrameRendering();
            }
        }
        #endregion

        #region 鼠标事件处理
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            
            // 首先尝试ROI2D处理
            bool handledByROI = _roi2DIntegration?.HandleMouseDown(e) ?? false;
            
            // 如果ROI2D没有处理，则交给3D控件处理
            if (!handledByROI)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var renderScaling = topLevel?.RenderScaling ?? 1.0;
                _inputHandler?.HandlePointerPressed(e, renderScaling);
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            
            // 首先尝试ROI2D处理
            bool handledByROI = _roi2DIntegration?.HandleMouseMove(e) ?? false;
            
            // 如果ROI2D没有处理，则交给3D控件处理
            if (!handledByROI)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var renderScaling = topLevel?.RenderScaling ?? 1.0;
                _inputHandler?.HandlePointerMoved(e, renderScaling);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            
            // 首先尝试ROI2D处理
            bool handledByROI = _roi2DIntegration?.HandleMouseUp(e) ?? false;
            
            // 如果ROI2D没有处理，则交给3D控件处理
            if (!handledByROI)
            {
                _inputHandler?.HandlePointerReleased(e);
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            _inputHandler?.HandlePointerWheelChanged(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // 首先尝试ROI2D处理
            bool handledByROI = _roi2DIntegration?.HandleKeyDown(e) ?? false;
            
            // 如果ROI2D没有处理，可以添加其他键盘处理逻辑
            if (!handledByROI)
            {
                // 3D控件的键盘处理逻辑
            }
        }

        #endregion

        #region 公共方法
        public void SetShadingMode(ShadingMode mode)
        {
            _currentShadingMode = mode;
            RequestNextFrameRendering();
        }

        public void SetRenderMode(RenderMode mode)
        {
            _currentRenderMode = mode;
            RequestNextFrameRendering();
        }

        public void AddModel(Model3D model)
        {
            // 订阅模型的渲染请求事件
            model.RenderRequested += () => RequestNextFrameRendering();
            Scene.Models.Add(model);
            RequestNextFrameRendering();
        }

        public void RemoveModel(Model3D model)
        {
            Scene.Models.Remove(model);
            RequestNextFrameRendering();
        }

        public void ClearModels()
        {
            Scene.Models.Clear();
            RequestNextFrameRendering();
        }

        public void ResetCamera()
        {
            _cameraController?.Reset();
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 设置视图锁定模式
        /// </summary>
        /// <param name="lockMode">视图锁定模式</param>
        public void SetViewLock(ViewLockMode lockMode)
        {
            Scene.Camera.SetViewLock(lockMode);
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 获取当前视图锁定模式
        /// </summary>
        /// <returns>当前视图锁定模式</returns>
        public ViewLockMode GetViewLockMode()
        {
            return Scene.Camera.ViewLock;
        }

        public void SetCoordinateAxesVisible(bool show)
        {
            Scene.SetCoordinateAxesVisible(show);
            RequestNextFrameRendering();
        }

        public void SetCurrentModel(string? modelType)
        {
            // 清空现有模型
            ClearModels();
            
            // 如果指定了模型类型，创建新模型
            if (!string.IsNullOrEmpty(modelType))
            {
                var model = GeometryFactory.CreateModel(modelType);
                if (model != null)
                {
                    // 订阅模型的渲染请求事件
                    model.RenderRequested += () => RequestNextFrameRendering();
                    AddModel(model);
                }
            }
        }
        
        /// <summary>
        /// 切换到正交投影模式
        /// </summary>
        public void SwitchToOrthographic()
        {
            Scene.Camera.SwitchToOrthographic();
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 切换到透视投影模式
        /// </summary>
        public void SwitchToPerspective()
        {
            Scene.Camera.SwitchToPerspective();
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 获取当前投影模式
        /// </summary>
        /// <returns>当前投影模式</returns>
        public ProjectionMode GetProjectionMode()
        {
            return Scene.Camera.Mode;
        }
        
        /// <summary>
        /// 设置梯度条可见性
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        public void SetGradientBarVisible(bool isVisible)
        {
            _renderer?.SetGradientBarVisible(isVisible);
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置梯度条位置
        /// </summary>
        /// <param name="position">梯度条位置</param>
        public void SetGradientBarPosition(GradientBarPosition position)
        {
            _renderer?.SetGradientBarPosition(position);
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置梯度条颜色梯度类型
        /// </summary>
        public void SetGradientBarType(ColorGradientType gradientType)
        {
            _renderer?.SetGradientBarType(gradientType);
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置梯度条是否使用归一化刻度（-1~1），否则显示实际最小最大值
        /// </summary>
        public void SetGradientBarUseNormalizedScale(bool useNormalized)
        {
            _renderer?.SetGradientBarUseNormalizedScale(useNormalized);
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 设置是否显示梯度条刻度
        /// </summary>
        public void SetGradientBarShowTicks(bool show)
        {
            _renderer?.SetGradientBarShowTicks(show);
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 启用ROI2D功能
        /// </summary>
        public void EnableROI2D()
        {
            ROI2D.IsEnabled = true;
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 禁用ROI2D功能
        /// </summary>
        public void DisableROI2D()
        {
            ROI2D.IsEnabled = false;
            RequestNextFrameRendering();
        }

        /// <summary>
        /// 获取ROI2D是否启用
        /// </summary>
        public bool IsROI2DEnabled => ROI2D.IsEnabled;

        /// <summary>
        /// 切换到2D模式（正交投影 + 启用ROI2D）
        /// </summary>
        public void SwitchTo2DMode()
        {
            SwitchToOrthographic();
            EnableROI2D();
        }

        /// <summary>
        /// 切换到3D模式（透视投影 + 禁用ROI2D）
        /// </summary>
        public void SwitchTo3DMode()
        {
            SwitchToPerspective();
            DisableROI2D();
        }

        #endregion

        #region ICustomHitTest实现
        public bool HitTest(Point point)
        {
            // 检查点是否在控件边界内
            if (!Bounds.Contains(point))
                return false;

            // 检查控件是否可见和启用
            if (!IsVisible || !IsEnabled)
                return false;

            // 检查控件是否可以接收命中测试
            if (!IsHitTestVisible)
                return false;

            // 考虑DPI缩放的精确命中测试
            var topLevel = TopLevel.GetTopLevel(this);
            var renderScaling = topLevel?.RenderScaling ?? 1.0;

            // 转换为像素坐标进行更精确的测试
            var pixelPoint = new Point(
                point.X * renderScaling,
                point.Y * renderScaling
            );

            var pixelBounds = new Rect(
                0, 0,
                Bounds.Width * renderScaling,
                Bounds.Height * renderScaling
            );

            return pixelBounds.Contains(pixelPoint);
        }
        #endregion
    }
}
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
using Avalonia3DControl.Materials;
using Avalonia3DControl.Rendering.OpenGL;
using Avalonia3DControl.Geometry.Factories;

namespace Avalonia3DControl
{
    /// <summary>
    /// 基础的三维OpenGL控件，继承自OpenGlControlBase
    /// </summary>
    public class OpenGL3DControl : OpenGlControlBase, ICustomHitTest
    {
        #region 常量定义
        private const float ROTATION_SENSITIVITY = 0.01f;
        private const float TRANSLATION_SENSITIVITY = 0.005f;
        private const float ZOOM_SENSITIVITY = 0.1f;
        private const float ZOOM_SMOOTHING = 0.15f; // 缩放平滑系数
        private const float MIN_ZOOM = 0.2f;
        private const float MAX_ZOOM = 10.0f;
        private const float CAMERA_DISTANCE = 3.0f;
        private const float ROTATION_LIMIT_OFFSET = 0.1f;
        #endregion

        #region 私有字段
        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;
        private float _zoom = 1.0f;
        private float _targetZoom = 1.0f; // 目标缩放值，用于平滑缩放
        private float _orthographicSize = 5.0f; // 正交投影的视野大小
        private float _targetOrthographicSize = 5.0f; // 目标正交投影大小，用于平滑缩放
        private Vector2 _lastMousePosition;
        private bool _isMousePressed = false;
        private bool _isRightMousePressed = false;
        private Vector3 _cameraOffset = Vector3.Zero;

        // 渲染器
        private OpenGLRenderer? _renderer;
        private ShadingMode _currentShadingMode = ShadingMode.Vertex;
        private RenderMode _currentRenderMode = RenderMode.Fill;
        private bool _isOpenGLInitialized = false;
        #endregion

        #region 公共属性
        /// <summary>
        /// 3D场景管理器
        /// </summary>
        public Scene3D Scene { get; private set; } = new Scene3D();
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
            
            _renderer = new OpenGLRenderer();
            _renderer.Initialize(gl);
            _isOpenGLInitialized = true;
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
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
                var coordinateAxes = Scene.ShowCoordinateAxes ? Scene.CoordinateAxes : null;
                _renderer?.RenderSceneWithAxes(Scene.Camera, Scene.Models, Scene.Lights, Scene.BackgroundColor, _currentShadingMode, _currentRenderMode, coordinateAxes, Scene.MiniAxes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"渲染错误: {ex.Message}");
            }
        }

        private Matrix4 CreateViewMatrix()
        {
            var cameraPosition = new Vector3(
                (float)(Math.Sin(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE * _zoom),
                (float)(Math.Sin(_rotationX) * CAMERA_DISTANCE * _zoom),
                (float)(Math.Cos(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE * _zoom)
            ) + _cameraOffset;

            return Matrix4.LookAt(cameraPosition, _cameraOffset, Vector3.UnitY);
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
            bool needsContinuousRendering = false;
            
            if (Scene.Camera.Mode == ProjectionMode.Perspective)
            {
                // 透视投影模式：平滑插值到目标缩放值
                var zoomDifference = _targetZoom - _zoom;
                if (Math.Abs(zoomDifference) > 0.001f) // 避免无限小的变化
                {
                    _zoom += zoomDifference * ZOOM_SMOOTHING;
                    
                    // 如果非常接近目标值，直接设置为目标值
                    if (Math.Abs(zoomDifference) < 0.01f)
                    {
                        _zoom = _targetZoom;
                    }
                    else
                    {
                        needsContinuousRendering = true;
                    }
                }
                
                // 计算相机位置
                var cameraPosition = new Vector3(
                    (float)(Math.Sin(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE * _zoom),
                    (float)(Math.Sin(_rotationX) * CAMERA_DISTANCE * _zoom),
                    (float)(Math.Cos(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE * _zoom)
                ) + _cameraOffset;

                Scene.Camera.Position = cameraPosition;
            }
            else
            {
                // 正交投影模式：平滑插值到目标正交投影大小
                var orthographicDifference = _targetOrthographicSize - _orthographicSize;
                if (Math.Abs(orthographicDifference) > 0.001f)
                {
                    _orthographicSize += orthographicDifference * ZOOM_SMOOTHING;
                    
                    // 如果非常接近目标值，直接设置为目标值
                    if (Math.Abs(orthographicDifference) < 0.01f)
                    {
                        _orthographicSize = _targetOrthographicSize;
                    }
                    else
                    {
                        needsContinuousRendering = true;
                    }
                }
                
                // 正交投影模式下相机位置保持固定
                Scene.Camera.Position = new Vector3(0.0f, 0.0f, 5.0f);
                Scene.Camera.OrthographicSize = _orthographicSize;
            }

            // 更新Scene.Camera的通用参数
            Scene.Camera.Target = _cameraOffset;
            Scene.Camera.Up = Vector3.UnitY;
            Scene.Camera.AspectRatio = aspectRatio;
            Scene.Camera.FieldOfView = MathHelper.DegreesToRadians(45.0f);
            Scene.Camera.NearPlane = 0.1f;
            Scene.Camera.FarPlane = 100.0f;
            
            // 如果缩放值还在变化，继续请求渲染
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
            
            var position = e.GetPosition(this);
            var topLevel = TopLevel.GetTopLevel(this);
            var renderScaling = topLevel?.RenderScaling ?? 1.0;
            
            // 考虑DPI缩放的鼠标坐标
            _lastMousePosition = new Vector2(
                (float)(position.X * renderScaling), 
                (float)(position.Y * renderScaling)
            );
            
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isMousePressed = true;
                Focus();
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _isRightMousePressed = true;
                Focus();
            }
            
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            
            if (!_isMousePressed && !_isRightMousePressed)
                return;
            
            var position = e.GetPosition(this);
            var topLevel = TopLevel.GetTopLevel(this);
            var renderScaling = topLevel?.RenderScaling ?? 1.0;
            
            // 考虑DPI缩放的鼠标坐标
            var currentMousePosition = new Vector2(
                (float)(position.X * renderScaling), 
                (float)(position.Y * renderScaling)
            );
            var deltaPosition = currentMousePosition - _lastMousePosition;
            
            if (_isMousePressed)
            {
                // 左键拖拽：旋转
                _rotationY += deltaPosition.X * ROTATION_SENSITIVITY;
                _rotationX -= deltaPosition.Y * ROTATION_SENSITIVITY;
                
                // 限制X轴旋转角度
                _rotationX = Math.Max(-MathHelper.PiOver2 + ROTATION_LIMIT_OFFSET, 
                                    Math.Min(MathHelper.PiOver2 - ROTATION_LIMIT_OFFSET, _rotationX));
            }
            else if (_isRightMousePressed)
            {
                // 右键拖拽：平移
                var right = Vector3.Cross(Vector3.UnitY, GetCameraDirection()).Normalized();
                var up = Vector3.Cross(GetCameraDirection(), right).Normalized();
                
                // 修正平移方向：鼠标向右拖拽，场景向左移动（相机向右移动）
                _cameraOffset -= right * deltaPosition.X * TRANSLATION_SENSITIVITY * _zoom;
                _cameraOffset += up * deltaPosition.Y * TRANSLATION_SENSITIVITY * _zoom;
            }
            
            _lastMousePosition = currentMousePosition;
            RequestNextFrameRendering();
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            
            _isMousePressed = false;
            _isRightMousePressed = false;
            e.Handled = true;
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            
            var delta = (float)e.Delta.Y;
            
            if (Scene.Camera.Mode == ProjectionMode.Perspective)
            {
                // 透视投影模式：使用对数缩放获得更平滑的体验
                var logZoom = (float)Math.Log(_targetZoom);
                logZoom += delta * ZOOM_SENSITIVITY;
                
                // 转换回线性空间并应用限制
                _targetZoom = (float)Math.Exp(logZoom);
                _targetZoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _targetZoom));
            }
            else
            {
                // 正交投影模式：直接调整正交投影大小
                _targetOrthographicSize -= delta * ZOOM_SENSITIVITY * 2.0f; // 调整缩放敏感度
                _targetOrthographicSize = Math.Max(0.5f, Math.Min(20.0f, _targetOrthographicSize)); // 限制范围
            }
            
            RequestNextFrameRendering();
            e.Handled = true;
        }

        private Vector3 GetCameraDirection()
        {
            return new Vector3(
                (float)(Math.Sin(_rotationY) * Math.Cos(_rotationX)),
                (float)Math.Sin(_rotationX),
                (float)(Math.Cos(_rotationY) * Math.Cos(_rotationX))
            ).Normalized();
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
            _rotationX = 0.0f;
            _rotationY = 0.0f;
            _zoom = 1.0f;
            _targetZoom = 1.0f;
            _orthographicSize = 5.0f;
            _targetOrthographicSize = 5.0f;
            _cameraOffset = Vector3.Zero;
            RequestNextFrameRendering();
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
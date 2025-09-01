using OpenTK.Mathematics;
using System;
using Avalonia3DControl.Core.Cameras;

namespace Avalonia3DControl.Core.Cameras
{
    /// <summary>
    /// 相机控制器，处理相机的交互操作和状态更新
    /// </summary>
    /// <remarks>
    /// CameraController负责管理3D场景中的相机行为，包括：
    /// - 鼠标交互处理：拖拽旋转、滚轮缩放
    /// - 相机状态管理：位置、旋转角度、缩放级别
    /// - 视图矩阵计算：根据相机参数生成变换矩阵
    /// - 相机重置：恢复到默认视角
    /// 
    /// 相机采用轨道控制模式，围绕目标点进行旋转和缩放。
    /// 支持的操作：
    /// - 左键拖拽：旋转视角
    /// - 滚轮：缩放距离
    /// - 中键拖拽：平移视角（可选）
    /// </remarks>
    public class CameraController
    {
        #region 常量定义
        private const float ROTATION_SENSITIVITY = 0.01f;
        private const float TRANSLATION_SENSITIVITY = 0.005f;
        private const float ZOOM_SENSITIVITY = 0.3f;
        private const float ZOOM_SMOOTHING = 0.25f;
        private const float MIN_ZOOM = 0.2f;
        private const float MAX_ZOOM = 10.0f;
        private const float CAMERA_DISTANCE = 10.0f;
        private const float ROTATION_LIMIT_OFFSET = 0.1f;
        #endregion

        #region 私有字段
        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;
        private float _zoom = 1.0f;
        private float _targetZoom = 1.0f;
        private float _orthographicSize = 5.0f;
        private float _targetOrthographicSize = 5.0f;
        private Vector3 _cameraOffset = Vector3.Zero;
        private Scene3D _scene;
        #endregion

        #region 事件
        /// <summary>
        /// 相机状态改变时触发
        /// </summary>
        public event Action? CameraChanged;
        #endregion

        #region 构造函数
        public CameraController(Scene3D scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取当前缩放值
        /// </summary>
        public float Zoom => _zoom;

        /// <summary>
        /// 获取当前X轴旋转角度
        /// </summary>
        public float RotationX => _rotationX;

        /// <summary>
        /// 获取当前Y轴旋转角度
        /// </summary>
        public float RotationY => _rotationY;

        /// <summary>
        /// 获取相机偏移
        /// </summary>
        public Vector3 CameraOffset => _cameraOffset;
        #endregion

        #region 相机控制方法
        /// <summary>
        /// 处理旋转输入
        /// </summary>
        /// <param name="deltaX">X轴增量</param>
        /// <param name="deltaY">Y轴增量</param>
        public void HandleRotation(float deltaX, float deltaY)
        {
            if (_scene?.Camera?.ViewLock != ViewLockMode.None)
                return; // 视图锁定时不允许旋转

            _rotationY += deltaX * ROTATION_SENSITIVITY;
            _rotationX -= deltaY * ROTATION_SENSITIVITY;
            
            // 限制X轴旋转角度
            _rotationX = Math.Max(-MathHelper.PiOver2 + ROTATION_LIMIT_OFFSET, 
                                Math.Min(MathHelper.PiOver2 - ROTATION_LIMIT_OFFSET, _rotationX));
            
            OnCameraChanged();
        }

        /// <summary>
        /// 处理平移输入
        /// </summary>
        /// <param name="deltaX">X轴增量</param>
        /// <param name="deltaY">Y轴增量</param>
        public void HandleTranslation(float deltaX, float deltaY)
        {
            var right = Vector3.Cross(Vector3.UnitY, GetCameraDirection()).Normalized();
            var up = Vector3.Cross(GetCameraDirection(), right).Normalized();
            
            // 修正平移方向：鼠标向右拖拽，场景向左移动（相机向右移动）
            _cameraOffset -= right * deltaX * TRANSLATION_SENSITIVITY * _zoom;
            _cameraOffset += up * deltaY * TRANSLATION_SENSITIVITY * _zoom;
            
            OnCameraChanged();
        }

        /// <summary>
        /// 处理缩放输入
        /// </summary>
        /// <param name="delta">缩放增量</param>
        public void HandleZoom(float delta)
        {
            if (_scene?.Camera?.Mode == ProjectionMode.Perspective)
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
                _targetOrthographicSize -= delta * ZOOM_SENSITIVITY * 2.0f;
                _targetOrthographicSize = Math.Max(0.5f, Math.Min(20.0f, _targetOrthographicSize));
            }
            
            OnCameraChanged();
        }

        /// <summary>
        /// 重置相机到默认状态
        /// </summary>
        public void Reset()
        {
            _rotationX = 0.0f;
            _rotationY = 0.0f;
            _zoom = 1.0f;
            _targetZoom = 1.0f;
            _orthographicSize = 5.0f;
            _targetOrthographicSize = 5.0f;
            _cameraOffset = Vector3.Zero;
            
            OnCameraChanged();
        }

        /// <summary>
        /// 更新相机状态
        /// </summary>
        /// <param name="aspectRatio">宽高比</param>
        /// <returns>是否需要继续渲染（用于平滑动画）</returns>
        public bool UpdateCamera(float aspectRatio)
        {
            if (_scene?.Camera == null) return false;
            
            bool needsContinuousRendering = false;
            
            // 检查是否处于视图锁定模式
            if (_scene.Camera.ViewLock != ViewLockMode.None)
            {
                // 视图锁定模式：只处理缩放，不改变相机位置和方向
                needsContinuousRendering = UpdateZoomInLockedMode();
            }
            else
            {
                // 自由视角模式：原有的相机控制逻辑
                needsContinuousRendering = UpdateCameraInFreeMode();
            }

            // 更新通用相机参数
            _scene.Camera.AspectRatio = aspectRatio;
            _scene.Camera.FieldOfView = MathHelper.DegreesToRadians(45.0f);
            _scene.Camera.NearPlane = 0.1f;
            _scene.Camera.FarPlane = 100.0f;
            
            return needsContinuousRendering;
        }
        #endregion

        #region 私有方法
        private bool UpdateZoomInLockedMode()
        {
            bool needsContinuousRendering = false;
            
            if (_scene.Camera.Mode == ProjectionMode.Perspective)
            {
                // 透视投影模式：平滑插值到目标缩放值
                var zoomDifference = _targetZoom - _zoom;
                if (Math.Abs(zoomDifference) > 0.001f)
                {
                    _zoom += zoomDifference * ZOOM_SMOOTHING;
                    
                    if (Math.Abs(zoomDifference) < 0.01f)
                    {
                        _zoom = _targetZoom;
                    }
                    else
                    {
                        needsContinuousRendering = true;
                    }
                }
                
                // 在锁定视图中应用缩放到相机距离
                var basePosition = _scene.Camera.Position;
                var direction = Vector3.Normalize(basePosition - _scene.Camera.Target);
                _scene.Camera.Position = _scene.Camera.Target + direction * CAMERA_DISTANCE * _zoom;
            }
            else
            {
                // 正交投影模式：平滑插值到目标正交投影大小
                var orthographicDifference = _targetOrthographicSize - _orthographicSize;
                if (Math.Abs(orthographicDifference) > 0.001f)
                {
                    _orthographicSize += orthographicDifference * ZOOM_SMOOTHING;
                    
                    if (Math.Abs(orthographicDifference) < 0.01f)
                    {
                        _orthographicSize = _targetOrthographicSize;
                    }
                    else
                    {
                        needsContinuousRendering = true;
                    }
                }
                
                _scene.Camera.OrthographicSize = _orthographicSize;
            }
            
            return needsContinuousRendering;
        }

        private bool UpdateCameraInFreeMode()
        {
            bool needsContinuousRendering = false;
            
            if (_scene.Camera.Mode == ProjectionMode.Perspective)
            {
                // 透视投影模式：平滑插值到目标缩放值
                var zoomDifference = _targetZoom - _zoom;
                if (Math.Abs(zoomDifference) > 0.001f)
                {
                    _zoom += zoomDifference * ZOOM_SMOOTHING;
                    
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

                _scene.Camera.Position = cameraPosition;
            }
            else
            {
                // 正交投影模式：平滑插值到目标正交投影大小
                var orthographicDifference = _targetOrthographicSize - _orthographicSize;
                if (Math.Abs(orthographicDifference) > 0.001f)
                {
                    _orthographicSize += orthographicDifference * ZOOM_SMOOTHING;
                    
                    if (Math.Abs(orthographicDifference) < 0.01f)
                    {
                        _orthographicSize = _targetOrthographicSize;
                    }
                    else
                    {
                        needsContinuousRendering = true;
                    }
                }
                
                // 正交投影模式下也支持旋转，但距离固定
                var cameraPosition = new Vector3(
                    (float)(Math.Sin(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE),
                    (float)(Math.Sin(_rotationX) * CAMERA_DISTANCE),
                    (float)(Math.Cos(_rotationY) * Math.Cos(_rotationX) * CAMERA_DISTANCE)
                ) + _cameraOffset;
                
                _scene.Camera.Position = cameraPosition;
                _scene.Camera.OrthographicSize = _orthographicSize;
            }
            
            // 更新Scene.Camera的通用参数
            _scene.Camera.Target = _cameraOffset;
            _scene.Camera.Up = Vector3.UnitY;
            
            return needsContinuousRendering;
        }

        private Vector3 GetCameraDirection()
        {
            return new Vector3(
                (float)(Math.Sin(_rotationY) * Math.Cos(_rotationX)),
                (float)Math.Sin(_rotationX),
                (float)(Math.Cos(_rotationY) * Math.Cos(_rotationX))
            ).Normalized();
        }

        private void OnCameraChanged()
        {
            CameraChanged?.Invoke();
        }
        #endregion
    }
}
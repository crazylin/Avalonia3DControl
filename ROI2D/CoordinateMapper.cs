using System;
using OpenTK.Mathematics;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using Avalonia3DControl.Core.Cameras;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 坐标映射器，负责3D世界坐标与2D屏幕坐标的双向转换
    /// </summary>
    public class CoordinateMapper
    {
        private Matrix4 _projectionMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelMatrix;
        private Matrix4 _mvpMatrix;
        private Matrix4 _inverseMvpMatrix;
        private Vector4 _viewport;
        private bool _matricesValid;
        private Camera? _camera;

        /// <summary>
        /// 投影矩阵
        /// </summary>
        public Matrix4 ProjectionMatrix
        {
            get => _projectionMatrix;
            set
            {
                _projectionMatrix = value;
                _matricesValid = false;
            }
        }

        /// <summary>
        /// 视图矩阵
        /// </summary>
        public Matrix4 ViewMatrix
        {
            get => _viewMatrix;
            set
            {
                _viewMatrix = value;
                _matricesValid = false;
            }
        }

        /// <summary>
        /// 模型矩阵
        /// </summary>
        public Matrix4 ModelMatrix
        {
            get => _modelMatrix;
            set
            {
                _modelMatrix = value;
                _matricesValid = false;
            }
        }

        /// <summary>
        /// 视口参数 (x, y, width, height)
        /// </summary>
        public Vector4 Viewport
        {
            get => _viewport;
            set
            {
                _viewport = value;
                _matricesValid = false;
            }
        }

        public CoordinateMapper()
        {
            _projectionMatrix = Matrix4.Identity;
            _viewMatrix = Matrix4.Identity;
            _modelMatrix = Matrix4.Identity;
            _viewport = new Vector4(0, 0, 800, 600);
            _matricesValid = false;
        }

        /// <summary>
        /// 更新所有变换矩阵
        /// </summary>
        /// <param name="projection">投影矩阵</param>
        /// <param name="view">视图矩阵</param>
        /// <param name="model">模型矩阵</param>
        /// <param name="viewport">视口参数</param>
        public void UpdateMatrices(Matrix4 projection, Matrix4 view, Matrix4 model, Vector4 viewport)
        {
            _projectionMatrix = projection;
            _viewMatrix = view;
            _modelMatrix = model;
            _viewport = viewport;
            _matricesValid = false;
        }

        /// <summary>
        /// 从相机更新矩阵
        /// </summary>
        /// <param name="camera">相机对象</param>
        /// <param name="modelMatrix">模型矩阵</param>
        /// <param name="viewport">视口参数</param>
        public void UpdateFromCamera(Camera camera, Matrix4 modelMatrix, Vector4 viewport)
        {
            _camera = camera;
            _projectionMatrix = camera.GetProjectionMatrix();
            _viewMatrix = camera.GetViewMatrix();
            _modelMatrix = modelMatrix;
            _viewport = viewport;
            _matricesValid = false;
        }

        /// <summary>
        /// 验证并更新内部矩阵
        /// </summary>
        private void ValidateMatrices()
        {
            if (!_matricesValid)
            {
                _mvpMatrix = _modelMatrix * _viewMatrix * _projectionMatrix;
                
                // 计算逆矩阵用于反投影
                try
                {
                    _inverseMvpMatrix = Matrix4.Invert(_mvpMatrix);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("无法计算MVP矩阵的逆矩阵，可能矩阵不可逆");
                }
                
                _matricesValid = true;
            }
        }

        /// <summary>
        /// 将3D世界坐标转换为2D屏幕坐标
        /// </summary>
        /// <param name="worldPos">3D世界坐标</param>
        /// <returns>2D屏幕坐标，Z分量表示深度</returns>
        public Vector3 WorldToScreen(Vector3 worldPos)
        {
            ValidateMatrices();

            // 转换为齐次坐标
            Vector4 worldPosH = new Vector4(worldPos, 1.0f);
            
            // 应用MVP变换
            Vector4 clipPos = _mvpMatrix * worldPosH;
            
            // 透视除法
            if (Math.Abs(clipPos.W) < float.Epsilon)
            {
                throw new InvalidOperationException("透视除法中W分量接近零");
            }
            
            Vector3 ndcPos = new Vector3(clipPos.X / clipPos.W, clipPos.Y / clipPos.W, clipPos.Z / clipPos.W);
            
            // NDC到屏幕坐标转换
            Vector3 screenPos = new Vector3(
                _viewport.X + (_viewport.Z * (ndcPos.X + 1.0f) * 0.5f),
                _viewport.Y + (_viewport.W * (1.0f - ndcPos.Y) * 0.5f), // Y轴翻转
                (ndcPos.Z + 1.0f) * 0.5f // 深度值归一化到[0,1]
            );
            
            return screenPos;
        }

        /// <summary>
        /// 将2D屏幕坐标转换为3D世界坐标射线
        /// </summary>
        /// <param name="screenPos">2D屏幕坐标</param>
        /// <param name="rayOrigin">射线起点</param>
        /// <param name="rayDirection">射线方向</param>
        public void ScreenToWorldRay(Vector2 screenPos, out Vector3 rayOrigin, out Vector3 rayDirection)
        {
            ValidateMatrices();

            // 屏幕坐标转换为NDC坐标
            float ndcX = (2.0f * (screenPos.X - _viewport.X)) / _viewport.Z - 1.0f;
            float ndcY = 1.0f - (2.0f * (screenPos.Y - _viewport.Y)) / _viewport.W; // Y轴翻转
            
            // 处理正交投影和透视投影的不同情况
            if (_camera?.Mode == ProjectionMode.Orthographic)
            {
                // 正交投影：射线方向固定，起点在屏幕平面上
                Vector4 screenPoint = new Vector4(ndcX, ndcY, 0.0f, 1.0f);
                Vector4 worldPoint = _inverseMvpMatrix * screenPoint;
                
                if (Math.Abs(worldPoint.W) > float.Epsilon)
                {
                    worldPoint /= worldPoint.W;
                }
                
                // 正交投影的射线方向是相机的前向量
                Vector3 cameraForward = Vector3.Normalize(_camera.Target - _camera.Position);
                
                rayOrigin = worldPoint.Xyz;
                rayDirection = cameraForward;
            }
            else
            {
                // 透视投影：从相机位置发出射线
                Vector4 nearPointNDC = new Vector4(ndcX, ndcY, -1.0f, 1.0f);
                Vector4 farPointNDC = new Vector4(ndcX, ndcY, 1.0f, 1.0f);
                
                Vector4 nearPointWorld = _inverseMvpMatrix * nearPointNDC;
                Vector4 farPointWorld = _inverseMvpMatrix * farPointNDC;
                
                if (Math.Abs(nearPointWorld.W) < float.Epsilon || Math.Abs(farPointWorld.W) < float.Epsilon)
                {
                    throw new InvalidOperationException("反投影中W分量接近零");
                }
                
                Vector3 nearPoint = new Vector3(nearPointWorld.X / nearPointWorld.W, 
                                              nearPointWorld.Y / nearPointWorld.W, 
                                              nearPointWorld.Z / nearPointWorld.W);
                Vector3 farPoint = new Vector3(farPointWorld.X / farPointWorld.W, 
                                             farPointWorld.Y / farPointWorld.W, 
                                             farPointWorld.Z / farPointWorld.W);
                
                rayOrigin = nearPoint;
                rayDirection = Vector3.Normalize(farPoint - nearPoint);
            }
        }

        /// <summary>
        /// 将2D屏幕坐标投影到指定的3D平面上
        /// </summary>
        /// <param name="screenPos">2D屏幕坐标</param>
        /// <param name="planePoint">平面上的一点</param>
        /// <param name="planeNormal">平面法向量</param>
        /// <returns>投影到平面上的3D坐标</returns>
        public Vector3? ScreenToPlane(Vector2 screenPos, Vector3 planePoint, Vector3 planeNormal)
        {
            ScreenToWorldRay(screenPos, out Vector3 rayOrigin, out Vector3 rayDirection);
            
            // 射线与平面的交点计算
            float denominator = Vector3.Dot(rayDirection, planeNormal);
            
            // 检查射线是否与平面平行
            if (Math.Abs(denominator) < float.Epsilon)
            {
                return null; // 射线与平面平行，无交点
            }
            
            float t = Vector3.Dot(planePoint - rayOrigin, planeNormal) / denominator;
            
            // 检查交点是否在射线的正方向上
            if (t < 0)
            {
                return null; // 交点在射线的反方向上
            }
            
            return rayOrigin + t * rayDirection;
        }

        /// <summary>
        /// 检查3D点是否在视锥体内
        /// </summary>
        /// <param name="worldPos">3D世界坐标</param>
        /// <returns>是否在视锥体内</returns>
        public bool IsPointInFrustum(Vector3 worldPos)
        {
            ValidateMatrices();
            
            Vector4 worldPosH = new Vector4(worldPos, 1.0f);
            Vector4 clipPos = _mvpMatrix * worldPosH;
            
            // 检查是否在裁剪空间内
            if (Math.Abs(clipPos.W) < float.Epsilon)
            {
                return false;
            }
            
            Vector3 ndcPos = new Vector3(clipPos.X / clipPos.W, clipPos.Y / clipPos.W, clipPos.Z / clipPos.W);
            
            return ndcPos.X >= -1.0f && ndcPos.X <= 1.0f &&
                   ndcPos.Y >= -1.0f && ndcPos.Y <= 1.0f &&
                   ndcPos.Z >= -1.0f && ndcPos.Z <= 1.0f;
        }

        /// <summary>
        /// 计算屏幕空间中两点之间的距离对应的世界空间距离
        /// </summary>
        /// <param name="screenPos1">屏幕坐标点1</param>
        /// <param name="screenPos2">屏幕坐标点2</param>
        /// <param name="referenceDepth">参考深度（世界坐标Z值）</param>
        /// <returns>世界空间距离</returns>
        public float ScreenDistanceToWorldDistance(Vector2 screenPos1, Vector2 screenPos2, float referenceDepth)
        {
            // 在参考深度平面上创建两个点
            Vector3 planePoint = new Vector3(0, 0, referenceDepth);
            Vector3 planeNormal = new Vector3(0, 0, 1);
            
            Vector3? worldPos1 = ScreenToPlane(screenPos1, planePoint, planeNormal);
            Vector3? worldPos2 = ScreenToPlane(screenPos2, planePoint, planeNormal);
            
            if (worldPos1.HasValue && worldPos2.HasValue)
            {
                return Vector3.Distance(worldPos1.Value, worldPos2.Value);
            }
            
            return 0.0f;
        }
        
        /// <summary>
        /// 设置背景图片尺寸，用于更新视口参数
        /// </summary>
        /// <param name="width">背景图片宽度</param>
        /// <param name="height">背景图片高度</param>
        public void SetBackgroundSize(int width, int height)
        {
            // 更新视口尺寸以匹配背景图片
            _viewport = new Vector4(_viewport.X, _viewport.Y, width, height);
            _matricesValid = false;
        }
    }
}
using System;
using OpenTK.Mathematics;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Geometry.Factories;
using Avalonia3DControl.Core.Animation;

namespace Avalonia3DControl.Core.Models
{
    /// <summary>
    /// 三维模型类
    /// </summary>
    public class Model3D
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Color { get; set; }
        public float Alpha { get; set; }
        public int TextureId { get; set; }
        public Material Material { get; set; }
        public bool Visible { get; set; }
        public string Name { get; set; }

        // 几何数据
        public float[] Vertices { get; set; } = Array.Empty<float>();
        public uint[] Indices { get; set; } = Array.Empty<uint>();
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }
        
        // 动画相关
        /// <summary>
        /// 振型动画控制器
        /// </summary>
        public ModalAnimationController? AnimationController { get; set; }
        
        /// <summary>
        /// 是否启用动画
        /// </summary>
        public bool IsAnimationEnabled { get; set; } = false;
        
        /// <summary>
        /// 顶点是否需要更新（用于渲染器优化）
        /// </summary>
        public bool VerticesNeedUpdate { get; set; } = false;
        
        /// <summary>
        /// 请求渲染事件
        /// </summary>
        public event Action? RenderRequested;

        public Model3D()
        {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            Color = Vector3.One;
            Alpha = 1.0f;
            TextureId = 0;
            Material = new Material();
            Visible = true;
            Name = "Model";
            AnimationController = null;
            IsAnimationEnabled = false;
            VerticesNeedUpdate = false;
        }
        
        /// <summary>
        /// 启用振型动画
        /// </summary>
        /// <param name="modalDataSet">振型数据集</param>
        public void EnableModalAnimation(ModalDataSet modalDataSet)
        {
            if (AnimationController == null)
            {
                AnimationController = new ModalAnimationController();
                AnimationController.VerticesUpdated += OnVerticesUpdated;
            }
            
            AnimationController.SetModalDataSet(modalDataSet);
            AnimationController.SetTargetModel(this);
            IsAnimationEnabled = true;
        }
        
        /// <summary>
        /// 禁用振型动画
        /// </summary>
        public void DisableModalAnimation()
        {
            if (AnimationController != null)
            {
                AnimationController.Stop();
                AnimationController.VerticesUpdated -= OnVerticesUpdated;
                AnimationController.Dispose();
                AnimationController = null;
            }
            IsAnimationEnabled = false;
            VerticesNeedUpdate = false;
        }
        
        /// <summary>
        /// 更新动画（每帧调用）
        /// </summary>
        public void UpdateAnimation()
        {
            if (IsAnimationEnabled && AnimationController != null)
            {
                AnimationController.Update();
            }
        }
        
        /// <summary>
        /// 顶点更新回调
        /// </summary>
        /// <param name="model">更新的模型</param>
        private void OnVerticesUpdated(Model3D model)
        {
            VerticesNeedUpdate = true;
            RenderRequested?.Invoke();
        }

        /// <summary>
        /// 获取顶点数据
        /// </summary>
        /// <returns>顶点数据数组</returns>
        public virtual float[] GetVertexData()
        {
            return Vertices;
        }

        public virtual Matrix4 GetModelMatrix()
        {
            var translation = Matrix4.CreateTranslation(Position);
            var rotationX = Matrix4.CreateRotationX(Rotation.X);
            var rotationY = Matrix4.CreateRotationY(Rotation.Y);
            var rotationZ = Matrix4.CreateRotationZ(Rotation.Z);
            var scale = Matrix4.CreateScale(Scale);

            return translation * rotationZ * rotationY * rotationX * scale;
        }
        
        /// <summary>
        /// 计算模型的边界框（在世界坐标系中）
        /// </summary>
        /// <returns>边界框的最小和最大坐标</returns>
        public (Vector3 Min, Vector3 Max) GetBoundingBox()
        {
            if (Vertices == null || Vertices.Length == 0)
            {
                return (Vector3.Zero, Vector3.Zero);
            }
            
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            
            // 遍历所有顶点（假设顶点格式为 x,y,z,r,g,b）
            for (int i = 0; i < Vertices.Length; i += 6)
            {
                var vertex = new Vector3(Vertices[i], Vertices[i + 1], Vertices[i + 2]);
                
                // 应用模型变换矩阵
                var transformedVertex = Vector3.TransformPosition(vertex, GetModelMatrix());
                
                min = Vector3.ComponentMin(min, transformedVertex);
                max = Vector3.ComponentMax(max, transformedVertex);
            }
            
            return (min, max);
        }
        
        /// <summary>
        /// 获取模型的中心点（在世界坐标系中）
        /// </summary>
        /// <returns>模型中心点</returns>
        public Vector3 GetCenter()
        {
            var (min, max) = GetBoundingBox();
            return (min + max) * 0.5f;
        }
        
        /// <summary>
        /// 获取模型的尺寸
        /// </summary>
        /// <returns>模型在各轴上的尺寸</returns>
        public Vector3 GetSize()
        {
            var (min, max) = GetBoundingBox();
            return max - min;
        }
    }


}
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

        public virtual Matrix4 GetModelMatrix()
        {
            var translation = Matrix4.CreateTranslation(Position);
            var rotationX = Matrix4.CreateRotationX(Rotation.X);
            var rotationY = Matrix4.CreateRotationY(Rotation.Y);
            var rotationZ = Matrix4.CreateRotationZ(Rotation.Z);
            var scale = Matrix4.CreateScale(Scale);

            return translation * rotationZ * rotationY * rotationX * scale;
        }
    }

    /// <summary>
    /// 坐标轴专用模型类，始终保持在世界坐标原点
    /// </summary>
    public class CoordinateAxesModel : Model3D
    {
        /// <summary>
        /// 标识这是坐标轴模型，需要特殊渲染处理
        /// </summary>
        public bool IsCoordinateAxes => true;
        
        public CoordinateAxesModel()
        {
            var axesData = GeometryFactory.CreateCoordinateAxes();
            Vertices = axesData.Vertices;
            Indices = axesData.Indices;
            VertexCount = axesData.VertexCount;
            IndexCount = axesData.IndexCount;
            Material = axesData.Material;
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            Visible = true;
        }
        
        /// <summary>
        /// 坐标轴始终返回单位矩阵，不受任何变换影响
        /// </summary>
        /// <returns>单位矩阵</returns>
        public override Matrix4 GetModelMatrix()
        {
            return Matrix4.Identity;
        }
    }
}
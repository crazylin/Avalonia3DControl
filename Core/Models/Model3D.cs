using OpenTK.Mathematics;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Geometry.Factories;

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
        public float[] Vertices { get; set; }
        public uint[] Indices { get; set; }
        public int VertexCount { get; set; }
        public int IndexCount { get; set; }

        public Model3D()
        {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            Color = Vector3.One;
            Material = new Material();
            Visible = true;
            Name = "Model";
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
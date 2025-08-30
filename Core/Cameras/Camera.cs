using OpenTK.Mathematics;

namespace Avalonia3DControl.Core.Cameras
{
    /// <summary>
    /// 投影模式枚举
    /// </summary>
    public enum ProjectionMode
    {
        Perspective,  // 透视投影（3D模式）
        Orthographic  // 正交投影（2D模式）
    }

    /// <summary>
    /// 相机类
    /// </summary>
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public ProjectionMode Mode { get; set; }
        
        // 2D模式下的正交投影参数
        public float OrthographicSize { get; set; }

        public Camera()
        {
            Position = new Vector3(0.0f, 0.0f, 3.0f);
            Target = Vector3.Zero;
            Up = Vector3.UnitY;
            FieldOfView = MathHelper.DegreesToRadians(60.0f);
            AspectRatio = 1.0f;
            NearPlane = 0.1f;
            FarPlane = 100.0f;
            Mode = ProjectionMode.Perspective;
            OrthographicSize = 5.0f; // 2D模式下的视野大小
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Target, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            if (Mode == ProjectionMode.Perspective)
            {
                return Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
            }
            else
            {
                // 正交投影矩阵
                float halfHeight = OrthographicSize * 0.5f;
                float halfWidth = halfHeight * AspectRatio;
                return Matrix4.CreateOrthographic(halfWidth * 2, halfHeight * 2, NearPlane, FarPlane);
            }
        }
        
        /// <summary>
        /// 切换到正交投影模式
        /// </summary>
        public void SwitchToOrthographic()
        {
            Mode = ProjectionMode.Orthographic;
            // 调整相机位置以适合2D视图
            Position = new Vector3(0.0f, 0.0f, 5.0f);
            Target = Vector3.Zero;
        }
        
        /// <summary>
        /// 切换到透视投影模式
        /// </summary>
        public void SwitchToPerspective()
        {
            Mode = ProjectionMode.Perspective;
            // 恢复3D相机位置
            Position = new Vector3(0.0f, 0.0f, 3.0f);
            Target = Vector3.Zero;
        }
    }
}
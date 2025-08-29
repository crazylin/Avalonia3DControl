using OpenTK.Mathematics;

namespace Avalonia3DControl.Core.Cameras
{
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

        public Camera()
        {
            Position = new Vector3(0.0f, 0.0f, 3.0f);
            Target = Vector3.Zero;
            Up = Vector3.UnitY;
            FieldOfView = MathHelper.DegreesToRadians(60.0f);
            AspectRatio = 1.0f;
            NearPlane = 0.1f;
            FarPlane = 100.0f;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Target, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }
    }
}
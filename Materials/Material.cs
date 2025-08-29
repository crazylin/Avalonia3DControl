using OpenTK.Mathematics;

namespace Avalonia3DControl.Materials
{
    /// <summary>
    /// 着色模式枚举
    /// </summary>
    public enum ShadingMode
    {
        Vertex,         // 顶点显示
        Texture         // 纹理模式
    }

    /// <summary>
    /// 渲染模式枚举
    /// </summary>
    public enum RenderMode
    {
        Point,          // 点模式
        Line,           // 线框模式
        Fill            // 填充模式
    }

    /// <summary>
    /// 材质类
    /// </summary>
    public class Material
    {
        public Vector3 Ambient { get; set; }      // 环境光反射
        public Vector3 Diffuse { get; set; }      // 漫反射
        public Vector3 Specular { get; set; }     // 镜面反射
        public float Shininess { get; set; }      // 光泽度
        public Vector3 Emission { get; set; }     // 自发光
        public ShadingMode ShadingMode { get; set; } // 着色模式
        public RenderMode RenderMode { get; set; }   // 渲染模式

        public Material()
        {
            Ambient = new Vector3(0.2f, 0.2f, 0.2f);
            Diffuse = new Vector3(0.8f, 0.8f, 0.8f);
            Specular = new Vector3(1.0f, 1.0f, 1.0f);
            Shininess = 32.0f;
            Emission = Vector3.Zero;
            ShadingMode = ShadingMode.Vertex;
            RenderMode = RenderMode.Fill;
        }

        // 预定义材质
        public static Material CreatePlastic(Vector3 color)
        {
            return new Material
            {
                Ambient = color * 0.2f,
                Diffuse = color * 0.8f,
                Specular = new Vector3(0.5f, 0.5f, 0.5f),
                Shininess = 32.0f
            };
        }

        public static Material CreateMetal(Vector3 color)
        {
            return new Material
            {
                Ambient = color * 0.1f,
                Diffuse = color * 0.3f,
                Specular = new Vector3(0.9f, 0.9f, 0.9f),
                Shininess = 128.0f
            };
        }

        public static Material CreateGlass(Vector3 color)
        {
            return new Material
            {
                Ambient = color * 0.05f,
                Diffuse = color * 0.2f,
                Specular = new Vector3(1.0f, 1.0f, 1.0f),
                Shininess = 256.0f
            };
        }
    }
}
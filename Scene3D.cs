using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Avalonia3DControl
{
    /// <summary>
    /// 三维场景管理类
    /// </summary>
    public class Scene3D
    {
        public Camera Camera { get; set; }
        public List<Light> Lights { get; set; }
        public List<Model3D> Models { get; set; }
        public Vector3 BackgroundColor { get; set; }

        public Scene3D()
        {
            Camera = new Camera();
            Lights = new List<Light>();
            Models = new List<Model3D>();
            BackgroundColor = new Vector3(0.2f, 0.3f, 0.3f);
            
            // 添加默认光源
            AddDefaultLight();
            
            // 场景初始化为空，模型将按需动态加载
        }
        
        /// <summary>
        /// 动态创建指定类型的模型
        /// </summary>
        /// <param name="modelType">模型类型名称</param>
        /// <returns>创建的模型，如果类型不支持则返回null</returns>
        public Model3D? CreateModel(string modelType)
        {
            Model3D? model = null;
            
            switch (modelType.ToLower())
            {
                case "cube":
                    model = GeometryFactory.CreateCube();
                    model.Name = "Cube";
                    model.Material = Material.CreatePlastic(new Vector3(0.8f, 0.2f, 0.2f)); // 红色塑料
                    break;
                    
                case "sphere":
                    model = GeometryFactory.CreateSphere();
                    model.Name = "Sphere";
                    model.Material = Material.CreateMetal(new Vector3(0.2f, 0.8f, 0.2f)); // 绿色金属
                    break;
                    
                case "wave":
                    model = GeometryFactory.CreateWave();
                    model.Name = "Wave";
                    model.Material = Material.CreateGlass(new Vector3(0.2f, 0.6f, 0.9f)); // 蓝色玻璃
                    break;
                    
                case "waterdrop":
                    model = GeometryFactory.CreateWaterDrop();
                    model.Name = "WaterDrop";
                    model.Scale = new Vector3(0.8f, 0.8f, 0.8f);
                    model.Material = Material.CreateGlass(new Vector3(0.2f, 0.9f, 0.8f)); // 青色玻璃
                    break;
                    
                default:
                    return null;
            }
            
            if (model != null)
            {
                model.Position = new Vector3(0.0f, 0.0f, 0.0f);
                model.Visible = true;
            }
            
            return model;
        }
        
        /// <summary>
        /// 清空场景中的所有模型
        /// </summary>
        public void ClearModels()
        {
            Models.Clear();
        }
        
        /// <summary>
        /// 设置当前显示的模型（清空其他模型）
        /// </summary>
        /// <param name="modelType">要显示的模型类型，null表示清空所有模型</param>
        /// <returns>创建的模型，如果类型不支持或为null则返回null</returns>
        public Model3D? SetCurrentModel(string? modelType)
        {
            // 清空现有模型
            ClearModels();
            
            // 如果指定了模型类型，创建并添加新模型
            if (!string.IsNullOrEmpty(modelType))
            {
                var model = CreateModel(modelType);
                if (model != null)
                {
                    Models.Add(model);
                }
                return model;
            }
            
            return null;
        }

        private void AddDefaultLight()
        {
            var directionalLight = new DirectionalLight
            {
                Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, -1.0f)),
                Color = new Vector3(1.0f, 1.0f, 1.0f),
                Intensity = 1.0f
            };
            Lights.Add(directionalLight);
        }

        public void AddModel(Model3D model)
        {
            Models.Add(model);
        }

        public void RemoveModel(Model3D model)
        {
            Models.Remove(model);
        }

        public void AddLight(Light light)
        {
            Lights.Add(light);
        }

        public void RemoveLight(Light light)
        {
            Lights.Remove(light);
        }
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

    /// <summary>
    /// 光源基类
    /// </summary>
    public abstract class Light
    {
        public Vector3 Color { get; set; }
        public float Intensity { get; set; }
        public bool Enabled { get; set; }

        protected Light()
        {
            Color = Vector3.One;
            Intensity = 1.0f;
            Enabled = true;
        }
    }

    /// <summary>
    /// 方向光
    /// </summary>
    public class DirectionalLight : Light
    {
        public Vector3 Direction { get; set; }

        public DirectionalLight()
        {
            Direction = new Vector3(0.0f, -1.0f, 0.0f);
        }
    }

    /// <summary>
    /// 点光源
    /// </summary>
    public class PointLight : Light
    {
        public Vector3 Position { get; set; }
        public float Range { get; set; }
        public Vector3 Attenuation { get; set; } // x: constant, y: linear, z: quadratic

        public PointLight()
        {
            Position = Vector3.Zero;
            Range = 10.0f;
            Attenuation = new Vector3(1.0f, 0.09f, 0.032f);
        }
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
            ShadingMode = ShadingMode.Phong;
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

    /// <summary>
    /// 着色模式枚举
    /// </summary>
    public enum ShadingMode
    {
        Flat,           // 平面着色
        Gouraud,        // 顶点着色（Gouraud着色）
        Phong,          // 片段着色（Phong着色）
        Wireframe,      // 线框模式
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

        public Matrix4 GetModelMatrix()
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
    /// 预定义几何体工厂
    /// </summary>
    public static class GeometryFactory
    {
        public static Model3D CreateCube(float size = 1.0f)
        {
            float halfSize = size * 0.5f;
            
            // 立方体的8个唯一顶点 (位置3 + 颜色3)
            var vertices = new float[]
            {
                // 前面4个顶点
                -halfSize, -halfSize,  halfSize,  1.0f, 0.0f, 0.0f, // 0: 前左下 - 红色
                 halfSize, -halfSize,  halfSize,  0.0f, 1.0f, 0.0f, // 1: 前右下 - 绿色
                 halfSize,  halfSize,  halfSize,  0.0f, 0.0f, 1.0f, // 2: 前右上 - 蓝色
                -halfSize,  halfSize,  halfSize,  1.0f, 1.0f, 0.0f, // 3: 前左上 - 黄色
                
                // 后面4个顶点
                -halfSize, -halfSize, -halfSize,  1.0f, 0.0f, 1.0f, // 4: 后左下 - 紫色
                 halfSize, -halfSize, -halfSize,  0.0f, 1.0f, 1.0f, // 5: 后右下 - 青色
                 halfSize,  halfSize, -halfSize,  1.0f, 0.5f, 0.0f, // 6: 后右上 - 橙色
                -halfSize,  halfSize, -halfSize,  0.5f, 0.5f, 0.5f, // 7: 后左上 - 灰色
            };

            var indices = new uint[]
            {
                // 前面 (z = +halfSize) - 逆时针绕序
                0, 1, 2, 2, 3, 0,
                // 后面 (z = -halfSize) - 逆时针绕序（从外部看）
                5, 4, 7, 7, 6, 5,
                // 左面 (x = -halfSize) - 逆时针绕序（从外部看）
                4, 0, 3, 3, 7, 4,
                // 右面 (x = +halfSize) - 逆时针绕序（从外部看）
                1, 5, 6, 6, 2, 1,
                // 底面 (y = -halfSize) - 逆时针绕序（从外部看）
                4, 5, 1, 1, 0, 4,
                // 顶面 (y = +halfSize) - 逆时针绕序（从外部看）
                3, 2, 6, 6, 7, 3
            };

            var cube = new Model3D
            {
                Name = "Cube",
                Vertices = vertices,
                Indices = indices,
                VertexCount = 8, // 立方体有8个唯一顶点
                IndexCount = indices.Length
            };
            
            // 设置塑料材质
            cube.Material = Material.CreatePlastic(new Vector3(0.8f, 0.2f, 0.2f));
            return cube;
        }

        public static Model3D CreateSphere(float radius = 1.0f, int segments = 32)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            // 生成球体顶点
            for (int i = 0; i <= segments; i++)
            {
                float phi = MathF.PI * i / segments;
                for (int j = 0; j <= segments; j++)
                {
                    float theta = 2.0f * MathF.PI * j / segments;
                    
                    float x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                    float y = radius * MathF.Cos(phi);
                    float z = radius * MathF.Sin(phi) * MathF.Sin(theta);
                    
                    // 位置
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    
                    // 颜色（基于位置生成）
                    vertices.Add((x + radius) / (2 * radius));
                    vertices.Add((y + radius) / (2 * radius));
                    vertices.Add((z + radius) / (2 * radius));
                }
            }

            // 生成索引
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    uint first = (uint)(i * (segments + 1) + j);
                    uint second = (uint)(first + segments + 1);
                    
                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);
                    
                    indices.Add(second);
                    indices.Add(second + 1);
                    indices.Add(first + 1);
                }
            }

            var sphere = new Model3D
            {
                Name = "Sphere",
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                VertexCount = vertices.Count / 6, // 每个顶点6个分量（位置3 + 颜色3）
                IndexCount = indices.Count
            };
            
            // 设置金属材质
            sphere.Material = Material.CreateMetal(new Vector3(0.2f, 0.8f, 0.2f));
            return sphere;
        }

        public static Model3D CreateWave(float width = 4.0f, float height = 1.0f, int segments = 64, float time = 0.0f)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            // 生成波浪网格
            for (int i = 0; i <= segments; i++)
            {
                for (int j = 0; j <= segments; j++)
                {
                    float x = (i / (float)segments - 0.5f) * width;
                    float z = (j / (float)segments - 0.5f) * width;
                    
                    // 波浪函数：多个正弦波叠加
                    float y = height * (MathF.Sin(x * 2.0f + time) * 0.5f + 
                                       MathF.Sin(z * 1.5f + time * 0.8f) * 0.3f + 
                                       MathF.Sin((x + z) * 1.2f + time * 1.2f) * 0.2f);
                    
                    // 位置
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    
                    // 基于高度的颜色
                    float normalizedY = (y + height) / (2 * height);
                    vertices.Add(0.1f + normalizedY * 0.3f); // 红色分量
                    vertices.Add(0.3f + normalizedY * 0.4f); // 绿色分量
                    vertices.Add(0.8f + normalizedY * 0.2f); // 蓝色分量
                }
            }

            // 生成索引
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    uint topLeft = (uint)(i * (segments + 1) + j);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = (uint)((i + 1) * (segments + 1) + j);
                    uint bottomRight = bottomLeft + 1;
                    
                    // 第一个三角形
                    indices.Add(topLeft);
                    indices.Add(bottomLeft);
                    indices.Add(topRight);
                    
                    // 第二个三角形
                    indices.Add(topRight);
                    indices.Add(bottomLeft);
                    indices.Add(bottomRight);
                }
            }

            var wave = new Model3D
            {
                Name = "Wave",
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                VertexCount = vertices.Count / 6, // 每个顶点6个分量（位置3 + 颜色3）
                IndexCount = indices.Count
            };
            
            // 设置玻璃材质
            wave.Material = Material.CreateGlass(new Vector3(0.2f, 0.6f, 0.9f));
            return wave;
        }

        public static Model3D CreateWaterDrop(float radius = 1.0f, int segments = 32)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            // 水滴形状：上半部分是球体，下半部分是拉长的椭球
            for (int i = 0; i <= segments; i++)
            {
                float phi = MathF.PI * i / segments;
                for (int j = 0; j <= segments; j++)
                {
                    float theta = 2.0f * MathF.PI * j / segments;
                    
                    float x, y, z;
                    float nx, ny, nz;
                    
                    if (phi <= MathF.PI * 0.6f) // 上半部分：球体
                    {
                        x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                        y = radius * MathF.Cos(phi) + radius * 0.5f; // 向上偏移
                        z = radius * MathF.Sin(phi) * MathF.Sin(theta);
                        
                        // 球体部分的法线（指向球心外）
                        nx = MathF.Sin(phi) * MathF.Cos(theta);
                        ny = MathF.Cos(phi);
                        nz = MathF.Sin(phi) * MathF.Sin(theta);
                    }
                    else // 下半部分：拉长的椭球
                    {
                        float adjustedPhi = (phi - MathF.PI * 0.6f) / (MathF.PI * 0.4f) * MathF.PI * 0.8f;
                        float stretchFactor = 1.0f - (phi - MathF.PI * 0.6f) / (MathF.PI * 0.4f) * 0.8f;
                        
                        x = radius * stretchFactor * MathF.Sin(adjustedPhi) * MathF.Cos(theta);
                        y = -radius * 1.5f * MathF.Cos(adjustedPhi) + radius * 0.5f;
                        z = radius * stretchFactor * MathF.Sin(adjustedPhi) * MathF.Sin(theta);
                        
                        // 椭球部分的法线（近似计算）
                        nx = stretchFactor * MathF.Sin(adjustedPhi) * MathF.Cos(theta);
                        ny = -1.5f * MathF.Cos(adjustedPhi);
                        nz = stretchFactor * MathF.Sin(adjustedPhi) * MathF.Sin(theta);
                        
                        // 归一化法线
                        float length = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
                        if (length > 0)
                        {
                            nx /= length;
                            ny /= length;
                            nz /= length;
                        }
                    }
                    
                    // 位置
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    
                    // 水滴颜色：透明蓝色
                    vertices.Add(0.3f + (y + radius) / (3 * radius) * 0.4f);
                    vertices.Add(0.6f + (y + radius) / (3 * radius) * 0.3f);
                    vertices.Add(0.9f);
                }
            }

            // 生成索引（与球体相同）
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    uint first = (uint)(i * (segments + 1) + j);
                    uint second = (uint)(first + segments + 1);
                    
                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);
                    
                    indices.Add(second);
                    indices.Add(second + 1);
                    indices.Add(first + 1);
                }
            }

            var waterDrop = new Model3D
            {
                Name = "WaterDrop",
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                VertexCount = vertices.Count / 6, // 每个顶点6个分量（位置3 + 颜色3）
                IndexCount = indices.Count
            };
            
            // 设置玻璃材质
            waterDrop.Material = Material.CreateGlass(new Vector3(0.4f, 0.7f, 1.0f));
            return waterDrop;
        }
    }
}
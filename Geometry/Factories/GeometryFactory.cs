using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Materials;

namespace Avalonia3DControl.Geometry.Factories
{
    /// <summary>
    /// 预定义几何体工厂
    /// </summary>
    public static class GeometryFactory
    {
        /// <summary>
        /// 根据模型类型创建模型
        /// </summary>
        /// <param name="modelType">模型类型名称</param>
        /// <returns>创建的模型，如果类型不支持则返回null</returns>
        public static Model3D? CreateModel(string modelType)
        {
            Model3D? model = null;
            
            switch (modelType.ToLower())
            {
                case "cube":
                    model = CreateCube();
                    model.Name = "Cube";
                    model.Material = Material.CreatePlastic(new Vector3(0.8f, 0.2f, 0.2f)); // 红色塑料
                    break;
                    
                case "sphere":
                    model = CreateSphere();
                    model.Name = "Sphere";
                    model.Material = Material.CreateMetal(new Vector3(0.2f, 0.8f, 0.2f)); // 绿色金属
                    break;
                    
                case "wave":
                    model = CreateWave();
                    model.Name = "Wave";
                    model.Material = Material.CreateGlass(new Vector3(0.2f, 0.6f, 0.9f)); // 蓝色玻璃
                    break;
                    
                case "waterdrop":
                    model = CreateWaterDrop();
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

        public static Model3D CreateCube(float size = 1.0f)
        {
            float halfSize = size * 0.5f;
            
            // 立方体的24个顶点 (每个面4个顶点，位置3 + 颜色3 = 6个分量)
            var vertices = new float[]
            {
                // 前面 (z = +halfSize)
                -halfSize, -halfSize,  halfSize,  1.0f, 0.0f, 0.0f, // 0
                 halfSize, -halfSize,  halfSize,  1.0f, 0.0f, 0.0f, // 1
                 halfSize,  halfSize,  halfSize,  1.0f, 0.0f, 0.0f, // 2
                -halfSize,  halfSize,  halfSize,  1.0f, 0.0f, 0.0f, // 3
                
                // 后面 (z = -halfSize)
                 halfSize, -halfSize, -halfSize,  0.0f, 1.0f, 0.0f, // 4
                -halfSize, -halfSize, -halfSize,  0.0f, 1.0f, 0.0f, // 5
                -halfSize,  halfSize, -halfSize,  0.0f, 1.0f, 0.0f, // 6
                 halfSize,  halfSize, -halfSize,  0.0f, 1.0f, 0.0f, // 7
                
                // 左面 (x = -halfSize)
                -halfSize, -halfSize, -halfSize,  0.0f, 0.0f, 1.0f, // 8
                -halfSize, -halfSize,  halfSize,  0.0f, 0.0f, 1.0f, // 9
                -halfSize,  halfSize,  halfSize,  0.0f, 0.0f, 1.0f, // 10
                -halfSize,  halfSize, -halfSize,  0.0f, 0.0f, 1.0f, // 11
                
                // 右面 (x = +halfSize)
                 halfSize, -halfSize,  halfSize,  1.0f, 1.0f, 0.0f, // 12
                 halfSize, -halfSize, -halfSize,  1.0f, 1.0f, 0.0f, // 13
                 halfSize,  halfSize, -halfSize,  1.0f, 1.0f, 0.0f, // 14
                 halfSize,  halfSize,  halfSize,  1.0f, 1.0f, 0.0f, // 15
                
                // 底面 (y = -halfSize)
                -halfSize, -halfSize, -halfSize,  1.0f, 0.0f, 1.0f, // 16
                 halfSize, -halfSize, -halfSize,  1.0f, 0.0f, 1.0f, // 17
                 halfSize, -halfSize,  halfSize,  1.0f, 0.0f, 1.0f, // 18
                -halfSize, -halfSize,  halfSize,  1.0f, 0.0f, 1.0f, // 19
                
                // 顶面 (y = +halfSize)
                -halfSize,  halfSize,  halfSize,  0.0f, 1.0f, 1.0f, // 20
                 halfSize,  halfSize,  halfSize,  0.0f, 1.0f, 1.0f, // 21
                 halfSize,  halfSize, -halfSize,  0.0f, 1.0f, 1.0f, // 22
                -halfSize,  halfSize, -halfSize,  0.0f, 1.0f, 1.0f, // 23
            };

            var indices = new uint[]
            {
                // 前面
                0, 1, 2, 2, 3, 0,
                // 后面
                4, 5, 6, 6, 7, 4,
                // 左面
                8, 9, 10, 10, 11, 8,
                // 右面
                12, 13, 14, 14, 15, 12,
                // 底面
                16, 17, 18, 18, 19, 16,
                // 顶面
                20, 21, 22, 22, 23, 20
            };

            var cube = new Model3D
            {
                Name = "Cube",
                Vertices = vertices,
                Indices = indices,
                VertexCount = vertices.Length / 6, // 立方体有24个顶点（每个面4个），每个顶点6个分量
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

        public static Model3D CreateWave(float width = 4.0f, float height = 1.0f, int segments = 128, float time = 0.0f)
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
                    
                    // 波浪函数：多个正弦波叠加，创建更复杂的波浪效果
                    float y = height * (
                        MathF.Sin(x * 3.0f + time) * 0.4f +                    // 主波浪
                        MathF.Sin(z * 2.5f + time * 0.8f) * 0.3f +            // 横向波浪
                        MathF.Sin((x + z) * 2.0f + time * 1.2f) * 0.2f +      // 对角波浪
                        MathF.Sin(x * 5.0f + z * 3.0f + time * 1.5f) * 0.15f + // 高频细节波
                        MathF.Sin((x - z) * 1.8f + time * 0.6f) * 0.25f +     // 反对角波浪
                        MathF.Sin(x * 4.5f + time * 2.0f) * 0.1f +             // X方向高频波
                        MathF.Sin(z * 4.0f + time * 1.8f) * 0.12f +            // Z方向高频波
                        MathF.Sin(MathF.Sqrt(x*x + z*z) * 2.5f + time * 1.3f) * 0.18f // 径向波浪
                    );
                    
                    // 位置
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    
                    // 基于高度的渐变颜色：最低点蓝色 -> 绿色 -> 黄色 -> 最高点红色
                    float normalizedY = (y + height) / (2 * height); // 归一化到[0,1]
                    
                    float r, g, b;
                    if (normalizedY < 0.33f) // 蓝色到绿色
                    {
                        float t = normalizedY / 0.33f;
                        r = 0.0f;
                        g = t;
                        b = 1.0f - t;
                    }
                    else if (normalizedY < 0.66f) // 绿色到黄色
                    {
                        float t = (normalizedY - 0.33f) / 0.33f;
                        r = t;
                        g = 1.0f;
                        b = 0.0f;
                    }
                    else // 黄色到红色
                    {
                        float t = (normalizedY - 0.66f) / 0.34f;
                        r = 1.0f;
                        g = 1.0f - t;
                        b = 0.0f;
                    }
                    
                    vertices.Add(r); // 红色分量
                    vertices.Add(g); // 绿色分量
                    vertices.Add(b); // 蓝色分量
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
                    
                    if (phi <= MathF.PI / 2) // 上半部分：标准球体
                    {
                        x = radius * MathF.Sin(phi) * MathF.Cos(theta);
                        y = radius * MathF.Cos(phi);
                        z = radius * MathF.Sin(phi) * MathF.Sin(theta);
                    }
                    else // 下半部分：拉长的椭球
                    {
                        float stretchFactor = 1.5f; // 拉伸系数
                        x = radius * MathF.Sin(phi) * MathF.Cos(theta) * (2.0f - phi / MathF.PI);
                        y = -radius * MathF.Cos(phi) * stretchFactor;
                        z = radius * MathF.Sin(phi) * MathF.Sin(theta) * (2.0f - phi / MathF.PI);
                    }
                    
                    // 位置
                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);
                    
                    // 水滴颜色：青蓝色渐变
                    float normalizedY = (y + radius * 1.5f) / (radius * 2.5f);
                    vertices.Add(0.2f + normalizedY * 0.3f); // 红色分量
                    vertices.Add(0.6f + normalizedY * 0.3f); // 绿色分量
                    vertices.Add(0.8f + normalizedY * 0.2f); // 蓝色分量
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

            var waterDrop = new Model3D
            {
                Name = "WaterDrop",
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                VertexCount = vertices.Count / 6, // 每个顶点6个分量（位置3 + 颜色3）
                IndexCount = indices.Count
            };
            
            waterDrop.Material = Material.CreateGlass(new Vector3(0.2f, 0.9f, 0.8f));
            return waterDrop;
        }

        /// <summary>
        /// 创建专业的3D坐标轴（圆柱+圆锥+原点球）
        /// </summary>
        /// <param name="length">坐标轴长度</param>
        /// <returns>坐标轴模型</returns>
        public static Model3D CreateCoordinateAxes(float length = 2.0f)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();
            uint vertexIndex = 0;
            
            float cylinderRadius = 0.02f;
            float coneRadius = 0.05f;
            float coneHeight = 0.15f;
            float sphereRadius = 0.06f; // 增大原点球体半径
            int segments = 8; // 圆形分段数
            
            // 1. 创建原点球体（橙色）
            CreateSphere(vertices, indices, ref vertexIndex, Vector3.Zero, sphereRadius, 
                        new Vector3(1.0f, 0.5f, 0.0f), segments); // 橙色
            
            // 2. 创建X轴（红色）
            CreateAxis(vertices, indices, ref vertexIndex, 
                      Vector3.UnitX, length, cylinderRadius, coneRadius, coneHeight,
                      new Vector3(1.0f, 0.0f, 0.0f), segments);
            
            // 3. 创建Y轴（绿色）
            CreateAxis(vertices, indices, ref vertexIndex, 
                      Vector3.UnitY, length, cylinderRadius, coneRadius, coneHeight,
                      new Vector3(0.0f, 1.0f, 0.0f), segments);
            
            // 4. 创建Z轴（蓝色）
            CreateAxis(vertices, indices, ref vertexIndex, 
                      Vector3.UnitZ, length, cylinderRadius, coneRadius, coneHeight,
                      new Vector3(0.0f, 0.0f, 1.0f), segments);
            
            var axes = new Model3D
            {
                Name = "CoordinateAxes",
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray(),
                VertexCount = vertices.Count / 6, // 每个顶点6个float（位置+颜色）
                IndexCount = indices.Count
            };
            
            axes.Material = Material.CreateMetal(new Vector3(0.8f, 0.8f, 0.8f));
            return axes;
        }
        
        /// <summary>
        /// 创建单个坐标轴（圆柱+圆锥）
        /// </summary>
        private static void CreateAxis(List<float> vertices, List<uint> indices, ref uint vertexIndex,
                                     Vector3 direction, float length, float cylinderRadius, 
                                     float coneRadius, float coneHeight, Vector3 color, int segments)
        {
            float cylinderLength = length - coneHeight;
            
            // 创建圆柱体
            CreateCylinder(vertices, indices, ref vertexIndex, Vector3.Zero, direction, 
                          cylinderLength, cylinderRadius, color, segments);
            
            // 创建圆锥体（箭头）
            Vector3 coneBase = direction * cylinderLength;
            CreateCone(vertices, indices, ref vertexIndex, coneBase, direction, 
                      coneHeight, coneRadius, color, segments);
        }
        
        /// <summary>
        /// 创建圆柱体
        /// </summary>
        private static void CreateCylinder(List<float> vertices, List<uint> indices, ref uint vertexIndex,
                                         Vector3 start, Vector3 direction, float height, float radius, 
                                         Vector3 color, int segments)
        {
            // 计算垂直于方向的两个正交向量
            Vector3 up = Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) < 0.9f ? Vector3.UnitY : Vector3.UnitZ;
            Vector3 right = Vector3.Normalize(Vector3.Cross(direction, up));
            up = Vector3.Cross(right, direction);
            
            uint baseIndex = vertexIndex;
            
            // 创建圆柱体顶点
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2.0f * MathF.PI * i / segments;
                float x = MathF.Cos(angle) * radius;
                float y = MathF.Sin(angle) * radius;
                
                Vector3 offset = right * x + up * y;
                
                // 底部顶点
                Vector3 bottomPos = start + offset;
                vertices.AddRange(new[] { bottomPos.X, bottomPos.Y, bottomPos.Z, color.X, color.Y, color.Z });
                
                // 顶部顶点
                Vector3 topPos = start + direction * height + offset;
                vertices.AddRange(new[] { topPos.X, topPos.Y, topPos.Z, color.X, color.Y, color.Z });
            }
            
            // 创建圆柱体侧面三角形
            for (int i = 0; i < segments; i++)
            {
                uint bottom1 = baseIndex + (uint)(i * 2);
                uint top1 = bottom1 + 1;
                uint bottom2 = baseIndex + (uint)((i + 1) * 2);
                uint top2 = bottom2 + 1;
                
                // 两个三角形组成一个四边形
                indices.AddRange(new[] { bottom1, top1, bottom2 });
                indices.AddRange(new[] { top1, top2, bottom2 });
            }
            
            vertexIndex += (uint)((segments + 1) * 2);
        }
        
        /// <summary>
        /// 创建圆锥体
        /// </summary>
        private static void CreateCone(List<float> vertices, List<uint> indices, ref uint vertexIndex,
                                     Vector3 baseCenter, Vector3 direction, float height, float radius, 
                                     Vector3 color, int segments)
        {
            // 计算垂直于方向的两个正交向量
            Vector3 up = Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) < 0.9f ? Vector3.UnitY : Vector3.UnitZ;
            Vector3 right = Vector3.Normalize(Vector3.Cross(direction, up));
            up = Vector3.Cross(right, direction);
            
            uint baseIndex = vertexIndex;
            
            // 圆锥顶点
            Vector3 tip = baseCenter + direction * height;
            vertices.AddRange(new[] { tip.X, tip.Y, tip.Z, color.X, color.Y, color.Z });
            vertexIndex++;
            
            // 圆锥底面顶点
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2.0f * MathF.PI * i / segments;
                float x = MathF.Cos(angle) * radius;
                float y = MathF.Sin(angle) * radius;
                
                Vector3 offset = right * x + up * y;
                Vector3 pos = baseCenter + offset;
                vertices.AddRange(new[] { pos.X, pos.Y, pos.Z, color.X, color.Y, color.Z });
            }
            
            // 创建圆锥侧面三角形
            for (int i = 0; i < segments; i++)
            {
                uint tip_idx = baseIndex;
                uint base1 = baseIndex + 1 + (uint)i;
                uint base2 = baseIndex + 1 + (uint)((i + 1) % (segments + 1));
                
                indices.AddRange(new[] { tip_idx, base1, base2 });
            }
            
            vertexIndex += (uint)(segments + 1);
        }
        
        /// <summary>
        /// 创建球体
        /// </summary>
        private static void CreateSphere(List<float> vertices, List<uint> indices, ref uint vertexIndex,
                                       Vector3 center, float radius, Vector3 color, int segments)
        {
            uint baseIndex = vertexIndex;
            
            // 简化的球体：使用八面体细分
            // 顶点
            vertices.AddRange(new[] { center.X, center.Y + radius, center.Z, color.X, color.Y, color.Z }); // 顶部
            vertices.AddRange(new[] { center.X, center.Y - radius, center.Z, color.X, color.Y, color.Z }); // 底部
            vertices.AddRange(new[] { center.X + radius, center.Y, center.Z, color.X, color.Y, color.Z }); // 右
            vertices.AddRange(new[] { center.X - radius, center.Y, center.Z, color.X, color.Y, color.Z }); // 左
            vertices.AddRange(new[] { center.X, center.Y, center.Z + radius, color.X, color.Y, color.Z }); // 前
            vertices.AddRange(new[] { center.X, center.Y, center.Z - radius, color.X, color.Y, color.Z }); // 后
            
            // 八面体的8个三角形面
            uint[] sphereIndices = {
                0, 2, 4,  0, 4, 3,  0, 3, 5,  0, 5, 2,  // 上半部分
                1, 4, 2,  1, 3, 4,  1, 5, 3,  1, 2, 5   // 下半部分
            };
            
            foreach (uint idx in sphereIndices)
            {
                indices.Add(baseIndex + idx);
            }
            
            vertexIndex += 6;
        }
    }
}
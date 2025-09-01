using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI 2D渲染器，负责ROI图形的OpenGL渲染
    /// </summary>
    public class ROI2DRenderer : IDisposable
    {
        private readonly Dictionary<string, uint> _shaderPrograms;
        private readonly Dictionary<string, uint> _vertexArrays;
        private readonly Dictionary<string, uint> _vertexBuffers;
        private readonly Dictionary<string, uint> _indexBuffers;
        
        // 渲染状态
        private Matrix4 _projectionMatrix;
        private Matrix4 _viewMatrix;
        private bool _isRendering;
        
        // 顶点数据缓存
        private readonly List<Vertex2D> _vertices;
        private readonly List<uint> _indices;
        
        // 渲染配置
        public RenderConfig Config { get; set; }
        
        public ROI2DRenderer()
        {
            _shaderPrograms = new Dictionary<string, uint>();
            _vertexArrays = new Dictionary<string, uint>();
            _vertexBuffers = new Dictionary<string, uint>();
            _indexBuffers = new Dictionary<string, uint>();
            _vertices = new List<Vertex2D>();
            _indices = new List<uint>();
            
            Config = new RenderConfig();
            
            InitializeShaders();
            InitializeBuffers();
        }

        /// <summary>
        /// 初始化着色器程序
        /// </summary>
        private void InitializeShaders()
        {
            // 基础2D着色器
            string basicVertexShader = @"
                #version 330 core
                layout (location = 0) in vec2 aPosition;
                layout (location = 1) in vec4 aColor;
                layout (location = 2) in vec2 aTexCoord;
                
                uniform mat4 uProjection;
                uniform mat4 uView;
                
                out vec4 vertexColor;
                out vec2 texCoord;
                
                void main()
                {
                    gl_Position = uProjection * uView * vec4(aPosition, 0.0, 1.0);
                    vertexColor = aColor;
                    texCoord = aTexCoord;
                }
            ";
            
            string basicFragmentShader = @"
                #version 330 core
                in vec4 vertexColor;
                in vec2 texCoord;
                
                uniform bool uUseTexture;
                uniform sampler2D uTexture;
                uniform float uLineWidth;
                uniform bool uAntiAlias;
                
                out vec4 FragColor;
                
                void main()
                {
                    vec4 color = vertexColor;
                    
                    if (uUseTexture) {
                        color *= texture(uTexture, texCoord);
                    }
                    
                    // 简单的抗锯齿处理
                    if (uAntiAlias) {
                        float alpha = smoothstep(0.0, 1.0, color.a);
                        color.a = alpha;
                    }
                    
                    FragColor = color;
                }
            ";
            
            _shaderPrograms["basic"] = CreateShaderProgram(basicVertexShader, basicFragmentShader);
            
            // 圆形着色器（用于抗锯齿圆形渲染）
            string circleFragmentShader = @"
                #version 330 core
                in vec4 vertexColor;
                in vec2 texCoord;
                
                uniform float uRadius;
                uniform vec2 uCenter;
                uniform bool uFilled;
                uniform float uLineWidth;
                
                out vec4 FragColor;
                
                void main()
                {
                    vec2 pos = texCoord - vec2(0.5, 0.5);
                    float dist = length(pos) * 2.0;
                    
                    float alpha = 1.0;
                    
                    if (uFilled) {
                        alpha = 1.0 - smoothstep(0.98, 1.0, dist);
                    } else {
                        float innerRadius = 1.0 - uLineWidth;
                        alpha = smoothstep(innerRadius - 0.02, innerRadius, dist) - 
                               smoothstep(0.98, 1.0, dist);
                    }
                    
                    FragColor = vec4(vertexColor.rgb, vertexColor.a * alpha);
                }
            ";
            
            _shaderPrograms["circle"] = CreateShaderProgram(basicVertexShader, circleFragmentShader);
        }

        /// <summary>
        /// 创建着色器程序
        /// </summary>
        private uint CreateShaderProgram(string vertexSource, string fragmentSource)
        {
            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            
            uint program = (uint)GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            
            // 检查链接状态
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog((int)program);
                throw new Exception($"着色器程序链接失败: {infoLog}");
            }
            
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            
            return program;
        }

        /// <summary>
        /// 编译着色器
        /// </summary>
        private uint CompileShader(ShaderType type, string source)
        {
            uint shader = (uint)GL.CreateShader(type);
            GL.ShaderSource((int)shader, source);
            GL.CompileShader(shader);
            
            GL.GetShader((int)shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog((int)shader);
                throw new Exception($"着色器编译失败 ({type}): {infoLog}");
            }
            
            return shader;
        }

        /// <summary>
        /// 初始化缓冲区
        /// </summary>
        private void InitializeBuffers()
        {
            // 创建基础几何体的VAO和VBO
            CreateGeometryBuffers("point", GeneratePointGeometry());
            CreateGeometryBuffers("line", GenerateLineGeometry());
            CreateGeometryBuffers("quad", GenerateQuadGeometry());
            CreateGeometryBuffers("circle", GenerateCircleGeometry(32));
        }

        /// <summary>
        /// 创建几何体缓冲区
        /// </summary>
        private void CreateGeometryBuffers(string name, (Vertex2D[] vertices, uint[] indices) geometry)
        {
            uint vao = (uint)GL.GenVertexArray();
            uint vbo = (uint)GL.GenBuffer();
            uint ebo = (uint)GL.GenBuffer();
            
            GL.BindVertexArray(vao);
            
            // 顶点缓冲
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, geometry.vertices.Length * Vertex2D.SizeInBytes, 
                         geometry.vertices, BufferUsageHint.StaticDraw);
            
            // 索引缓冲
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, geometry.indices.Length * sizeof(uint), 
                         geometry.indices, BufferUsageHint.StaticDraw);
            
            // 顶点属性
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);
            
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            
            GL.BindVertexArray(0);
            
            _vertexArrays[name] = vao;
            _vertexBuffers[name] = vbo;
            _indexBuffers[name] = ebo;
        }

        /// <summary>
        /// 生成点几何体
        /// </summary>
        private (Vertex2D[] vertices, uint[] indices) GeneratePointGeometry()
        {
            var vertices = new Vertex2D[4];
            float size = 0.5f;
            
            vertices[0] = new Vertex2D { Position = new Vector2(-size, -size), Color = Vector4.One, TexCoord = new Vector2(0, 0) };
            vertices[1] = new Vertex2D { Position = new Vector2(size, -size), Color = Vector4.One, TexCoord = new Vector2(1, 0) };
            vertices[2] = new Vertex2D { Position = new Vector2(size, size), Color = Vector4.One, TexCoord = new Vector2(1, 1) };
            vertices[3] = new Vertex2D { Position = new Vector2(-size, size), Color = Vector4.One, TexCoord = new Vector2(0, 1) };
            
            var indices = new uint[] { 0, 1, 2, 2, 3, 0 };
            
            return (vertices, indices);
        }

        /// <summary>
        /// 生成线几何体
        /// </summary>
        private (Vertex2D[] vertices, uint[] indices) GenerateLineGeometry()
        {
            var vertices = new Vertex2D[2];
            
            vertices[0] = new Vertex2D { Position = new Vector2(0, 0), Color = Vector4.One, TexCoord = new Vector2(0, 0) };
            vertices[1] = new Vertex2D { Position = new Vector2(1, 0), Color = Vector4.One, TexCoord = new Vector2(1, 0) };
            
            var indices = new uint[] { 0, 1 };
            
            return (vertices, indices);
        }

        /// <summary>
        /// 生成四边形几何体
        /// </summary>
        private (Vertex2D[] vertices, uint[] indices) GenerateQuadGeometry()
        {
            var vertices = new Vertex2D[4];
            
            vertices[0] = new Vertex2D { Position = new Vector2(-0.5f, -0.5f), Color = Vector4.One, TexCoord = new Vector2(0, 0) };
            vertices[1] = new Vertex2D { Position = new Vector2(0.5f, -0.5f), Color = Vector4.One, TexCoord = new Vector2(1, 0) };
            vertices[2] = new Vertex2D { Position = new Vector2(0.5f, 0.5f), Color = Vector4.One, TexCoord = new Vector2(1, 1) };
            vertices[3] = new Vertex2D { Position = new Vector2(-0.5f, 0.5f), Color = Vector4.One, TexCoord = new Vector2(0, 1) };
            
            var indices = new uint[] { 0, 1, 2, 2, 3, 0 };
            
            return (vertices, indices);
        }

        /// <summary>
        /// 生成圆形几何体
        /// </summary>
        private (Vertex2D[] vertices, uint[] indices) GenerateCircleGeometry(int segments)
        {
            var vertices = new List<Vertex2D>();
            var indices = new List<uint>();
            
            // 中心点
            vertices.Add(new Vertex2D { Position = Vector2.Zero, Color = Vector4.One, TexCoord = new Vector2(0.5f, 0.5f) });
            
            // 圆周点
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)(2.0 * Math.PI * i / segments);
                float x = (float)Math.Cos(angle);
                float y = (float)Math.Sin(angle);
                
                vertices.Add(new Vertex2D 
                { 
                    Position = new Vector2(x, y), 
                    Color = Vector4.One, 
                    TexCoord = new Vector2((x + 1) * 0.5f, (y + 1) * 0.5f) 
                });
            }
            
            // 三角形索引
            for (int i = 1; i <= segments; i++)
            {
                indices.Add(0);
                indices.Add((uint)i);
                indices.Add((uint)(i % segments + 1));
            }
            
            return (vertices.ToArray(), indices.ToArray());
        }

        /// <summary>
        /// 开始渲染
        /// </summary>
        public void BeginRender()
        {
            if (_isRendering)
                throw new InvalidOperationException("渲染已经开始");
            
            _isRendering = true;
            
            // 启用混合
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            // 启用抗锯齿
            if (Config.EnableAntiAliasing)
            {
                GL.Enable(EnableCap.Multisample);
                GL.Enable(EnableCap.LineSmooth);
                GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            }
            
            // 设置线宽
            GL.LineWidth(Config.DefaultLineWidth);
        }

        /// <summary>
        /// 结束渲染
        /// </summary>
        public void EndRender()
        {
            if (!_isRendering)
                throw new InvalidOperationException("渲染尚未开始");
            
            _isRendering = false;
            
            GL.Disable(EnableCap.Blend);
            if (Config.EnableAntiAliasing)
            {
                GL.Disable(EnableCap.Multisample);
                GL.Disable(EnableCap.LineSmooth);
            }
        }

        /// <summary>
        /// 设置投影和视图矩阵
        /// </summary>
        public void SetMatrices(Matrix4 projection, Matrix4 view)
        {
            _projectionMatrix = projection;
            _viewMatrix = view;
        }

        /// <summary>
        /// 渲染点
        /// </summary>
        public void RenderPoint(OpenTK.Mathematics.Vector2 position, bool selected = false)
        {
            var color = selected ? Config.SelectedColor : Config.DefaultColor;
            var size = selected ? Config.SelectedPointSize : Config.DefaultPointSize;
            
            var transform = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position.X, position.Y, 0);
            
            UseShader("basic");
            SetUniforms("basic", transform, color);
            
            GL.BindVertexArray(_vertexArrays["point"]);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// 渲染线
        /// </summary>
        public void RenderLine((OpenTK.Mathematics.Vector2 start, OpenTK.Mathematics.Vector2 end) line, bool selected = false)
        {
            var color = selected ? Config.SelectedColor : Config.DefaultColor;
            var width = selected ? Config.SelectedLineWidth : Config.DefaultLineWidth;
            
            Vector2 direction = line.end - line.start;
            float length = direction.Length;
            float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            var transform = Matrix4.CreateScale(length, width, 1) * 
                           Matrix4.CreateRotationZ(angle) * 
                           Matrix4.CreateTranslation(line.start.X, line.start.Y, 0);
            
            UseShader("basic");
            SetUniforms("basic", transform, color);
            
            GL.BindVertexArray(_vertexArrays["line"]);
            GL.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// 渲染矩形
        /// </summary>
        public void RenderRectangle((OpenTK.Mathematics.Vector2 center, OpenTK.Mathematics.Vector2 size, float rotation) rect, bool selected = false)
        {
            var color = selected ? Config.SelectedColor : Config.DefaultColor;
            
            var transform = Matrix4.CreateScale(rect.size.X, rect.size.Y, 1) * 
                           Matrix4.CreateRotationZ(rect.rotation) * 
                           Matrix4.CreateTranslation(rect.center.X, rect.center.Y, 0);
            
            UseShader("basic");
            SetUniforms("basic", transform, color);
            
            GL.BindVertexArray(_vertexArrays["quad"]);
            
            if (Config.FillShapes)
            {
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                GL.DrawElements(PrimitiveType.LineLoop, 4, DrawElementsType.UnsignedInt, 0);
            }
        }

        /// <summary>
        /// 渲染圆形
        /// </summary>
        public void RenderCircle((OpenTK.Mathematics.Vector2 center, float radius) circle, bool selected = false)
        {
            var color = selected ? Config.SelectedColor : Config.DefaultColor;
            
            var transform = Matrix4.CreateScale(circle.radius * 2) * 
                           Matrix4.CreateTranslation(circle.center.X, circle.center.Y, 0);
            
            UseShader("circle");
            SetUniforms("circle", transform, color);
            
            // 设置圆形特定的uniform
            int program = (int)_shaderPrograms["circle"];
            GL.Uniform1(GL.GetUniformLocation(program, "uRadius"), circle.radius);
            GL.Uniform2(GL.GetUniformLocation(program, "uCenter"), circle.center);
            GL.Uniform1(GL.GetUniformLocation(program, "uFilled"), Config.FillShapes ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(program, "uLineWidth"), Config.DefaultLineWidth / circle.radius);
            
            GL.BindVertexArray(_vertexArrays["circle"]);
            GL.DrawElements(PrimitiveType.Triangles, 32 * 3, DrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// 渲染多边形
        /// </summary>
        public void RenderPolygon(OpenTK.Mathematics.Vector2[] vertices, bool selected = false)
        {
            if (vertices.Length < 3) return;
            
            var color = selected ? Config.SelectedColor : Config.DefaultColor;
            
            // 动态创建多边形几何体
            var polygonVertices = new List<Vertex2D>();
            var polygonIndices = new List<uint>();
            
            // 添加顶点
            for (int i = 0; i < vertices.Length; i++)
            {
                polygonVertices.Add(new Vertex2D 
                { 
                    Position = vertices[i], 
                    Color = new Vector4(color.X, color.Y, color.Z, color.W), 
                    TexCoord = Vector2.Zero 
                });
            }
            
            // 三角剖分（简单的扇形三角剖分）
            for (int i = 1; i < vertices.Length - 1; i++)
            {
                polygonIndices.Add(0);
                polygonIndices.Add((uint)i);
                polygonIndices.Add((uint)(i + 1));
            }
            
            // 创建临时缓冲区
            uint tempVAO = (uint)GL.GenVertexArray();
            uint tempVBO = (uint)GL.GenBuffer();
            uint tempEBO = (uint)GL.GenBuffer();
            
            GL.BindVertexArray(tempVAO);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, polygonVertices.Count * Vertex2D.SizeInBytes, 
                         polygonVertices.ToArray(), BufferUsageHint.DynamicDraw);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, tempEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, polygonIndices.Count * sizeof(uint), 
                         polygonIndices.ToArray(), BufferUsageHint.DynamicDraw);
            
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeInBytes, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            
            UseShader("basic");
            SetUniforms("basic", Matrix4.Identity, color);
            
            if (Config.FillShapes)
            {
                GL.DrawElements(PrimitiveType.Triangles, polygonIndices.Count, DrawElementsType.UnsignedInt, 0);
            }
            else
            {
                GL.DrawElements(PrimitiveType.LineLoop, vertices.Length, DrawElementsType.UnsignedInt, 0);
            }
            
            // 清理临时缓冲区
            GL.DeleteVertexArray(tempVAO);
            GL.DeleteBuffer(tempVBO);
            GL.DeleteBuffer(tempEBO);
        }

        /// <summary>
        /// 使用指定的着色器
        /// </summary>
        private void UseShader(string shaderName)
        {
            if (_shaderPrograms.TryGetValue(shaderName, out uint program))
            {
                GL.UseProgram(program);
            }
        }

        /// <summary>
        /// 设置着色器uniform变量
        /// </summary>
        private void SetUniforms(string shaderName, Matrix4 modelMatrix, Vector4 color)
        {
            if (_shaderPrograms.TryGetValue(shaderName, out uint program))
            {
                int projLoc = GL.GetUniformLocation((int)program, "uProjection");
                int viewLoc = GL.GetUniformLocation((int)program, "uView");
                int colorLoc = GL.GetUniformLocation((int)program, "vertexColor");
                
                if (projLoc >= 0)
                    GL.UniformMatrix4(projLoc, false, ref _projectionMatrix);
                if (viewLoc >= 0)
                    GL.UniformMatrix4(viewLoc, false, ref _viewMatrix);
                if (colorLoc >= 0)
                    GL.Uniform4(colorLoc, color);
            }
        }

        /// <summary>
        /// 渲染ROI形状
        /// </summary>
        public void RenderROIShape(ROIShape shape)
        {
            if (shape == null || !shape.IsVisible) return;
            
            switch (shape)
            {
                case PointROI point:
                    RenderPoint(new Vector2((float)point.Center.X, (float)point.Center.Y), shape.IsSelected);
                    break;
                    
                case LineROI line:
                    if (line.Points.Count >= 2)
                    {
                        for (int i = 0; i < line.Points.Count - 1; i++)
                        {
                            RenderLine((new Vector2((float)line.Points[i].X, (float)line.Points[i].Y), new Vector2((float)line.Points[i + 1].X, (float)line.Points[i + 1].Y)), shape.IsSelected);
                        }
                        if (line.IsClosed && line.Points.Count > 2)
                        {
                            RenderLine((new Vector2((float)line.Points[line.Points.Count - 1].X, (float)line.Points[line.Points.Count - 1].Y), new Vector2((float)line.Points[0].X, (float)line.Points[0].Y)), shape.IsSelected);
                        }
                    }
                    break;
                    
                case RectangleROI rect:
                    RenderRectangle((new Vector2((float)rect.Center.X, (float)rect.Center.Y), new Vector2((float)rect.Width, (float)rect.Height), (float)rect.Angle), shape.IsSelected);
                    break;
                    
                case CircleROI circle:
                    RenderCircle((new Vector2((float)circle.Center.X, (float)circle.Center.Y), (float)circle.Radius), shape.IsSelected);
                    break;
                    
                case PolygonROI polygon:
                    if (polygon.Vertices.Count >= 3)
                    {
                        var vertices = polygon.Vertices.Select(v => new Vector2((float)v.X, (float)v.Y)).ToArray();
                        RenderPolygon(vertices, shape.IsSelected);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 渲染ROI图层
        /// </summary>
        public void RenderROILayer(ROILayer layer)
        {
            if (layer == null || !layer.IsVisible) return;
            
            foreach (var shape in layer.Shapes)
            {
                RenderROIShape(shape);
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var program in _shaderPrograms.Values)
            {
                GL.DeleteProgram(program);
            }
            
            foreach (var vao in _vertexArrays.Values)
            {
                GL.DeleteVertexArray(vao);
            }
            
            foreach (var vbo in _vertexBuffers.Values)
            {
                GL.DeleteBuffer(vbo);
            }
            
            foreach (var ebo in _indexBuffers.Values)
            {
                GL.DeleteBuffer(ebo);
            }
        }
    }

    /// <summary>
    /// 2D顶点结构
    /// </summary>
    public struct Vertex2D
    {
        public Vector2 Position;
        public Vector4 Color;
        public Vector2 TexCoord;
        
        public static int SizeInBytes => 8 * sizeof(float);
    }

    /// <summary>
    /// 渲染配置
    /// </summary>
    public class RenderConfig
    {
        public Vector4 DefaultColor { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public Vector4 SelectedColor { get; set; } = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
        public float DefaultLineWidth { get; set; } = 2.0f;
        public float SelectedLineWidth { get; set; } = 3.0f;
        public float DefaultPointSize { get; set; } = 5.0f;
        public float SelectedPointSize { get; set; } = 7.0f;
        public bool EnableAntiAliasing { get; set; } = true;
        public bool FillShapes { get; set; } = false;
    }
}
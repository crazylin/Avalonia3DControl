using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Avalonia3DControl.Core;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Core.Lighting;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.Materials;

namespace Avalonia3DControl.Rendering.OpenGL
{
    /// <summary>
    /// OpenGL渲染器，负责所有OpenGL相关的渲染逻辑
    /// </summary>
    public class OpenGLRenderer : IDisposable
    {
        #region 私有字段
        private Dictionary<Model3D, ModelRenderData> _modelRenderData;
        private Dictionary<ShadingMode, int> _shaderPrograms;
        private int _defaultTexture = 0;
        private bool _isInitialized = false;
        #endregion

        #region 内部类
        private class ModelRenderData
        {
            public int VAO { get; set; }
            public int VBO { get; set; }
            public int EBO { get; set; }
        }
        #endregion

        #region 构造函数
        public OpenGLRenderer()
        {
            _modelRenderData = new Dictionary<Model3D, ModelRenderData>();
            _shaderPrograms = new Dictionary<ShadingMode, int>();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化OpenGL渲染器
        /// </summary>
        /// <param name="gl">Avalonia OpenGL接口</param>
        public void Initialize(Avalonia.OpenGL.GlInterface gl)
        {
            if (_isInitialized) return;
            
            // 初始化OpenTK绑定
            try
            {
                var context = new AvaloniaGLInterface(gl);
                OpenTK.Graphics.OpenGL4.GL.LoadBindings(context);
                
                // 检查OpenGL上下文是否可用
                var version = GL.GetString(StringName.Version);
                if (string.IsNullOrEmpty(version))
                {
                    throw new InvalidOperationException("OpenGL context is not available");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize OpenGL context: " + ex.Message, ex);
            }
            
            ConfigureOpenGLState();
            InitializeShaders();
            CreateDefaultTexture();
            _isInitialized = true;
        }

        private void ConfigureOpenGLState()
        {
            // 启用深度测试
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
            
            // 暂时禁用背面剔除进行调试
            GL.Disable(EnableCap.CullFace);
            
            // 启用混合
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private void InitializeShaders()
        {
            try
            {
                CreateVertexShader();
                CreateTextureShader();
                CreateMaterialShader();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"着色器初始化异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        private void CreateDefaultTexture()
        {
            _defaultTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _defaultTexture);
            
            // 创建1x1白色纹理作为默认纹理
            byte[] whitePixel = { 255, 255, 255, 255 };
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, whitePixel);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        #endregion

        #region 渲染方法
        /// <summary>
        /// 渲染场景
        /// </summary>
        /// <param name="camera">相机</param>
        /// <param name="models">模型列表</param>
        /// <param name="lights">光源列表</param>
        /// <param name="backgroundColor">背景色</param>
        /// <param name="shadingMode">着色模式</param>
        /// <param name="renderMode">渲染模式</param>
        public void RenderScene(Camera camera, List<Model3D> models, List<Light> lights, Vector3 backgroundColor, ShadingMode shadingMode, RenderMode renderMode)
        {
            if (!_isInitialized) return;
            
            // 清除缓冲区
            GL.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // 设置渲染模式
            SetRenderMode(renderMode);
            
            // 获取着色器程序
            if (!_shaderPrograms.TryGetValue(shadingMode, out int shaderProgram))
            {
                return; // 如果着色器不存在，跳过渲染
            }
            
            GL.UseProgram(shaderProgram);
            
            // 设置矩阵
            var viewMatrix = camera.GetViewMatrix();
            var projectionMatrix = camera.GetProjectionMatrix();
            
            SetMatrix(shaderProgram, "view", viewMatrix);
            SetMatrix(shaderProgram, "projection", projectionMatrix);
            
            // 光照已移除，不再需要设置
            
            // 渲染所有模型
            foreach (var model in models)
            {
                if (model.Visible)
                {
                    RenderModel(model, shaderProgram);
                }
            }
            
            GL.UseProgram(0);
        }
        
        /// <summary>
        /// 渲染场景（包含坐标轴）
        /// </summary>
        public void RenderSceneWithAxes(Camera camera, List<Model3D> models, List<Light> lights, Vector3 backgroundColor, ShadingMode shadingMode, RenderMode renderMode, Model3D? coordinateAxes = null, MiniAxes? miniAxes = null)
        {
            if (!_isInitialized) 
            {
                return;
            }
            
            // 清除缓冲区
            CheckGLError("清除缓冲区前");
            GL.ClearColor(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, 1.0f);
            CheckGLError("设置清除颜色后");
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            CheckGLError("清除缓冲区后");
            
            // 启用混合以支持透明度
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            CheckGLError("启用混合后");
            
            // 设置渲染模式
            SetRenderMode(renderMode);
            
            // 获取着色器程序
            if (!_shaderPrograms.TryGetValue(shadingMode, out int shaderProgram))
            {
                return; // 如果着色器不存在，跳过渲染
            }
            
            GL.UseProgram(shaderProgram);
            
            // 设置矩阵
            var viewMatrix = camera.GetViewMatrix();
            var projectionMatrix = camera.GetProjectionMatrix();
            
            SetMatrix(shaderProgram, "view", viewMatrix);
            SetMatrix(shaderProgram, "projection", projectionMatrix);
            
            // 设置光照
            // 光照已移除，不再需要设置
            
            // 渲染所有模型
            foreach (var model in models)
            {
                if (model.Visible)
                {
                    RenderModel(model, shaderProgram);
                }
            }
            
            // 渲染坐标轴（如果存在且可见）
            if (coordinateAxes != null && coordinateAxes.Visible)
            {
                // 坐标轴始终使用顶点着色器和填充模式渲染，不受全局着色模式和渲染模式影响
                if (_shaderPrograms.TryGetValue(ShadingMode.Vertex, out int axesShaderProgram))
                {
                    GL.UseProgram(axesShaderProgram);
                    
                    // 重新设置矩阵（因为切换了着色器）
                    SetMatrix(axesShaderProgram, "view", viewMatrix);
                    SetMatrix(axesShaderProgram, "projection", projectionMatrix);
                    
                    // 坐标轴始终使用填充模式渲染
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                    RenderModel(coordinateAxes, axesShaderProgram);
                    
                    // 恢复原始着色器和渲染模式
                    GL.UseProgram(shaderProgram);
                    SetRenderMode(renderMode);
                }
            }
            
            // 渲染迷你坐标轴（如果存在且可见）
            if (miniAxes != null && miniAxes.Visible && miniAxes.AxesModel != null)
            {
                RenderMiniAxes(miniAxes, camera, shaderProgram);
            }
            
            GL.UseProgram(0);
        }

        /// <summary>
        /// 渲染单个模型
        /// </summary>
        /// <param name="model">要渲染的模型</param>
        /// <param name="shaderProgram">着色器程序</param>
        private void RenderModel(Model3D model, int shaderProgram)
        {
            // 确保模型的渲染数据已创建
            if (!_modelRenderData.ContainsKey(model))
            {
                CreateModelRenderData(model);
            }
            
            var renderData = _modelRenderData[model];
            
            // 设置模型矩阵
            var modelMatrix = model.GetModelMatrix();
            SetMatrix(shaderProgram, "model", modelMatrix);
            
            // 设置材质属性
            if (model.Material != null)
            {
                // 设置透明度
                int alphaLocation = GL.GetUniformLocation(shaderProgram, "materialAlpha");
                if (alphaLocation != -1)
                {
                    GL.Uniform1(alphaLocation, model.Material.Alpha);
                }
                
                // 为材质着色器设置材质属性
                int ambientLocation = GL.GetUniformLocation(shaderProgram, "materialAmbient");
                if (ambientLocation != -1)
                {
                    GL.Uniform3(ambientLocation, model.Material.Ambient.X, model.Material.Ambient.Y, model.Material.Ambient.Z);
                }
                
                int diffuseLocation = GL.GetUniformLocation(shaderProgram, "materialDiffuse");
                if (diffuseLocation != -1)
                {
                    GL.Uniform3(diffuseLocation, model.Material.Diffuse.X, model.Material.Diffuse.Y, model.Material.Diffuse.Z);
                }
                
                int specularLocation = GL.GetUniformLocation(shaderProgram, "materialSpecular");
                if (specularLocation != -1)
                {
                    GL.Uniform3(specularLocation, model.Material.Specular.X, model.Material.Specular.Y, model.Material.Specular.Z);
                }
                
                int shininessLocation = GL.GetUniformLocation(shaderProgram, "materialShininess");
                if (shininessLocation != -1)
                {
                    GL.Uniform1(shininessLocation, model.Material.Shininess);
                }
            }
            else
            {
                // 如果没有材质，使用默认值
                int alphaLocation = GL.GetUniformLocation(shaderProgram, "materialAlpha");
                if (alphaLocation != -1)
                {
                    GL.Uniform1(alphaLocation, 1.0f);
                }
                
                // 为材质着色器设置默认材质属性
                int ambientLocation = GL.GetUniformLocation(shaderProgram, "materialAmbient");
                if (ambientLocation != -1)
                {
                    GL.Uniform3(ambientLocation, 0.2f, 0.2f, 0.2f);
                }
                
                int diffuseLocation = GL.GetUniformLocation(shaderProgram, "materialDiffuse");
                if (diffuseLocation != -1)
                {
                    GL.Uniform3(diffuseLocation, 0.8f, 0.8f, 0.8f);
                }
                
                int specularLocation = GL.GetUniformLocation(shaderProgram, "materialSpecular");
                if (specularLocation != -1)
                {
                    GL.Uniform3(specularLocation, 1.0f, 1.0f, 1.0f);
                }
                
                int shininessLocation = GL.GetUniformLocation(shaderProgram, "materialShininess");
                if (shininessLocation != -1)
                {
                    GL.Uniform1(shininessLocation, 32.0f);
                }
            }
            
            // 绑定VAO和VBO
            GL.BindVertexArray(renderData.VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, renderData.VBO);
            
            // 动态获取并设置顶点属性位置（新格式：位置3+颜色3=6个分量）
            int stride = 6 * sizeof(float);
            
            int positionLocation = GL.GetAttribLocation(shaderProgram, "aPosition");
            if (positionLocation != -1)
            {
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, stride, 0);
                GL.EnableVertexAttribArray(positionLocation);
            }
            
            int colorLocation = GL.GetAttribLocation(shaderProgram, "aColor");
            if (colorLocation != -1)
            {
                GL.VertexAttribPointer(colorLocation, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
                GL.EnableVertexAttribArray(colorLocation);
            }
            
            // 法向量和纹理坐标属性已移除，不再绑定
            
            // 绑定默认纹理到纹理单元0（避免OpenGL警告）
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _defaultTexture);
            
            // 设置纹理采样器uniform
            int textureLocation = GL.GetUniformLocation(shaderProgram, "texture0");
            if (textureLocation != -1)
            {
                GL.Uniform1(textureLocation, 0); // 绑定到纹理单元0
            }
            
            // 为纹理着色器设置hasTexture uniform
            int hasTextureLocation = GL.GetUniformLocation(shaderProgram, "hasTexture");
            if (hasTextureLocation != -1)
            {
                GL.Uniform1(hasTextureLocation, 0); // 暂时设为false，使用程序生成的棋盘格纹理
            }
            
            // 渲染
            GL.DrawElements(PrimitiveType.Triangles, model.IndexCount, DrawElementsType.UnsignedInt, 0);
            
            // 清理
            if (positionLocation != -1) GL.DisableVertexAttribArray(positionLocation);
            if (colorLocation != -1) GL.DisableVertexAttribArray(colorLocation);
            
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// 创建模型的渲染数据
        /// </summary>
        /// <param name="model">模型</param>
        private void CreateModelRenderData(Model3D model)
        {
            var renderData = new ModelRenderData();
            
            // 生成VAO, VBO, EBO
            renderData.VAO = GL.GenVertexArray();
            renderData.VBO = GL.GenBuffer();
            renderData.EBO = GL.GenBuffer();
            
            GL.BindVertexArray(renderData.VAO);
            
            // 绑定顶点缓冲区
            GL.BindBuffer(BufferTarget.ArrayBuffer, renderData.VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, model.Vertices.Length * sizeof(float), model.Vertices, BufferUsageHint.StaticDraw);
            
            // 绑定索引缓冲区
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, model.Indices.Length * sizeof(uint), model.Indices, BufferUsageHint.StaticDraw);
            
            GL.BindVertexArray(0);
            
            _modelRenderData[model] = renderData;
        }
        #endregion

        #region 辅助方法
        private void SetRenderMode(RenderMode renderMode)
        {
            switch (renderMode)
            {
                case RenderMode.Fill:
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                    break;
                case RenderMode.Line:
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                    GL.LineWidth(3.0f); // 增加线条粗细
                    break;
                case RenderMode.Point:
                    GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Point);
                    GL.PointSize(5.0f); // 增加点的大小
                    break;
            }
        }

        private void SetMatrix(int shaderProgram, string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(shaderProgram, name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, false, ref matrix);
            }
        }

        private void SetLighting(int shaderProgram, List<Light> lights, Vector3 viewPos)
        {
            // 设置观察位置
            int viewPosLocation = GL.GetUniformLocation(shaderProgram, "viewPos");
            if (viewPosLocation != -1)
            {
                GL.Uniform3(viewPosLocation, viewPos);
            }
            
            // 设置主光源（使用第一个方向光或点光源）
            if (lights.Count > 0)
            {
                var mainLight = lights[0];
                
                int lightColorLocation = GL.GetUniformLocation(shaderProgram, "lightColor");
                if (lightColorLocation != -1)
                {
                    GL.Uniform3(lightColorLocation, mainLight.Color * mainLight.Intensity);
                }
                
                if (mainLight is DirectionalLight dirLight)
                {
                    // 对于方向光，我们将其转换为一个远距离的点光源位置
                    // 这样可以兼容使用lightPos的着色器
                    Vector3 lightPos = viewPos - dirLight.Direction * 100.0f;
                    
                    int lightPosLocation = GL.GetUniformLocation(shaderProgram, "lightPos");
                    if (lightPosLocation != -1)
                    {
                        GL.Uniform3(lightPosLocation, lightPos);
                    }
                    
                    // 同时也设置lightDir，以兼容可能使用方向光的着色器
                    int lightDirLocation = GL.GetUniformLocation(shaderProgram, "lightDir");
                    if (lightDirLocation != -1)
                    {
                        GL.Uniform3(lightDirLocation, dirLight.Direction);
                    }
                }
                else if (mainLight is PointLight pointLight)
                {
                    int lightPosLocation = GL.GetUniformLocation(shaderProgram, "lightPos");
                    if (lightPosLocation != -1)
                    {
                        GL.Uniform3(lightPosLocation, pointLight.Position);
                    }
                }
            }
        }

        private void SetMaterial(int shaderProgram, Material material)
        {
            int ambientLocation = GL.GetUniformLocation(shaderProgram, "materialAmbient");
            if (ambientLocation != -1)
            {
                GL.Uniform3(ambientLocation, material.Ambient);
            }
            
            int diffuseLocation = GL.GetUniformLocation(shaderProgram, "materialDiffuse");
            if (diffuseLocation != -1)
            {
                GL.Uniform3(diffuseLocation, material.Diffuse);
            }
            
            int specularLocation = GL.GetUniformLocation(shaderProgram, "materialSpecular");
            if (specularLocation != -1)
            {
                GL.Uniform3(specularLocation, material.Specular);
            }
            
            int shininessLocation = GL.GetUniformLocation(shaderProgram, "materialShininess");
            if (shininessLocation != -1)
            {
                GL.Uniform1(shininessLocation, material.Shininess);
            }
        }
        #endregion

        #region OpenGL错误检查
        private void CheckGLError(string operation)
        {
            ErrorCode error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine($"OpenGL错误 ({operation}): {error}");
            }
        }
        #endregion

        #region 着色器创建方法






        private void CreateVertexShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;
attribute vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

varying vec3 vertexColor;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    vertexColor = aColor;
}
";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec3 vertexColor;

uniform float materialAlpha;

void main()
{
    gl_FragColor = vec4(vertexColor, materialAlpha);
}
";

            _shaderPrograms[ShadingMode.Vertex] = CompileShaderProgram(vertexSource, fragmentSource);
        }

        private void CreateTextureShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;
attribute vec3 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

varying vec3 Color;
varying vec2 TexCoord;

void main()
{
    Color = aColor;
    TexCoord = (aPosition.xy + 1.0) * 0.5;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec3 Color;
varying vec2 TexCoord;

uniform bool hasTexture;
uniform sampler2D texture0;
uniform float materialAlpha;

void main()
{
    vec3 textureColor;
    
    if (hasTexture) {
        textureColor = texture2D(texture0, TexCoord).rgb;
    } else {
        float scale = 8.0;
        vec2 scaledCoord = TexCoord * scale;
        vec2 grid = floor(scaledCoord);
        float checker = mod(grid.x + grid.y, 2.0);
        textureColor = mix(vec3(0.8, 0.8, 0.8), vec3(0.2, 0.2, 0.2), checker);
    }
    
    gl_FragColor = vec4(textureColor * Color, materialAlpha);
}";

            _shaderPrograms[ShadingMode.Texture] = CompileShaderProgram(vertexSource, fragmentSource);
        }

        private void CreateMaterialShader()
        {
            try
            {
                Console.WriteLine("开始创建材质着色器...");
                string vertexSource = "#version 100\n" +
                    "precision highp float;\n" +
                    "attribute vec3 aPosition;\n" +
                    "attribute vec3 aColor;\n" +
                    "uniform mat4 model;\n" +
                    "uniform mat4 view;\n" +
                    "uniform mat4 projection;\n" +
                    "varying vec3 vertexColor;\n" +
                    "varying vec3 worldPos;\n" +
                    "varying vec3 normal;\n" +
                    "void main() {\n" +
                    "    vec4 worldPosition = model * vec4(aPosition, 1.0);\n" +
                    "    worldPos = worldPosition.xyz;\n" +
                    "    normal = normalize(mat3(model) * vec3(0.0, 0.0, 1.0));\n" +
                    "    gl_Position = projection * view * worldPosition;\n" +
                    "    vertexColor = aColor;\n" +
                    "}\n";



            string fragmentSource = "#version 100\n" +
                "precision highp float;\n" +
                "varying vec3 vertexColor;\n" +
                "varying vec3 worldPos;\n" +
                "varying vec3 normal;\n" +
                "uniform vec3 materialAmbient;\n" +
                "uniform vec3 materialDiffuse;\n" +
                "uniform vec3 materialSpecular;\n" +
                "uniform float materialShininess;\n" +
                "uniform float materialAlpha;\n" +
                "void main() {\n" +
                "    vec3 norm = normalize(normal);\n" +
                "    vec3 lightDir1 = normalize(vec3(1.0, 1.0, 1.0));\n" +
                "    vec3 lightDir2 = normalize(vec3(-0.5, 0.5, -0.5));\n" +
                "    vec3 lightColor = vec3(0.9, 0.9, 0.9);\n" +
                "    vec3 ambientLight = vec3(0.7, 0.7, 0.7);\n" +
                "    \n" +
                "    vec3 ambient = ambientLight * materialAmbient;\n" +
                "    \n" +
                "    float diff1 = max(dot(norm, lightDir1), 0.0);\n" +
                "    float diff2 = max(dot(norm, lightDir2), 0.0);\n" +
                "    vec3 diffuse = (diff1 + diff2 * 0.5) * lightColor * materialDiffuse;\n" +
                "    \n" +
                "    \n" +
                "    // Specular reflection calculation\n" +
                "    vec3 viewDir = normalize(vec3(0.0, 0.0, 1.0));\n" +
                "    vec3 reflectDir1 = reflect(-lightDir1, norm);\n" +
                "    vec3 reflectDir2 = reflect(-lightDir2, norm);\n" +
                "    float spec1 = pow(max(dot(viewDir, reflectDir1), 0.0), max(materialShininess, 1.0));\n" +
                "    float spec2 = pow(max(dot(viewDir, reflectDir2), 0.0), max(materialShininess, 1.0));\n" +
                "    vec3 specular = (spec1 + spec2 * 0.5) * lightColor * materialSpecular;\n" +
                "    \n" +
                "    vec3 result = ambient + diffuse + specular;\n" +
                "    gl_FragColor = vec4(result, materialAlpha);\n" +
                "}\n";

                _shaderPrograms[ShadingMode.Material] = CompileShaderProgram(vertexSource, fragmentSource);
                Console.WriteLine("材质着色器创建成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"材质着色器创建异常: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        private int CompileShaderProgram(string vertexSource, string fragmentSource)
        {
            // 调试输出已移除
            
            // 编译顶点着色器
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            
            // 检查编译错误
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexSuccess);
            if (vertexSuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                throw new Exception($"顶点着色器编译失败: {infoLog}");
            }
            
            // 编译片段着色器
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);
            
            // 检查编译错误
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentSuccess);
            if (fragmentSuccess == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                throw new Exception($"片段着色器编译失败: {infoLog}");
            }
            
            // 创建着色器程序
            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            
            // 检查链接错误
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linkSuccess);
            if (linkSuccess == 0)
            {
                string infoLog = GL.GetProgramInfoLog(shaderProgram);
                throw new Exception($"着色器程序链接失败: {infoLog}");
            }
            
            // 清理着色器对象
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            
            return shaderProgram;
        }
        #endregion

        #region 清理方法
        /// <summary>
        /// 清理OpenGL资源
        /// </summary>
        public void Cleanup()
        {
            // 清理模型渲染数据
            foreach (var renderData in _modelRenderData.Values)
            {
                GL.DeleteVertexArray(renderData.VAO);
                GL.DeleteBuffer(renderData.VBO);
                GL.DeleteBuffer(renderData.EBO);
            }
            _modelRenderData.Clear();
            
            // 清理着色器程序
            foreach (var shaderProgram in _shaderPrograms.Values)
            {
                GL.DeleteProgram(shaderProgram);
            }
            _shaderPrograms.Clear();
            
            // 清理纹理
            if (_defaultTexture != 0)
            {
                GL.DeleteTexture(_defaultTexture);
                _defaultTexture = 0;
            }
            
            _isInitialized = false;
        }
        #endregion

        /// <summary>
        /// 渲染迷你坐标轴
        /// </summary>
        /// <param name="miniAxes">迷你坐标轴对象</param>
        /// <param name="camera">相机</param>
        /// <param name="shaderProgram">着色器程序</param>
        private void RenderMiniAxes(MiniAxes miniAxes, Camera camera, int shaderProgram)
        {
            if (miniAxes.AxesModel == null) return;
            
            // 迷你坐标轴始终使用顶点着色器
            if (!_shaderPrograms.TryGetValue(ShadingMode.Vertex, out int miniAxesShaderProgram))
            {
                return; // 如果顶点着色器不存在，跳过渲染
            }
            
            // 保存当前的视口设置
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            int screenWidth = viewport[2];
            int screenHeight = viewport[3];
            
            // 获取迷你坐标轴在屏幕上的位置
            Vector2 screenPos = miniAxes.GetScreenPosition(screenWidth, screenHeight);
            
            // 设置小视口用于渲染迷你坐标轴
            int miniViewportSize = 150; // 迷你坐标轴视口大小
            int miniX = Math.Max(0, (int)(screenPos.X - miniViewportSize / 2));
            int miniY = Math.Max(0, (int)(screenHeight - screenPos.Y - miniViewportSize / 2)); // OpenGL坐标系Y轴翻转
            
            // 确保视口不超出屏幕边界
            miniX = Math.Min(miniX, screenWidth - miniViewportSize);
            miniY = Math.Min(miniY, screenHeight - miniViewportSize);
            
            // 保存原始视口
            int[] originalViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, originalViewport);
            
            GL.Viewport(miniX, miniY, miniViewportSize, miniViewportSize);
            
            // 切换到顶点着色器
            GL.UseProgram(miniAxesShaderProgram);
            
            // 创建迷你坐标轴的投影矩阵（正交投影，扩大视场范围避免截取）
            Matrix4 miniProjection = Matrix4.CreateOrthographic(3.5f, 3.5f, 0.1f, 10.0f);
            
            // 创建迷你坐标轴的视图矩阵（跟随主相机的旋转）
            Vector3 cameraDirection = Vector3.Normalize(camera.Target - camera.Position);
            Vector3 cameraUp = camera.Up;
            Vector3 miniCameraPos = -cameraDirection * 3.0f; // 距离原点3个单位
            Matrix4 miniView = Matrix4.LookAt(miniCameraPos, Vector3.Zero, cameraUp);
            
            // 设置迷你坐标轴的矩阵
            SetMatrix(miniAxesShaderProgram, "view", miniView);
            SetMatrix(miniAxesShaderProgram, "projection", miniProjection);
            
            // 强制使用填充模式渲染迷你坐标轴
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            
            // 渲染迷你坐标轴模型
            RenderModel(miniAxes.AxesModel, miniAxesShaderProgram);
            
            // 禁用深度测试以确保标注可见
            GL.Disable(EnableCap.DepthTest);
            
            // 渲染XYZ标注
            RenderAxisLabels(miniAxesShaderProgram, miniView, miniProjection);
            
            // 重新启用深度测试
            GL.Enable(EnableCap.DepthTest);
            
            // 恢复原始视口和着色器
            GL.Viewport(originalViewport[0], originalViewport[1], originalViewport[2], originalViewport[3]);
            GL.UseProgram(shaderProgram); // 恢复原始着色器
        }
        
        /// <summary>
        /// 渲染坐标轴标注（XYZ）
        /// </summary>
        private void RenderAxisLabels(int shaderProgram, Matrix4 view, Matrix4 projection)
        {
            // 设置线条渲染模式，增加线宽使标注更清晰
            GL.LineWidth(6.0f);
            
            // 标注位置（在坐标轴末端附近）
            float labelDistance = 0.9f;
            Vector3[] labelPositions = {
                new Vector3(labelDistance, 0, 0), // X标注位置
                new Vector3(0, labelDistance, 0), // Y标注位置
                new Vector3(0, 0, labelDistance)  // Z标注位置
            };
            
            Vector3[] labelColors = {
                new Vector3(1.0f, 0.0f, 0.0f), // X - 红色
                new Vector3(0.0f, 1.0f, 0.0f), // Y - 绿色
                new Vector3(0.0f, 0.0f, 1.0f)  // Z - 蓝色
            };
            
            // 渲染每个标注
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = labelPositions[i];
                Vector3 color = labelColors[i];
                
                // 设置模型矩阵（移动到标注位置）
                Matrix4 labelModel = Matrix4.CreateTranslation(position);
                SetMatrix(shaderProgram, "model", labelModel);
                
                // 根据轴绘制对应的字母
                switch (i)
                {
                    case 0: // X
                        DrawLetterX(color);
                        break;
                    case 1: // Y
                        DrawLetterY(color);
                        break;
                    case 2: // Z
                        DrawLetterZ(color);
                        break;
                }
            }
            
            GL.LineWidth(1.0f); // 恢复默认线宽
        }
        
        /// <summary>
        /// 绘制字母X
        /// </summary>
        private void DrawLetterX(Vector3 color)
        {
            float size = 0.15f;
            float[] vertices = {
                // 第一条对角线
                -size, -size, 0, color.X, color.Y, color.Z,
                 size,  size, 0, color.X, color.Y, color.Z,
                // 第二条对角线
                -size,  size, 0, color.X, color.Y, color.Z,
                 size, -size, 0, color.X, color.Y, color.Z
            };
            
            DrawLines(vertices, 4);
        }
        
        /// <summary>
        /// 绘制字母Y
        /// </summary>
        private void DrawLetterY(Vector3 color)
        {
            float size = 0.15f;
            float[] vertices = {
                // 左上到中心
                -size,  size, 0, color.X, color.Y, color.Z,
                    0,     0, 0, color.X, color.Y, color.Z,
                // 右上到中心
                 size,  size, 0, color.X, color.Y, color.Z,
                    0,     0, 0, color.X, color.Y, color.Z,
                // 中心到下方
                    0,     0, 0, color.X, color.Y, color.Z,
                    0, -size, 0, color.X, color.Y, color.Z
            };
            
            DrawLines(vertices, 6);
        }
        
        /// <summary>
        /// 绘制字母Z
        /// </summary>
        private void DrawLetterZ(Vector3 color)
        {
            float size = 0.15f;
            float[] vertices = {
                // 上横线
                -size,  size, 0, color.X, color.Y, color.Z,
                 size,  size, 0, color.X, color.Y, color.Z,
                // 对角线
                 size,  size, 0, color.X, color.Y, color.Z,
                -size, -size, 0, color.X, color.Y, color.Z,
                // 下横线
                -size, -size, 0, color.X, color.Y, color.Z,
                 size, -size, 0, color.X, color.Y, color.Z
            };
            
            DrawLines(vertices, 6);
        }
        
        /// <summary>
        /// 绘制线条
        /// </summary>
        private void DrawLines(float[] vertices, int vertexCount)
        {
            // 创建临时VAO和VBO
            uint vao = (uint)GL.GenVertexArray();
            uint vbo = (uint)GL.GenBuffer();
            
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            
            // 设置顶点属性
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            // 绘制线条
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
            
            // 清理
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vao);
            GL.DeleteBuffer(vbo);
        }
        
        #region IDisposable实现
        public void Dispose()
        {
            // 清理所有模型的渲染数据
            foreach (var renderData in _modelRenderData.Values)
            {
                GL.DeleteVertexArray(renderData.VAO);
                GL.DeleteBuffer(renderData.VBO);
                GL.DeleteBuffer(renderData.EBO);
            }
            _modelRenderData.Clear();
            
            // 清理所有着色器程序
            foreach (var shaderProgram in _shaderPrograms.Values)
            {
                GL.DeleteProgram(shaderProgram);
            }
            _shaderPrograms.Clear();
            
            // 清理纹理资源
            if (_defaultTexture != 0)
            {
                GL.DeleteTexture(_defaultTexture);
                _defaultTexture = 0;
            }
        }
        #endregion
    }
}
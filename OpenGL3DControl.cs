using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia3DControl
{
    /// <summary>
    /// 基础的三维OpenGL控件，继承自OpenGlControlBase
    /// </summary>
    public class OpenGL3DControl : OpenGlControlBase, ICustomHitTest
    {
        #region 常量定义
        private const float ROTATION_SENSITIVITY = 0.01f;
        private const float TRANSLATION_SENSITIVITY = 0.005f;
        private const float ZOOM_SENSITIVITY = 0.1f;
        private const float MIN_ZOOM = 0.2f;
        private const float MAX_ZOOM = 10.0f;
        private const float CAMERA_DISTANCE = 3.0f;
        private const float ROTATION_LIMIT_OFFSET = 0.1f;
        #endregion

        #region 私有字段
        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;
        private float _zoom = 1.0f;
        private Vector2 _lastMousePosition;
        private bool _isMousePressed = false;
        private bool _isRightMousePressed = false;
        private Vector3 _cameraOffset = Vector3.Zero;

        // OpenGL资源
        private Dictionary<Model3D, ModelRenderData> _modelRenderData;
        private Dictionary<ShadingMode, int> _shaderPrograms;
        private ShadingMode _currentShadingMode = ShadingMode.Vertex;
        private RenderMode _currentRenderMode = RenderMode.Fill;
        private bool _isOpenGLInitialized = false;
        
        // 纹理资源
        private int _defaultTexture = 0;
        #endregion

        #region 公共属性
        /// <summary>
        /// 3D场景管理器
        /// </summary>
        public Scene3D Scene { get; private set; }
        #endregion
        
        #region 构造函数
        public OpenGL3DControl()
        {
            InitializeComponent();
            InitializeScene();
        }

        private void InitializeComponent()
        {
            Scene = new Scene3D();
            _modelRenderData = new Dictionary<Model3D, ModelRenderData>();
            _shaderPrograms = new Dictionary<ShadingMode, int>();
            
            // 确保控件可以接收焦点和输入事件
            Focusable = true;
            IsHitTestVisible = true;
            
            // 设置控件布局属性，确保填充整个容器
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
        }

        private void InitializeScene()
        {
            // 场景初始化为空，模型将按需动态加载
        }
        #endregion
        
        #region 内部类
        private class ModelRenderData
        {
            public int VAO { get; set; }
            public int VBO { get; set; }
            public int EBO { get; set; }
        }
        #endregion

        #region OpenGL初始化和清理
        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            
            InitializeOpenTK(gl);
            ConfigureOpenGLState();
            CreateShaderProgram();
            CreateDefaultTexture();
            _isOpenGLInitialized = true;
            // 不再预加载所有模型，改为按需加载
        }

        private void InitializeOpenTK(GlInterface gl)
        {
            try
            {
                // 初始化OpenTK绑定
                var glInterface = new AvaloniaGLInterface(gl);
                OpenTK.Graphics.OpenGL4.GL.LoadBindings(glInterface);
            }
            catch (Exception ex)
            {
                // 如果OpenTK初始化失败，记录错误但继续
                System.Diagnostics.Debug.WriteLine($"OpenTK initialization failed: {ex.Message}");
            }
        }

        private void ConfigureOpenGLState()
        {
            // 启用深度测试
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f); // 更深的背景色以便看到模型
            
            // 暂时禁用背面剔除来测试渲染问题
            GL.Disable(EnableCap.CullFace);
            
            // 启用混合
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }


        #endregion

        #region 着色器和缓冲区管理
        private void CreateShaderProgram()
        {
            // 创建不同着色模式的着色器程序
            CreateFlatShader();
            CreateGouraudShader();
            CreatePhongShader();
            CreateWireframeShader();
            CreateVertexShader();
            CreateTextureShader();
        }
        
        private void CreateFlatShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;
attribute vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 materialAmbient;
uniform vec3 materialDiffuse;

varying vec3 flatColor;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    
    vec3 normal = normalize(mat3(model) * aNormal);
    float diff = max(dot(normal, -lightDir), 0.0);
    
    vec3 ambient = materialAmbient * 0.3;
    vec3 diffuse = materialDiffuse * lightColor * diff;
    flatColor = ambient + diffuse;
}
";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec3 flatColor;

void main()
{
    gl_FragColor = vec4(flatColor, 1.0);
}
";

            _shaderPrograms[ShadingMode.Flat] = CompileShaderProgram(vertexSource, fragmentSource);
        }
        
        private void CreateGouraudShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;
attribute vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 materialAmbient;
uniform vec3 materialDiffuse;
uniform vec3 materialSpecular;
uniform float materialShininess;
uniform vec3 viewPos;

varying vec3 gouraudColor;

void main()
{
    vec4 worldPos = model * vec4(aPosition, 1.0);
    gl_Position = projection * view * worldPos;
    
    vec3 normal = normalize(mat3(model) * aNormal);
    vec3 lightDirection = normalize(-lightDir);
    vec3 viewDirection = normalize(viewPos - worldPos.xyz);
    vec3 reflectDirection = reflect(-lightDirection, normal);
    
    vec3 ambient = materialAmbient * 0.3;
    vec3 diffuse = materialDiffuse * lightColor * max(dot(normal, lightDirection), 0.0);
    vec3 specular = materialSpecular * lightColor * pow(max(dot(viewDirection, reflectDirection), 0.0), materialShininess);
    
    gouraudColor = ambient + diffuse + specular;
}
";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec3 gouraudColor;

void main()
{
    gl_FragColor = vec4(gouraudColor, 1.0);
}
";

            _shaderPrograms[ShadingMode.Gouraud] = CompileShaderProgram(vertexSource, fragmentSource);
        }
        
        private void CreatePhongShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;
attribute vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

varying vec3 fragPos;
varying vec3 fragNormal;

void main()
{
    vec4 worldPos = model * vec4(aPosition, 1.0);
    gl_Position = projection * view * worldPos;
    
    fragPos = worldPos.xyz;
    fragNormal = mat3(model) * aNormal;
}
";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec3 fragPos;
varying vec3 fragNormal;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 materialAmbient;
uniform vec3 materialDiffuse;
uniform vec3 materialSpecular;
uniform float materialShininess;
uniform vec3 viewPos;

void main()
{
    vec3 normal = normalize(fragNormal);
    vec3 lightDirection = normalize(-lightDir);
    vec3 viewDirection = normalize(viewPos - fragPos);
    vec3 reflectDirection = reflect(-lightDirection, normal);
    
    vec3 ambient = materialAmbient * 0.3;
    vec3 diffuse = materialDiffuse * lightColor * max(dot(normal, lightDirection), 0.0);
    vec3 specular = materialSpecular * lightColor * pow(max(dot(viewDirection, reflectDirection), 0.0), materialShininess);
    
    gl_FragColor = vec4(ambient + diffuse + specular, 1.0);
}
";

            _shaderPrograms[ShadingMode.Phong] = CompileShaderProgram(vertexSource, fragmentSource);
        }
        
        private void CreateWireframeShader()
        {
            string vertexSource = @"
#version 100
precision highp float;
attribute vec3 aPosition;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}
";

            string fragmentSource = @"
#version 100
precision highp float;
uniform vec3 wireframeColor;

void main()
{
    gl_FragColor = vec4(wireframeColor, 1.0);
}
";

            _shaderPrograms[ShadingMode.Wireframe] = CompileShaderProgram(vertexSource, fragmentSource);
        }
        
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

void main()
{
    gl_FragColor = vec4(vertexColor, 1.0);
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
attribute vec2 aTexCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

varying vec2 texCoord;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
    texCoord = aTexCoord;
}
";

            string fragmentSource = @"
#version 100
precision highp float;
varying vec2 texCoord;

uniform sampler2D uTexture;

void main()
{
    gl_FragColor = texture2D(uTexture, texCoord);
}
";

            _shaderPrograms[ShadingMode.Texture] = CompileShaderProgram(vertexSource, fragmentSource);
        }
        
        private void CreateDefaultTexture()
        {
            // 创建一个简单的棋盘格纹理
            int width = 256;
            int height = 256;
            byte[] textureData = new byte[width * height * 3];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * 3;
                    bool isWhite = ((x / 32) + (y / 32)) % 2 == 0;
                    byte color = (byte)(isWhite ? 255 : 128);
                    textureData[index] = color;     // R
                    textureData[index + 1] = color; // G
                    textureData[index + 2] = color; // B
                }
            }
            
            // 生成纹理
            _defaultTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _defaultTexture);
            
            // 上传纹理数据
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType.UnsignedByte, textureData);
            
            // 设置纹理参数
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        private int CompileShaderProgram(string vertexSource, string fragmentSource)
        {
            // 编译顶点着色器
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            
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


        
        private void CreateModelBuffers(Model3D model)
        {
            // 检查OpenGL上下文是否可用
            if (!_isOpenGLInitialized)
            {
                return;
            }
            
            var renderData = new ModelRenderData();
            
            // 生成VAO, VBO, EBO
            renderData.VAO = GL.GenVertexArray();
            renderData.VBO = GL.GenBuffer();
            renderData.EBO = GL.GenBuffer();
            
            // 检查缓冲区是否成功生成
            if (renderData.VAO == 0 || renderData.VBO == 0 || renderData.EBO == 0)
            {
                return;
            }

            GL.BindVertexArray(renderData.VAO);

            // 绑定VBO并上传顶点数据
            GL.BindBuffer(BufferTarget.ArrayBuffer, renderData.VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, model.Vertices.Length * sizeof(float), model.Vertices, BufferUsageHint.StaticDraw);

            // 绑定EBO并上传索引数据
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, model.Indices.Length * sizeof(uint), model.Indices, BufferUsageHint.StaticDraw);

            // 暂时不设置顶点属性，在渲染时动态设置
            // 这样可以避免在初始化时依赖特定的着色器程序

            GL.BindVertexArray(0);
            
            _modelRenderData[model] = renderData;
        }
        #endregion

        #region 渲染方法
        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            // 设置视口 - 自动适应DPI缩放
            var topLevel = TopLevel.GetTopLevel(this);
            var scaling = topLevel?.RenderScaling ?? 1.0;
            int viewportWidth = (int)(Bounds.Width * scaling);
            int viewportHeight = (int)(Bounds.Height * scaling);
            GL.Viewport(0, 0, viewportWidth, viewportHeight);
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // 使用当前着色模式对应的着色器程序
            int currentShaderProgram = _shaderPrograms[_currentShadingMode];
            GL.UseProgram(currentShaderProgram);

            // 更新相机
            Scene.Camera.AspectRatio = (float)Bounds.Width / (float)Bounds.Height;
            
            // 计算相机位置：围绕目标点旋转
            var basePosition = new Vector3(0.0f, 0.0f, CAMERA_DISTANCE * _zoom);
            var rotationMatrix = Matrix4.CreateRotationX(_rotationX) * Matrix4.CreateRotationY(_rotationY);
            var rotatedPosition = Vector3.TransformPosition(basePosition, rotationMatrix);
            Scene.Camera.Position = rotatedPosition + Scene.Camera.Target + _cameraOffset;
            

            


            // 获取矩阵
            var view = Scene.Camera.GetViewMatrix();
            var projection = Scene.Camera.GetProjectionMatrix();

            // 设置uniform变量
            int viewLoc = GL.GetUniformLocation(currentShaderProgram, "view");
            int projLoc = GL.GetUniformLocation(currentShaderProgram, "projection");
            int modelLoc = GL.GetUniformLocation(currentShaderProgram, "model");

            if (viewLoc >= 0) GL.UniformMatrix4(viewLoc, false, ref view);
            if (projLoc >= 0) GL.UniformMatrix4(projLoc, false, ref projection);

            // 设置光照和材质uniform变量（除线框模式外）
            if (_currentShadingMode != ShadingMode.Wireframe && _currentShadingMode != ShadingMode.Vertex)
            {
                var light = Scene.Lights.FirstOrDefault() as DirectionalLight;
                if (light != null)
                {
                    int lightDirLoc = GL.GetUniformLocation(currentShaderProgram, "lightDir");
                    int lightColorLoc = GL.GetUniformLocation(currentShaderProgram, "lightColor");
                    int viewPosLoc = GL.GetUniformLocation(currentShaderProgram, "viewPos");
                    
                    if (lightDirLoc >= 0) GL.Uniform3(lightDirLoc, light.Direction.X, light.Direction.Y, light.Direction.Z);
                    if (lightColorLoc >= 0) GL.Uniform3(lightColorLoc, light.Color.X, light.Color.Y, light.Color.Z);
                    if (viewPosLoc >= 0) GL.Uniform3(viewPosLoc, Scene.Camera.Position.X, Scene.Camera.Position.Y, Scene.Camera.Position.Z);
                }
            }

            
            // 渲染所有模型
            foreach (var model in Scene.Models)
            {
                if (model.Visible)
                {
                    // 如果模型没有渲染数据，先创建
                    if (!_modelRenderData.ContainsKey(model))
                    {
                        CreateModelBuffers(model);
                    }
                    
                    // 检查是否成功创建了渲染数据
                    if (!_modelRenderData.ContainsKey(model))
                    {
                        continue;
                    }
                    var renderData = _modelRenderData[model];
                    var modelMatrix = model.GetModelMatrix();
                    
                    if (modelLoc >= 0) 
                     {
                         GL.UniformMatrix4(modelLoc, false, ref modelMatrix);
                     }
                    
                    // 设置材质uniform变量
                    if (_currentShadingMode != ShadingMode.Wireframe && _currentShadingMode != ShadingMode.Vertex && model.Material != null)
                    {
                        int ambientLoc = GL.GetUniformLocation(currentShaderProgram, "materialAmbient");
                        int diffuseLoc = GL.GetUniformLocation(currentShaderProgram, "materialDiffuse");
                        int specularLoc = GL.GetUniformLocation(currentShaderProgram, "materialSpecular");
                        int shininessLoc = GL.GetUniformLocation(currentShaderProgram, "materialShininess");
                        
                        if (ambientLoc >= 0) GL.Uniform3(ambientLoc, model.Material.Ambient.X, model.Material.Ambient.Y, model.Material.Ambient.Z);
                        if (diffuseLoc >= 0) GL.Uniform3(diffuseLoc, model.Material.Diffuse.X, model.Material.Diffuse.Y, model.Material.Diffuse.Z);
                        if (specularLoc >= 0) GL.Uniform3(specularLoc, model.Material.Specular.X, model.Material.Specular.Y, model.Material.Specular.Z);
                         if (shininessLoc >= 0) GL.Uniform1(shininessLoc, model.Material.Shininess);
                    }
                    else if (_currentShadingMode == ShadingMode.Wireframe)
                     {
                         int wireframeColorLoc = GL.GetUniformLocation(currentShaderProgram, "wireframeColor");
                         if (wireframeColorLoc >= 0) GL.Uniform3(wireframeColorLoc, 1.0f, 1.0f, 1.0f); // 白色线框
                     }
                    else if (_currentShadingMode == ShadingMode.Texture)
                    {
                        // 绑定纹理
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, _defaultTexture);
                        int textureLoc = GL.GetUniformLocation(currentShaderProgram, "uTexture");
                         if (textureLoc >= 0) GL.Uniform1(textureLoc, 0);
                     }
                     
                     // 绑定VAO并设置顶点属性
                     GL.BindVertexArray(renderData.VAO);
                    
                    // 动态设置顶点属性指针
                    int positionLoc = GL.GetAttribLocation(currentShaderProgram, "aPosition");
                    if (positionLoc >= 0)
                    {
                        if (_currentShadingMode == ShadingMode.Texture)
                        {
                            GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                        }
                        else
                        {
                            GL.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                        }
                        GL.EnableVertexAttribArray(positionLoc);
                    }
                    
                    // 根据着色模式设置不同的顶点属性
                    if (_currentShadingMode == ShadingMode.Texture)
                    {
                        int texCoordLoc = GL.GetAttribLocation(currentShaderProgram, "aTexCoord");
                        if (texCoordLoc >= 0)
                        {
                            GL.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
                            GL.EnableVertexAttribArray(texCoordLoc);
                        }
                    }
                    else if (_currentShadingMode == ShadingMode.Vertex)
                    {
                        int colorLoc = GL.GetAttribLocation(currentShaderProgram, "aColor");
                        if (colorLoc >= 0)
                        {
                            GL.VertexAttribPointer(colorLoc, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                            GL.EnableVertexAttribArray(colorLoc);
                        }
                    }
                    else if (_currentShadingMode != ShadingMode.Wireframe)
                    {
                        // 对于其他着色模式，我们仍然使用颜色数据，因为立方体顶点数据中没有法线
                        int colorLoc = GL.GetAttribLocation(currentShaderProgram, "aColor");
                        if (colorLoc >= 0)
                        {
                            GL.VertexAttribPointer(colorLoc, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                            GL.EnableVertexAttribArray(colorLoc);
                        }
                    }
                    
                    // 设置多边形模式
                    bool isWireframe = (_currentRenderMode == RenderMode.Line) || (_currentShadingMode == ShadingMode.Wireframe);
                    if (isWireframe)
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    }
                    else
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    }
                    
                    // 绘制模型
                    switch (_currentRenderMode)
                    {
                        case RenderMode.Point:
                            GL.DrawElements(PrimitiveType.Points, model.IndexCount, DrawElementsType.UnsignedInt, 0);
                            break;
                        case RenderMode.Line:
                        case RenderMode.Fill:
                        default:
                            GL.DrawElements(PrimitiveType.Triangles, model.IndexCount, DrawElementsType.UnsignedInt, 0);
                            break;
                    }
                }
            }
            
            // 渲染坐标轴（如果启用）
            if (Scene.ShowCoordinateAxes && Scene.CoordinateAxes != null)
            {
                // 如果坐标轴没有渲染数据，先创建
                if (!_modelRenderData.ContainsKey(Scene.CoordinateAxes))
                {
                    CreateModelBuffers(Scene.CoordinateAxes);
                }
                
                // 检查是否成功创建了渲染数据
                if (_modelRenderData.ContainsKey(Scene.CoordinateAxes))
                {
                    var renderData = _modelRenderData[Scene.CoordinateAxes];
                    var modelMatrix = Scene.CoordinateAxes.GetModelMatrix();
                    
                    if (modelLoc >= 0) 
                    {
                        GL.UniformMatrix4(modelLoc, false, ref modelMatrix);
                    }
                    
                    // 绑定VAO并设置顶点属性
                    GL.BindVertexArray(renderData.VAO);
                    
                    // 设置顶点属性（坐标轴使用顶点着色）
                    int posLoc = GL.GetAttribLocation(currentShaderProgram, "aPosition");
                    if (posLoc >= 0)
                    {
                        GL.VertexAttribPointer(posLoc, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
                        GL.EnableVertexAttribArray(posLoc);
                    }
                    
                    int colorLoc = GL.GetAttribLocation(currentShaderProgram, "aColor");
                    if (colorLoc >= 0)
                    {
                        GL.VertexAttribPointer(colorLoc, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
                        GL.EnableVertexAttribArray(colorLoc);
                    }
                    
                    // 坐标轴始终以线框模式渲染
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.DrawElements(PrimitiveType.Lines, Scene.CoordinateAxes.IndexCount, DrawElementsType.UnsignedInt, 0);
                }
            }
            
            GL.BindVertexArray(0);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 设置着色模式
        /// </summary>
        public void SetShadingMode(ShadingMode mode)
        {
            _currentShadingMode = mode;
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置渲染模式
        /// </summary>
        public void SetRenderMode(RenderMode mode)
        {
            _currentRenderMode = mode;
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 获取当前着色模式
        /// </summary>
        public ShadingMode GetShadingMode() => _currentShadingMode;
        
        /// <summary>
        /// 获取当前渲染模式
        /// </summary>
        public RenderMode GetRenderMode() => _currentRenderMode;
        
        /// <summary>
        /// 添加模型到场景
        /// </summary>
        public void AddModel(Model3D model)
        {
            Scene.Models.Add(model);
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 从场景移除模型
        /// </summary>
        public void RemoveModel(Model3D model)
        {
            Scene.Models.Remove(model);
            if (_modelRenderData.ContainsKey(model))
            {
                var renderData = _modelRenderData[model];
                GL.DeleteVertexArray(renderData.VAO);
                GL.DeleteBuffer(renderData.VBO);
                GL.DeleteBuffer(renderData.EBO);
                _modelRenderData.Remove(model);
            }
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 清空所有模型和渲染数据
        /// </summary>
        public void ClearAllModels()
        {
            // 清理所有模型的渲染数据
            foreach (var renderData in _modelRenderData.Values)
            {
                GL.DeleteVertexArray(renderData.VAO);
                GL.DeleteBuffer(renderData.VBO);
                GL.DeleteBuffer(renderData.EBO);
            }
            _modelRenderData.Clear();
            
            // 清空场景中的模型
            Scene.ClearModels();
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置当前显示的模型（按需加载）
        /// </summary>
        /// <param name="modelType">模型类型名称，null表示清空所有模型</param>
        public void SetCurrentModel(string? modelType)
        {
            // 清空现有模型和渲染数据
            ClearAllModels();
            
            // 如果指定了模型类型，创建新模型
            if (!string.IsNullOrEmpty(modelType))
            {
                var model = Scene.SetCurrentModel(modelType);
                if (model != null)
                {
                    // 如果OpenGL已初始化，立即创建缓冲区；否则在渲染时创建
                    if (_isOpenGLInitialized)
                    {
                        CreateModelBuffers(model);
                    }
                }
            }
            
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 设置坐标轴显示状态
        /// </summary>
        /// <param name="show">是否显示坐标轴</param>
        public void SetCoordinateAxesVisible(bool show)
        {
            Scene.SetCoordinateAxesVisible(show);
            RequestNextFrameRendering();
        }
        
        /// <summary>
        /// 获取坐标轴显示状态
        /// </summary>
        /// <returns>是否显示坐标轴</returns>
        public bool GetCoordinateAxesVisible()
        {
            return Scene.ShowCoordinateAxes;
        }
        #endregion

        #region 事件处理
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            
            var position = e.GetPosition(this);
            _lastMousePosition = new Vector2((float)position.X, (float)position.Y);
            
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isMousePressed = true;
                e.Pointer.Capture(this);
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                _isRightMousePressed = true;
                e.Pointer.Capture(this);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isMousePressed = false;
            _isRightMousePressed = false;
            e.Pointer.Capture(null);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            
            var currentPos = new Vector2((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
            var delta = currentPos - _lastMousePosition;
            
            if (_isMousePressed)
            {
                // 左键拖拽：旋转
                _rotationY += delta.X * ROTATION_SENSITIVITY;
                _rotationX += delta.Y * ROTATION_SENSITIVITY;
                
                // 限制X轴旋转角度，避免翻转
                _rotationX = Math.Max(-MathHelper.PiOver2 + ROTATION_LIMIT_OFFSET, Math.Min(MathHelper.PiOver2 - ROTATION_LIMIT_OFFSET, _rotationX));
                
                RequestNextFrameRendering();
            }
            else if (_isRightMousePressed)
            {
                // 右键拖拽：平移
                _cameraOffset.X -= delta.X * TRANSLATION_SENSITIVITY;
                _cameraOffset.Y += delta.Y * TRANSLATION_SENSITIVITY;
                
                RequestNextFrameRendering();
            }
            
            _lastMousePosition = currentPos;
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            
            // 更平滑的缩放
            var zoomFactor = 1.0f + (float)e.Delta.Y * ZOOM_SENSITIVITY;
            _zoom *= zoomFactor;
            
            // 限制缩放范围
            _zoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoom));
            
            RequestNextFrameRendering();
        }
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }
        
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
        }
        
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
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
            
            base.OnOpenGlDeinit(gl);
        }

        // 实现ICustomHitTest接口
        public bool HitTest(Point point)
        {
            // 返回true表示这个点在控件范围内，可以接收输入事件
            return Bounds.Contains(point);
        }
        #endregion
    }
}
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using Avalonia3DControl.Core.Animation;

namespace Avalonia3DControl.UI
{
    /// <summary>
    /// 梯度条位置枚举
    /// </summary>
    public enum GradientBarPosition
    {
        Left,
        Right
    }

    /// <summary>
    /// 梯度条类，用于在OpenGL窗体中显示颜色梯度条
    /// </summary>
    public class GradientBar : IDisposable
    {
        #region 私有字段
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _shaderProgram;
        private bool _isInitialized = false;
        private bool _disposed = false;
        #endregion

        #region 公共属性
        /// <summary>
        /// 是否显示梯度条
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 梯度条位置（左侧或右侧）
        /// </summary>
        private GradientBarPosition _position = GradientBarPosition.Right;
        private bool _verticesNeedUpdate = false;
        
        public GradientBarPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _verticesNeedUpdate = true;
                    Console.WriteLine($"梯度条位置已更改为: {value}，标记顶点需要更新");
                }
            }
        }

        /// <summary>
        /// 梯度条宽度（NDC坐标系）
        /// </summary>
        public float Width { get; set; } = 0.02f;

        /// <summary>
        /// 梯度条高度（相对于窗口高度的比例）
        /// </summary>
        public float Height { get; set; } = 0.7f;

        /// <summary>
        /// 梯度条距离边缘的偏移（相对于窗口宽度的比例）
        /// </summary>
        public float EdgeOffset { get; set; } = 0.015f;

        /// <summary>
        /// 当前颜色梯度类型
        /// </summary>
        public ColorGradientType GradientType { get; set; } = ColorGradientType.Classic;

        /// <summary>
        /// 最小值
        /// </summary>
        public float MinValue { get; set; } = -1.0f;

        /// <summary>
        /// 最大值
        /// </summary>
        public float MaxValue { get; set; } = 1.0f;
        #endregion

        #region 初始化和清理
        /// <summary>
        /// 初始化梯度条
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            Console.WriteLine("开始初始化梯度条...");
            
            try
            {
                Console.WriteLine("开始创建梯度条着色器...");
                CreateShader();
                Console.WriteLine("梯度条着色器创建成功");
                
                Console.WriteLine("开始创建梯度条几何体...");
                CreateGeometry();
                Console.WriteLine("梯度条几何体创建成功");
                
                _verticesNeedUpdate = true; // 初始化时需要计算顶点数据
            _isInitialized = true;
            Console.WriteLine("梯度条初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"梯度条初始化失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 创建着色器
        /// </summary>
        private void CreateShader()
        {
            string vertexSource = "#version 100\n" +
                "precision highp float;\n" +
                "attribute vec3 aPosition;\n" +
                "attribute vec2 aTexCoord;\n" +
                "varying vec2 TexCoord;\n" +
                "void main() {\n" +
                "    gl_Position = vec4(aPosition, 1.0);\n" +
                "    TexCoord = aTexCoord;\n" +
                "}\n";

            string fragmentSource = "#version 100\n" +
                "precision highp float;\n" +
                "varying vec2 TexCoord;\n" +
                "uniform int gradientType;\n" +
                "uniform bool isSymmetric;\n" +
                "uniform float minValue;\n" +
                "uniform float maxValue;\n" +
                "vec3 getClassicColor(float t) {\n" +
                "    if (t < 0.33) {\n" +
                "        return mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 0.0), t * 3.0);\n" +
                "    } else if (t < 0.67) {\n" +
                "        return mix(vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 0.0), (t - 0.33) * 3.0);\n" +
                "    } else {\n" +
                "        return mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 0.0, 0.0), (t - 0.67) * 3.0);\n" +
                "    }\n" +
                "}\n" +
                "vec3 getThermalColor(float t) {\n" +
                "    if (t < 0.33) {\n" +
                "        return mix(vec3(0.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), t * 3.0);\n" +
                "    } else if (t < 0.67) {\n" +
                "        return mix(vec3(1.0, 0.0, 0.0), vec3(1.0, 1.0, 0.0), (t - 0.33) * 3.0);\n" +
                "    } else {\n" +
                "        return mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0), (t - 0.67) * 3.0);\n" +
                "    }\n" +
                "}\n" +
                "vec3 getRainbowColor(float t) {\n" +
                "    float h = t * 5.0;\n" +
                "    float f = fract(h);\n" +
                "    if (h < 1.0) return mix(vec3(1.0, 0.0, 0.0), vec3(1.0, 0.5, 0.0), f);\n" +
                "    else if (h < 2.0) return mix(vec3(1.0, 0.5, 0.0), vec3(1.0, 1.0, 0.0), f);\n" +
                "    else if (h < 3.0) return mix(vec3(1.0, 1.0, 0.0), vec3(0.0, 1.0, 0.0), f);\n" +
                "    else if (h < 4.0) return mix(vec3(0.0, 1.0, 0.0), vec3(0.0, 0.0, 1.0), f);\n" +
                "    else return mix(vec3(0.0, 0.0, 1.0), vec3(0.5, 0.0, 1.0), f);\n" +
                "}\n" +
                "vec3 getMonochromeColor(float t) {\n" +
                "    return mix(vec3(0.0, 0.0, 0.5), vec3(0.5, 0.5, 1.0), t);\n" +
                "}\n" +
                "vec3 getOceanColor(float t) {\n" +
                "    if (t < 0.33) {\n" +
                "        return mix(vec3(0.0, 0.0, 0.5), vec3(0.0, 0.5, 0.5), t * 3.0);\n" +
                "    } else if (t < 0.67) {\n" +
                "        return mix(vec3(0.0, 0.5, 0.5), vec3(0.0, 1.0, 0.0), (t - 0.33) * 3.0);\n" +
                "    } else {\n" +
                "        return mix(vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0), (t - 0.67) * 3.0);\n" +
                "    }\n" +
                "}\n" +
                "vec3 getFireColor(float t) {\n" +
                "    if (t < 0.25) {\n" +
                "        return mix(vec3(0.0, 0.0, 0.0), vec3(1.0, 0.0, 0.0), t * 4.0);\n" +
                "    } else if (t < 0.5) {\n" +
                "        return mix(vec3(1.0, 0.0, 0.0), vec3(1.0, 0.5, 0.0), (t - 0.25) * 4.0);\n" +
                "    } else if (t < 0.75) {\n" +
                "        return mix(vec3(1.0, 0.5, 0.0), vec3(1.0, 1.0, 0.0), (t - 0.5) * 4.0);\n" +
                "    } else {\n" +
                "        return mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 1.0, 1.0), (t - 0.75) * 4.0);\n" +
                "    }\n" +
                "}\n" +
                "void main() {\n" +
                "    float t = 1.0 - TexCoord.y;\n" +
                "    if (isSymmetric) {\n" +
                "        t = abs(2.0 * t - 1.0);\n" +
                "    }\n" +
                "    vec3 color;\n" +
                "    if (gradientType == 0) color = getClassicColor(t);\n" +
                "    else if (gradientType == 1) color = getThermalColor(t);\n" +
                "    else if (gradientType == 2) color = getRainbowColor(t);\n" +
                "    else if (gradientType == 3) color = getMonochromeColor(t);\n" +
                "    else if (gradientType == 4) color = getOceanColor(t);\n" +
                "    else if (gradientType == 5) color = getFireColor(t);\n" +
                "    else color = getClassicColor(t);\n" +
                "    gl_FragColor = vec4(color, 1.0);\n" +
                "}\n";

            // 编译着色器
            int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vertexShader);
            GL.AttachShader(_shaderProgram, fragmentShader);

            // 显式绑定attribute位置，确保aPosition=0，aTexCoord=1，避免位置与纹理坐标错乱
            GL.BindAttribLocation(_shaderProgram, 0, "aPosition");
            GL.BindAttribLocation(_shaderProgram, 1, "aTexCoord");

            GL.LinkProgram(_shaderProgram);

            // 检查链接状态
            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_shaderProgram);
                throw new Exception($"梯度条着色器链接失败: {infoLog}");
            }

            // 清理着色器对象
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        /// <summary>
        /// 编译着色器
        /// </summary>
        private int CompileShader(ShaderType type, string source)
        {
            Console.WriteLine($"开始编译{type}着色器...");
            
            int shader = GL.CreateShader(type);
            Console.WriteLine($"创建着色器对象: {shader}");
            
            GL.ShaderSource(shader, source);
            Console.WriteLine("设置着色器源码完成");
            
            GL.CompileShader(shader);
            Console.WriteLine("编译着色器完成");

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            Console.WriteLine($"编译状态: {success}");
            
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                Console.WriteLine($"着色器编译失败详细信息: {infoLog}");
                Console.WriteLine($"着色器源码:\n{source}");
                throw new Exception($"着色器编译失败 ({type}): {infoLog}");
            }

            Console.WriteLine($"{type}着色器编译成功");
            return shader;
        }

        /// <summary>
        /// 创建几何体
        /// </summary>
        private void CreateGeometry()
        {
            // 创建VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            // 顶点数据（位置 + 纹理坐标）
            float[] vertices = {
                // 位置        纹理坐标
                -1.0f,  1.0f, 0.0f,  0.0f, 1.0f, // 左上
                -1.0f, -1.0f, 0.0f,  0.0f, 0.0f, // 左下
                 1.0f, -1.0f, 0.0f,  1.0f, 0.0f, // 右下
                 1.0f,  1.0f, 0.0f,  1.0f, 1.0f  // 右上
            };

            // 索引数据
            uint[] indices = {
                0, 1, 2,
                2, 3, 0
            };

            // 创建VBO
            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // 创建EBO
            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // 设置顶点属性
            // 位置属性
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // 纹理坐标属性
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }
        #endregion

        #region 渲染方法
        /// <summary>
        /// 渲染梯度条
        /// </summary>
        /// <param name="viewportWidth">视口宽度</param>
        /// <param name="viewportHeight">视口高度</param>
        /// <param name="dpiScale">DPI缩放比例</param>
        public void Render(int viewportWidth, int viewportHeight, double dpiScale = 1.0)
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (!IsVisible)
            {
                return;
            }

            // 记录并临时覆盖视口，保证梯度条按全屏视口渲染
            int[] prevViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, prevViewport);
            bool viewportOverridden = prevViewport[0] != 0 || prevViewport[1] != 0 || prevViewport[2] != viewportWidth || prevViewport[3] != viewportHeight;
            if (viewportOverridden)
            {
                GL.Viewport(0, 0, viewportWidth, viewportHeight);
            }

            try
            {
                // 保存当前OpenGL状态
                GL.GetInteger(GetPName.CurrentProgram, out int currentProgram);
                GL.GetInteger(GetPName.VertexArrayBinding, out int currentVAO);
                bool depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
                bool blendEnabled = GL.IsEnabled(EnableCap.Blend);

                // 设置渲染状态 - 确保梯度条在最前面
                GL.Disable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.Clear(ClearBufferMask.DepthBufferBit); // 清除深度缓冲确保在最前面

                // 使用梯度条着色器
                GL.UseProgram(_shaderProgram);

                // 设置uniform变量
                int gradientTypeLocation = GL.GetUniformLocation(_shaderProgram, "gradientType");
                int isSymmetricLocation = GL.GetUniformLocation(_shaderProgram, "isSymmetric");
                int minValueLocation = GL.GetUniformLocation(_shaderProgram, "minValue");
                int maxValueLocation = GL.GetUniformLocation(_shaderProgram, "maxValue");

                GL.Uniform1(gradientTypeLocation, (int)GradientType.BaseType);
                GL.Uniform1(isSymmetricLocation, GradientType.IsSymmetric ? 1 : 0);
                GL.Uniform1(minValueLocation, MinValue);
                GL.Uniform1(maxValueLocation, MaxValue);
                
                // 只在需要时更新顶点数据
                if (_verticesNeedUpdate)
                {
                    // 考虑DPI缩放
                    float scale = (float)dpiScale;
                    float barWidth = Width * scale;
                    float barHeight = Height * scale; 
                    float offsetX = EdgeOffset * scale;

                    float left, right;
                    if (Position == GradientBarPosition.Left)
                    {
                        left = -1.0f + offsetX;
                        right = left + barWidth;
                    }
                    else
                    {
                        right = 1.0f - offsetX;
                        left = right - barWidth;
                    }

                    float bottom = -barHeight * 0.5f;
                    float top = barHeight * 0.5f;

                    // 更新顶点数据
                    // 计算纹理坐标的宽度比例，使其与几何宽度成正比
                    float texWidth = Math.Min(barWidth * 50.0f, 1.0f); // 限制最大为1.0
                    float[] vertices = {
                        left,  top,    0.0f,  0.0f, 1.0f,     // 左上
                        left,  bottom, 0.0f,  0.0f, 0.0f,     // 左下
                        right, bottom, 0.0f,  texWidth, 0.0f, // 右下
                        right, top,    0.0f,  texWidth, 1.0f  // 右上
                    };
                    
                    // 更新VBO
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
                    
                    _verticesNeedUpdate = false;
                }

                // 绑定VAO并渲染
                GL.BindVertexArray(_vao);
                
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

                // 恢复OpenGL状态
                GL.UseProgram(currentProgram);
                GL.BindVertexArray(currentVAO);
                
                if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
                else GL.Disable(EnableCap.DepthTest);
                
                if (!blendEnabled) GL.Disable(EnableCap.Blend);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"梯度条渲染失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            finally
            {
                // 恢复之前的视口（若被覆盖）
                if (viewportOverridden)
                {
                    GL.Viewport(prevViewport[0], prevViewport[1], prevViewport[2], prevViewport[3]);
                }
            }
        }
        #endregion

        #region IDisposable实现
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_isInitialized)
                {
                    GL.DeleteVertexArray(_vao);
                    GL.DeleteBuffer(_vbo);
                    GL.DeleteBuffer(_ebo);
                    GL.DeleteProgram(_shaderProgram);
                }
                _disposed = true;
            }
        }

        ~GradientBar()
        {
            Dispose(false);
        }
        #endregion
    }
}
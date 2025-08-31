using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
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
        
        // 刻度线/文字渲染
        private int _lineShaderProgram;
        private int _lineVao;
        private int _lineVbo;
        private bool _lineResourcesCreated = false;
        private float _lastLeft, _lastRight, _lastTop, _lastBottom;
        
        // 日志已移除
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
        
        public GradientBarPosition Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
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

        /// <summary>
        /// 是否使用归一化刻度（-1~1）；false 则显示实际 Min~Max
        /// </summary>
        public bool UseNormalizedScale { get; set; } = true;

        /// <summary>
        /// 是否显示刻度
        /// </summary>
        public bool ShowTicks { get; set; } = true;

        /// <summary>
        /// 刻度数量（包含两端），建议为奇数，如5或7
        /// </summary>
        public int TickCount { get; set; } = 5;
        #endregion

        #region 初始化和清理
        /// <summary>
        /// 初始化梯度条
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                CreateShader();
                CreateGeometry();
                if (!_lineResourcesCreated) CreateLineResources();
                _isInitialized = true;
            }
            catch (Exception)
            {
                _isInitialized = false;
            }
        }

        private void CreateLineResources()
        {
            // 创建线条着色器
            string vertexSource =
                "#version 330 core\n" +
                "layout(location = 0) in vec2 aPosition;\n" +
                "void main() {\n" +
                "    gl_Position = vec4(aPosition.xy, 0.0, 1.0);\n" +
                "}\n";

            string fragmentSource =
                "#version 330 core\n" +
                "uniform vec3 uColor;\n" +
                "out vec4 FragColor;\n" +
                "void main() {\n" +
                "    FragColor = vec4(uColor, 1.0);\n" +
                "}\n";

            int vs = CompileShader(ShaderType.VertexShader, vertexSource);
            int fs = CompileShader(ShaderType.FragmentShader, fragmentSource);
            _lineShaderProgram = GL.CreateProgram();
            GL.AttachShader(_lineShaderProgram, vs);
            GL.AttachShader(_lineShaderProgram, fs);
            GL.LinkProgram(_lineShaderProgram);
            GL.GetProgram(_lineShaderProgram, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_lineShaderProgram);
                throw new Exception($"刻度线着色器链接失败: {infoLog}");
            }
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            // 创建线条VAO/VBO
            _lineVao = GL.GenVertexArray();
            _lineVbo = GL.GenBuffer();
            GL.BindVertexArray(_lineVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, 1024 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw); // 预分配
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);

            _lineResourcesCreated = true;
        }
        #endregion

        #region 着色器与几何体
        private void CreateShader()
        {
            // 顶点着色器
            string vertexSource =
                "#version 330 core\n" +
                "layout(location = 0) in vec3 aPosition;\n" +
                "layout(location = 1) in vec2 aTexCoord;\n" +
                "out vec2 TexCoord;\n" +
                "void main() {\n" +
                "    gl_Position = vec4(aPosition, 1.0);\n" +
                "    TexCoord = aTexCoord;\n" +
                "}\n";

            // 片段着色器
            string fragmentSource =
                "#version 330 core\n" +
                "in vec2 TexCoord;\n" +
                "out vec4 FragColor;\n" +
                "uniform int gradientType;\n" +
                "uniform int isSymmetric;\n" +
                "uniform float minValue;\n" +
                "uniform float maxValue;\n" +
                "vec3 getClassicColor(float t) {\n" +
                "    if (t < 0.33) {\n" +
                "        float ratio = t / 0.33;\n" +
                "        return mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 0.0), ratio);\n" +
                "    } else if (t < 0.66) {\n" +
                "        float ratio = (t - 0.33) / 0.33;\n" +
                "        return mix(vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 0.0), ratio);\n" +
                "    } else {\n" +
                "        float ratio = (t - 0.66) / 0.34;\n" +
                "        return mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 0.0, 0.0), ratio);\n" +
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
                "    if (isSymmetric == 1) {\n" +
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
                "    FragColor = vec4(color, 1.0);\n" +
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

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"着色器编译失败: {infoLog}");
            }
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
                
                // 计算位置（每帧计算，便于刻度布局）
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
                _lastLeft = left; _lastRight = right; _lastTop = top; _lastBottom = bottom;

                // 更新顶点数据（每帧根据视口与DPI计算，避免初始为全屏顶点）
                {
                    // 更新顶点数据
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
                }

                // 绑定VAO并渲染梯度条矩形
                GL.BindVertexArray(_vao);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);

                // 渲染刻度和标签
                if (ShowTicks)
                {
                    RenderTicksAndLabels();
                }

                // 恢复OpenGL状态
                GL.UseProgram(currentProgram);
                GL.BindVertexArray(currentVAO);
                
                if (depthTestEnabled) GL.Enable(EnableCap.DepthTest);
                else GL.Disable(EnableCap.DepthTest);
                
                if (!blendEnabled) GL.Disable(EnableCap.Blend);
            }
            catch (Exception)
            {
                // 静默处理渲染异常
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

        private void RenderTicksAndLabels()
        {
            if (!_lineResourcesCreated) return;

            // 计算刻度参数
            float barWidth = _lastRight - _lastLeft;
            float barHeight = _lastTop - _lastBottom;
            float tickLength = barWidth * 0.5f;
            float labelPadding = barWidth * 0.25f;
            float textHeight = MathF.Min(barHeight * 0.035f, 0.035f);
            float textWidth = textHeight * 0.55f; // 单字符宽度

            int n = Math.Max(2, TickCount);

            // 准备线段数据
            var lineVerts = new List<float>(1024);

            // 刻度线颜色（白色）
            Vector3 tickColor = new Vector3(1f, 1f, 1f);

            // 计算刻度位置与标签
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (float)(n - 1); // 0..1 顶->底或底->顶？我们定义顶端为1
                float y = Lerp(_lastBottom, _lastTop, t);

                // 画刻度线（根据位置选择内侧）
                float x1, x2;
                if (Position == GradientBarPosition.Right)
                {
                    x1 = _lastLeft - 0.002f; // 稍微离开条一点
                    x2 = x1 - tickLength;
                }
                else
                {
                    x1 = _lastRight + 0.002f;
                    x2 = x1 + tickLength;
                }
                // 添加线段
                lineVerts.Add(x1); lineVerts.Add(y);
                lineVerts.Add(x2); lineVerts.Add(y);

                // 计算标签文本
                string label = FormatTickLabel(1.0f - t); // 顶部为1，底部为-1 或 Min/Max

                // 文字绘制起点
                float textStartX;
                if (Position == GradientBarPosition.Right)
                {
                    textStartX = x2 - labelPadding - label.Length * textWidth;
                }
                else
                {
                    textStartX = x2 + labelPadding;
                }
                float textBaselineY = y - textHeight * 0.5f; // 垂直居中

                // 将字符线段添加到lineVerts
                AddTextLineSegments(lineVerts, label, textStartX, textBaselineY, textWidth, textHeight);
            }

            // 提交并绘制
            if (lineVerts.Count > 0)
            {
                GL.UseProgram(_lineShaderProgram);
                int colorLoc = GL.GetUniformLocation(_lineShaderProgram, "uColor");
                GL.Uniform3(colorLoc, tickColor);

                GL.BindVertexArray(_lineVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, lineVerts.Count * sizeof(float), lineVerts.ToArray(), BufferUsageHint.DynamicDraw);
                GL.DrawArrays(PrimitiveType.Lines, 0, lineVerts.Count / 2);
                GL.BindVertexArray(0);
            }
        }

        private string FormatTickLabel(float normalizedTopToBottom)
        {
            // normalizedTopToBottom: 顶部1 -> 底部0
            float v;
            if (UseNormalizedScale)
            {
                // 映射到 [-1, 1]
                v = 1f - normalizedTopToBottom * 2f;
                // 为了整洁，常用值保留一位小数
                if (MathF.Abs(v) < 1e-4f) v = 0f;
                return v.ToString("0.0");
            }
            else
            {
                // 映射到 [Max, Min]
                v = Lerp(MaxValue, MinValue, normalizedTopToBottom);
                return v.ToString("0.00");
            }
        }

        // 线段文字渲染与字符片段方法保持不变
        private void AddTextLineSegments(List<float> verts, string text, float startX, float startY, float charW, float charH)
        {
            float x = startX;
            foreach (char c in text)
            {
                AddCharSegments(verts, c, x, startY, charW, charH);
                x += charW * 0.75f; // 字间距
            }
        }

        private void AddCharSegments(List<float> verts, char c, float x, float y, float w, float h)
        {
            // 段坐标（相对0..1）
            // 7段: a(上), b(右上), c(右下), d(下), e(左下), f(左上), g(中)
            var seg = new Dictionary<char, bool>();
            void AddSeg(char name)
            {
                switch (name)
                {
                    case 'a': AddLine(verts, x + w*0.1f, y + h*0.95f, x + w*0.9f, y + h*0.95f); break;
                    case 'b': AddLine(verts, x + w*0.9f, y + h*0.95f, x + w*0.9f, y + h*0.5f); break;
                    case 'c': AddLine(verts, x + w*0.9f, y + h*0.5f, x + w*0.9f, y + h*0.05f); break;
                    case 'd': AddLine(verts, x + w*0.1f, y + h*0.05f, x + w*0.9f, y + h*0.05f); break;
                    case 'e': AddLine(verts, x + w*0.1f, y + h*0.5f, x + w*0.1f, y + h*0.05f); break;
                    case 'f': AddLine(verts, x + w*0.1f, y + h*0.95f, x + w*0.1f, y + h*0.5f); break;
                    case 'g': AddLine(verts, x + w*0.15f, y + h*0.5f, x + w*0.85f, y + h*0.5f); break;
                }
            }

            void AddLine(List<float> v, float x1, float y1, float x2, float y2)
            {
                v.Add(x1); v.Add(y1);
                v.Add(x2); v.Add(y2);
            }

            switch (c)
            {
                case '0': AddSeg('a'); AddSeg('b'); AddSeg('c'); AddSeg('d'); AddSeg('e'); AddSeg('f'); break;
                case '1': AddSeg('b'); AddSeg('c'); break;
                case '2': AddSeg('a'); AddSeg('b'); AddSeg('g'); AddSeg('e'); AddSeg('d'); break;
                case '3': AddSeg('a'); AddSeg('b'); AddSeg('g'); AddSeg('c'); AddSeg('d'); break;
                case '4': AddSeg('f'); AddSeg('g'); AddSeg('b'); AddSeg('c'); break;
                case '5': AddSeg('a'); AddSeg('f'); AddSeg('g'); AddSeg('c'); AddSeg('d'); break;
                case '6': AddSeg('a'); AddSeg('f'); AddSeg('g'); AddSeg('e'); AddSeg('c'); AddSeg('d'); break;
                case '7': AddSeg('a'); AddSeg('b'); AddSeg('c'); break;
                case '8': AddSeg('a'); AddSeg('b'); AddSeg('c'); AddSeg('d'); AddSeg('e'); AddSeg('f'); AddSeg('g'); break;
                case '9': AddSeg('a'); AddSeg('b'); AddSeg('c'); AddSeg('d'); AddSeg('f'); AddSeg('g'); break;
                case '-': AddSeg('g'); break;
                case '.':
                {
                    // 右下角一个小点（用短线代替）
                    float cx = x + w*0.85f, cy = y + h*0.02f;
                    AddLine(verts, cx - w*0.03f, cy - h*0.02f, cx + w*0.03f, cy + h*0.02f);
                    break;
                }
                case ' ':
                default:
                    break;
            }
        }
        #endregion

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

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
                    if (_lineResourcesCreated)
                    {
                        GL.DeleteVertexArray(_lineVao);
                        GL.DeleteBuffer(_lineVbo);
                        GL.DeleteProgram(_lineShaderProgram);
                    }
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
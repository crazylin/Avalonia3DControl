using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.UI;

namespace Avalonia3DControl.Rendering.OpenGL
{
    /// <summary>
    /// 包围盒渲染器，负责渲染模型的外接矩形包围盒和坐标轴刻度
    /// </summary>
    public class BoundingBoxRenderer
    {
        private uint _vao;
        private uint _vbo;
        private bool _isInitialized = false;
        // 仅控制"0"数字标签的绘制次数：
        // - 每帧在 RenderAxisTicks 开头重置为 false
        // - AddSingleAxisTicks 中遇到 value==0 且尚未绘制时，调用 AddTickLabel 后置为 true
        // - 其它非 0 刻度的标签总是绘制（不受该标志限制）
        private bool _zeroLabelDrawn = false; // 每帧仅绘制一次"0"标签
        
        /// <summary>
        /// 是否显示包围盒
        /// </summary>
        public bool Visible { get; set; } = false;
        
        /// <summary>
        /// 是否显示坐标轴刻度
        /// </summary>
        public bool ShowAxisTicks { get; set; } = true;
        
        /// <summary>
        /// 包围盒线条颜色
        /// </summary>
        public Vector3 LineColor { get; set; } = new Vector3(1.0f, 1.0f, 0.0f); // 黄色
        
        /// <summary>
        /// 包围盒线条宽度
        /// </summary>
        public float LineWidth { get; set; } = 2.0f;
        
        /// <summary>
        /// 坐标轴刻度颜色
        /// </summary>
        public Vector3 TickColor { get; set; } = new Vector3(0.8f, 0.8f, 0.8f); // 浅灰色
        
        /// <summary>
        /// 刻度线长度
        /// </summary>
        public float TickLength { get; set; } = 0.1f;
        /// <summary>
        /// 刻度字体缩放
        /// </summary>
        public float LabelScale { get; set; } = 1.0f;
        /// <summary>
        /// 标签线宽（用于数字字体线段）
        /// </summary>
        public float LabelLineWidth { get; set; } = 3.0f;
        
        /// <summary>
        /// 初始化渲染器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            // 生成VAO和VBO
            GL.GenVertexArrays(1, out _vao);
            GL.GenBuffers(1, out _vbo);
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 添加刻度数字标签
        /// </summary>
        private void AddTickLabel(List<float> labelVertices, Vector3 position, Vector3 direction, float value, Vector3 color, Matrix4 view)
        {
            // 智能格式化数值（支持非0刻度显示标签）
            string valueText = FormatTickValue(value);
            
            // 计算标签位置（在刻度线旁边）
            Vector3 labelOffset;
            if (Math.Abs(Vector3.Dot(direction, Vector3.UnitX)) > 0.9f) // X轴
            {
                labelOffset = -Vector3.UnitY * TickLength * (1.0f * LabelScale);
            }
            else if (Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.9f) // Y轴
            {
                labelOffset = -Vector3.UnitX * TickLength * (1.0f * LabelScale);
            }
            else // Z轴
            {
                labelOffset = Vector3.UnitX * TickLength * (1.0f * LabelScale);
            }
        
            Vector3 labelPos = position + labelOffset;
        
            // 计算相机朝向向量（将文本线段转换为始终朝向相机的3D线段）
            var invView = view.Inverted();
            Vector3 camRight = invView.Column0.Xyz.Normalized(); // 相机右方向（世界坐标）
            Vector3 camUp = invView.Column1.Xyz.Normalized();    // 相机上方向（世界坐标）
            Vector3 camPos = invView.Column3.Xyz;                // 相机位置（世界坐标）
            
            // 将标签沿着朝向相机的方向轻微前移，避免与包围盒/刻度发生Z冲突
            var toCam = (camPos - labelPos).Normalized();
            labelPos += toCam * (TickLength * 0.2f);
        
            // 生成文本的局部2D线段（起点从0,0开始）
            var tempVerts = new List<float>();
            float charWidth = TickLength * 0.40f * LabelScale;
            float charHeight = TickLength * 0.65f * LabelScale;
            CharacterRenderer.AddTextLineSegments(tempVerts, valueText, 0.0f, 0.0f, charWidth, charHeight);
        
            for (int i = 0; i < tempVerts.Count; i += 4)
            {
                var x1 = tempVerts[i];
                var y1 = tempVerts[i + 1];
                var x2 = tempVerts[i + 2];
                var y2 = tempVerts[i + 3];
       
                Vector3 p1 = labelPos + camRight * x1 + camUp * y1;
                Vector3 p2 = labelPos + camRight * x2 + camUp * y2;
       
                labelVertices.AddRange(new float[] { p1.X, p1.Y, p1.Z, color.X, color.Y, color.Z });
                labelVertices.AddRange(new float[] { p2.X, p2.Y, p2.Z, color.X, color.Y, color.Z });
            }
        }
        
        /// <summary>
        /// 智能格式化刻度值
        /// </summary>
        private string FormatTickValue(float value)
        {
            // 如果是整数或接近整数，显示为整数
            if (Math.Abs(value - Math.Round(value)) < 0.001f)
            {
                return Math.Round(value).ToString("0");
            }
            
            // 如果是小数，根据大小选择合适的精度
            if (Math.Abs(value) >= 10)
            {
                return value.ToString("F1"); // 大数值保留1位小数
            }
            else if (Math.Abs(value) >= 1)
            {
                return value.ToString("F2"); // 中等数值保留2位小数
            }
            else
            {
                return value.ToString("F3"); // 小数值保留3位小数
            }
        }
        
        /// <summary>
        /// 渲染包围盒和坐标轴刻度
        /// </summary>
        /// <param name="models">模型列表</param>
        /// <param name="shaderProgram">着色器程序</param>
        /// <param name="view">视图矩阵</param>
        /// <param name="projection">投影矩阵</param>
        public void Render(List<Model3D> models, int shaderProgram, Matrix4 view, Matrix4 projection)
        {
            if (!Visible || !_isInitialized || models.Count == 0)
                return;
            
            // 计算所有模型的总包围盒
            var (min, max) = CalculateSceneBoundingBox(models);
            
            // 渲染包围盒
            RenderBoundingBox(min, max, shaderProgram, view, projection);
            
            // 渲染坐标轴刻度
            if (ShowAxisTicks)
            {
                RenderAxisTicks(min, max, shaderProgram, view, projection);
            }
        }
        
        /// <summary>
        /// 计算场景中所有模型的总包围盒
        /// </summary>
        private (Vector3 Min, Vector3 Max) CalculateSceneBoundingBox(List<Model3D> models)
        {
            var sceneMin = new Vector3(float.MaxValue);
            var sceneMax = new Vector3(float.MinValue);
            
            foreach (var model in models)
            {
                var (modelMin, modelMax) = model.GetBoundingBox();
                sceneMin = Vector3.ComponentMin(sceneMin, modelMin);
                sceneMax = Vector3.ComponentMax(sceneMax, modelMax);
            }
            
            return (sceneMin, sceneMax);
        }
        
        /// <summary>
        /// 渲染包围盒线框
        /// </summary>
        private void RenderBoundingBox(Vector3 min, Vector3 max, int shaderProgram, Matrix4 view, Matrix4 projection)
        {
            // 创建包围盒的12条边的顶点数据
            var vertices = new float[]
            {
                // 底面4条边
                min.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                min.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                // 顶面4条边
                min.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                min.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                // 4条竖直边
                min.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, min.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, min.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                max.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                max.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z,
                
                min.X, max.Y, min.Z, LineColor.X, LineColor.Y, LineColor.Z,
                min.X, max.Y, max.Z, LineColor.X, LineColor.Y, LineColor.Z
            };
            
            RenderLines(vertices, shaderProgram, view, projection);
        }
        
        /// <summary>
        /// 渲染坐标轴刻度（只显示三条边）
        /// </summary>
        private void RenderAxisTicks(Vector3 min, Vector3 max, int shaderProgram, Matrix4 view, Matrix4 projection)
        {
            var tickVertices = new List<float>();
            var labelVertices = new List<float>();
            _zeroLabelDrawn = false; // 新一帧开始，重置仅绘制一次标志
            
            // 在包围盒一个角（min）绘制三条轴刻度，参考轴从该角沿三条边延伸
            var size = max - min;
            var originCorner = min;
            var half = size * 0.5f; // 保留，避免大段改动（即使未使用）
        
            // X轴：从 min 沿 +X 到 max.X
            var xTickColor = new Vector3(1.0f, 0.0f, 0.0f);
            var xStart = originCorner;
            var xLen = MathF.Max(0.0001f, size.X);
            // 基线（沿包围盒边）
            var xEnd = xStart + Vector3.UnitX * xLen;
            tickVertices.AddRange(new float[] {
                xStart.X, xStart.Y, xStart.Z, xTickColor.X, xTickColor.Y, xTickColor.Z,
                xEnd.X,   xEnd.Y,   xEnd.Z,   xTickColor.X, xTickColor.Y, xTickColor.Z,
            });
            // 刻度（将值域映射为 [0, size.X]，使 0 对应角点）
            AddSingleAxisTicks(tickVertices, labelVertices, xStart, Vector3.UnitX, xLen, xTickColor, 0.0f, +size.X, view);
        
            // Y轴：从 min 沿 +Y 到 max.Y
            var yTickColor = new Vector3(0.0f, 1.0f, 0.0f);
            var yStart = originCorner;
            var yLen = MathF.Max(0.0001f, size.Y);
            var yEnd = yStart + Vector3.UnitY * yLen;
            tickVertices.AddRange(new float[] {
                yStart.X, yStart.Y, yStart.Z, yTickColor.X, yTickColor.Y, yTickColor.Z,
                yEnd.X,   yEnd.Y,   yEnd.Z,   yTickColor.X, yTickColor.Y, yTickColor.Z,
            });
            AddSingleAxisTicks(tickVertices, labelVertices, yStart, Vector3.UnitY, yLen, yTickColor, 0.0f, +size.Y, view);
        
            // Z轴：从 min 沿 +Z 到 max.Z
            var zTickColor = new Vector3(0.0f, 0.0f, 1.0f);
            var zStart = originCorner;
            var zLen = MathF.Max(0.0001f, size.Z);
            var zEnd = zStart + Vector3.UnitZ * zLen;
            tickVertices.AddRange(new float[] {
                zStart.X, zStart.Y, zStart.Z, zTickColor.X, zTickColor.Y, zTickColor.Z,
                zEnd.X,   zEnd.Y,   zEnd.Z,   zTickColor.X, zTickColor.Y, zTickColor.Z,
            });
            AddSingleAxisTicks(tickVertices, labelVertices, zStart, Vector3.UnitZ, zLen, zTickColor, 0.0f, +size.Z, view);
        
            // 渲染刻度线（使用更粗的线宽以提高可见性）
            if (tickVertices.Count > 0)
            {
                float tickLineWidth = MathF.Max(LineWidth, 2.5f);
                RenderLines(tickVertices.ToArray(), shaderProgram, view, projection, tickLineWidth);
            }
        
            // 渲染“0”数字标签（使用更粗的线宽）——为了可见性，禁用深度测试并关闭深度写入
            if (labelVertices.Count > 0)
            {
                GL.Disable(EnableCap.DepthTest);
                GL.DepthMask(false);
                RenderLines(labelVertices.ToArray(), shaderProgram, view, projection, LabelLineWidth);
                GL.DepthMask(true);
                GL.Enable(EnableCap.DepthTest);
            }
        }
        
        /// <summary>
        /// 计算合适的刻度值
        /// </summary>
        private List<float> CalculateNiceTickValues(float min, float max, int targetCount)
        {
            var tickValues = new List<float>();
            
            if (Math.Abs(max - min) < 0.001f)
            {
                tickValues.Add(min);
                return tickValues;
            }
            
            // 计算合适的刻度间隔
            float range = max - min;
            float roughStep = range / (targetCount - 1);
            
            // 找到合适的步长（使用10的幂次）
            float magnitude = (float)Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            float normalizedStep = roughStep / magnitude;
            
            float niceStep;
            if (normalizedStep <= 1.0f)
                niceStep = 1.0f * magnitude;
            else if (normalizedStep <= 2.0f)
                niceStep = 2.0f * magnitude;
            else if (normalizedStep <= 5.0f)
                niceStep = 5.0f * magnitude;
            else
                niceStep = 10.0f * magnitude;
            
            // 计算起始点（向下取整到最近的步长倍数）
            float niceMin = (float)(Math.Floor(min / niceStep) * niceStep);
            float niceMax = (float)(Math.Ceiling(max / niceStep) * niceStep);
            
            // 生成刻度值
            for (float tick = niceMin; tick <= niceMax + niceStep * 0.001f; tick += niceStep)
            {
                if (tick >= min - niceStep * 0.001f && tick <= max + niceStep * 0.001f)
                {
                    tickValues.Add((float)Math.Round(tick, 6)); // 避免浮点精度问题
                }
            }
            
            // 确保包含0点（如果在范围内）
            if (min < 0 && max > 0 && !tickValues.Any(v => Math.Abs(v) < 0.001f))
            {
                tickValues.Add(0.0f);
                tickValues = tickValues.OrderBy(v => v).ToList();
            }
            
            return tickValues;
        }
        
        /// <summary>
        /// 添加单个轴的刻度线和数字标签（只在一条边上显示）
        /// </summary>
        private void AddSingleAxisTicks(List<float> tickVertices, List<float> labelVertices, Vector3 origin, Vector3 direction, float length, Vector3 color, float minValue, float maxValue, Matrix4 view)
        {
            // 计算合适的刻度值
            var tickValues = CalculateNiceTickValues(minValue, maxValue, 5);

            // 根据轴长度自适应刻度线长度，避免在大模型下过短不可见
            float tickLen = MathF.Max(TickLength, 0.03f * MathF.Max(0.0001f, length));
        
            foreach (float value in tickValues)
            {
                // 计算刻度在轴上的位置
                float t = length * (value - minValue) / (maxValue - minValue);
                var tickPos = origin + direction * t;
        
                // 创建单条刻度线（垂直于轴方向）
                Vector3 perpendicular;
                if (Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) < 0.9f)
                {
                    perpendicular = Vector3.Cross(direction, Vector3.UnitY).Normalized();
                }
                else
                {
                    perpendicular = Vector3.Cross(direction, Vector3.UnitX).Normalized();
                }
        
                // 添加一条垂直的刻度线（从轴线开始向外延伸）
                var tickStart = tickPos; // 起点在轴线上
                var tickEnd = tickPos + perpendicular * tickLen; // 终点向外延伸
        
                // 添加刻度线
                tickVertices.AddRange(new float[] {
                    tickStart.X, tickStart.Y, tickStart.Z, color.X, color.Y, color.Z,
                    tickEnd.X, tickEnd.Y, tickEnd.Z, color.X, color.Y, color.Z
                });
        
                // 特别标记0点（如果是0点，添加额外的标记）
                if (Math.Abs(value) < 0.001f)
                {
                    // 为0点添加一个十字标记（从轴线开始向外延伸）
                    Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).Normalized();
                    var zeroMark1Start = tickPos; // 起点在轴线上
                    var zeroMark1End = tickPos + perpendicular2 * tickLen * 0.6f; // 向外延伸
        
                    tickVertices.AddRange(new float[] {
                        zeroMark1Start.X, zeroMark1Start.Y, zeroMark1Start.Z, color.X, color.Y, color.Z,
                        zeroMark1End.X, zeroMark1End.Y, zeroMark1End.Z, color.X, color.Y, color.Z
                    });
        
                    // 仅在0刻度处添加一个数字标签，并确保整个帧内只绘制一次
                    if (!_zeroLabelDrawn)
                    {
                        AddTickLabel(labelVertices, tickPos, direction, value, color, view);
                        _zeroLabelDrawn = true;
                    }
                }
                else
                {
                    // 为非 0 刻度总是添加数字标签
                    AddTickLabel(labelVertices, tickPos, direction, value, color, view);
                }
            }
        }
        
        /// <summary>
        /// 渲染线条
        /// </summary>
        private void RenderLines(float[] vertices, int shaderProgram, Matrix4 view, Matrix4 projection)
        {
            // 保留原有签名，使用默认线宽渲染（用于包围盒和刻度线）
            RenderLines(vertices, shaderProgram, view, projection, LineWidth);
        }
        private void RenderLines(float[] vertices, int shaderProgram, Matrix4 view, Matrix4 projection, float lineWidth)
        {
            GL.UseProgram(shaderProgram);
            
            var model = Matrix4.Identity;
            var mvp = model * view * projection;
            
            int mvpLocation = GL.GetUniformLocation(shaderProgram, "uMVP");
            if (mvpLocation >= 0)
            {
                GL.UniformMatrix4(mvpLocation, false, ref mvp);
            }
            
            // 绑定VAO和VBO
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            
            // 上传顶点数据
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
            
            // 设置顶点属性
            // 位置属性 (location = 0)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            
            // 颜色属性 (location = 1)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            
            // 设置线条宽度
            GL.LineWidth(lineWidth);
            
            // 渲染线条
            GL.DrawArrays(PrimitiveType.Lines, 0, vertices.Length / 6);
            
            // 清理
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            if (_isInitialized)
            {
                GL.DeleteVertexArrays(1, ref _vao);
                GL.DeleteBuffers(1, ref _vbo);
                _isInitialized = false;
            }
        }
    }
}
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Core;

namespace Avalonia3DControl.Rendering.OpenGL
{
    /// <summary>
    /// 模型渲染器，负责管理模型的VAO/VBO和渲染逻辑
    /// </summary>
    public class ModelRenderer : IDisposable
    {
        #region 私有字段
        private Dictionary<Model3D, ModelRenderData> _modelRenderData;
        private int _defaultTexture;
        #endregion

        #region 内部类
        private class ModelRenderData
        {
            public int VAO { get; set; }
            public int VBO { get; set; }
            public int EBO { get; set; }
            public int LineEBO { get; set; }
            public int LineIndexCount { get; set; }
        }
        #endregion

        #region 构造函数
        public ModelRenderer(int defaultTexture)
        {
            _modelRenderData = new Dictionary<Model3D, ModelRenderData>();
            _defaultTexture = defaultTexture;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 渲染模型
        /// </summary>
        /// <param name="model">要渲染的模型</param>
        /// <param name="shaderProgram">着色器程序</param>
        /// <param name="renderMode">渲染模式</param>
        public void RenderModel(Model3D model, int shaderProgram, RenderMode renderMode)
        {
            if (model == null || !model.Visible) 
            {
                return;
            }

            // 确保模型有渲染数据
            if (!_modelRenderData.ContainsKey(model))
            {
                CreateModelRenderData(model);
            }

            var renderData = _modelRenderData[model];

            // 设置模型矩阵
            Matrix4 modelMatrix = model.GetModelMatrix();
            SetMatrix(shaderProgram, "model", modelMatrix);

            // 设置材质属性
            SetMaterialProperties(shaderProgram, model);
            
            // 设置材质透明度
            var alphaLocation = GL.GetUniformLocation(shaderProgram, "materialAlpha");
            if (alphaLocation != -1)
            {
                // 坐标轴相关模型始终保持完全不透明，不受UI透明度控制影响
                bool isAxesModel = model.Name == "MiniAxes" || model.Name == "CoordinateAxes";
                float alpha = isAxesModel ? 1.0f : (model.Material?.Alpha ?? model.Alpha);
                GL.Uniform1(alphaLocation, alpha);
            }
            
            // 设置点模式相关的uniform变量
            int pointModeLocation = GL.GetUniformLocation(shaderProgram, "uPointMode");
            if (pointModeLocation != -1)
            {
                GL.Uniform1(pointModeLocation, renderMode == RenderMode.Point ? 1 : 0);
            }
            
            int pointSizeLocation = GL.GetUniformLocation(shaderProgram, "uPointSize");
            if (pointSizeLocation != -1)
            {
                GL.Uniform1(pointSizeLocation, 5.0f);
            }

            // 绑定VAO
            GL.BindVertexArray(renderData.VAO);

            // 处理纹理
            HandleTexture(model);

            // 根据渲染模式绘制
            DrawModel(model, renderData, renderMode);

            // 解绑VAO
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// 更新模型顶点缓冲区
        /// </summary>
        /// <param name="model">要更新的模型</param>
        public void UpdateModelVertexBuffer(Model3D model)
        {
            if (!_modelRenderData.TryGetValue(model, out var renderData))
            {
                CreateModelRenderData(model);
                return;
            }

            // 检查缓冲区是否有效
            if (!GL.IsBuffer(renderData.VBO))
            {
                CreateModelRenderData(model);
                return;
            }

            try
            {
                // 重新分配顶点缓冲区数据
                GL.BindBuffer(BufferTarget.ArrayBuffer, renderData.VBO);
                var vertices = model.GetVertexData();
                GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新顶点缓冲区时出错: {ex.Message}");
                // 重新创建渲染数据
                CreateModelRenderData(model);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            foreach (var renderData in _modelRenderData.Values)
            {
                GL.DeleteVertexArray(renderData.VAO);
                GL.DeleteBuffer(renderData.VBO);
                GL.DeleteBuffer(renderData.EBO);
                if (renderData.LineEBO != 0) GL.DeleteBuffer(renderData.LineEBO);
            }
            _modelRenderData.Clear();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 创建模型渲染数据
        /// </summary>
        /// <param name="model">模型</param>
        private void CreateModelRenderData(Model3D model)
        {
            // 如果已存在，先清理
            if (_modelRenderData.TryGetValue(model, out var existingData))
            {
                GL.DeleteVertexArray(existingData.VAO);
                GL.DeleteBuffer(existingData.VBO);
                GL.DeleteBuffer(existingData.EBO);
                if (existingData.LineEBO != 0) GL.DeleteBuffer(existingData.LineEBO);
            }

            var renderData = new ModelRenderData();

            // 创建VAO
            renderData.VAO = GL.GenVertexArray();
            GL.BindVertexArray(renderData.VAO);

            // 创建VBO
            renderData.VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, renderData.VBO);
            var vertices = model.GetVertexData();
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // 设置顶点属性
            SetupVertexAttributes();

            // 创建EBO（用于三角形）
            if (model.Indices != null && model.Indices.Length > 0)
            {
                renderData.EBO = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.EBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, model.Indices.Length * sizeof(uint), model.Indices, BufferUsageHint.StaticDraw);
            }

            // 创建线框索引缓冲区
            CreateLineIndices(model, renderData);

            GL.BindVertexArray(0);
            _modelRenderData[model] = renderData;
        }

        /// <summary>
        /// 设置顶点属性
        /// </summary>
        private void SetupVertexAttributes()
        {
            // 位置属性 (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // 颜色属性 (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // 注意：当前模型只有位置和颜色属性（6个分量），不包含纹理坐标和法向量
            // 如果需要纹理坐标和法向量，需要在GeometryFactory中修改顶点数据格式
        }

        /// <summary>
        /// 创建线框索引
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="renderData">渲染数据</param>
        private void CreateLineIndices(Model3D model, ModelRenderData renderData)
        {
            if (model.Indices == null || model.Indices.Length == 0) return;

            var lineIndices = new List<uint>();
            for (int i = 0; i < model.Indices.Length; i += 3)
            {
                if (i + 2 < model.Indices.Length)
                {
                    uint i0 = model.Indices[i];
                    uint i1 = model.Indices[i + 1];
                    uint i2 = model.Indices[i + 2];

                    // 添加三角形的三条边
                    lineIndices.Add(i0); lineIndices.Add(i1);
                    lineIndices.Add(i1); lineIndices.Add(i2);
                    lineIndices.Add(i2); lineIndices.Add(i0);
                }
            }

            if (lineIndices.Count > 0)
            {
                renderData.LineEBO = GL.GenBuffer();
                renderData.LineIndexCount = lineIndices.Count;
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.LineEBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, lineIndices.Count * sizeof(uint), lineIndices.ToArray(), BufferUsageHint.StaticDraw);
            }
        }

        /// <summary>
        /// 设置材质属性
        /// </summary>
        /// <param name="shaderProgram">着色器程序</param>
        /// <param name="model">模型</param>
        private void SetMaterialProperties(int shaderProgram, Model3D model)
        {
            // 设置透明度
            int alphaLocation = GL.GetUniformLocation(shaderProgram, "alpha");
            if (alphaLocation != -1)
            {
                GL.Uniform1(alphaLocation, model.Alpha);
            }

            // 设置材质属性
            if (model.Material != null)
            {
                SetMaterial(shaderProgram, model.Material);
            }
        }

        /// <summary>
        /// 设置材质
        /// </summary>
        /// <param name="shaderProgram">着色器程序</param>
        /// <param name="material">材质</param>
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

            int alphaLocation = GL.GetUniformLocation(shaderProgram, "materialAlpha");
            if (alphaLocation != -1)
            {
                GL.Uniform1(alphaLocation, material.Alpha);
            }
        }

        /// <summary>
        /// 处理纹理
        /// </summary>
        /// <param name="model">模型</param>
        private void HandleTexture(Model3D model)
        {
            if (model.TextureId > 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, model.TextureId);
            }
            else
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _defaultTexture);
            }
        }

        /// <summary>
        /// 绘制模型
        /// </summary>
        /// <param name="model">模型</param>
        /// <param name="renderData">渲染数据</param>
        /// <param name="renderMode">渲染模式</param>
        private void DrawModel(Model3D model, ModelRenderData renderData, RenderMode renderMode)
        {
            switch (renderMode)
            {
                case RenderMode.Line:
                    if (renderData.LineEBO != 0)
                    {
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.LineEBO);
                        GL.DrawElements(PrimitiveType.Lines, renderData.LineIndexCount, DrawElementsType.UnsignedInt, 0);
                    }
                    break;

                case RenderMode.Point:
                    if (model.Indices != null && model.Indices.Length > 0)
                    {
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.EBO);
                        GL.DrawElements(PrimitiveType.Points, model.Indices.Length, DrawElementsType.UnsignedInt, 0);
                    }
                    break;

                case RenderMode.Fill:
                default:
                    if (model.Indices != null && model.Indices.Length > 0)
                    {
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, renderData.EBO);
                        GL.DrawElements(PrimitiveType.Triangles, model.Indices.Length, DrawElementsType.UnsignedInt, 0);
                    }
                    break;
            }
        }

        /// <summary>
        /// 设置矩阵
        /// </summary>
        /// <param name="shaderProgram">着色器程序</param>
        /// <param name="name">uniform名称</param>
        /// <param name="matrix">矩阵</param>
        private void SetMatrix(int shaderProgram, string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(shaderProgram, name);
            if (location != -1)
            {
                GL.UniformMatrix4(location, false, ref matrix);
            }
        }
        #endregion

        #region IDisposable实现
        public void Dispose()
        {
            Cleanup();
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Avalonia3DControl.Core.Lighting;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.Geometry.Factories;
using Avalonia3DControl.Materials;

namespace Avalonia3DControl.Core
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
        
        private Model3D? _currentModel;
        private bool _coordinateAxesVisible = false;
        
        // 独立的坐标轴模型
        public Model3D? CoordinateAxes { get; private set; }
        public bool ShowCoordinateAxes => _coordinateAxesVisible && CoordinateAxes != null;
        
        // 迷你坐标轴
        public MiniAxes MiniAxes { get; private set; }

        public Scene3D()
        {
            Camera = new Camera();
            Lights = new List<Light>();
            Models = new List<Model3D>();
            BackgroundColor = new Vector3(0.2f, 0.3f, 0.3f);
            
            // 添加默认光源
            AddDefaultLight();
            
            // 初始化坐标轴
            CoordinateAxes = new CoordinateAxesModel();
            CoordinateAxes.Name = "CoordinateAxes";
            
            // 初始化迷你坐标轴
            MiniAxes = new MiniAxes();
        }
        
        /// <summary>
        /// 动态创建指定类型的模型
        /// </summary>
        /// <param name="modelType">模型类型名称</param>
        /// <returns>创建的模型，如果类型不支持则返回null</returns>
        public Model3D? CreateModel(string modelType)
        {
            return GeometryFactory.CreateModel(modelType);
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
            
            Model3D? mainModel = null;
            
            // 如果指定了模型类型，创建并添加新模型
            if (!string.IsNullOrEmpty(modelType))
            {
                mainModel = CreateModel(modelType);
                if (mainModel != null)
                {
                    Models.Add(mainModel);
                }
            }
            
            // 坐标轴不再作为普通模型处理，将独立渲染
            // 确保坐标轴不在普通模型列表中
            if (CoordinateAxes != null && Models.Contains(CoordinateAxes))
            {
                Models.Remove(CoordinateAxes);
            }
            
            return mainModel;
        }
        
        /// <summary>
        /// 设置坐标轴显示状态
        /// </summary>
        /// <param name="visible">是否显示坐标轴</param>
        public void SetCoordinateAxesVisible(bool visible)
        {
            _coordinateAxesVisible = visible;
            
            if (visible)
            {
                // 创建坐标轴模型（如果还没有创建）
                 if (CoordinateAxes == null)
                 {
                     CoordinateAxes = GeometryFactory.CreateCoordinateAxes();
                 }
            }
            
            // 坐标轴不再作为普通模型处理，完全独立渲染
            // 移除坐标轴从普通模型列表中（如果存在）
            if (CoordinateAxes != null && Models.Contains(CoordinateAxes))
            {
                Models.Remove(CoordinateAxes);
            }
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
}
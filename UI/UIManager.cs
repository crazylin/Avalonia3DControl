using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using OpenTK.Mathematics;
using System;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Core;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.Rendering;

namespace Avalonia3DControl.UI
{
    /// <summary>
    /// UI管理器，负责处理UI事件和控件交互
    /// </summary>
    public class UIManager
    {
        private readonly Window _window;
        private readonly OpenGL3DControl _openGLControl;

        public UIManager(Window window, OpenGL3DControl openGLControl)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _openGLControl = openGLControl ?? throw new ArgumentNullException(nameof(openGLControl));
        }

        /// <summary>
        /// 设置所有UI事件处理器
        /// </summary>
        public void SetupEventHandlers()
        {
            SetupShadingModeHandlers();
            SetupRenderModeHandlers();
            SetupProjectionModeHandlers();
            SetupViewLockHandlers();
            SetupMaterialHandlers();
            SetupTransparencyHandlers();
            SetupModelSelectionHandlers();
            SetupCoordinateAxesHandler();
            SetupMiniAxesHandlers();
        }

        /// <summary>
        /// 设置着色模式事件处理器
        /// </summary>
        private void SetupShadingModeHandlers()
        {
            var shadingModes = new[]
            {
                ("VertexShadingRadio", ShadingMode.Vertex),
                ("TextureShadingRadio", ShadingMode.Texture),
                ("MaterialShadingRadio", ShadingMode.Material)
            };

            foreach (var (controlName, mode) in shadingModes)
            {
                var radio = _window.FindControl<RadioButton>(controlName);
                if (radio != null)
                {
                    radio.IsCheckedChanged += (s, e) =>
                    {
                        if (radio.IsChecked == true)
                            _openGLControl.SetShadingMode(mode);
                    };
                }
            }
        }

        /// <summary>
        /// 设置渲染模式事件处理器
        /// </summary>
        private void SetupRenderModeHandlers()
        {
            var renderModes = new[]
            {
                ("PointRenderRadio", RenderMode.Point),
                ("LineRenderRadio", RenderMode.Line),
                ("FillRenderRadio", RenderMode.Fill)
            };

            foreach (var (controlName, mode) in renderModes)
            {
                var radio = _window.FindControl<RadioButton>(controlName);
                if (radio != null)
                {
                    radio.IsCheckedChanged += (s, e) =>
                    {
                        if (radio.IsChecked == true)
                            _openGLControl.SetRenderMode(mode);
                    };
                }
            }
        }

        /// <summary>
        /// 设置投影模式事件处理器
        /// </summary>
        private void SetupProjectionModeHandlers()
        {
            var perspective3DRadio = _window.FindControl<RadioButton>("Perspective3DRadio");
            var orthographic2DRadio = _window.FindControl<RadioButton>("Orthographic2DRadio");

            if (perspective3DRadio != null)
            {
                perspective3DRadio.IsCheckedChanged += (s, e) =>
                {
                    if (perspective3DRadio.IsChecked == true)
                        _openGLControl.SwitchToPerspective();
                };
            }

            if (orthographic2DRadio != null)
            {
                orthographic2DRadio.IsCheckedChanged += (s, e) =>
                {
                    if (orthographic2DRadio.IsChecked == true)
                        _openGLControl.SwitchToOrthographic();
                };
            }
        }

        /// <summary>
        /// 设置视图锁定事件处理器
        /// </summary>
        private void SetupViewLockHandlers()
        {
            var noViewLockRadio = _window.FindControl<RadioButton>("NoViewLockRadio");
            var xyViewLockRadio = _window.FindControl<RadioButton>("XYViewLockRadio");
            var yzViewLockRadio = _window.FindControl<RadioButton>("YZViewLockRadio");
            var xzViewLockRadio = _window.FindControl<RadioButton>("XZViewLockRadio");

            if (noViewLockRadio != null)
            {
                noViewLockRadio.IsCheckedChanged += (s, e) =>
                {
                    if (noViewLockRadio.IsChecked == true)
                        _openGLControl.SetViewLock(ViewLockMode.None);
                };
            }

            if (xyViewLockRadio != null)
            {
                xyViewLockRadio.IsCheckedChanged += (s, e) =>
                {
                    if (xyViewLockRadio.IsChecked == true)
                        _openGLControl.SetViewLock(ViewLockMode.XY);
                };
            }

            if (yzViewLockRadio != null)
            {
                yzViewLockRadio.IsCheckedChanged += (s, e) =>
                {
                    if (yzViewLockRadio.IsChecked == true)
                        _openGLControl.SetViewLock(ViewLockMode.YZ);
                };
            }

            if (xzViewLockRadio != null)
            {
                xzViewLockRadio.IsCheckedChanged += (s, e) =>
                {
                    if (xzViewLockRadio.IsChecked == true)
                        _openGLControl.SetViewLock(ViewLockMode.XZ);
                };
            }
        }

        /// <summary>
        /// 设置材质预设按钮事件处理器
        /// </summary>
        private void SetupMaterialHandlers()
        {
            var materials = new (string, Func<Material>)[]
            {
                ("PlasticMaterialBtn", () => Material.CreatePlastic(new Vector3(0.9f, 0.9f, 0.9f))),
                ("MetalMaterialBtn", () => Material.CreateMetal(new Vector3(0.7f, 0.7f, 0.8f))),
                ("GlassMaterialBtn", () => Material.CreateGlass(new Vector3(0.2f, 0.6f, 0.9f)))
            };

            foreach (var (controlName, materialFactory) in materials)
            {
                var button = _window.FindControl<Button>(controlName);
                if (button != null)
                {
                    button.Click += (s, e) => ApplyMaterialToAllModels(materialFactory());
                }
            }
        }

        /// <summary>
        /// 设置模型选择事件处理器
        /// </summary>
        private void SetupModelSelectionHandlers()
        {
            var models = new[]
            {
                ("NoneModelRadio", (string?)null),
                ("CubeModelRadio", "Cube"),
                ("SphereModelRadio", "Sphere"),
                ("WaveModelRadio", "Wave"),
                ("WaterDropModelRadio", "WaterDrop")
            };

            foreach (var (controlName, modelName) in models)
            {
                var radio = _window.FindControl<RadioButton>(controlName);
                if (radio != null)
                {
                    radio.IsCheckedChanged += (s, e) =>
                    {
                        if (radio.IsChecked == true)
                            ShowOnlyModel(modelName);
                    };
                }
            }
        }

        /// <summary>
        /// 设置坐标轴显示事件处理器
        /// </summary>
        private void SetupCoordinateAxesHandler()
        {
            var showCoordinateAxesCheckBox = _window.FindControl<CheckBox>("ShowCoordinateAxesCheckBox");
            if (showCoordinateAxesCheckBox != null)
            {
                showCoordinateAxesCheckBox.IsCheckedChanged += (s, e) =>
                {
                    _openGLControl.SetCoordinateAxesVisible(showCoordinateAxesCheckBox.IsChecked == true);
                };
            }
        }

        /// <summary>
        /// 应用材质到所有模型
        /// </summary>
        /// <param name="material">要应用的材质</param>
        private void ApplyMaterialToAllModels(Material material)
        {
            var scene = _openGLControl.Scene;
            if (scene != null)
            {
                // 应用材质到普通模型
                foreach (var model in scene.Models)
                {
                    model.Material = material;
                }
                
                // 对坐标轴只更新透明度，保持其原有的金属材质特性
                if (scene.CoordinateAxes != null && scene.CoordinateAxes.Material != null)
                {
                    scene.CoordinateAxes.Material.Alpha = material.Alpha;
                }
                
                // 对迷你坐标轴只更新透明度
                if (scene.MiniAxes?.AxesModel != null && scene.MiniAxes.AxesModel.Material != null)
                {
                    scene.MiniAxes.AxesModel.Material.Alpha = material.Alpha;
                }
                
                // 强制重绘以显示材质变化
                _openGLControl.RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// 设置透明度控制事件处理器
        /// </summary>
        private void SetupTransparencyHandlers()
        {
            var alphaSlider = _window.FindControl<Slider>("AlphaSlider");
            var alphaValueText = _window.FindControl<TextBlock>("AlphaValueText");
            
            if (alphaSlider != null)
            {
                alphaSlider.ValueChanged += (s, e) =>
                {
                    float alphaValue = (float)alphaSlider.Value;
                    
                    // 更新显示文本
                    if (alphaValueText != null)
                    {
                        int percentage = (int)(alphaValue * 100);
                        alphaValueText.Text = $"透明度: {percentage}%";
                    }
                    
                    // 应用透明度到所有模型
                    ApplyAlphaToAllModels(alphaValue);
                };
            }
            
            // 设置材质预设按钮事件
            SetupMaterialPresetHandlers();
        }
        
        /// <summary>
        /// 设置材质预设按钮事件处理器
        /// </summary>
        private void SetupMaterialPresetHandlers()
        {
            var plasticButton = _window.FindControl<Button>("PlasticButton");
            var metalButton = _window.FindControl<Button>("MetalButton");
            var glassButton = _window.FindControl<Button>("GlassButton");
            var alphaSlider = _window.FindControl<Slider>("AlphaSlider");
            
            if (plasticButton != null)
             {
                 plasticButton.Click += (s, e) =>
                 {
                     float alpha = alphaSlider?.Value != null ? (float)alphaSlider.Value : 1.0f;
                     var color = new Vector3(0.9f, 0.9f, 0.9f); // 白色塑料
                     var material = Material.CreatePlastic(color, alpha);
                     ApplyMaterialToAllModels(material);
                 };
             }
             
             if (metalButton != null)
             {
                 metalButton.Click += (s, e) =>
                 {
                     float alpha = alphaSlider?.Value != null ? (float)alphaSlider.Value : 1.0f;
                     var color = new Vector3(0.7f, 0.7f, 0.8f); // 银色金属
                     var material = Material.CreateMetal(color, alpha);
                     ApplyMaterialToAllModels(material);
                 };
             }
             
             if (glassButton != null)
             {
                 glassButton.Click += (s, e) =>
                 {
                     float alpha = alphaSlider?.Value != null ? (float)alphaSlider.Value : 0.7f;
                     var color = new Vector3(0.8f, 0.9f, 1.0f); // 淡蓝色玻璃
                     var material = Material.CreateGlass(color, alpha);
                     ApplyMaterialToAllModels(material);
                     
                     // 更新滑块值以反映玻璃的默认透明度
                     if (alphaSlider != null)
                     {
                         alphaSlider.Value = alpha;
                     }
                 };
             }
        }

        /// <summary>
        /// 应用透明度到所有模型
        /// </summary>
        /// <param name="alpha">透明度值 (0.0-1.0)</param>
        private void ApplyAlphaToAllModels(float alpha)
        {
            var scene = _openGLControl.Scene;
            if (scene != null)
            {
                // 应用透明度到普通模型
                foreach (var model in scene.Models)
                {
                    if (model.Material != null)
                    {
                        model.Material.Alpha = alpha;
                    }
                }
                
                // 应用透明度到坐标轴
                if (scene.CoordinateAxes != null && scene.CoordinateAxes.Material != null)
                {
                    scene.CoordinateAxes.Material.Alpha = alpha;
                }
                
                // 应用透明度到迷你坐标轴
                if (scene.MiniAxes?.AxesModel != null && scene.MiniAxes.AxesModel.Material != null)
                {
                    scene.MiniAxes.AxesModel.Material.Alpha = alpha;
                }
                
                // 强制重绘以显示透明度变化
                _openGLControl.RequestNextFrameRendering();
            }
        }

        /// <summary>
        /// 显示指定模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        private void ShowOnlyModel(string? modelName)
        {
            // 切换到模型
            _openGLControl.SetCurrentModel(modelName);
        }

        /// <summary>
        /// 设置迷你坐标轴事件处理器
        /// </summary>
        private void SetupMiniAxesHandlers()
        {
            var showMiniAxesCheckBox = _window.FindControl<CheckBox>("ShowMiniAxesCheckBox");
            if (showMiniAxesCheckBox != null)
            {
                showMiniAxesCheckBox.IsCheckedChanged += (s, e) =>
                {
                    var scene = _openGLControl.Scene;
                    if (scene != null)
                    {
                        scene.MiniAxes.Visible = showMiniAxesCheckBox.IsChecked == true;
                    }
                };
            }
            
            // 设置迷你坐标轴位置选择事件处理器
            var positionRadios = new[]
            {
                ("MiniAxesTopLeftRadio", MiniAxesPosition.TopLeft),
                ("MiniAxesTopRightRadio", MiniAxesPosition.TopRight),
                ("MiniAxesBottomLeftRadio", MiniAxesPosition.BottomLeft),
                ("MiniAxesBottomRightRadio", MiniAxesPosition.BottomRight)
            };
            
            foreach (var (controlName, position) in positionRadios)
            {
                var radioButton = _window.FindControl<RadioButton>(controlName);
                if (radioButton != null)
                {
                    radioButton.IsCheckedChanged += (s, e) =>
                    {
                        if (radioButton.IsChecked == true)
                        {
                            var scene = _openGLControl.Scene;
                            if (scene != null)
                            {
                                scene.MiniAxes.Position = position;
                                // 触发重新渲染
                                _openGLControl.RequestNextFrameRendering();
                            }
                        }
                    };
                }
            }
        }
        
        /// <summary>
        /// 触发默认模型加载
        /// </summary>
        public void TriggerDefaultModelLoad()
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var cubeModelRadio = _window.FindControl<RadioButton>("CubeModelRadio");
                if (cubeModelRadio?.IsChecked == true)
                {
                    ShowOnlyModel("Cube");
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using OpenTK.Mathematics;
using System;
using System.Linq;
using Avalonia3DControl.Materials;
using Avalonia3DControl.Core;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.Core.Cameras;
using Avalonia3DControl.Rendering;
using Avalonia3DControl.Core.Animation;

namespace Avalonia3DControl.UI
{
    /// <summary>
    /// UI管理器，负责处理UI事件和控件交互
    /// </summary>
    public class UIManager
    {
        private readonly Window _window;
        private readonly OpenGL3DControl _openGLControl;
        private ModalAnimationPanel? _modalAnimationPanel;

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
            SetupGradientBarHandlers();
            SetupModalAnimationPanel();
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
                ("WaterDropModelRadio", "WaterDrop"),
                ("CantileverBeamModelRadio", "CantileverBeam")
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
                if (scene.CoordinateAxes?.AxesModel != null && scene.CoordinateAxes.AxesModel.Material != null)
                {
                    scene.CoordinateAxes.AxesModel.Material.Alpha = material.Alpha;
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
                if (scene.CoordinateAxes?.AxesModel != null && scene.CoordinateAxes.AxesModel.Material != null)
                {
                    scene.CoordinateAxes.AxesModel.Material.Alpha = alpha;
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
            Console.WriteLine($"[UIManager] 显示模型: {modelName}");
            
            // 切换到模型
            _openGLControl.SetCurrentModel(modelName);
            Console.WriteLine($"[UIManager] 已设置当前模型: {modelName}");
            
            // 如果加载了模型，为其设置动画控制器
            if (!string.IsNullOrEmpty(modelName) && _modalAnimationPanel != null)
            {
                Console.WriteLine("[UIManager] 开始设置动画控制器和梯度条事件");
                
                // 订阅梯度条事件
                _modalAnimationPanel.GradientBarVisibilityChanged += OnGradientBarVisibilityChanged;
                _modalAnimationPanel.GradientBarPositionChanged += OnGradientBarPositionChanged;
                // 订阅颜色梯度变化事件
                _modalAnimationPanel.ColorGradientTypeChanged += (newType) =>
                {
                    _openGLControl?.SetGradientBarType(newType);
                };
                
                // 初始设置动画控制器（将在模型选择时更新）
                SetupAnimationForCurrentModel();
                Console.WriteLine("[UIManager] 动画控制器设置完成");
            }
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
            Console.WriteLine("[UIManager] 触发默认模型加载");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var cubeModelRadio = _window.FindControl<RadioButton>("CubeModelRadio");
                Console.WriteLine($"[UIManager] CubeModelRadio状态: {cubeModelRadio?.IsChecked}");
                if (cubeModelRadio?.IsChecked == true)
                {
                    Console.WriteLine("[UIManager] 开始加载立方体模型");
                    ShowOnlyModel("Cube");
                }
                else
                {
                    Console.WriteLine("[UIManager] 立方体单选按钮未选中，手动选中并加载");
                    if (cubeModelRadio != null)
                    {
                        cubeModelRadio.IsChecked = true;
                    }
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        
        /// <summary>
        /// 设置振型动画控制面板
        /// </summary>
        private void SetupModalAnimationPanel()
        {
            _modalAnimationPanel = _window.FindControl<ModalAnimationPanel>("ModalAnimationPanel");
            
            if (_modalAnimationPanel != null)
            {
                // 订阅梯度条事件
                _modalAnimationPanel.GradientBarVisibilityChanged += OnGradientBarVisibilityChanged;
                _modalAnimationPanel.GradientBarPositionChanged += OnGradientBarPositionChanged;
                
                // 初始设置动画控制器（将在模型选择时更新）
                SetupAnimationForCurrentModel();
            }
        }
        
        /// <summary>
        /// 为当前模型设置动画
        /// </summary>
        private void SetupAnimationForCurrentModel()
        {
            var scene = _openGLControl.Scene;
            if (scene?.Models != null && scene.Models.Count > 0)
            {
                // 为第一个模型启用振型动画（通常是主模型）
                var mainModel = scene.Models.FirstOrDefault();
                if (mainModel != null && _modalAnimationPanel != null)
                {
                    // 根据模型名称选择合适的振型数据
                    ModalDataSet modalDataSet;
                    if (mainModel.Name != null && mainModel.Name.Contains("CantileverBeam"))
                    {
                        // 为悬臂梁模型使用专门的振型数据
                        modalDataSet = _modalAnimationPanel.CreateCantileverBeamModalData();
                    }
                    else
                    {
                        // 为其他模型使用通用振型数据
                        modalDataSet = _modalAnimationPanel.CreateSampleModalData();
                    }
                    
                    // 启用模型的振型动画
                    mainModel.EnableModalAnimation(modalDataSet);
                    
                    // 将动画控制器绑定到UI面板
                    if (mainModel.AnimationController != null)
                    {
                        _modalAnimationPanel.SetAnimationController(mainModel.AnimationController, mainModel);
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置梯度条控制事件处理器
        /// </summary>
        private void SetupGradientBarHandlers()
        {
            var showGradientBarCheckBox = _window.FindControl<CheckBox>("ShowGradientBarCheckBox");
            if (showGradientBarCheckBox != null)
            {
                showGradientBarCheckBox.IsCheckedChanged += (s, e) =>
                {
                    OnGradientBarVisibilityChanged(showGradientBarCheckBox.IsChecked == true);
                };
            }

            // 新增：显示刻度/归一化刻度
            var gradientBarShowTicksCheckBox = _window.FindControl<CheckBox>("GradientBarShowTicksCheckBox");
            if (gradientBarShowTicksCheckBox != null)
            {
                gradientBarShowTicksCheckBox.IsCheckedChanged += (s, e) =>
                {
                    _openGLControl.SetGradientBarShowTicks(gradientBarShowTicksCheckBox.IsChecked == true);
                };
            }

            var gradientBarNormalizedScaleCheckBox = _window.FindControl<CheckBox>("GradientBarNormalizedScaleCheckBox");
            if (gradientBarNormalizedScaleCheckBox != null)
            {
                gradientBarNormalizedScaleCheckBox.IsCheckedChanged += (s, e) =>
                {
                    _openGLControl.SetGradientBarUseNormalizedScale(gradientBarNormalizedScaleCheckBox.IsChecked == true);
                };
            }

            var gradientBarLeftRadio = _window.FindControl<RadioButton>("GradientBarLeftRadio");
            var gradientBarRightRadio = _window.FindControl<RadioButton>("GradientBarRightRadio");
            
            
            if (gradientBarLeftRadio != null)
            {
                gradientBarLeftRadio.IsCheckedChanged += (s, e) =>
                {

                    if (gradientBarLeftRadio.IsChecked == true)
                        OnGradientBarPositionChanged(GradientBarPosition.Left);
                };
            }
            
            if (gradientBarRightRadio != null)
            {
                gradientBarRightRadio.IsCheckedChanged += (s, e) =>
                {

                    if (gradientBarRightRadio.IsChecked == true)
                        OnGradientBarPositionChanged(GradientBarPosition.Right);
                };
            }
        }
        
        /// <summary>
        /// 处理梯度条可见性变化事件
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        private void OnGradientBarVisibilityChanged(bool isVisible)
        {
            _openGLControl.SetGradientBarVisible(isVisible);
        }
        
        /// <summary>
        /// 处理梯度条位置变化事件
        /// </summary>
        /// <param name="position">梯度条位置</param>
        private void OnGradientBarPositionChanged(GradientBarPosition position)
        {
            _openGLControl.SetGradientBarPosition(position);
        }
    }
}
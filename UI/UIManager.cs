using Avalonia.Controls;
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
                ("TextureShadingRadio", ShadingMode.Texture)
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
                ("PlasticMaterialBtn", () => Material.CreatePlastic(new Vector3(0.8f, 0.2f, 0.2f))),
                ("MetalMaterialBtn", () => Material.CreateMetal(new Vector3(0.2f, 0.8f, 0.2f))),
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
                foreach (var model in scene.Models)
                {
                    model.Material = material;
                }
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
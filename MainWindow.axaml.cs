using Avalonia.Controls;
using System;
using System.Linq;
using OpenTK.Mathematics;

namespace Avalonia3DControl;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Console.WriteLine("MainWindow构造函数开始...");
        InitializeComponent();
        Console.WriteLine("MainWindow初始化完成");
        
        // 设置事件处理器
        SetupEventHandlers();
        
        // 确保窗口显示
        this.Show();
        Console.WriteLine("MainWindow显示完成");
        
        // 延迟触发默认选中的模型加载，确保OpenGL上下文已初始化
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var cubeModelRadio = this.FindControl<RadioButton>("CubeModelRadio");
            if (cubeModelRadio?.IsChecked == true)
            {
                var openGLControl = this.FindControl<OpenGL3DControl>("OpenGLControl");
                if (openGLControl != null)
                {
                    ShowOnlyModel(openGLControl, "Cube");
                }
            }
        }, Avalonia.Threading.DispatcherPriority.Background);
    }
    
    private void SetupEventHandlers()
    {
        // 获取UI控件引用
        var flatShadingRadio = this.FindControl<RadioButton>("FlatShadingRadio");
        var gouraudShadingRadio = this.FindControl<RadioButton>("GouraudShadingRadio");
        var phongShadingRadio = this.FindControl<RadioButton>("PhongShadingRadio");
        var wireframeShadingRadio = this.FindControl<RadioButton>("WireframeShadingRadio");
        var vertexShadingRadio = this.FindControl<RadioButton>("VertexShadingRadio");
        var textureShadingRadio = this.FindControl<RadioButton>("TextureShadingRadio");
        
        var pointRenderRadio = this.FindControl<RadioButton>("PointRenderRadio");
        var lineRenderRadio = this.FindControl<RadioButton>("LineRenderRadio");
        var fillRenderRadio = this.FindControl<RadioButton>("FillRenderRadio");
        
        var plasticMaterialBtn = this.FindControl<Button>("PlasticMaterialBtn");
        var metalMaterialBtn = this.FindControl<Button>("MetalMaterialBtn");
        var glassMaterialBtn = this.FindControl<Button>("GlassMaterialBtn");
        
        var cubeVisibilityCheck = this.FindControl<CheckBox>("CubeVisibilityCheck");
        var sphereVisibilityCheck = this.FindControl<CheckBox>("SphereVisibilityCheck");
        var waveVisibilityCheck = this.FindControl<CheckBox>("WaveVisibilityCheck");
        var waterDropVisibilityCheck = this.FindControl<CheckBox>("WaterDropVisibilityCheck");
        
        var showAllModelsBtn = this.FindControl<Button>("ShowAllModelsBtn");
        var hideAllModelsBtn = this.FindControl<Button>("HideAllModelsBtn");
        
        var openGLControl = this.FindControl<OpenGL3DControl>("OpenGLControl");
        
        if (openGLControl == null) return;
        
        // 着色模式事件处理
        if (flatShadingRadio != null)
            flatShadingRadio.IsCheckedChanged += (s, e) => { if (flatShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Flat); };
        if (gouraudShadingRadio != null)
            gouraudShadingRadio.IsCheckedChanged += (s, e) => { if (gouraudShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Gouraud); };
        if (phongShadingRadio != null)
            phongShadingRadio.IsCheckedChanged += (s, e) => { if (phongShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Phong); };
        if (wireframeShadingRadio != null)
            wireframeShadingRadio.IsCheckedChanged += (s, e) => { if (wireframeShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Wireframe); };
        if (vertexShadingRadio != null)
            vertexShadingRadio.IsCheckedChanged += (s, e) => { if (vertexShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Vertex); };
        if (textureShadingRadio != null)
            textureShadingRadio.IsCheckedChanged += (s, e) => { if (textureShadingRadio.IsChecked == true) openGLControl.SetShadingMode(ShadingMode.Texture); };
        
        // 渲染模式事件处理
        if (pointRenderRadio != null)
            pointRenderRadio.IsCheckedChanged += (s, e) => { if (pointRenderRadio.IsChecked == true) openGLControl.SetRenderMode(RenderMode.Point); };
        if (lineRenderRadio != null)
            lineRenderRadio.IsCheckedChanged += (s, e) => { if (lineRenderRadio.IsChecked == true) openGLControl.SetRenderMode(RenderMode.Line); };
        if (fillRenderRadio != null)
            fillRenderRadio.IsCheckedChanged += (s, e) => { if (fillRenderRadio.IsChecked == true) openGLControl.SetRenderMode(RenderMode.Fill); };
        
        // 材质预设按钮事件处理
        if (plasticMaterialBtn != null)
            plasticMaterialBtn.Click += (s, e) => ApplyMaterialToAllModels(openGLControl, Material.CreatePlastic(new Vector3(0.8f, 0.2f, 0.2f)));
        if (metalMaterialBtn != null)
            metalMaterialBtn.Click += (s, e) => ApplyMaterialToAllModels(openGLControl, Material.CreateMetal(new Vector3(0.2f, 0.8f, 0.2f)));
        if (glassMaterialBtn != null)
            glassMaterialBtn.Click += (s, e) => ApplyMaterialToAllModels(openGLControl, Material.CreateGlass(new Vector3(0.2f, 0.6f, 0.9f)));
        
        // 模型选择单选按钮事件处理
        var noneModelRadio = this.FindControl<RadioButton>("NoneModelRadio");
        var cubeModelRadio = this.FindControl<RadioButton>("CubeModelRadio");
        var sphereModelRadio = this.FindControl<RadioButton>("SphereModelRadio");
        var waveModelRadio = this.FindControl<RadioButton>("WaveModelRadio");
        var waterDropModelRadio = this.FindControl<RadioButton>("WaterDropModelRadio");
        
        if (noneModelRadio != null)
            noneModelRadio.IsCheckedChanged += (s, e) => { if (noneModelRadio.IsChecked == true) ShowOnlyModel(openGLControl, null); };
        if (cubeModelRadio != null)
            cubeModelRadio.IsCheckedChanged += (s, e) => { if (cubeModelRadio.IsChecked == true) ShowOnlyModel(openGLControl, "Cube"); };
        if (sphereModelRadio != null)
            sphereModelRadio.IsCheckedChanged += (s, e) => { if (sphereModelRadio.IsChecked == true) ShowOnlyModel(openGLControl, "Sphere"); };
        if (waveModelRadio != null)
            waveModelRadio.IsCheckedChanged += (s, e) => { if (waveModelRadio.IsChecked == true) ShowOnlyModel(openGLControl, "Wave"); };
        if (waterDropModelRadio != null)
            waterDropModelRadio.IsCheckedChanged += (s, e) => { if (waterDropModelRadio.IsChecked == true) ShowOnlyModel(openGLControl, "WaterDrop"); };
    }
    
    private void ApplyMaterialToAllModels(OpenGL3DControl openGLControl, Material material)
    {
        // 获取场景中的所有模型并应用材质
        var scene = openGLControl.Scene;
        if (scene != null)
        {
            foreach (var model in scene.Models)
            {
                model.Material = material;
            }
        }
    }
    
    private void ShowOnlyModel(OpenGL3DControl openGLControl, string? modelName)
    {
        // 使用新的按需加载机制，只加载指定的模型
        Console.WriteLine($"切换到模型: {modelName}");
        openGLControl.SetCurrentModel(modelName);
    }
}
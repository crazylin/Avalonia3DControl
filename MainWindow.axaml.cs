using Avalonia.Controls;
using System;
using Avalonia3DControl.UI;

namespace Avalonia3DControl;

public partial class MainWindow : Window
{
    private UIManager? _uiManager;

    public MainWindow()
    {
        Console.WriteLine("[MainWindow] 开始初始化MainWindow");
        InitializeComponent();
        Console.WriteLine("[MainWindow] InitializeComponent完成");
        
        // 初始化UI管理器
        InitializeUIManager();
        Console.WriteLine("[MainWindow] UI管理器初始化完成");
        
        // 确保窗口显示
        this.Show();
        Console.WriteLine("[MainWindow] 窗口显示完成");
        
        // 延迟触发默认选中的模型加载，确保OpenGL上下文已初始化
        _uiManager?.TriggerDefaultModelLoad();
        Console.WriteLine("[MainWindow] 默认模型加载触发完成");
    }

    private void InitializeUIManager()
    {
        Console.WriteLine("[MainWindow] 开始查找OpenGL控件");
        var openGLControl = this.FindControl<OpenGL3DControl>("OpenGLControl");
        if (openGLControl != null)
        {
            Console.WriteLine("[MainWindow] OpenGL控件找到，开始创建UIManager");
            _uiManager = new UIManager(this, openGLControl);
            _uiManager.SetupEventHandlers();
            Console.WriteLine("[MainWindow] UIManager创建并设置事件处理完成");
        }
        else
        {
            Console.WriteLine("[MainWindow] 错误：未找到OpenGL控件！");
        }
    }

}
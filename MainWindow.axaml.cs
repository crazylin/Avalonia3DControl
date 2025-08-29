using Avalonia.Controls;
using System;
using Avalonia3DControl.UI;

namespace Avalonia3DControl;

public partial class MainWindow : Window
{
    private UIManager? _uiManager;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化UI管理器
        InitializeUIManager();
        
        // 确保窗口显示
        this.Show();
        
        // 延迟触发默认选中的模型加载，确保OpenGL上下文已初始化
        _uiManager?.TriggerDefaultModelLoad();
    }

    private void InitializeUIManager()
    {
        var openGLControl = this.FindControl<OpenGL3DControl>("OpenGLControl");
        if (openGLControl != null)
        {
            _uiManager = new UIManager(this, openGLControl);
            _uiManager.SetupEventHandlers();
        }
    }

}
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia3DControl.ROI2D;

namespace Avalonia3DControl;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.WriteLine("[App] 开始创建MainWindow");
            desktop.MainWindow = new MainWindow();
            Console.WriteLine("[App] MainWindow创建完成");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia3DControl.Core.Animation;
using Avalonia3DControl.Core.Models;
using Avalonia3DControl.UI;
using OpenTK.Mathematics;

namespace Avalonia3DControl.UI
{
    /// <summary>
    /// 振型动画控制面板
    /// </summary>
    public partial class ModalAnimationPanel : UserControl
    {
        private ModalAnimationController? _animationController;
        private Model3D? _targetModel;
        
        // 梯度条相关事件
        public event Action<bool>? GradientBarVisibilityChanged;
        public event Action<GradientBarPosition>? GradientBarPositionChanged;
        
        // 控件引用
        private Button? _playButton;
        private Button? _pauseButton;
        private Button? _prevFrameButton;
        private Button? _nextFrameButton;
        private Button? _stopButton;
        private Button? _resetButton;
        private Slider? _speedSlider;
        private Slider? _amplitudeSlider;
        private CheckBox? _loopCheckBox;
        private ComboBox? _modeComboBox;
        private ComboBox? _gradientTypeComboBox;
        private CheckBox? _symmetricCheckBox;
        private CheckBox? _gradientBarVisibleCheckBox;
        private ComboBox? _gradientBarPositionComboBox;
        private TextBlock? _speedValueText;
        private TextBlock? _amplitudeValueText;
        private TextBlock? _statusText;

        
        public ModalAnimationPanel()
        {
            InitializeComponent();
            InitializeControls();
            SetupEventHandlers();
        }
        
        /// <summary>
        /// 初始化控件引用
        /// </summary>
        private void InitializeControls()
        {
            _playButton = this.FindControl<Button>("PlayButton");
            _pauseButton = this.FindControl<Button>("PauseButton");
            _prevFrameButton = this.FindControl<Button>("PrevFrameButton");
            _nextFrameButton = this.FindControl<Button>("NextFrameButton");
            _stopButton = this.FindControl<Button>("StopButton");
            _resetButton = this.FindControl<Button>("ResetButton");
            _speedSlider = this.FindControl<Slider>("SpeedSlider");
            _amplitudeSlider = this.FindControl<Slider>("AmplitudeSlider");
            _loopCheckBox = this.FindControl<CheckBox>("LoopCheckBox");
            _modeComboBox = this.FindControl<ComboBox>("ModeComboBox");
            _gradientTypeComboBox = this.FindControl<ComboBox>("GradientTypeComboBox");
            _symmetricCheckBox = this.FindControl<CheckBox>("SymmetricCheckBox");
            _gradientBarVisibleCheckBox = this.FindControl<CheckBox>("GradientBarVisibleCheckBox");
            _gradientBarPositionComboBox = this.FindControl<ComboBox>("GradientBarPositionComboBox");
            _speedValueText = this.FindControl<TextBlock>("SpeedValueText");
            _amplitudeValueText = this.FindControl<TextBlock>("AmplitudeValueText");
            _statusText = this.FindControl<TextBlock>("StatusText");

            
            // 设置初始选中项
            if (_modeComboBox != null)
            {
                _modeComboBox.SelectedIndex = 0;
            }
            
            if (_gradientTypeComboBox != null)
            {
                _gradientTypeComboBox.SelectedIndex = 0; // 默认选择经典梯度
            }
            
            if (_gradientBarPositionComboBox != null)
            {
                _gradientBarPositionComboBox.SelectedIndex = 1; // 默认选择右侧
            }
        }
        
        /// <summary>
        /// 设置事件处理器
        /// </summary>
        private void SetupEventHandlers()
        {
            // 播放控制按钮事件
            if (_playButton != null)
                _playButton.Click += OnPlayClick;
            if (_pauseButton != null)
                _pauseButton.Click += OnPauseClick;
            if (_prevFrameButton != null)
                _prevFrameButton.Click += OnPrevFrameClick;
            if (_nextFrameButton != null)
                _nextFrameButton.Click += OnNextFrameClick;
            if (_stopButton != null)
                _stopButton.Click += OnStopClick;
            if (_resetButton != null)
                _resetButton.Click += OnResetClick;
            
            // 滑块值变化事件
            if (_speedSlider != null)
            {
                _speedSlider.ValueChanged += OnSpeedChanged;
            }
            
            if (_amplitudeSlider != null)
            {
                _amplitudeSlider.ValueChanged += OnAmplitudeChanged;
            }
            
            // 等高线层数滑块事件
            
            // 循环播放复选框事件
            if (_loopCheckBox != null)
            {
                _loopCheckBox.IsCheckedChanged += OnLoopChanged;
            }
            
            // 振型选择事件
            if (_modeComboBox != null)
            {
                _modeComboBox.SelectionChanged += OnModeSelectionChanged;
            }
            
            // 颜色梯度选择事件
            if (_gradientTypeComboBox != null)
            {
                _gradientTypeComboBox.SelectionChanged += OnGradientTypeSelectionChanged;
            }
            
            // 对称性切换事件
            if (_symmetricCheckBox != null)
            {
                _symmetricCheckBox.IsCheckedChanged += OnSymmetricCheckBoxChanged;
            }
            
            // 梯度条显示切换事件
            if (_gradientBarVisibleCheckBox != null)
            {
                _gradientBarVisibleCheckBox.IsCheckedChanged += OnGradientBarVisibleChanged;
            }
            
            // 梯度条位置选择事件
            if (_gradientBarPositionComboBox != null)
            {
                _gradientBarPositionComboBox.SelectionChanged += OnGradientBarPositionChanged;
            }
        }
        
        /// <summary>
        /// 设置动画控制器
        /// </summary>
        /// <param name="controller">动画控制器</param>
        /// <param name="model">目标模型</param>
        public void SetAnimationController(ModalAnimationController controller, Model3D model)
        {
            _animationController = controller;
            _targetModel = model;
            
            // 订阅动画状态变化事件
            if (_animationController != null)
            {
                _animationController.AnimationStateChanged += OnAnimationStateChanged;
            }
            
            UpdateUI();
        }
        
        /// <summary>
        /// 创建示例振型数据
        /// </summary>
        /// <returns>振型数据集</returns>
        public ModalDataSet CreateSampleModalData()
        {
            var modalDataSet = new ModalDataSet();
            
            // 创建三个示例振型
            for (int mode = 0; mode < 3; mode++)
            {
                var modalData = new ModalData(100.0f + mode * 100.0f); // 100Hz, 200Hz, 300Hz
                
                // 为每个振型添加示例数据点
                // 这里创建一个简单的正弦波形状
                for (int i = 0; i < 100; i++)
                {
                    double angle = (double)i / 100.0 * Math.PI * 2;
                    double amplitude = Math.Sin(angle) * (1.0 + mode * 0.5);
                    
                    var point = new ModalPoint
                    {
                        AmplitudeX = (float)(amplitude * 0.1),
                        AmplitudeY = (float)(amplitude * 0.2),
                        AmplitudeZ = (float)(amplitude * 0.05),
                        PhaseX = 0.0f,
                        PhaseY = (float)(Math.PI / 4),
                        PhaseZ = (float)(Math.PI / 2)
                    };
                    
                    modalData.AddPoint(point);
                }
                
                modalDataSet.AddMode(modalData);
            }
            
            return modalDataSet;
        }
        
        /// <summary>
        /// 为悬臂梁模型创建特定的振型数据
        /// </summary>
        /// <returns>悬臂梁振型数据集</returns>
        public ModalDataSet CreateCantileverBeamModalData()
        {
            var modalDataSet = new ModalDataSet();
            
            // 悬臂梁的前三阶振型频率（典型值）
            var frequencies = new[] { 15.4f, 96.5f, 270.1f }; // Hz
            var modeNames = new[] { "一阶弯曲", "二阶弯曲", "三阶弯曲" };
            
            // 悬臂梁网格参数（与GeometryFactory中的参数一致）
            int lengthSegments = 40;
            int widthSegments = 8;
            int totalVertices = (lengthSegments + 1) * (widthSegments + 1); // 41 * 9 = 369个顶点
            
            for (int mode = 0; mode < 3; mode++)
            {
                var modalData = new ModalData(frequencies[mode], modeNames[mode]);
                
                // 为每个顶点生成振型数据
                for (int i = 0; i <= lengthSegments; i++)
                {
                    for (int j = 0; j <= widthSegments; j++)
                    {
                        // 计算顶点索引
                        int vertexIndex = i * (widthSegments + 1) + j;
                        
                        // 归一化长度方向位置 (0到1，从固定端到自由端)
                        double x = (double)i / lengthSegments;
                        
                        // 悬臂梁振型函数（简化版）
                        double modeShape = 0.0;
                        switch (mode)
                        {
                            case 0: // 一阶弯曲振型
                                modeShape = Math.Sin(1.875 * x) - Math.Sinh(1.875 * x) - 
                                           0.734 * (Math.Cos(1.875 * x) - Math.Cosh(1.875 * x));
                                break;
                            case 1: // 二阶弯曲振型
                                modeShape = Math.Sin(4.694 * x) - Math.Sinh(4.694 * x) - 
                                           1.018 * (Math.Cos(4.694 * x) - Math.Cosh(4.694 * x));
                                break;
                            case 2: // 三阶弯曲振型
                                modeShape = Math.Sin(7.855 * x) - Math.Sinh(7.855 * x) - 
                                           0.999 * (Math.Cos(7.855 * x) - Math.Cosh(7.855 * x));
                                break;
                        }
                        
                        // 归一化振型幅值
                        modeShape *= 0.2; // 增加振幅使效果更明显

                        // 为不同位置的点添加不同的相位，实现动态颜色变化
                        float phaseZ = 0;//(float)(2.0 * Math.PI * x); // 基于位置的相位变化
                        
                        var point = new ModalPoint
                        {
                            AmplitudeX = 0.0f, // 悬臂梁主要在Z方向振动（垂直于梁平面）
                            AmplitudeY = 0.0f,
                            AmplitudeZ = (float)modeShape, // Z方向振动
                            PhaseX = 0.0f,
                            PhaseY = 0.0f,
                            PhaseZ = phaseZ, // 基于位置的相位变化，实现波动效果
                            VertexIndex = vertexIndex
                        };
                        
                        modalData.AddPoint(point);
                    }
                }
                
                modalDataSet.AddMode(modalData);
            }
            
            return modalDataSet;
        }
        
        #region 事件处理器
        
        private void OnPlayClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.Play();
        }
        
        private void OnPauseClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.Pause();
        }
        
        private void OnStopClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.Stop();
        }
        
        private void OnResetClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.Reset();
        }
        
        private void OnPrevFrameClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.PreviousFrame();
        }
        
        private void OnNextFrameClick(object? sender, RoutedEventArgs e)
        {
            _animationController?.NextFrame();
        }
        
        private void OnSpeedChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_animationController != null && _speedSlider != null)
            {
                _animationController.AnimationSpeed = (float)_speedSlider.Value;
                
                if (_speedValueText != null)
                {
                    _speedValueText.Text = $"{_speedSlider.Value:F1}x";
                }
            }
        }
        
        private void OnAmplitudeChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_animationController != null && _amplitudeSlider != null)
            {
                _animationController.AmplificationFactor = (float)_amplitudeSlider.Value;
                
                if (_amplitudeValueText != null)
                {
                    _amplitudeValueText.Text = $"{_amplitudeSlider.Value:F1}x";
                }
            }
        }
        
        private void OnLoopChanged(object? sender, RoutedEventArgs e)
        {
            if (_animationController != null && _loopCheckBox != null)
            {
                _animationController.IsLooping = _loopCheckBox.IsChecked == true;
            }
        }
        
        private void OnModeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_animationController != null && _modeComboBox != null)
            {
                var selectedItem = _modeComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag is string tagStr && int.TryParse(tagStr, out int modeIndex))
                {
                    _animationController.SetCurrentMode(modeIndex);
                }
            }
        }
        
        private void OnGradientTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateColorGradientType();
        }
        
        private void OnSymmetricCheckBoxChanged(object? sender, RoutedEventArgs e)
        {
            UpdateColorGradientType();
        }
        
        public event Action<ColorGradientType>? ColorGradientTypeChanged;
        
        private void UpdateColorGradientType()
        {
            if (_animationController != null && _gradientTypeComboBox != null && _symmetricCheckBox != null)
            {
                var selectedItem = _gradientTypeComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag is string baseTypeStr && Enum.TryParse<GradientBaseType>(baseTypeStr, out var baseType))
                {
                    bool isSymmetric = _symmetricCheckBox.IsChecked ?? false;
                    _animationController.ColorGradientType = new ColorGradientType(baseType, isSymmetric);
                    // 通知外部（如 UIManager / OpenGL 渲染器）更新梯度条与渲染
                    ColorGradientTypeChanged?.Invoke(new ColorGradientType(baseType, isSymmetric));
                }
            }
        }
        

        
        private void OnAnimationStateChanged(AnimationState state)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateStatusText(state);
            });
        }
        
        /// <summary>
        /// 梯度条显示切换事件处理
        /// </summary>
        private void OnGradientBarVisibleChanged(object? sender, RoutedEventArgs e)
        {
            if (_gradientBarVisibleCheckBox?.IsChecked == true)
            {
                // 通知主窗口显示梯度条
                GradientBarVisibilityChanged?.Invoke(true);
            }
            else
            {
                // 通知主窗口隐藏梯度条
                GradientBarVisibilityChanged?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 梯度条位置选择事件处理
        /// </summary>
        private void OnGradientBarPositionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_gradientBarPositionComboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                string? position = selectedItem.Tag?.ToString();
                if (position == "Left")
                {
                    GradientBarPositionChanged?.Invoke(GradientBarPosition.Left);
                }
                else if (position == "Right")
                {
                    GradientBarPositionChanged?.Invoke(GradientBarPosition.Right);
                }
            }
        }
        
        #endregion
        
        #region UI更新方法
        
        private void UpdateUI()
        {
            if (_animationController != null)
            {
                // 更新滑块值
                if (_speedSlider != null)
                {
                    _speedSlider.Value = _animationController.AnimationSpeed;
                }
                
                if (_amplitudeSlider != null)
                {
                    _amplitudeSlider.Value = _animationController.AmplificationFactor;
                }
                
                if (_loopCheckBox != null)
                {
                    _loopCheckBox.IsChecked = _animationController.IsLooping;
                }
                
                // 更新文本显示
                UpdateValueTexts();
                UpdateStatusText(_animationController.CurrentState);
            }
        }
        
        private void UpdateValueTexts()
        {
            if (_speedValueText != null && _speedSlider != null)
            {
                _speedValueText.Text = $"{_speedSlider.Value:F1}x";
            }
            
            if (_amplitudeValueText != null && _amplitudeSlider != null)
            {
                _amplitudeValueText.Text = $"{_amplitudeSlider.Value:F1}x";
            }
        }
        
        private void UpdateStatusText(AnimationState state)
        {
            if (_statusText != null)
            {
                _statusText.Text = state switch
                {
                    AnimationState.Stopped => "已停止",
                    AnimationState.Playing => "播放中",
                    AnimationState.Paused => "已暂停",
                    _ => "未知状态"
                };
            }
        }
        
        #endregion
    }
}
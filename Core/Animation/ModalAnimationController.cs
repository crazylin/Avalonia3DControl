using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Mathematics;
using Avalonia3DControl.Core.Models;

namespace Avalonia3DControl.Core.Animation
{
    /// <summary>
/// 动画状态枚举，定义模态动画的播放状态
/// </summary>
/// <remarks>
/// 动画状态控制模态动画的生命周期：
/// - Stopped: 动画已停止
/// - Playing: 动画正在播放
/// - Paused: 动画已暂停
/// </remarks>
public enum AnimationState
    {
        Stopped,
        Playing,
        Paused
    }
    
    /// <summary>
/// 颜色梯度基础类型，定义不同的颜色渐变方案
/// </summary>
/// <remarks>
/// 颜色梯度基础类型用于将位移值映射为可视化颜色：
/// - Classic: 经典梯度（蓝色->绿色->黄色->红色）
/// - Thermal: 热力图梯度（黑色->红色->黄色->白色）
/// - Rainbow: 彩虹梯度（紫色->蓝色->绿色->黄色->红色）
/// - Monochrome: 单色梯度（灰色->白色）
/// - Ocean: 海洋梯度（深蓝->浅蓝->青色->白色）
/// - Fire: 火焰梯度（深红->红色->橙色->黄色）
/// </remarks>
public enum GradientBaseType
    {
        Classic,        // 经典：蓝色->绿色->黄色->红色
        Thermal,        // 热力图：黑色->红色->黄色->白色
        Rainbow,        // 彩虹：紫色->蓝色->绿色->黄色->红色
        Monochrome,     // 单色：灰色->白色
        Ocean,          // 海洋：深蓝->浅蓝->青色->白色
        Fire            // 火焰：深红->红色->橙色->黄色
    }
    
    /// <summary>
    /// 颜色梯度类型（包含对称标志）
    /// </summary>
    public struct ColorGradientType
    {
        public GradientBaseType BaseType { get; set; }
        public bool IsSymmetric { get; set; }
        
        public ColorGradientType(GradientBaseType baseType, bool isSymmetric = false)
        {
            BaseType = baseType;
            IsSymmetric = isSymmetric;
        }
        
        // 预定义的常用梯度类型
        public static readonly ColorGradientType Classic = new(GradientBaseType.Classic, false);
        public static readonly ColorGradientType ClassicSymmetric = new(GradientBaseType.Classic, true);
        public static readonly ColorGradientType Thermal = new(GradientBaseType.Thermal, false);
        public static readonly ColorGradientType ThermalSymmetric = new(GradientBaseType.Thermal, true);
        public static readonly ColorGradientType Rainbow = new(GradientBaseType.Rainbow, false);
        public static readonly ColorGradientType RainbowSymmetric = new(GradientBaseType.Rainbow, true);
        public static readonly ColorGradientType Monochrome = new(GradientBaseType.Monochrome, false);
        public static readonly ColorGradientType MonochromeSymmetric = new(GradientBaseType.Monochrome, true);
        public static readonly ColorGradientType Ocean = new(GradientBaseType.Ocean, false);
        public static readonly ColorGradientType OceanSymmetric = new(GradientBaseType.Ocean, true);
        public static readonly ColorGradientType Fire = new(GradientBaseType.Fire, false);
        public static readonly ColorGradientType FireSymmetric = new(GradientBaseType.Fire, true);
        
        public override string ToString()
        {
            return IsSymmetric ? $"{BaseType}Symmetric" : BaseType.ToString();
        }
    }
    
    /// <summary>
/// 模态动画控制器，管理3D模型的模态振动动画
/// </summary>
/// <remarks>
/// ModalAnimationController负责处理模态分析结果的动画播放，包括：
/// - 模态数据管理：加载和存储模态分析结果
/// - 动画播放控制：播放、暂停、停止、重置
/// - 顶点位移计算：根据模态数据计算实时顶点位置
/// - 颜色渐变映射：将位移量映射为可视化颜色
/// - 性能优化：缓存计算结果，减少重复计算
/// 
/// 支持的功能：
/// - 多模态叠加：同时播放多个模态
/// - 实时颜色更新：根据位移动态更新顶点颜色
/// - 放大系数控制：调整动画的可视化幅度
/// - 多种颜色渐变：支持不同的颜色映射方案
/// </remarks>
public class ModalAnimationController : IDisposable
    {
        #region 私有字段
        private readonly Stopwatch _stopwatch;
        private float _pausedTime;
        private AnimationState _state;
        private ModalDataSet? _modalDataSet;
        private Model3D? _targetModel;
        private float[] _originalVertices;
        private float _animationSpeed = 0.005f;
        private float _amplificationFactor = 1.0f;
        private bool _isLooping = true;
        private int _currentModeIndex = 0;
        private float _maxObservedDisplacement = 0.1f; // 跟踪观察到的最大位移
        private float _minObservedDisplacement = -0.1f; // 跟踪观察到的最小位移
        
        // 重用字典以避免每帧分配
        private readonly Dictionary<int, Vector3> _displacementCache = new();
        private ColorGradientType _colorGradientType = ColorGradientType.Classic; // 当前颜色梯度类型
        private const float FRAME_TIME_STEP = 1.0f / 120.0f; // 固定相位步长（每周期120步）
        #endregion
        
        #region 公共属性
        /// <summary>
        /// 动画状态
        /// </summary>
        public AnimationState CurrentState => _state;
        
        /// <summary>
        /// 当前播放相位（归一化到0-1，一个周期为1.0）
        /// </summary>
        public float CurrentTime
        {
            get
            {
                if (_state == AnimationState.Playing)
                {
                    return _pausedTime + (float)_stopwatch.Elapsed.TotalSeconds * _animationSpeed;
                }
                return _pausedTime;
            }
        }
        
        /// <summary>
        /// 动画速度
        /// </summary>
        public float AnimationSpeed
        {
            get => _animationSpeed;
            set => _animationSpeed = Math.Max(0.001f, value);
        }
        
        /// <summary>
        /// 振幅放大系数
        /// </summary>
        public float AmplificationFactor
        {
            get => _amplificationFactor;
            set => _amplificationFactor = Math.Max(0.1f, value);
        }
        
        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool IsLooping
        {
            get => _isLooping;
            set => _isLooping = value;
        }
        
        /// <summary>
        /// 当前选中的振型数据
        /// </summary>
        public ModalData? CurrentModalData => _modalDataSet?.CurrentMode;
        
        /// <summary>
        /// 目标模型
        /// </summary>
        public Model3D? TargetModel => _targetModel;
        
        /// <summary>
        /// 颜色梯度类型
        /// </summary>
        public ColorGradientType ColorGradientType
        {
            get => _colorGradientType;
            set => _colorGradientType = value;
        }
        

        #endregion
        
        #region 事件
        /// <summary>
        /// 顶点更新事件
        /// </summary>
        public event Action<Model3D>? VerticesUpdated;
        
        /// <summary>
        /// 动画状态改变事件
        /// </summary>
        public event Action<AnimationState>? AnimationStateChanged;
        #endregion
        
        #region 构造函数
        public ModalAnimationController()
        {
            _stopwatch = new Stopwatch();
            _pausedTime = 0.0f;
            _state = AnimationState.Stopped;
            _originalVertices = Array.Empty<float>();
        }
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 设置振型数据集
        /// </summary>
        /// <param name="modalDataSet">振型数据集</param>
        public void SetModalDataSet(ModalDataSet modalDataSet)
        {
            _modalDataSet = modalDataSet ?? throw new ArgumentNullException(nameof(modalDataSet));
        }
        
        /// <summary>
        /// 设置目标模型
        /// </summary>
        /// <param name="model">目标模型</param>
        public void SetTargetModel(Model3D model)
        {
            _targetModel = model ?? throw new ArgumentNullException(nameof(model));
            
            // 保存原始顶点位置
            if (_targetModel.Vertices != null && _targetModel.Vertices.Length > 0)
            {
                _originalVertices = new float[_targetModel.Vertices.Length];
                Array.Copy(_targetModel.Vertices, _originalVertices, _targetModel.Vertices.Length);
            }
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        public void Play()
        {
            if (_state != AnimationState.Playing)
            {
                _state = AnimationState.Playing;
                _stopwatch.Start();
                AnimationStateChanged?.Invoke(_state);
                
                // 立即更新一次顶点以确保动画开始时就有视觉变化
                if (_targetModel != null && _modalDataSet?.CurrentMode != null)
                {
                    var currentTime = CurrentTime;
                    UpdateVertices(currentTime, _modalDataSet.CurrentMode);
                }
            }
        }
        
        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            if (_state == AnimationState.Playing)
            {
                _state = AnimationState.Paused;
                _stopwatch.Stop();
                _pausedTime = CurrentTime;
                AnimationStateChanged?.Invoke(_state);
            }
        }
        
        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            _state = AnimationState.Stopped;
            _stopwatch.Stop();
            _stopwatch.Reset();
            _pausedTime = 0.0f;
            
            // 恢复原始顶点位置
            RestoreOriginalVertices();
            AnimationStateChanged?.Invoke(_state);
        }
        
        /// <summary>
        /// 重置动画到开始位置
        /// </summary>
        public void Reset()
        {
            var wasPlaying = _state == AnimationState.Playing;
            
            _stopwatch.Stop();
            _stopwatch.Reset();
            _pausedTime = 0.0f;
            
            // 立即更新顶点到时间0的状态
            if (_targetModel != null && _modalDataSet?.CurrentMode != null)
            {
                UpdateVertices(0.0f, _modalDataSet.CurrentMode);
            }
            else
            {
                // 如果没有振型数据，则恢复原始顶点位置
                RestoreOriginalVertices();
            }
            
            if (wasPlaying)
            {
                _state = AnimationState.Playing;
                _stopwatch.Start();
            }
            else
            {
                _state = AnimationState.Stopped;
            }
            
            AnimationStateChanged?.Invoke(_state);
        }
        
        /// <summary>
        /// 前一帧（仅在暂停状态下有效）
        /// </summary>
        public void PreviousFrame()
        {
            if (_state == AnimationState.Paused && _modalDataSet?.CurrentMode != null)
            {
                // 减少一个相位步长
                if (_pausedTime > 0.0f)
                {
                    _pausedTime = Math.Max(0.0f, _pausedTime - FRAME_TIME_STEP);
                }
                else
                {
                    if (_isLooping)
                    {
                        // 从周期末尾回绕
                        _pausedTime = Math.Max(0.0f, 1.0f - FRAME_TIME_STEP);
                    }
                    else
                    {
                        _pausedTime = 0.0f;
                    }
                }
                
                // 立即更新顶点显示
                if (_targetModel != null)
                {
                    UpdateVertices(_pausedTime, _modalDataSet.CurrentMode);
                }
            }
        }
        
        /// <summary>
        /// 后一帧（仅在暂停状态下有效）
        /// </summary>
        public void NextFrame()
        {
            if (_state == AnimationState.Paused && _modalDataSet?.CurrentMode != null)
            {
                // 增加一个帧时间步长
                _pausedTime += FRAME_TIME_STEP;
                
                // 检查是否超过一个周期
                if (_pausedTime >= 1.0f)
                {
                    if (_isLooping)
                    {
                        _pausedTime = _pausedTime - 1.0f; // 循环到开始
                    }
                    else
                    {
                        _pausedTime = 1.0f; // 限制在最大值
                    }
                }
                
                // 立即更新顶点显示
                if (_targetModel != null)
                {
                    UpdateVertices(_pausedTime, _modalDataSet.CurrentMode);
                }
            }
        }
        
        /// <summary>
        /// 设置当前振型模式
        /// </summary>
        /// <param name="modeIndex">模式索引</param>
        public void SetCurrentMode(int modeIndex)
        {
            if (_modalDataSet != null && modeIndex >= 0 && modeIndex < _modalDataSet.GetModeCount())
            {
                _currentModeIndex = modeIndex;
                _modalDataSet.SetCurrentModeIndex(modeIndex);
                
                // 立即更新顶点以显示新的振型
                if (_targetModel != null && _modalDataSet.CurrentMode != null)
                {
                    var currentTime = CurrentTime;
                    UpdateVertices(currentTime, _modalDataSet.CurrentMode);
                }
            }
        }
        
        /// <summary>
        /// 更新动画（每帧调用）
        /// </summary>
        public void Update()
        {
            if (_state != AnimationState.Playing || _targetModel == null || _modalDataSet?.CurrentMode == null)
            {
                return;
            }
            
            var currentTime = CurrentTime;
            var modalData = _modalDataSet.CurrentMode;
            
            // 检查是否需要循环（一个周期对应归一化相位1.0）
            if (currentTime >= 1.0f)
            {
                if (_isLooping)
                {
                    // 重置时间但保持播放状态
                    _stopwatch.Reset();
                    _stopwatch.Start();
                    _pausedTime = 0.0f;
                    currentTime = 0.0f;
                }
                else
                {
                    // 停止动画
                    Stop();
                    return;
                }
            }
            
            // 更新顶点位置
            UpdateVertices(currentTime, modalData);
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 更新顶点位置
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <param name="modalData">振型数据</param>
        private void UpdateVertices(float time, ModalData modalData)
        {
            if (_targetModel?.Vertices == null || _originalVertices.Length == 0)
                return;
            
            // 将归一化相位转换为物理时间，使不同频率下角频率一致：omega * t = 2π * time
            float timeSeconds = time;
            if (modalData.Frequency > 0.0f)
            {
                timeSeconds = time / modalData.Frequency;
            }
            
            var vertices = _targetModel.Vertices;
            
            // 计算所有顶点的位移并更新观察到的范围
            _displacementCache.Clear(); // 清空缓存而不是创建新对象
            float currentMaxDisplacement = float.MinValue;
            float currentMinDisplacement = float.MaxValue;
            float currentMaxAbsDisplacement = 0f; // 当前帧的最大绝对位移
            
            foreach (var point in modalData.Points)
            {
                var displacement = point.GetDisplacement(timeSeconds, modalData.Frequency);
                displacement *= _amplificationFactor;
                _displacementCache[point.VertexIndex] = displacement;
                
                float zDisplacement = displacement.Z;
                currentMaxDisplacement = Math.Max(currentMaxDisplacement, zDisplacement);
                currentMinDisplacement = Math.Min(currentMinDisplacement, zDisplacement);
                currentMaxAbsDisplacement = Math.Max(currentMaxAbsDisplacement, Math.Abs(zDisplacement));
            }
            
            // 使用指数移动平均更新观察到的范围，确保平滑过渡
            float alpha = 0.1f; // 平滑因子
            _maxObservedDisplacement = _maxObservedDisplacement * (1 - alpha) + currentMaxDisplacement * alpha;
            _minObservedDisplacement = _minObservedDisplacement * (1 - alpha) + currentMinDisplacement * alpha;
            
            // 确保范围不为零
            float observedRange = _maxObservedDisplacement - _minObservedDisplacement;
            if (observedRange < 1e-6f)
            {
                observedRange = 0.2f;
                _maxObservedDisplacement = 0.1f;
                _minObservedDisplacement = -0.1f;
            }
            
            // 确保当前帧的最大绝对位移不为零
            if (currentMaxAbsDisplacement < 1e-6f)
            {
                currentMaxAbsDisplacement = 0.1f;
            }
            
            // 第二遍：更新顶点位置和颜色
            foreach (var point in modalData.Points)
            {
                var vertexIndex = point.VertexIndex * 6; // 每个顶点6个分量（位置3+颜色3）
                
                if (vertexIndex + 5 < vertices.Length && _displacementCache.ContainsKey(point.VertexIndex))
                {
                    var displacement = _displacementCache[point.VertexIndex];
                    
                    // 更新顶点位置（原始位置 + 位移）
                    vertices[vertexIndex] = _originalVertices[vertexIndex] + displacement.X;
                    vertices[vertexIndex + 1] = _originalVertices[vertexIndex + 1] + displacement.Y;
                    vertices[vertexIndex + 2] = _originalVertices[vertexIndex + 2] + displacement.Z;
                    
                    // 根据振动位移更新颜色
                    float zDisplacement = displacement.Z;
                    
                    // 使用观察到的位移范围进行归一化，而不是当前帧的最大绝对位移
                    // 这样可以确保帧与帧之间的颜色渐变更加平滑
                    float normalizedSigned;
                    float observedMaxAbsDisplacement = Math.Max(Math.Abs(_maxObservedDisplacement), Math.Abs(_minObservedDisplacement));
                    
                    if (observedMaxAbsDisplacement > 1e-6f)
                    {
                        normalizedSigned = Math.Max(-1.0f, Math.Min(1.0f, zDisplacement / observedMaxAbsDisplacement));
                    }
                    else
                    {
                        normalizedSigned = 0.0f;
                    }
                    
                    // 使用平滑过渡函数处理归一化值
                    // 应用平滑函数，确保在-1到1之间有更好的颜色过渡
                    float smoothedSigned = normalizedSigned * (1.5f - 0.5f * Math.Abs(normalizedSigned));
                    
                    // 根据颜色梯度类型选择不同的处理方式
                    (float r, float g, float b) color;
                    
                    if (_colorGradientType.IsSymmetric)
                    {
                        // 对于对称梯度，使用连续的颜色映射
                        // 将[-1,1]范围的值直接映射到[0,1]，确保平滑过渡
                        float t = (smoothedSigned + 1.0f) * 0.5f;
                        t = Math.Max(0.0f, Math.Min(1.0f, t));
                        color = CalculateGradientColor(t, _colorGradientType);
                    }
                    else
                    {
                        // 非对称梯度，将[-1,1]映射到[0,1]
                        float normalizedDisplacement = (smoothedSigned + 1.0f) * 0.5f;
                        normalizedDisplacement = Math.Max(0.0f, Math.Min(1.0f, normalizedDisplacement));
                        color = CalculateGradientColor(normalizedDisplacement, _colorGradientType);
                    }
                    
                    vertices[vertexIndex + 3] = color.r;
                    vertices[vertexIndex + 4] = color.g;
                    vertices[vertexIndex + 5] = color.b;
                }
            }
            
            // 触发顶点更新事件
            VerticesUpdated?.Invoke(_targetModel);
        }
        
        /// <summary>
        /// 恢复原始顶点位置
        /// </summary>
        private void RestoreOriginalVertices()
        {
            if (_targetModel?.Vertices != null && _originalVertices.Length > 0)
            {
                Array.Copy(_originalVertices, _targetModel.Vertices, Math.Min(_originalVertices.Length, _targetModel.Vertices.Length));
                VerticesUpdated?.Invoke(_targetModel);
            }
        }
        
        #endregion
        
        #region 颜色梯度计算
        
        /// <summary>
        /// 判断是否为对称梯度类型
        /// </summary>
        private bool IsSymmetricGradient(ColorGradientType gradientType)
        {
            return gradientType.IsSymmetric;
        }
        /// <summary>
        /// 根据归一化位移值和梯度类型计算颜色
        /// </summary>
        /// <param name="normalizedValue">归一化的位移值 (0-1)</param>
        /// <param name="gradientType">颜色梯度类型</param>
        /// <returns>RGB颜色值</returns>
        private (float r, float g, float b) CalculateGradientColor(float normalizedValue, ColorGradientType gradientType)
        {
            normalizedValue = Math.Max(0.0f, Math.Min(1.0f, normalizedValue));
            
            // 根据基础类型选择梯度计算函数
            Func<float, (float r, float g, float b)> baseGradientFunc = gradientType.BaseType switch
            {
                GradientBaseType.Classic => CalculateClassicGradient,
                GradientBaseType.Thermal => CalculateHeatmapGradient,
                GradientBaseType.Rainbow => CalculateRainbowGradient,
                GradientBaseType.Monochrome => CalculateMonochromeGradient,
                GradientBaseType.Ocean => CalculateOceanGradient,
                GradientBaseType.Fire => CalculateFireGradient,
                _ => CalculateClassicGradient
            };
            
            // 根据是否对称应用相应的计算
            return gradientType.IsSymmetric 
                ? CalculateSymmetricVariant(normalizedValue, baseGradientFunc)
                : baseGradientFunc(normalizedValue);
        }
        
        /// <summary>
        /// 经典梯度：蓝色 -> 青色 -> 绿色 -> 黄色 -> 红色
        /// 根据实际颜色数量自动计算每种颜色的占比，实现平滑过渡
        /// </summary>
        private (float r, float g, float b) CalculateClassicGradient(float t)
        {
            // 确保t在[0,1]范围内
            t = Math.Max(0.0f, Math.Min(1.0f, t));
            
            // 定义主要颜色及其HSV色相值
            var hsvColors = new List<(float h, float s, float v)>
            {
                (240.0f, 1.0f, 0.7f), // 深蓝色
                (180.0f, 1.0f, 1.0f), // 青色
                (120.0f, 1.0f, 1.0f), // 绿色
                (60.0f, 1.0f, 1.0f),  // 黄色
                (0.0f, 1.0f, 1.0f)    // 红色
            };
            
            // 计算每个颜色段的大小
            int colorCount = hsvColors.Count - 1; // 颜色段数量 = 颜色数量 - 1
            float segmentSize = 1.0f / colorCount;
            
            // 确定当前t所在的颜色段
            int segment = Math.Min((int)(t / segmentSize), colorCount - 1);
            
            // 计算在当前颜色段内的比例
            float ratio = (t - segment * segmentSize) / segmentSize;
            
            // 在两个HSV颜色之间进行线性插值
            var hsv1 = hsvColors[segment];
            var hsv2 = hsvColors[segment + 1];
            
            // 对HSV值进行插值
            float h = hsv1.h + (hsv2.h - hsv1.h) * ratio;
            float s = hsv1.s + (hsv2.s - hsv1.s) * ratio;
            float v = hsv1.v + (hsv2.v - hsv1.v) * ratio;
            
            // 将HSV转换为RGB
            return HsvToRgb(h, s, v);
        }

    /// <summary>
    /// 热力图梯度：黑色 -> 红色 -> 黄色 -> 白色
    /// </summary>
    private (float r, float g, float b) CalculateHeatmapGradient(float t)
    {
        // 定义颜色列表
        var colors = new List<(float r, float g, float b)>
            {
                (0.0f, 0.0f, 0.0f), // 黑色
                (1.0f, 0.0f, 0.0f), // 红色
                (1.0f, 1.0f, 0.0f), // 黄色
                (1.0f, 1.0f, 1.0f)  // 白色
            };

        return CalculateGradientFromColors(t, colors);
    }
    /// <summary>
    /// 彩虹梯度：红色 -> 橙色 -> 黄色 -> 绿色 -> 蓝色 -> 紫色
    /// </summary>
    private (float r, float g, float b) CalculateRainbowGradient(float t)
        {
            // 定义颜色列表（使用HSV色相值）
            var hsvColors = new List<(float h, float s, float v)>
            {
                (0.0f, 1.0f, 1.0f),   // 红色
                (30.0f, 1.0f, 1.0f),  // 橙色
                (60.0f, 1.0f, 1.0f),  // 黄色
                (120.0f, 1.0f, 1.0f), // 绿色
                (240.0f, 1.0f, 1.0f), // 蓝色
                (300.0f, 1.0f, 1.0f)  // 紫色
            };
            
            return CalculateGradientFromHsvColors(t, hsvColors);
        }
        
        /// <summary>
        /// 单色梯度：深蓝到浅蓝
        /// </summary>
        private (float r, float g, float b) CalculateMonochromeGradient(float t)
        {
            // 定义颜色列表
            var colors = new List<(float r, float g, float b)>
            {
                (0.0f, 0.08f, 0.2f), // 深蓝
                (0.0f, 0.4f, 1.0f)   // 浅蓝
            };
            
            return CalculateGradientFromColors(t, colors);
        }
        
        /// <summary>
        /// 海洋梯度：深蓝 -> 青色 -> 浅绿 -> 白色
        /// </summary>
        private (float r, float g, float b) CalculateOceanGradient(float t)
        {
            // 定义颜色列表
            var colors = new List<(float r, float g, float b)>
            {
                (0.0f, 0.0f, 0.5f), // 深蓝
                (0.0f, 0.5f, 1.0f), // 青色
                (0.0f, 1.0f, 0.7f), // 浅绿
                (0.8f, 1.0f, 1.0f)  // 白色
            };
            
            return CalculateGradientFromColors(t, colors);
        }
        
        /// <summary>
        /// 火焰梯度：黑色 -> 深红 -> 红色 -> 橙色 -> 黄色 -> 白色
        /// </summary>
        private (float r, float g, float b) CalculateFireGradient(float t)
        {
            // 确保t在[0,1]范围内
            t = Math.Max(0.0f, Math.Min(1.0f, t));
            
            // 定义颜色列表
            var colors = new List<(float r, float g, float b)>
            {
                (0.0f, 0.0f, 0.0f), // 黑色
                (0.5f, 0.0f, 0.0f), // 深红
                (1.0f, 0.0f, 0.0f), // 红色
                (1.0f, 0.5f, 0.0f), // 橙色
                (1.0f, 1.0f, 0.0f), // 黄色
                (1.0f, 1.0f, 1.0f)  // 白色
            };
            
            return CalculateGradientFromColors(t, colors);
        }
        

        
        /// <summary>
        /// 通用颜色梯度计算方法，根据传入的颜色数组计算梯度
        /// </summary>
        /// <param name="t">归一化的位置值，范围[0,1]</param>
        /// <param name="colors">颜色数组，RGB格式</param>
        /// <returns>插值后的RGB颜色</returns>
        private (float r, float g, float b) CalculateGradientFromColors(float t, List<(float r, float g, float b)> colors)
        {
            // 确保t在[0,1]范围内
            t = Math.Max(0.0f, Math.Min(1.0f, t));
            
            // 确保至少有两种颜色
            if (colors.Count < 2)
            {
                throw new ArgumentException("颜色数组至少需要包含两种颜色");
            }
            
            // 计算每个颜色段的大小
            int colorCount = colors.Count - 1; // 颜色段数量 = 颜色数量 - 1
            float segmentSize = 1.0f / colorCount;
            
            // 确定当前t所在的颜色段
            int segment = Math.Min((int)(t / segmentSize), colorCount - 1);
            
            // 计算在当前颜色段内的比例
            float ratio = (t - segment * segmentSize) / segmentSize;
            
            // 在两个颜色之间进行线性插值
            var color1 = colors[segment];
            var color2 = colors[segment + 1];
            
            return (
                color1.r + (color2.r - color1.r) * ratio,
                color1.g + (color2.g - color1.g) * ratio,
                color1.b + (color2.b - color1.b) * ratio
            );
        }
        
        /// <summary>
        /// 通用HSV颜色梯度计算方法，根据传入的HSV颜色数组计算梯度
        /// </summary>
        /// <param name="t">归一化的位置值，范围[0,1]</param>
        /// <param name="hsvColors">HSV颜色数组</param>
        /// <returns>插值后的RGB颜色</returns>
        private (float r, float g, float b) CalculateGradientFromHsvColors(float t, List<(float h, float s, float v)> hsvColors)
        {
            // 确保t在[0,1]范围内
            t = Math.Max(0.0f, Math.Min(1.0f, t));
            
            // 确保至少有两种颜色
            if (hsvColors.Count < 2)
            {
                throw new ArgumentException("颜色数组至少需要包含两种颜色");
            }
            
            // 计算每个颜色段的大小
            int colorCount = hsvColors.Count - 1; // 颜色段数量 = 颜色数量 - 1
            float segmentSize = 1.0f / colorCount;
            
            // 确定当前t所在的颜色段
            int segment = Math.Min((int)(t / segmentSize), colorCount - 1);
            
            // 计算在当前颜色段内的比例
            float ratio = (t - segment * segmentSize) / segmentSize;
            
            // 在两个HSV颜色之间进行线性插值
            var hsv1 = hsvColors[segment];
            var hsv2 = hsvColors[segment + 1];
            
            // 对HSV值进行插值
            float h = hsv1.h + (hsv2.h - hsv1.h) * ratio;
            float s = hsv1.s + (hsv2.s - hsv1.s) * ratio;
            float v = hsv1.v + (hsv2.v - hsv1.v) * ratio;
            
            // 将HSV转换为RGB
            return HsvToRgb(h, s, v);
        }
        
        /// <summary>
        /// HSV到RGB颜色空间转换
        /// </summary>
        private (float r, float g, float b) HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60.0f) % 2 - 1));
            float m = v - c;
            
            float r1, g1, b1;
            
            if (h < 60)
            {
                r1 = c; g1 = x; b1 = 0;
            }
            else if (h < 120)
            {
                r1 = x; g1 = c; b1 = 0;
            }
            else if (h < 180)
            {
                r1 = 0; g1 = c; b1 = x;
            }
            else if (h < 240)
            {
                r1 = 0; g1 = x; b1 = c;
            }
            else if (h < 300)
            {
                r1 = x; g1 = 0; b1 = c;
            }
            else
            {
                r1 = c; g1 = 0; b1 = x;
            }
            
            return (r1 + m, g1 + m, b1 + m);
        }
        
        /// <summary>
        /// 通用对称梯度计算方法
        /// 实现完整的颜色序列对称：例如蓝->绿->黄->红变成红->黄->绿->蓝->绿->黄->红
        /// </summary>
        /// <param name="t">归一化值 [0,1]</param>
        /// <param name="baseGradientFunc">基础梯度计算函数</param>
        /// <returns>对称梯度的RGB颜色值</returns>
        private (float r, float g, float b) CalculateSymmetricVariant(float t, Func<float, (float r, float g, float b)> baseGradientFunc)
        {
            // 实现真正的对称颜色过渡
            // 将[0,1]映射为[0,0.5,1]，使得t=0.5时位于中心点
            float mappedT;
            
            if (t < 0.5f)
            {
                // 前半段：从0到0.5映射为0到1
                mappedT = t * 2.0f;
            }
            else
            {
                // 后半段：从0.5到1映射为1到0
                mappedT = (1.0f - t) * 2.0f;
            }
            
            return baseGradientFunc(mappedT);
        }
        #endregion
        
        #region IDisposable实现
        
        private bool _disposed = false;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    _stopwatch?.Stop();
                }
                _disposed = true;
            }
        }
        
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Mathematics;
using Avalonia3DControl.Core.Models;

namespace Avalonia3DControl.Core.Animation
{
    /// <summary>
    /// 动画状态枚举
    /// </summary>
    public enum AnimationState
    {
        Stopped,
        Playing,
        Paused
    }
    
    /// <summary>
    /// 颜色梯度基础类型
    /// </summary>
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
    /// 振型动画控制器
    /// </summary>
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
            var displacements = new Dictionary<int, Vector3>();
            float currentMaxDisplacement = float.MinValue;
            float currentMinDisplacement = float.MaxValue;
            float currentMaxAbsDisplacement = 0f; // 当前帧的最大绝对位移
            
            foreach (var point in modalData.Points)
            {
                var displacement = point.GetDisplacement(timeSeconds, modalData.Frequency);
                displacement *= _amplificationFactor;
                displacements[point.VertexIndex] = displacement;
                
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
                
                if (vertexIndex + 5 < vertices.Length && displacements.ContainsKey(point.VertexIndex))
                {
                    var displacement = displacements[point.VertexIndex];
                    
                    // 更新顶点位置（原始位置 + 位移）
                    vertices[vertexIndex] = _originalVertices[vertexIndex] + displacement.X;
                    vertices[vertexIndex + 1] = _originalVertices[vertexIndex + 1] + displacement.Y;
                    vertices[vertexIndex + 2] = _originalVertices[vertexIndex + 2] + displacement.Z;
                    
                    // 根据振动位移更新颜色
                    float zDisplacement = displacement.Z;
                    
                    // 统一归一化到[-1, 1]（以当前帧的最大绝对位移为基准），无论对称/非对称梯度
                    float normalizedSigned;
                    if (currentMaxAbsDisplacement > 1e-6f)
                    {
                        normalizedSigned = Math.Max(-1.0f, Math.Min(1.0f, zDisplacement / currentMaxAbsDisplacement));
                    }
                    else
                    {
                        normalizedSigned = 0.0f;
                    }
                    
                    // 颜色查找仍使用[0,1]，将[-1,1]映射到[0,1]
                    float normalizedDisplacement = (normalizedSigned + 1.0f) * 0.5f;
                    normalizedDisplacement = Math.Max(0.0f, Math.Min(1.0f, normalizedDisplacement));
                    
                    // 根据选择的颜色梯度类型计算颜色
                    var color = CalculateGradientColor(normalizedDisplacement, _colorGradientType);
                    
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
        /// 经典梯度：蓝色 -> 绿色 -> 黄色 -> 红色
        /// </summary>
        private (float r, float g, float b) CalculateClassicGradient(float t)
        {
            if (t < 0.33f)
            {
                // 蓝色到绿色 (0-0.33)
                float ratio = t / 0.33f;
                return (0.0f, ratio, 1.0f - ratio);
            }
            else if (t < 0.66f)
            {
                // 绿色到黄色 (0.33-0.66)
                float ratio = (t - 0.33f) / 0.33f;
                return (ratio, 1.0f, 0.0f);
            }
            else
            {
                // 黄色到红色 (0.66-1.0)
                float ratio = (t - 0.66f) / 0.34f;
                return (1.0f, 1.0f - ratio, 0.0f);
            }
        }
        
        /// <summary>
        /// 热力图梯度：黑色 -> 红色 -> 黄色 -> 白色
        /// </summary>
        private (float r, float g, float b) CalculateHeatmapGradient(float t)
        {
            if (t < 0.25f)
            {
                // 黑色到红色
                float ratio = t / 0.25f;
                return (ratio, 0.0f, 0.0f);
            }
            else if (t < 0.5f)
            {
                // 红色到黄色
                float ratio = (t - 0.25f) / 0.25f;
                return (1.0f, ratio, 0.0f);
            }
            else if (t < 0.75f)
            {
                // 黄色到白色（增加蓝色分量）
                float ratio = (t - 0.5f) / 0.25f;
                return (1.0f, 1.0f, ratio);
            }
            else
            {
                // 保持白色
                return (1.0f, 1.0f, 1.0f);
            }
        }
        
        /// <summary>
        /// 彩虹梯度：红色 -> 橙色 -> 黄色 -> 绿色 -> 蓝色 -> 紫色
        /// </summary>
        private (float r, float g, float b) CalculateRainbowGradient(float t)
        {
            float hue = t * 300.0f; // 0-300度，避免回到红色
            return HsvToRgb(hue, 1.0f, 1.0f);
        }
        
        /// <summary>
        /// 单色梯度：深蓝到浅蓝
        /// </summary>
        private (float r, float g, float b) CalculateMonochromeGradient(float t)
        {
            float intensity = 0.2f + t * 0.8f; // 从20%到100%强度
            return (0.0f, 0.4f * intensity, intensity);
        }
        
        /// <summary>
        /// 海洋梯度：深蓝 -> 青色 -> 浅绿 -> 白色
        /// </summary>
        private (float r, float g, float b) CalculateOceanGradient(float t)
        {
            if (t < 0.33f)
            {
                // 深蓝到青色
                float ratio = t / 0.33f;
                return (0.0f, ratio * 0.5f, 0.5f + ratio * 0.5f);
            }
            else if (t < 0.66f)
            {
                // 青色到浅绿
                float ratio = (t - 0.33f) / 0.33f;
                return (0.0f, 0.5f + ratio * 0.5f, 1.0f - ratio * 0.3f);
            }
            else
            {
                // 浅绿到白色
                float ratio = (t - 0.66f) / 0.34f;
                return (ratio * 0.8f, 1.0f, 0.7f + ratio * 0.3f);
            }
        }
        
        /// <summary>
        /// 火焰梯度：黑色 -> 深红 -> 橙色 -> 黄色 -> 白色
        /// </summary>
        private (float r, float g, float b) CalculateFireGradient(float t)
        {
            if (t < 0.2f)
            {
                // 黑色到深红
                float ratio = t / 0.2f;
                return (ratio * 0.5f, 0.0f, 0.0f);
            }
            else if (t < 0.4f)
            {
                // 深红到红色
                float ratio = (t - 0.2f) / 0.2f;
                return (0.5f + ratio * 0.5f, 0.0f, 0.0f);
            }
            else if (t < 0.6f)
            {
                // 红色到橙色
                float ratio = (t - 0.4f) / 0.2f;
                return (1.0f, ratio * 0.5f, 0.0f);
            }
            else if (t < 0.8f)
            {
                // 橙色到黄色
                float ratio = (t - 0.6f) / 0.2f;
                return (1.0f, 0.5f + ratio * 0.5f, 0.0f);
            }
            else
            {
                // 黄色到白色
                float ratio = (t - 0.8f) / 0.2f;
                return (1.0f, 1.0f, ratio);
            }
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
            // 对称梯度：中心为蓝色，两端为红色
            // t=0.5（对应位移0）→蓝色，t=0或t=1（对应位移-1或1）→红色
            // 将t映射为距离中心的距离，然后应用基础梯度
            
            // 计算距离中心点的距离：0.5→0, 0或1→1
            float distanceFromCenter = Math.Abs(t - 0.5f) * 2.0f;
            
            // 使用距离作为梯度参数：距离0→蓝色，距离1→红色
            return baseGradientFunc(distanceFromCenter);
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
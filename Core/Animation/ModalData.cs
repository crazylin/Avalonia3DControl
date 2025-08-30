using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Avalonia3DControl.Core.Animation
{
    /// <summary>
    /// 振型数据点，包含幅值和相位信息
    /// </summary>
    public struct ModalPoint
    {
        /// <summary>
        /// X方向幅值
        /// </summary>
        public float AmplitudeX { get; set; }
        
        /// <summary>
        /// Y方向幅值
        /// </summary>
        public float AmplitudeY { get; set; }
        
        /// <summary>
        /// Z方向幅值
        /// </summary>
        public float AmplitudeZ { get; set; }
        
        /// <summary>
        /// X方向相位（弧度）
        /// </summary>
        public float PhaseX { get; set; }
        
        /// <summary>
        /// Y方向相位（弧度）
        /// </summary>
        public float PhaseY { get; set; }
        
        /// <summary>
        /// Z方向相位（弧度）
        /// </summary>
        public float PhaseZ { get; set; }
        
        /// <summary>
        /// 测量点索引（对应3D模型中的顶点索引）
        /// </summary>
        public int VertexIndex { get; set; }
        
        public ModalPoint(float ampX, float ampY, float ampZ, float phaseX, float phaseY, float phaseZ, int vertexIndex)
        {
            AmplitudeX = ampX;
            AmplitudeY = ampY;
            AmplitudeZ = ampZ;
            PhaseX = phaseX;
            PhaseY = phaseY;
            PhaseZ = phaseZ;
            VertexIndex = vertexIndex;
        }
        
        /// <summary>
        /// 根据时间计算当前位移
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（Hz）</param>
        /// <param name="amplificationFactor">放大系数</param>
        /// <returns>位移向量</returns>
        public Vector3 CalculateDisplacement(float time, float frequency, float amplificationFactor = 1.0f)
        {
            float omega = 2.0f * MathF.PI * frequency;
            
            float dispX = AmplitudeX * MathF.Cos(omega * time + PhaseX) * amplificationFactor;
            float dispY = AmplitudeY * MathF.Cos(omega * time + PhaseY) * amplificationFactor;
            float dispZ = AmplitudeZ * MathF.Cos(omega * time + PhaseZ) * amplificationFactor;
            
            return new Vector3(dispX, dispY, dispZ);
        }
        
        /// <summary>
        /// 根据时间和频率获取位移（不带放大系数）
        /// </summary>
        /// <param name="time">时间（秒）</param>
        /// <param name="frequency">频率（Hz）</param>
        /// <returns>位移向量</returns>
        public Vector3 GetDisplacement(float time, float frequency)
        {
            return CalculateDisplacement(time, frequency, 1.0f);
        }
    }
    
    /// <summary>
    /// 振型数据，包含某个频率下所有测量点的振型信息
    /// </summary>
    public class ModalData
    {
        /// <summary>
        /// 频率（Hz）
        /// </summary>
        public float Frequency { get; set; }
        
        /// <summary>
        /// 振型名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 振型数据点列表
        /// </summary>
        public List<ModalPoint> Points { get; set; }
        
        /// <summary>
        /// 最大幅值（用于归一化）
        /// </summary>
        public float MaxAmplitude { get; private set; }
        
        public ModalData(float frequency, string name = "")
        {
            Frequency = frequency;
            Name = string.IsNullOrEmpty(name) ? $"Mode_{frequency:F2}Hz" : name;
            Points = new List<ModalPoint>();
            MaxAmplitude = 0.0f;
        }
        
        /// <summary>
        /// 添加振型数据点
        /// </summary>
        /// <param name="point">振型数据点</param>
        public void AddPoint(ModalPoint point)
        {
            Points.Add(point);
            UpdateMaxAmplitude(point);
        }
        
        /// <summary>
        /// 批量添加振型数据点
        /// </summary>
        /// <param name="points">振型数据点数组</param>
        public void AddPoints(ModalPoint[] points)
        {
            Points.AddRange(points);
            foreach (var point in points)
            {
                UpdateMaxAmplitude(point);
            }
        }
        
        /// <summary>
        /// 从幅值和相位数组创建振型数据
        /// </summary>
        /// <param name="amplitudesX">X方向幅值数组</param>
        /// <param name="amplitudesY">Y方向幅值数组</param>
        /// <param name="amplitudesZ">Z方向幅值数组</param>
        /// <param name="phasesX">X方向相位数组（弧度）</param>
        /// <param name="phasesY">Y方向相位数组（弧度）</param>
        /// <param name="phasesZ">Z方向相位数组（弧度）</param>
        public void SetFromArrays(float[] amplitudesX, float[] amplitudesY, float[] amplitudesZ,
                                 float[] phasesX, float[] phasesY, float[] phasesZ)
        {
            if (amplitudesX.Length != amplitudesY.Length || amplitudesY.Length != amplitudesZ.Length ||
                amplitudesZ.Length != phasesX.Length || phasesX.Length != phasesY.Length ||
                phasesY.Length != phasesZ.Length)
            {
                throw new ArgumentException("所有数组的长度必须相同");
            }
            
            Points.Clear();
            MaxAmplitude = 0.0f;
            
            for (int i = 0; i < amplitudesX.Length; i++)
            {
                var point = new ModalPoint(
                    amplitudesX[i], amplitudesY[i], amplitudesZ[i],
                    phasesX[i], phasesY[i], phasesZ[i],
                    i
                );
                AddPoint(point);
            }
        }
        
        /// <summary>
        /// 更新最大幅值
        /// </summary>
        /// <param name="point">振型数据点</param>
        private void UpdateMaxAmplitude(ModalPoint point)
        {
            float maxAmp = MathF.Max(MathF.Max(MathF.Abs(point.AmplitudeX), MathF.Abs(point.AmplitudeY)), MathF.Abs(point.AmplitudeZ));
            if (maxAmp > MaxAmplitude)
            {
                MaxAmplitude = maxAmp;
            }
        }
        
        /// <summary>
        /// 获取归一化的振型数据点
        /// </summary>
        /// <param name="index">点索引</param>
        /// <returns>归一化的振型数据点</returns>
        public ModalPoint GetNormalizedPoint(int index)
        {
            if (index < 0 || index >= Points.Count || MaxAmplitude == 0.0f)
            {
                return new ModalPoint();
            }
            
            var point = Points[index];
            return new ModalPoint(
                point.AmplitudeX / MaxAmplitude,
                point.AmplitudeY / MaxAmplitude,
                point.AmplitudeZ / MaxAmplitude,
                point.PhaseX,
                point.PhaseY,
                point.PhaseZ,
                point.VertexIndex
            );
        }
    }
    
    /// <summary>
    /// 振型数据集合，管理多个频率的振型数据
    /// </summary>
    public class ModalDataSet
    {
        /// <summary>
        /// 振型数据字典，键为频率
        /// </summary>
        public Dictionary<float, ModalData> Modes { get; set; }
        
        /// <summary>
        /// 当前选中的振型
        /// </summary>
        public ModalData? CurrentMode { get; set; }
        
        public ModalDataSet()
        {
            Modes = new Dictionary<float, ModalData>();
        }
        
        /// <summary>
        /// 添加振型数据
        /// </summary>
        /// <param name="modalData">振型数据</param>
        public void AddMode(ModalData modalData)
        {
            Modes[modalData.Frequency] = modalData;
            if (CurrentMode == null)
            {
                CurrentMode = modalData;
            }
        }
        
        /// <summary>
        /// 选择振型
        /// </summary>
        /// <param name="frequency">频率</param>
        /// <returns>是否成功选择</returns>
        public bool SelectMode(float frequency)
        {
            if (Modes.TryGetValue(frequency, out var mode))
            {
                CurrentMode = mode;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 获取所有频率列表
        /// </summary>
        /// <returns>频率列表</returns>
        public List<float> GetFrequencies()
        {
            var frequencies = new List<float>(Modes.Keys);
            frequencies.Sort();
            return frequencies;
        }
        
        /// <summary>
        /// 获取振型数量
        /// </summary>
        /// <returns>振型数量</returns>
        public int GetModeCount()
        {
            return Modes.Count;
        }
        
        /// <summary>
        /// 根据索引设置当前振型
        /// </summary>
        /// <param name="index">振型索引</param>
        public void SetCurrentModeIndex(int index)
        {
            var frequencies = GetFrequencies();
            if (index >= 0 && index < frequencies.Count)
            {
                SelectMode(frequencies[index]);
            }
        }
    }
}
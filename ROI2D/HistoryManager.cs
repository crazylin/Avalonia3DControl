using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 历史记录操作类型
    /// </summary>
    public enum HistoryActionType
    {
        Create,         // 创建
        Delete,         // 删除
        Modify,         // 修改
        Move,           // 移动
        Rotate,         // 旋转
        Scale,          // 缩放
        Copy,           // 复制
        Paste,          // 粘贴
        Group,          // 组合
        Ungroup,        // 取消组合
        GeometryOp,     // 几何运算
        BatchOperation  // 批量操作
    }

    /// <summary>
    /// 历史记录项
    /// </summary>
    public class HistoryItem
    {
        /// <summary>
        /// 操作ID
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public HistoryActionType ActionType { get; set; }
        
        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 操作前的状态数据
        /// </summary>
        public string BeforeState { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作后的状态数据
        /// </summary>
        public string AfterState { get; set; } = string.Empty;
        
        /// <summary>
        /// 受影响的ROI ID列表
        /// </summary>
        public List<Guid> AffectedROIIds { get; set; } = new List<Guid>();
        
        /// <summary>
        /// 操作参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo { get; set; } = true;
        
        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo { get; set; } = true;
        
        /// <summary>
        /// 操作用户
        /// </summary>
        public string User { get; set; } = Environment.UserName;
        
        /// <summary>
        /// 操作大小（字节）
        /// </summary>
        public long Size => (BeforeState?.Length ?? 0) + (AfterState?.Length ?? 0);
    }

    /// <summary>
    /// 历史记录状态
    /// </summary>
    public class HistoryState
    {
        /// <summary>
        /// ROI形状列表的JSON序列化
        /// </summary>
        public string ROIShapesJson { get; set; } = string.Empty;
        
        /// <summary>
        /// 选中的ROI ID列表
        /// </summary>
        public List<Guid> SelectedROIIds { get; set; } = new List<Guid>();
        
        /// <summary>
        /// 视图状态
        /// </summary>
        public Dictionary<string, object> ViewState { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 历史记录管理器配置
    /// </summary>
    public class HistoryManagerConfig
    {
        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistoryCount { get; set; } = 100;
        
        /// <summary>
        /// 最大内存使用量（MB）
        /// </summary>
        public int MaxMemoryUsageMB { get; set; } = 50;
        
        /// <summary>
        /// 是否启用自动保存
        /// </summary>
        public bool EnableAutoSave { get; set; } = true;
        
        /// <summary>
        /// 自动保存间隔（秒）
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 30;
        
        /// <summary>
        /// 是否压缩历史数据
        /// </summary>
        public bool CompressHistoryData { get; set; } = true;
        
        /// <summary>
        /// 是否启用增量保存
        /// </summary>
        public bool EnableIncrementalSave { get; set; } = true;
    }

    /// <summary>
    /// 历史记录管理器
    /// </summary>
    public class HistoryManager
    {
        private readonly List<HistoryItem> _history = new List<HistoryItem>();
        private int _currentIndex = -1;
        private readonly HistoryManagerConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        
        /// <summary>
        /// 历史记录变更事件
        /// </summary>
        public event EventHandler<HistoryChangedEventArgs> HistoryChanged;
        
        /// <summary>
        /// 内存使用量变更事件
        /// </summary>
        public event EventHandler<MemoryUsageEventArgs> MemoryUsageChanged;
        
        public HistoryManager(HistoryManagerConfig config = null)
        {
            _config = config ?? new HistoryManagerConfig();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new ROIShapeJsonConverter() }
            };
        }
        
        /// <summary>
        /// 当前历史记录数量
        /// </summary>
        public int Count => _history.Count;
        
        /// <summary>
        /// 当前索引
        /// </summary>
        public int CurrentIndex => _currentIndex;
        
        /// <summary>
        /// 是否可以撤销
        /// </summary>
        public bool CanUndo => _currentIndex >= 0 && _currentIndex < _history.Count && _history[_currentIndex].CanUndo;
        
        /// <summary>
        /// 是否可以重做
        /// </summary>
        public bool CanRedo => _currentIndex + 1 < _history.Count && _history[_currentIndex + 1].CanRedo;
        
        /// <summary>
        /// 当前内存使用量（MB）
        /// </summary>
        public double MemoryUsageMB => _history.Sum(h => h.Size) / (1024.0 * 1024.0);
        
        /// <summary>
        /// 添加历史记录
        /// </summary>
        public void AddHistory(HistoryActionType actionType, string description, 
            List<ROIShape> beforeShapes, List<ROIShape> afterShapes, 
            List<Guid> affectedIds = null, Dictionary<string, object> parameters = null)
        {
            try
            {
                var historyItem = new HistoryItem
                {
                    ActionType = actionType,
                    Description = description,
                    BeforeState = SerializeShapes(beforeShapes),
                    AfterState = SerializeShapes(afterShapes),
                    AffectedROIIds = affectedIds ?? new List<Guid>(),
                    Parameters = parameters ?? new Dictionary<string, object>()
                };
                
                // 移除当前索引之后的所有历史记录
                if (_currentIndex + 1 < _history.Count)
                {
                    _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
                }
                
                // 添加新的历史记录
                _history.Add(historyItem);
                _currentIndex = _history.Count - 1;
                
                // 检查并清理历史记录
                CleanupHistory();
                
                // 触发事件
                OnHistoryChanged(new HistoryChangedEventArgs
                {
                    ActionType = HistoryChangeType.Added,
                    Item = historyItem,
                    CurrentIndex = _currentIndex,
                    TotalCount = _history.Count
                });
                
                OnMemoryUsageChanged(new MemoryUsageEventArgs
                {
                    CurrentUsageMB = MemoryUsageMB,
                    MaxUsageMB = _config.MaxMemoryUsageMB
                });
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，避免影响主要功能
                System.Diagnostics.Debug.WriteLine($"Failed to add history: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 撤销操作
        /// </summary>
        public HistoryItem Undo()
        {
            if (!CanUndo) return null;
            
            var item = _history[_currentIndex];
            _currentIndex--;
            
            OnHistoryChanged(new HistoryChangedEventArgs
            {
                ActionType = HistoryChangeType.Undo,
                Item = item,
                CurrentIndex = _currentIndex,
                TotalCount = _history.Count
            });
            
            return item;
        }
        
        /// <summary>
        /// 重做操作
        /// </summary>
        public HistoryItem Redo()
        {
            if (!CanRedo) return null;
            
            _currentIndex++;
            var item = _history[_currentIndex];
            
            OnHistoryChanged(new HistoryChangedEventArgs
            {
                ActionType = HistoryChangeType.Redo,
                Item = item,
                CurrentIndex = _currentIndex,
                TotalCount = _history.Count
            });
            
            return item;
        }
        
        /// <summary>
        /// 获取历史记录列表
        /// </summary>
        public List<HistoryItem> GetHistory(int maxCount = -1)
        {
            if (maxCount <= 0)
                return new List<HistoryItem>(_history);
            
            return _history.TakeLast(maxCount).ToList();
        }
        
        /// <summary>
        /// 获取指定操作类型的历史记录
        /// </summary>
        public List<HistoryItem> GetHistoryByType(HistoryActionType actionType)
        {
            return _history.Where(h => h.ActionType == actionType).ToList();
        }
        
        /// <summary>
        /// 获取指定ROI的历史记录
        /// </summary>
        public List<HistoryItem> GetHistoryByROI(Guid roiId)
        {
            return _history.Where(h => h.AffectedROIIds.Contains(roiId)).ToList();
        }
        
        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            _currentIndex = -1;
            
            OnHistoryChanged(new HistoryChangedEventArgs
            {
                ActionType = HistoryChangeType.Cleared,
                CurrentIndex = _currentIndex,
                TotalCount = _history.Count
            });
            
            OnMemoryUsageChanged(new MemoryUsageEventArgs
            {
                CurrentUsageMB = 0,
                MaxUsageMB = _config.MaxMemoryUsageMB
            });
        }
        
        /// <summary>
        /// 跳转到指定历史记录
        /// </summary>
        public bool JumpToHistory(int index)
        {
            if (index < -1 || index >= _history.Count)
                return false;
            
            var oldIndex = _currentIndex;
            _currentIndex = index;
            
            OnHistoryChanged(new HistoryChangedEventArgs
            {
                ActionType = HistoryChangeType.Jumped,
                Item = index >= 0 ? _history[index] : null,
                CurrentIndex = _currentIndex,
                TotalCount = _history.Count,
                PreviousIndex = oldIndex
            });
            
            return true;
        }
        
        /// <summary>
        /// 反序列化形状列表
        /// </summary>
        public List<ROIShape> DeserializeShapes(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<ROIShape>();
            
            try
            {
                return JsonSerializer.Deserialize<List<ROIShape>>(json, _jsonOptions) ?? new List<ROIShape>();
            }
            catch
            {
                return new List<ROIShape>();
            }
        }
        
        /// <summary>
        /// 序列化形状列表
        /// </summary>
        private string SerializeShapes(List<ROIShape> shapes)
        {
            if (shapes == null || shapes.Count == 0)
                return string.Empty;
            
            try
            {
                return JsonSerializer.Serialize(shapes, _jsonOptions);
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 清理历史记录
        /// </summary>
        private void CleanupHistory()
        {
            // 按数量限制清理
            while (_history.Count > _config.MaxHistoryCount)
            {
                _history.RemoveAt(0);
                _currentIndex--;
            }
            
            // 按内存使用量清理
            while (MemoryUsageMB > _config.MaxMemoryUsageMB && _history.Count > 1)
            {
                _history.RemoveAt(0);
                _currentIndex--;
            }
            
            // 确保索引有效
            if (_currentIndex < -1)
                _currentIndex = -1;
            if (_currentIndex >= _history.Count)
                _currentIndex = _history.Count - 1;
        }
        
        /// <summary>
        /// 触发历史记录变更事件
        /// </summary>
        protected virtual void OnHistoryChanged(HistoryChangedEventArgs e)
        {
            HistoryChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 触发内存使用量变更事件
        /// </summary>
        protected virtual void OnMemoryUsageChanged(MemoryUsageEventArgs e)
        {
            MemoryUsageChanged?.Invoke(this, e);
        }
        
        /// <summary>
        /// 获取历史记录统计信息
        /// </summary>
        public HistoryStatistics GetStatistics()
        {
            var stats = new HistoryStatistics
            {
                TotalCount = _history.Count,
                CurrentIndex = _currentIndex,
                MemoryUsageMB = MemoryUsageMB,
                CanUndo = CanUndo,
                CanRedo = CanRedo
            };
            
            if (_history.Count > 0)
            {
                stats.OldestTimestamp = _history.First().Timestamp;
                stats.NewestTimestamp = _history.Last().Timestamp;
                
                var actionCounts = _history.GroupBy(h => h.ActionType)
                                          .ToDictionary(g => g.Key, g => g.Count());
                stats.ActionTypeCounts = actionCounts;
                
                stats.AverageSize = _history.Average(h => h.Size);
                stats.TotalSize = _history.Sum(h => h.Size);
            }
            
            return stats;
        }
        
        /// <summary>
        /// 压缩历史数据
        /// </summary>
        public void CompressHistory()
        {
            if (!_config.CompressHistoryData) return;
            
            // 这里可以实现数据压缩逻辑
            // 例如：合并连续的相似操作、压缩JSON数据等
            
            // 示例：合并连续的移动操作
            for (int i = _history.Count - 1; i > 0; i--)
            {
                var current = _history[i];
                var previous = _history[i - 1];
                
                if (current.ActionType == HistoryActionType.Move && 
                    previous.ActionType == HistoryActionType.Move &&
                    current.AffectedROIIds.SequenceEqual(previous.AffectedROIIds) &&
                    (current.Timestamp - previous.Timestamp).TotalSeconds < 1.0)
                {
                    // 合并两个移动操作
                    previous.AfterState = current.AfterState;
                    previous.Timestamp = current.Timestamp;
                    previous.Description = $"Move (merged {current.Description})";
                    
                    _history.RemoveAt(i);
                    if (_currentIndex >= i)
                        _currentIndex--;
                }
            }
        }
        
        /// <summary>
        /// 导出历史记录
        /// </summary>
        public string ExportHistory()
        {
            try
            {
                var exportData = new
                {
                    ExportTime = DateTime.Now,
                    Config = _config,
                    CurrentIndex = _currentIndex,
                    History = _history
                };
                
                return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 导入历史记录
        /// </summary>
        public bool ImportHistory(string json)
        {
            try
            {
                var importData = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (importData.TryGetProperty("History", out var historyElement))
                {
                    var importedHistory = JsonSerializer.Deserialize<List<HistoryItem>>(historyElement.GetRawText());
                    if (importedHistory != null)
                    {
                        _history.Clear();
                        _history.AddRange(importedHistory);
                        
                        if (importData.TryGetProperty("CurrentIndex", out var indexElement))
                        {
                            _currentIndex = indexElement.GetInt32();
                        }
                        else
                        {
                            _currentIndex = _history.Count - 1;
                        }
                        
                        CleanupHistory();
                        
                        OnHistoryChanged(new HistoryChangedEventArgs
                        {
                            ActionType = HistoryChangeType.Imported,
                            CurrentIndex = _currentIndex,
                            TotalCount = _history.Count
                        });
                        
                        return true;
                    }
                }
            }
            catch
            {
                // 导入失败
            }
            
            return false;
        }
    }

    /// <summary>
    /// 历史记录变更类型
    /// </summary>
    public enum HistoryChangeType
    {
        Added,      // 添加
        Undo,       // 撤销
        Redo,       // 重做
        Jumped,     // 跳转
        Cleared,    // 清空
        Imported    // 导入
    }

    /// <summary>
    /// 历史记录变更事件参数
    /// </summary>
    public class HistoryChangedEventArgs : EventArgs
    {
        public HistoryChangeType ActionType { get; set; }
        public HistoryItem Item { get; set; }
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public int PreviousIndex { get; set; } = -1;
    }

    /// <summary>
    /// 内存使用量事件参数
    /// </summary>
    public class MemoryUsageEventArgs : EventArgs
    {
        public double CurrentUsageMB { get; set; }
        public double MaxUsageMB { get; set; }
        public bool IsOverLimit => CurrentUsageMB > MaxUsageMB;
    }

    /// <summary>
    /// 历史记录统计信息
    /// </summary>
    public class HistoryStatistics
    {
        public int TotalCount { get; set; }
        public int CurrentIndex { get; set; }
        public double MemoryUsageMB { get; set; }
        public bool CanUndo { get; set; }
        public bool CanRedo { get; set; }
        public DateTime OldestTimestamp { get; set; }
        public DateTime NewestTimestamp { get; set; }
        public Dictionary<HistoryActionType, int> ActionTypeCounts { get; set; } = new Dictionary<HistoryActionType, int>();
        public double AverageSize { get; set; }
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// ROI形状JSON转换器
    /// </summary>
    public class ROIShapeJsonConverter : JsonConverter<ROIShape>
    {
        public override ROIShape Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("$type", out var typeProperty))
                return null;
            
            var typeName = typeProperty.GetString();
            var json = root.GetRawText();
            
            return typeName switch
            {
                nameof(PointROI) => JsonSerializer.Deserialize<PointROI>(json, options),
                nameof(LineROI) => JsonSerializer.Deserialize<LineROI>(json, options),
                nameof(RectangleROI) => JsonSerializer.Deserialize<RectangleROI>(json, options),
                nameof(CircleROI) => JsonSerializer.Deserialize<CircleROI>(json, options),
                nameof(PolygonROI) => JsonSerializer.Deserialize<PolygonROI>(json, options),
                _ => null
            };
        }
        
        public override void Write(Utf8JsonWriter writer, ROIShape value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("$type", value.GetType().Name);
            
            var json = JsonSerializer.Serialize(value, value.GetType(), options);
            using var doc = JsonDocument.Parse(json);
            
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                property.WriteTo(writer);
            }
            
            writer.WriteEndObject();
        }
    }
}
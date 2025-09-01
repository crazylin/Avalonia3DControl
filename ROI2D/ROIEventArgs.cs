using System;
using System.Collections.Generic;
using System.Drawing;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI鼠标事件参数基类
    /// </summary>
    public abstract class ROIMouseEventArgs : EventArgs
    {
        /// <summary>
        /// 鼠标位置（屏幕坐标）
        /// </summary>
        public Point2D MousePosition { get; set; }
        
        /// <summary>
        /// 鼠标按钮
        /// </summary>
        public MouseButton Button { get; set; }
        
        /// <summary>
        /// 修饰键状态
        /// </summary>
        public ModifierKeys Modifiers { get; set; }
        
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }
        
        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ROI鼠标按下事件参数
    /// </summary>
    public class ROIMouseDownEventArgs : ROIMouseEventArgs
    {
        /// <summary>
        /// 点击次数
        /// </summary>
        public int ClickCount { get; set; } = 1;
        
        /// <summary>
        /// 是否开始拖拽
        /// </summary>
        public bool StartDrag { get; set; }
    }

    /// <summary>
    /// ROI鼠标移动事件参数
    /// </summary>
    public class ROIMouseMoveEventArgs : ROIMouseEventArgs
    {
        /// <summary>
        /// 鼠标移动增量
        /// </summary>
        public Point2D Delta { get; set; }
        
        /// <summary>
        /// 是否正在拖拽
        /// </summary>
        public bool IsDragging { get; set; }
        
        /// <summary>
        /// 拖拽开始位置
        /// </summary>
        public Point2D DragStartPosition { get; set; }
    }

    /// <summary>
    /// ROI鼠标抬起事件参数
    /// </summary>
    public class ROIMouseUpEventArgs : ROIMouseEventArgs
    {
        /// <summary>
        /// 是否结束拖拽
        /// </summary>
        public bool EndDrag { get; set; }
        
        /// <summary>
        /// 拖拽距离
        /// </summary>
        public double DragDistance { get; set; }
    }

    /// <summary>
    /// ROI鼠标滚轮事件参数
    /// </summary>
    public class ROIMouseWheelEventArgs : ROIMouseEventArgs
    {
        /// <summary>
        /// 滚轮增量
        /// </summary>
        public int Delta { get; set; }
        
        /// <summary>
        /// 缩放中心点
        /// </summary>
        public Point2D ZoomCenter { get; set; }
    }

    /// <summary>
    /// ROI选择事件参数
    /// </summary>
    public class ROISelectionEventArgs : EventArgs
    {
        /// <summary>
        /// 选中的ROI列表
        /// </summary>
        public List<ROIShape> SelectedROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 新增选中的ROI列表
        /// </summary>
        public List<ROIShape> AddedROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 移除选中的ROI列表
        /// </summary>
        public List<ROIShape> RemovedROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 选择类型
        /// </summary>
        public SelectionType SelectionType { get; set; }
        
        /// <summary>
        /// 选择区域（用于框选）
        /// </summary>
        public Rectangle2D SelectionArea { get; set; }
    }

    /// <summary>
    /// ROI创建事件参数
    /// </summary>
    public class ROICreatedEventArgs : EventArgs
    {
        /// <summary>
        /// 创建的ROI
        /// </summary>
        public ROIShape CreatedROI { get; set; }
        
        /// <summary>
        /// 创建类型
        /// </summary>
        public ROIType ROIType { get; set; }
        
        /// <summary>
        /// 创建参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// ROI修改事件参数
    /// </summary>
    public class ROIModifiedEventArgs : EventArgs
    {
        /// <summary>
        /// 修改的ROI
        /// </summary>
        public ROIShape ModifiedROI { get; set; }
        
        /// <summary>
        /// 修改前的状态
        /// </summary>
        public ROIShape OriginalROI { get; set; }
        
        /// <summary>
        /// 修改类型
        /// </summary>
        public ModificationType ModificationType { get; set; }
        
        /// <summary>
        /// 修改参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// ROI删除事件参数
    /// </summary>
    public class ROIDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// 删除的ROI列表
        /// </summary>
        public List<ROIShape> DeletedROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 删除原因
        /// </summary>
        public DeleteReason Reason { get; set; }
    }

    /// <summary>
    /// ROI几何运算事件参数
    /// </summary>
    public class ROIGeometryOperationEventArgs : EventArgs
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public GeometryOperationType OperationType { get; set; }
        
        /// <summary>
        /// 源ROI列表
        /// </summary>
        public List<ROIShape> SourceROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 结果ROI列表
        /// </summary>
        public List<ROIShape> ResultROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 操作参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 点生成事件参数
    /// </summary>
    public class PointGenerationEventArgs : EventArgs
    {
        /// <summary>
        /// 目标ROI
        /// </summary>
        public ROIShape TargetROI { get; set; }
        
        /// <summary>
        /// 生成的点列表
        /// </summary>
        public List<Point2D> GeneratedPoints { get; set; } = new List<Point2D>();
        
        /// <summary>
        /// 生成参数
        /// </summary>
        public PointGenerationParameters Parameters { get; set; }
        
        /// <summary>
        /// 生成结果
        /// </summary>
        public PointGenerationResult Result { get; set; }
    }

    /// <summary>
    /// 面片生成事件参数
    /// </summary>
    public class MeshGenerationEventArgs : EventArgs
    {
        /// <summary>
        /// 目标ROI
        /// </summary>
        public ROIShape TargetROI { get; set; }
        
        /// <summary>
        /// 输入点列表
        /// </summary>
        public List<Point2D> InputPoints { get; set; } = new List<Point2D>();
        
        /// <summary>
        /// 生成参数
        /// </summary>
        public MeshGenerationParameters Parameters { get; set; }
        
        /// <summary>
        /// 生成结果
        /// </summary>
        public MeshGenerationResult Result { get; set; }
    }

    /// <summary>
    /// ROI渲染事件参数
    /// </summary>
    public class ROIRenderEventArgs : EventArgs
    {
        /// <summary>
        /// 渲染的ROI列表
        /// </summary>
        public List<ROIShape> RenderedROIs { get; set; } = new List<ROIShape>();
        
        /// <summary>
        /// 渲染配置
        /// </summary>
        public RenderConfig RenderConfig { get; set; }
        
        /// <summary>
        /// 渲染时间（毫秒）
        /// </summary>
        public double RenderTimeMs { get; set; }
        
        /// <summary>
        /// 渲染的图元数量
        /// </summary>
        public int PrimitiveCount { get; set; }
    }

    /// <summary>
    /// 坐标转换事件参数
    /// </summary>
    public class CoordinateTransformEventArgs : EventArgs
    {
        /// <summary>
        /// 源坐标
        /// </summary>
        public Point2D SourceCoordinate { get; set; }
        
        /// <summary>
        /// 目标坐标
        /// </summary>
        public Point2D TargetCoordinate { get; set; }
        
        /// <summary>
        /// 转换类型
        /// </summary>
        public CoordinateTransformType TransformType { get; set; }
        
        /// <summary>
        /// 转换是否成功
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// 视图变更事件参数
    /// </summary>
    public class ViewChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 视图矩阵
        /// </summary>
        public Matrix3x3 ViewMatrix { get; set; }
        
        /// <summary>
        /// 投影矩阵
        /// </summary>
        public Matrix3x3 ProjectionMatrix { get; set; }
        
        /// <summary>
        /// 视口大小
        /// </summary>
        public Rectangle2D Viewport { get; set; }
        
        /// <summary>
        /// 缩放级别
        /// </summary>
        public double ZoomLevel { get; set; }
        
        /// <summary>
        /// 平移偏移
        /// </summary>
        public Point2D PanOffset { get; set; }
    }

    /// <summary>
    /// 键盘事件参数
    /// </summary>
    public class ROIKeyEventArgs : EventArgs
    {
        /// <summary>
        /// 按键
        /// </summary>
        public Key Key { get; set; }
        
        /// <summary>
        /// 修饰键
        /// </summary>
        public ModifierKeys Modifiers { get; set; }
        
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }
        
        /// <summary>
        /// 是否按下
        /// </summary>
        public bool IsDown { get; set; }
    }

    /// <summary>
    /// 图层事件参数
    /// </summary>
    public class LayerEventArgs : EventArgs
    {
        /// <summary>
        /// 图层ID
        /// </summary>
        public int LayerId { get; set; }
        
        /// <summary>
        /// 图层名称
        /// </summary>
        public string LayerName { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作类型
        /// </summary>
        public LayerOperationType OperationType { get; set; }
        
        /// <summary>
        /// 受影响的ROI列表
        /// </summary>
        public List<ROIShape> AffectedROIs { get; set; } = new List<ROIShape>();
    }

    /// <summary>
    /// 性能监控事件参数
    /// </summary>
    public class PerformanceEventArgs : EventArgs
    {
        /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; } = string.Empty;
        
        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public double ExecutionTimeMs { get; set; }
        
        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public double MemoryUsageMB { get; set; }
        
        /// <summary>
        /// CPU使用率（%）
        /// </summary>
        public double CpuUsagePercent { get; set; }
        
        /// <summary>
        /// 处理的对象数量
        /// </summary>
        public int ProcessedObjectCount { get; set; }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ROIErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception Exception { get; set; }
        
        /// <summary>
        /// 错误级别
        /// </summary>
        public ErrorLevel Level { get; set; }
        
        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode { get; set; }
        
        /// <summary>
        /// 相关ROI
        /// </summary>
        public ROIShape RelatedROI { get; set; }
    }

    // 枚举定义
    
    /// <summary>
    /// 鼠标按钮
    /// </summary>
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    /// <summary>
    /// 修饰键
    /// </summary>
    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 4,
        Windows = 8
    }

    /// <summary>
    /// 选择类型
    /// </summary>
    public enum SelectionType
    {
        Single,         // 单选
        Multiple,       // 多选
        Rectangle,      // 矩形框选
        Lasso,          // 套索选择
        All,            // 全选
        None,           // 取消选择
        Invert          // 反选
    }

    /// <summary>
    /// ROI类型
    /// </summary>
    public enum ROIType
    {
        Point,
        Line,
        Rectangle,
        Circle,
        Polygon,
        Ellipse,
        Spline,
        Text,
        Arrow,
        Custom
    }

    /// <summary>
    /// 修改类型
    /// </summary>
    public enum ModificationType
    {
        Move,           // 移动
        Resize,         // 调整大小
        Rotate,         // 旋转
        Scale,          // 缩放
        Transform,      // 变换
        Property,       // 属性修改
        Style,          // 样式修改
        Custom          // 自定义修改
    }

    /// <summary>
    /// 删除原因
    /// </summary>
    public enum DeleteReason
    {
        UserAction,     // 用户操作
        Cleanup,        // 清理
        Undo,           // 撤销
        Replace,        // 替换
        Error,          // 错误
        Timeout         // 超时
    }

    /// <summary>
    /// 几何运算类型
    /// </summary>
    public enum GeometryOperationType
    {
        Union,          // 并集
        Intersection,   // 交集
        Difference,     // 差集
        Clip,           // 裁剪
        Buffer,         // 缓冲区
        Simplify,       // 简化
        ConvexHull,     // 凸包
        Triangulate     // 三角剖分
    }

    /// <summary>
    /// 坐标转换类型
    /// </summary>
    public enum CoordinateTransformType
    {
        ScreenToWorld,  // 屏幕到世界坐标
        WorldToScreen,  // 世界到屏幕坐标
        ImageToWorld,   // 图像到世界坐标
        WorldToImage,   // 世界到图像坐标
        ScreenToImage,  // 屏幕到图像坐标
        ImageToScreen   // 图像到屏幕坐标
    }

    /// <summary>
    /// 图层操作类型
    /// </summary>
    public enum LayerOperationType
    {
        Create,         // 创建
        Delete,         // 删除
        Show,           // 显示
        Hide,           // 隐藏
        Lock,           // 锁定
        Unlock,         // 解锁
        Rename,         // 重命名
        Reorder         // 重新排序
    }

    /// <summary>
    /// 错误级别
    /// </summary>
    public enum ErrorLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 按键
    /// </summary>
    public enum Key
    {
        None,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        Enter, Escape, Space, Tab, Backspace, Delete,
        Left, Right, Up, Down,
        Home, End, PageUp, PageDown,
        Insert, PrintScreen, Pause,
        NumLock, CapsLock, ScrollLock,
        LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt,
        LeftWindows, RightWindows, Menu,
        Plus, Minus, Multiply, Divide, Decimal,
        NumPad0, NumPad1, NumPad2, NumPad3, NumPad4, NumPad5, NumPad6, NumPad7, NumPad8, NumPad9
    }
    
    /// <summary>
    /// ROI通用事件参数
    /// </summary>
    public class ROIEventArgs : EventArgs
    {
        /// <summary>
        /// 相关的ROI形状
        /// </summary>
        public ROIShape? Shape { get; set; }
        
        /// <summary>
        /// 事件类型
        /// </summary>
        public string EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// 附加数据
        /// </summary>
        public object? Data { get; set; }
        
        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }
    }
}
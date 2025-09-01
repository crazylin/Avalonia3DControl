using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI形状类型枚举
    /// </summary>
    public enum ROIShapeType
    {
        Point,
        Line,
        Rectangle,
        Circle,
        Polygon
    }
    
    /// <summary>
    /// 2D点结构
    /// </summary>
    public struct Point2D : IEquatable<Point2D>
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
        
        public static Point2D Zero => new Point2D(0, 0);
        
        public static Point2D operator +(Point2D a, Point2D b) => new Point2D(a.X + b.X, a.Y + b.Y);
        public static Point2D operator -(Point2D a, Point2D b) => new Point2D(a.X - b.X, a.Y - b.Y);
        public static Point2D operator *(Point2D a, double scale) => new Point2D(a.X * scale, a.Y * scale);
        public static Point2D operator /(Point2D a, double scale) => new Point2D(a.X / scale, a.Y / scale);
        public static bool operator ==(Point2D a, Point2D b) => a.Equals(b);
        public static bool operator !=(Point2D a, Point2D b) => !a.Equals(b);
        
        public double Distance(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        public double DistanceTo(Point2D other)
        {
            return Distance(other);
        }
        
        /// <summary>
        /// 转换为Vector2
        /// </summary>
        public System.Numerics.Vector2 ToVector2() => new System.Numerics.Vector2((float)X, (float)Y);
        
        public bool Equals(Point2D other) => Math.Abs(X - other.X) < 1e-10 && Math.Abs(Y - other.Y) < 1e-10;
        
        public override bool Equals(object obj) => obj is Point2D other && Equals(other);
        
        public override int GetHashCode() => HashCode.Combine(X, Y);
        
        public override string ToString() => $"({X:F2}, {Y:F2})";
    }
    
    /// <summary>
    /// 2D矩形结构
    /// </summary>
    public struct Rectangle2D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        
        public Rectangle2D(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        public double Left => X;
        public double Top => Y;
        public double Right => X + Width;
        public double Bottom => Y + Height;
        public Point2D Center => new Point2D(X + Width / 2, Y + Height / 2);
        
        public Point2D TopLeft => new Point2D(X, Y);
        public Point2D TopRight => new Point2D(X + Width, Y);
        public Point2D BottomLeft => new Point2D(X, Y + Height);
        public Point2D BottomRight => new Point2D(X + Width, Y + Height);
        
        public bool Contains(Point2D point)
        {
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }
        
        public bool Intersects(Rectangle2D other)
        {
            return X < other.X + other.Width && X + Width > other.X &&
                   Y < other.Y + other.Height && Y + Height > other.Y;
        }
        
        public override string ToString() => $"({X:F2}, {Y:F2}, {Width:F2}, {Height:F2})";
    }
    
    /// <summary>
    /// ROI形状基类
    /// </summary>
    public abstract class ROIShape
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Shape";
        public bool IsSelected { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// 形状类型
        /// </summary>
        public abstract ROIShapeType ShapeType { get; }
        public System.Drawing.Color StrokeColor { get; set; } = System.Drawing.Color.Red;
        public System.Drawing.Color FillColor { get; set; } = System.Drawing.Color.Transparent;
        public double StrokeWidth { get; set; } = 2.0;
        public double Opacity { get; set; } = 1.0;
        
        /// <summary>
        /// 获取形状的点集合
        /// </summary>
        public virtual List<Point2D> Points { get; set; } = new List<Point2D>();
        
        /// <summary>
        /// 圆形半径（仅圆形使用）
        /// </summary>
        public virtual double Radius { get; set; } = 0.0;
        
        /// <summary>
        /// 矩形宽度（仅矩形使用）
        /// </summary>
        public virtual double Width { get; set; } = 0.0;
        
        /// <summary>
        /// 矩形高度（仅矩形使用）
        /// </summary>
        public virtual double Height { get; set; } = 0.0;
        
        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public virtual double Rotation { get; set; } = 0.0;
        
        /// <summary>
        /// 获取形状边界
        /// </summary>
        public abstract Rectangle2D GetBounds();
        
        /// <summary>
        /// 检查点是否在形状内
        /// </summary>
        public abstract bool Contains(Point2D point);
        
        /// <summary>
        /// 移动形状
        /// </summary>
        public abstract void Move(Point2D offset);
        
        /// <summary>
         /// 克隆形状
         /// </summary>
         public abstract ROIShape Clone();
         
         /// <summary>
         /// 获取控制点列表
         /// </summary>
         public abstract List<Point2D> GetControlPoints();
         
         /// <summary>
         /// 计算形状面积
         /// </summary>
         public abstract double GetArea();
     }
     
     /// <summary>
     /// ROI输入模式
     /// </summary>
    public enum ROIInputMode
    {
        /// <summary>
        /// 指针模式 - 选择和编辑
        /// </summary>
        Pointer,
        
        /// <summary>
        /// 点绘制模式
        /// </summary>
        Point,
        
        /// <summary>
        /// 线绘制模式
        /// </summary>
        Line,
        
        /// <summary>
        /// 矩形绘制模式
        /// </summary>
        Rectangle,
        
        /// <summary>
        /// 圆形绘制模式
        /// </summary>
        Circle,
        
        /// <summary>
        /// 多边形绘制模式
        /// </summary>
        Polygon,
        
        /// <summary>
        /// 三角形绘制模式
        /// </summary>
        Triangle
    }
    
    /// <summary>
    /// ROI选择模式
    /// </summary>
    public enum ROISelectionMode
    {
        /// <summary>
        /// 单选模式
        /// </summary>
        Single,
        
        /// <summary>
        /// 多选模式
        /// </summary>
        Multiple,
        
        /// <summary>
        /// 框选模式
        /// </summary>
        Rectangle,
        
        /// <summary>
        /// 套索选择模式
        /// </summary>
        Lasso
    }
    
    /// <summary>
    /// ROI图层类
    /// </summary>
    public class ROILayer
    {
        public string Name { get; set; } = "Layer";
        public bool IsVisible { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public double Opacity { get; set; } = 1.0;
        public List<ROIShape> Shapes { get; set; } = new List<ROIShape>();
        
        public ROILayer() { }
        
        public ROILayer(string name)
        {
            Name = name;
        }
        
        /// <summary>
        /// 添加形状到图层
        /// </summary>
        public void AddShape(ROIShape shape)
        {
            if (shape != null && !Shapes.Contains(shape))
            {
                Shapes.Add(shape);
            }
        }
        
        /// <summary>
        /// 从图层移除形状
        /// </summary>
        public bool RemoveShape(ROIShape shape)
        {
            return Shapes.Remove(shape);
        }
        
        /// <summary>
        /// 清空图层
        /// </summary>
        public void Clear()
        {
            Shapes.Clear();
        }
        
        /// <summary>
        /// 获取图层边界
        /// </summary>
        public Rectangle2D GetBounds()
        {
            if (!Shapes.Any())
                return new Rectangle2D(0, 0, 0, 0);
            
            var bounds = Shapes.First().GetBounds();
            foreach (var shape in Shapes.Skip(1))
            {
                var shapeBounds = shape.GetBounds();
                var left = Math.Min(bounds.X, shapeBounds.X);
                var top = Math.Min(bounds.Y, shapeBounds.Y);
                var right = Math.Max(bounds.X + bounds.Width, shapeBounds.X + shapeBounds.Width);
                var bottom = Math.Max(bounds.Y + bounds.Height, shapeBounds.Y + shapeBounds.Height);
                bounds = new Rectangle2D(left, top, right - left, bottom - top);
            }
            
            return bounds;
        }
    }
}
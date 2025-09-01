using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Drawing;

namespace Avalonia3DControl.ROI2D
{




    /// <summary>
    /// 3x3变换矩阵
    /// </summary>
    public struct Matrix3x3
    {
        public float M11, M12, M13;
        public float M21, M22, M23;
        public float M31, M32, M33;
        
        public static Matrix3x3 Identity => new Matrix3x3
        {
            M11 = 1, M12 = 0, M13 = 0,
            M21 = 0, M22 = 1, M23 = 0,
            M31 = 0, M32 = 0, M33 = 1
        };
        
        public static Matrix3x3 CreateTranslation(double x, double y)
        {
            return new Matrix3x3
            {
                M11 = 1, M12 = 0, M13 = (float)x,
                M21 = 0, M22 = 1, M23 = (float)y,
                M31 = 0, M32 = 0, M33 = 1
            };
        }
        
        public static Matrix3x3 CreateRotation(double angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return new Matrix3x3
            {
                M11 = cos, M12 = -sin, M13 = 0,
                M21 = sin, M22 = cos, M23 = 0,
                M31 = 0, M32 = 0, M33 = 1
            };
        }
        
        public static Matrix3x3 CreateScale(double scaleX, double scaleY)
        {
            return new Matrix3x3
            {
                M11 = (float)scaleX, M12 = 0, M13 = 0,
                M21 = 0, M22 = (float)scaleY, M23 = 0,
                M31 = 0, M32 = 0, M33 = 1
            };
        }
        
        public Point2D Transform(Point2D point)
        {
            return new Point2D(
                M11 * point.X + M12 * point.Y + M13,
                M21 * point.X + M22 * point.Y + M23
            );
        }
    }



    /// <summary>
    /// 点ROI
    /// </summary>
    public class PointROI : ROIShape
    {
        public override ROIShapeType ShapeType => ROIShapeType.Point;
        
        public Point2D Center { get; set; }
        public new double Radius { get; set; } = 5.0;
        
        /// <summary>
        /// 点的X坐标
        /// </summary>
        public double X
        {
            get => Center.X;
            set => Center = new Point2D(value, Center.Y);
        }
        
        /// <summary>
        /// 点的Y坐标
        /// </summary>
        public double Y
        {
            get => Center.Y;
            set => Center = new Point2D(Center.X, value);
        }
        
        public PointROI() { }
        
        public PointROI(Point2D center, double radius = 5.0)
        {
            Center = center;
            Radius = radius;
        }
        
        public override bool Contains(Point2D point)
        {
            return Center.Distance(point) <= Radius;
        }
        
        public override Rectangle2D GetBounds()
        {
            return new Rectangle2D(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
        }
        
        public override void Move(Point2D delta)
        {
            Center = Center + delta;
        }
        
        public override ROIShape Clone()
        {
            return new PointROI(Center, Radius)
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                FillColor = FillColor,
                Opacity = Opacity
            };
        }
        
        public override List<Point2D> GetControlPoints()
        {
            return new List<Point2D> { Center };
        }
        
        public override double GetArea()
        {
            return Math.PI * Radius * Radius;
        }
    }

    /// <summary>
    /// 线ROI
    /// </summary>
    public class LineROI : ROIShape
    {
        public override ROIShapeType ShapeType => ROIShapeType.Line;
        
        public new List<Point2D> Points { get; set; } = new List<Point2D>();
        public bool IsClosed { get; set; } = false;
        public double LineWidth { get; set; } = 1.0;
        
        public double StartX => Points.Count > 0 ? Points[0].X : 0;
        public double StartY => Points.Count > 0 ? Points[0].Y : 0;
        public double EndX => Points.Count > 1 ? Points[1].X : 0;
        public double EndY => Points.Count > 1 ? Points[1].Y : 0;
        
        public LineROI() { }
        
        public LineROI(Point2D start, Point2D end)
        {
            Points.Add(start);
            Points.Add(end);
        }
        
        public LineROI(IEnumerable<Point2D> points, bool closed = false)
        {
            Points.AddRange(points);
            IsClosed = closed;
        }
        
        public override bool Contains(Point2D point)
        {
            if (Points.Count < 2) return false;
            
            double tolerance = LineWidth / 2.0;
            
            for (int i = 0; i < Points.Count - 1; i++)
            {
                if (DistanceToLineSegment(point, Points[i], Points[i + 1]) <= tolerance)
                    return true;
            }
            
            if (IsClosed && Points.Count > 2)
            {
                if (DistanceToLineSegment(point, Points[Points.Count - 1], Points[0]) <= tolerance)
                    return true;
            }
            
            return false;
        }
        
        private double DistanceToLineSegment(Point2D point, Point2D lineStart, Point2D lineEnd)
        {
            double A = point.X - lineStart.X;
            double B = point.Y - lineStart.Y;
            double C = lineEnd.X - lineStart.X;
            double D = lineEnd.Y - lineStart.Y;
            
            double dot = A * C + B * D;
            double lenSq = C * C + D * D;
            
            if (lenSq == 0) return point.Distance(lineStart);
            
            double param = dot / lenSq;
            
            Point2D projection;
            if (param < 0)
            {
                projection = lineStart;
            }
            else if (param > 1)
            {
                projection = lineEnd;
            }
            else
            {
                projection = new Point2D(lineStart.X + param * C, lineStart.Y + param * D);
            }
            
            return point.Distance(projection);
        }
        
        public override Rectangle2D GetBounds()
        {
            if (Points.Count == 0) return new Rectangle2D();
            
            double minX = Points.Min(p => p.X);
            double minY = Points.Min(p => p.Y);
            double maxX = Points.Max(p => p.X);
            double maxY = Points.Max(p => p.Y);
            
            return new Rectangle2D(minX, minY, maxX - minX, maxY - minY);
        }
        
        public override void Move(Point2D delta)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = Points[i] + delta;
            }
        }
        
        public override ROIShape Clone()
        {
            return new LineROI(Points, IsClosed)
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                FillColor = FillColor,
                Opacity = Opacity
            };
        }
        
        public override List<Point2D> GetControlPoints()
        {
            return new List<Point2D>(Points);
        }
        
        public override double GetArea()
        {
            return 0.0; // 线的面积为0
        }
    }

    /// <summary>
    /// 矩形ROI
    /// </summary>
    public class RectangleROI : ROIShape
    {
        public override ROIShapeType ShapeType => ROIShapeType.Rectangle;
        
        public Point2D Center { get; set; }
        public new double Width { get; set; }
        public new double Height { get; set; }
        public double Angle { get; set; } = 0.0; // 旋转角度（弧度）
        
        /// <summary>
        /// 矩形左上角X坐标
        /// </summary>
        public double X
        {
            get => Center.X - Width / 2;
            set => Center = new Point2D(value + Width / 2, Center.Y);
        }
        
        /// <summary>
        /// 矩形左上角Y坐标
        /// </summary>
        public double Y
        {
            get => Center.Y - Height / 2;
            set => Center = new Point2D(Center.X, value + Height / 2);
        }
        
        public Point2D TopLeft => new Point2D(X, Y);
        public Point2D TopRight => new Point2D(X + Width, Y);
        public Point2D BottomLeft => new Point2D(X, Y + Height);
        public Point2D BottomRight => new Point2D(X + Width, Y + Height);
        
        public bool IsFilled { get; set; } = true;
        public double LineWidth { get; set; } = 1.0;
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;
        
        public RectangleROI() { }
        
        public RectangleROI(Point2D center, double width, double height, double angle = 0.0)
        {
            Center = center;
            Width = width;
            Height = height;
            Angle = angle;
        }
        
        public override bool Contains(Point2D point)
        {
            // 将点转换到矩形的局部坐标系
            Point2D localPoint = point - Center;
            
            if (Math.Abs(Angle) > 1e-10)
            {
                double cos = Math.Cos(-Angle);
                double sin = Math.Sin(-Angle);
                localPoint = new Point2D(
                    localPoint.X * cos - localPoint.Y * sin,
                    localPoint.X * sin + localPoint.Y * cos
                );
            }
            
            return Math.Abs(localPoint.X) <= Width / 2 && Math.Abs(localPoint.Y) <= Height / 2;
        }
        
        public override Rectangle2D GetBounds()
        {
            if (Math.Abs(Angle) < 1e-10)
            {
                return new Rectangle2D(Center.X - Width / 2, Center.Y - Height / 2, Width, Height);
            }
            
            // 计算旋转后的边界
            var corners = GetCorners();
            double minX = corners.Min(p => p.X);
            double minY = corners.Min(p => p.Y);
            double maxX = corners.Max(p => p.X);
            double maxY = corners.Max(p => p.Y);
            
            return new Rectangle2D(minX, minY, maxX - minX, maxY - minY);
        }
        
        public List<Point2D> GetCorners()
        {
            double halfWidth = Width / 2;
            double halfHeight = Height / 2;
            
            var corners = new List<Point2D>
            {
                new Point2D(-halfWidth, -halfHeight),
                new Point2D(halfWidth, -halfHeight),
                new Point2D(halfWidth, halfHeight),
                new Point2D(-halfWidth, halfHeight)
            };
            
            if (Math.Abs(Angle) > 1e-10)
            {
                double cos = Math.Cos(Angle);
                double sin = Math.Sin(Angle);
                
                for (int i = 0; i < corners.Count; i++)
                {
                    Point2D p = corners[i];
                    corners[i] = new Point2D(
                        p.X * cos - p.Y * sin + Center.X,
                        p.X * sin + p.Y * cos + Center.Y
                    );
                }
            }
            else
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    corners[i] = corners[i] + Center;
                }
            }
            
            return corners;
        }
        
        public override void Move(Point2D delta)
        {
            Center = Center + delta;
        }
        
        public override ROIShape Clone()
        {
            return new RectangleROI(Center, Width, Height, Angle)
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                FillColor = FillColor,
                Opacity = Opacity
            };
        }
        
        public override List<Point2D> GetControlPoints()
        {
            return GetCorners();
        }
        
        public override double GetArea()
        {
            return Width * Height;
        }
    }

    /// <summary>
    /// 圆形ROI
    /// </summary>
    public class CircleROI : ROIShape
    {
        public override ROIShapeType ShapeType => ROIShapeType.Circle;
        
        public Point2D Center { get; set; }
        public new double Radius { get; set; }
        public bool IsFilled { get; set; } = true;
        public double LineWidth { get; set; } = 1.0;
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;
        
        /// <summary>
        /// 圆心X坐标
        /// </summary>
        public double CenterX
        {
            get => Center.X;
            set => Center = new Point2D(value, Center.Y);
        }
        
        /// <summary>
        /// 圆心Y坐标
        /// </summary>
        public double CenterY
        {
            get => Center.Y;
            set => Center = new Point2D(Center.X, value);
        }
        
        public CircleROI() { }
        
        public CircleROI(Point2D center, double radius)
        {
            Center = center;
            Radius = radius;
        }
        
        public override bool Contains(Point2D point)
        {
            return Center.Distance(point) <= Radius;
        }
        
        public override Rectangle2D GetBounds()
        {
            return new Rectangle2D(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
        }
        
        public override void Move(Point2D delta)
        {
            Center = Center + delta;
        }
        
        public override ROIShape Clone()
        {
            return new CircleROI(Center, Radius)
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                FillColor = FillColor,
                Opacity = Opacity
            };
        }
        
        public override List<Point2D> GetControlPoints()
        {
            // 返回圆心和圆周上的4个关键点
            return new List<Point2D>
            {
                Center, // 圆心
                new Point2D(Center.X + Radius, Center.Y), // 右
                new Point2D(Center.X, Center.Y + Radius), // 下
                new Point2D(Center.X - Radius, Center.Y), // 左
                new Point2D(Center.X, Center.Y - Radius)  // 上
            };
        }
        
        public override double GetArea()
        {
            return Math.PI * Radius * Radius;
        }
    }

    /// <summary>
    /// 多边形ROI
    /// </summary>
    public class PolygonROI : ROIShape
    {
        public override ROIShapeType ShapeType => ROIShapeType.Polygon;
        
        public List<Point2D> Vertices { get; set; } = new List<Point2D>();
        public bool IsFilled { get; set; } = true;
        public double LineWidth { get; set; } = 1.0;
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;
        
        public PolygonROI() { }
        
        public PolygonROI(IEnumerable<Point2D> vertices)
        {
            Vertices.AddRange(vertices);
        }
        
        public override bool Contains(Point2D point)
        {
            if (Vertices.Count < 3) return false;
            
            // 使用射线投射算法
            bool inside = false;
            int j = Vertices.Count - 1;
            
            for (int i = 0; i < Vertices.Count; i++)
            {
                if (((Vertices[i].Y > point.Y) != (Vertices[j].Y > point.Y)) &&
                    (point.X < (Vertices[j].X - Vertices[i].X) * (point.Y - Vertices[i].Y) / (Vertices[j].Y - Vertices[i].Y) + Vertices[i].X))
                {
                    inside = !inside;
                }
                j = i;
            }
            
            return inside;
        }
        
        public override Rectangle2D GetBounds()
        {
            if (Vertices.Count == 0) return new Rectangle2D();
            
            double minX = Vertices.Min(p => p.X);
            double minY = Vertices.Min(p => p.Y);
            double maxX = Vertices.Max(p => p.X);
            double maxY = Vertices.Max(p => p.Y);
            
            return new Rectangle2D(minX, minY, maxX - minX, maxY - minY);
        }
        
        public override void Move(Point2D delta)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = Vertices[i] + delta;
            }
        }
        
        public override ROIShape Clone()
        {
            return new PolygonROI(Vertices)
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                StrokeColor = StrokeColor,
                StrokeWidth = StrokeWidth,
                FillColor = FillColor,
                Opacity = Opacity
            };
        }
        
        public override List<Point2D> GetControlPoints()
        {
            return new List<Point2D>(Vertices);
        }
        
        public override double GetArea()
        {
            if (Vertices.Count < 3) return 0.0;
            
            double area = 0.0;
            int n = Vertices.Count;
            
            // 使用鞋带公式计算多边形面积
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += Vertices[i].X * Vertices[j].Y;
                area -= Vertices[j].X * Vertices[i].Y;
            }
            
            return Math.Abs(area) / 2.0;
        }
    }
}
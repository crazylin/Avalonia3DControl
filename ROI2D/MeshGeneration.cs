using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 面片生成模式
    /// </summary>
    public enum MeshGenerationMode
    {
        /// <summary>
        /// 三角面网格
        /// </summary>
        Triangular,
        
        /// <summary>
        /// 四边形网格
        /// </summary>
        Quadrilateral,
        
        /// <summary>
        /// 德劳内三角剖分
        /// </summary>
        Delaunay,
        
        /// <summary>
        /// 约束德劳内三角剖分
        /// </summary>
        ConstrainedDelaunay,
        
        /// <summary>
        /// 自适应网格
        /// </summary>
        Adaptive
    }
    
    /// <summary>
    /// 面片生成参数
    /// </summary>
    public class MeshGenerationParameters
    {
        /// <summary>
        /// 生成模式
        /// </summary>
        public MeshGenerationMode Mode { get; set; } = MeshGenerationMode.Triangular;
        
        /// <summary>
        /// 网格密度（单位长度内的面片数量）
        /// </summary>
        public double Density { get; set; } = 0.1;
        
        /// <summary>
        /// 最大面片大小
        /// </summary>
        public double MaxFaceSize { get; set; } = 50.0;
        
        /// <summary>
        /// 最小面片大小
        /// </summary>
        public double MinFaceSize { get; set; } = 5.0;
        
        /// <summary>
        /// 质量阈值（0-1，越高质量越好）
        /// </summary>
        public double QualityThreshold { get; set; } = 0.7;
        
        /// <summary>
        /// 最大三角形面积
        /// </summary>
        public double MaxTriangleArea { get; set; } = 100.0;
        
        /// <summary>
        /// 最小角度（度）
        /// </summary>
        public double MinAngle { get; set; } = 20.0;
        
        /// <summary>
        /// 边界密度
        /// </summary>
        public double BoundaryDensity { get; set; } = 0.1;
        
        /// <summary>
        /// 是否保持边界
        /// </summary>
        public bool PreserveBoundary { get; set; } = true;
        
        /// <summary>
        /// 随机种子
        /// </summary>
        public int RandomSeed { get; set; } = 42;
        
        /// <summary>
        /// 平滑强度（0-1，用于网格平滑处理）
        /// </summary>
        public double SmoothingStrength { get; set; } = 0.5;
        
        /// <summary>
        /// 最大迭代次数（用于网格优化算法）
        /// </summary>
        public int MaxIterations { get; set; } = 10;
        
        /// <summary>
        /// 是否优化质量
        /// </summary>
        public bool OptimizeQuality { get; set; } = true;
        
        /// <summary>
        /// 是否生成内部点
        /// </summary>
        public bool GenerateInteriorPoints { get; set; } = true;
        
        /// <summary>
        /// 面片类型
        /// </summary>
        public MeshType MeshType { get; set; } = MeshType.Triangle;
    }
    
    /// <summary>
    /// 三角面
    /// </summary>
    public class Triangle
    {
        public Point2D A { get; set; }
        public Point2D B { get; set; }
        public Point2D C { get; set; }
        
        public Triangle(Point2D a, Point2D b, Point2D c)
        {
            A = a;
            B = b;
            C = c;
        }
        
        /// <summary>
        /// 计算三角形面积
        /// </summary>
        public double Area
        {
            get
            {
                return Math.Abs((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y)) / 2.0;
            }
        }
        
        /// <summary>
        /// 计算三角形质量（面积与周长平方的比值）
        /// </summary>
        public double Quality
        {
            get
            {
                var area = Area;
                var perimeter = Distance(A, B) + Distance(B, C) + Distance(C, A);
                return 4.0 * Math.Sqrt(3.0) * area / (perimeter * perimeter);
            }
        }
        
        /// <summary>
        /// 计算外心
        /// </summary>
        public Point2D Circumcenter
        {
            get
            {
                var d = 2 * (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
                if (Math.Abs(d) < 1e-10) return new Point2D((A.X + B.X + C.X) / 3, (A.Y + B.Y + C.Y) / 3);
                
                var ux = ((A.X * A.X + A.Y * A.Y) * (B.Y - C.Y) + (B.X * B.X + B.Y * B.Y) * (C.Y - A.Y) + (C.X * C.X + C.Y * C.Y) * (A.Y - B.Y)) / d;
                var uy = ((A.X * A.X + A.Y * A.Y) * (C.X - B.X) + (B.X * B.X + B.Y * B.Y) * (A.X - C.X) + (C.X * C.X + C.Y * C.Y) * (B.X - A.X)) / d;
                
                return new Point2D(ux, uy);
            }
        }
        
        /// <summary>
        /// 判断点是否在三角形内
        /// </summary>
        public bool Contains(Point2D point)
        {
            var v0 = new System.Numerics.Vector2((float)(C.X - A.X), (float)(C.Y - A.Y));
            var v1 = new System.Numerics.Vector2((float)(B.X - A.X), (float)(B.Y - A.Y));
            var v2 = new System.Numerics.Vector2((float)(point.X - A.X), (float)(point.Y - A.Y));
            
            var dot00 = System.Numerics.Vector2.Dot(v0, v0);
            var dot01 = System.Numerics.Vector2.Dot(v0, v1);
            var dot02 = System.Numerics.Vector2.Dot(v0, v2);
            var dot11 = System.Numerics.Vector2.Dot(v1, v1);
            var dot12 = System.Numerics.Vector2.Dot(v1, v2);
            
            var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
            
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
        
        /// <summary>
        /// 计算角度（度）
        /// </summary>
        public double GetAngle(Point2D vertex, Point2D p1, Point2D p2)
        {
            var v1 = new System.Numerics.Vector2((float)(p1.X - vertex.X), (float)(p1.Y - vertex.Y));
            var v2 = new System.Numerics.Vector2((float)(p2.X - vertex.X), (float)(p2.Y - vertex.Y));
            
            v1 = System.Numerics.Vector2.Normalize(v1);
            v2 = System.Numerics.Vector2.Normalize(v2);
            
            var dot = System.Numerics.Vector2.Dot(v1, v2);
            return Math.Acos(Math.Clamp(dot, -1.0, 1.0)) * 180.0 / Math.PI;
        }
        
        /// <summary>
        /// 判断三角形质量是否良好
        /// </summary>
        public bool IsWellShaped => Quality > 0.3;
        
        /// <summary>
        /// 获取三角形重心
        /// </summary>
        public Point2D Centroid => new Point2D(
            (A.X + B.X + C.X) / 3.0,
            (A.Y + B.Y + C.Y) / 3.0
        );
        
        private static double Distance(Point2D a, Point2D b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
    
    /// <summary>
    /// 四边形面
    /// </summary>
    public class Quadrilateral
    {
        public Point2D A { get; set; }
        public Point2D B { get; set; }
        public Point2D C { get; set; }
        public Point2D D { get; set; }
        
        public Quadrilateral(Point2D a, Point2D b, Point2D c, Point2D d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }
        
        /// <summary>
        /// 计算四边形面积
        /// </summary>
        public double Area
        {
            get
            {
                // 使用鞋带公式
                return Math.Abs((A.X * B.Y - B.X * A.Y) + (B.X * C.Y - C.X * B.Y) + 
                               (C.X * D.Y - D.X * C.Y) + (D.X * A.Y - A.X * D.Y)) / 2.0;
            }
        }
        
        /// <summary>
        /// 转换为两个三角形
        /// </summary>
        public List<Triangle> ToTriangles()
        {
            return new List<Triangle>
            {
                new Triangle(A, B, C),
                new Triangle(A, C, D)
            };
        }
    }
    
    /// <summary>
    /// 面片生成器
    /// </summary>
    public static class MeshGeneration
    {
        /// <summary>
        /// 在ROI形状内生成面片
        /// </summary>
        public static List<Triangle> GenerateTriangles(ROIShape shape, MeshGenerationParameters parameters)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            return parameters.Mode switch
            {
                MeshGenerationMode.Triangular => GenerateRegularTriangles(shape, parameters),
                MeshGenerationMode.Delaunay => GenerateDelaunayTriangles(shape, parameters),
                MeshGenerationMode.ConstrainedDelaunay => GenerateConstrainedDelaunayTriangles(shape, parameters),
                MeshGenerationMode.Adaptive => GenerateAdaptiveTriangles(shape, parameters),
                _ => GenerateRegularTriangles(shape, parameters)
            };
        }
        
        /// <summary>
        /// 在ROI形状内生成四边形面片
        /// </summary>
        public static List<Quadrilateral> GenerateQuadrilaterals(ROIShape shape, MeshGenerationParameters parameters)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            return GenerateRegularQuadrilaterals(shape, parameters);
        }
        
        /// <summary>
        /// 生成规则三角网格
        /// </summary>
        private static List<Triangle> GenerateRegularTriangles(ROIShape shape, MeshGenerationParameters parameters)
        {
            var triangles = new List<Triangle>();
            var bounds = GetShapeBounds(shape);
            var spacing = Math.Sqrt(1.0 / parameters.Density);
            
            for (var y = bounds.MinY; y <= bounds.MaxY; y += spacing * Math.Sqrt(3) / 2)
            {
                var rowOffset = ((int)((y - bounds.MinY) / (spacing * Math.Sqrt(3) / 2)) % 2) * spacing / 2;
                
                for (var x = bounds.MinX + rowOffset; x <= bounds.MaxX; x += spacing)
                {
                    var p1 = new Point2D(x, y);
                    var p2 = new Point2D(x + spacing / 2, y + spacing * Math.Sqrt(3) / 2);
                    var p3 = new Point2D(x - spacing / 2, y + spacing * Math.Sqrt(3) / 2);
                    
                    if (IsTriangleInShape(new Triangle(p1, p2, p3), shape))
                    {
                        triangles.Add(new Triangle(p1, p2, p3));
                    }
                }
            }
            
            return triangles;
        }
        
        /// <summary>
        /// 生成德劳内三角剖分
        /// </summary>
        private static List<Triangle> GenerateDelaunayTriangles(ROIShape shape, MeshGenerationParameters parameters)
        {
            // 简化的德劳内三角剖分实现
            var points = GeneratePointsInShape(shape, parameters);
            return DelaunayTriangulation(points);
        }
        
        /// <summary>
        /// 生成约束德劳内三角剖分
        /// </summary>
        private static List<Triangle> GenerateConstrainedDelaunayTriangles(ROIShape shape, MeshGenerationParameters parameters)
        {
            var points = GeneratePointsInShape(shape, parameters);
            var boundaryPoints = GetBoundaryPoints(shape);
            points.AddRange(boundaryPoints);
            
            return DelaunayTriangulation(points);
        }
        
        /// <summary>
        /// 生成自适应三角网格
        /// </summary>
        private static List<Triangle> GenerateAdaptiveTriangles(ROIShape shape, MeshGenerationParameters parameters)
        {
            var triangles = GenerateRegularTriangles(shape, parameters);
            
            // 细化质量差的三角形
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i].Quality < parameters.QualityThreshold)
                {
                    var refined = RefineTriangle(triangles[i]);
                    triangles.RemoveAt(i);
                    triangles.InsertRange(i, refined);
                    i += refined.Count - 1;
                }
            }
            
            return triangles;
        }
        
        /// <summary>
        /// 生成规则四边形网格
        /// </summary>
        private static List<Quadrilateral> GenerateRegularQuadrilaterals(ROIShape shape, MeshGenerationParameters parameters)
        {
            var quads = new List<Quadrilateral>();
            var bounds = GetShapeBounds(shape);
            var spacing = Math.Sqrt(1.0 / parameters.Density);
            
            for (var y = bounds.MinY; y <= bounds.MaxY - spacing; y += spacing)
            {
                for (var x = bounds.MinX; x <= bounds.MaxX - spacing; x += spacing)
                {
                    var p1 = new Point2D(x, y);
                    var p2 = new Point2D(x + spacing, y);
                    var p3 = new Point2D(x + spacing, y + spacing);
                    var p4 = new Point2D(x, y + spacing);
                    
                    var quad = new Quadrilateral(p1, p2, p3, p4);
                    
                    if (IsQuadrilateralInShape(quad, shape))
                    {
                        quads.Add(quad);
                    }
                }
            }
            
            return quads;
        }
        
        /// <summary>
        /// 德劳内三角剖分（简化实现）
        /// </summary>
        private static List<Triangle> DelaunayTriangulation(List<Point2D> points)
        {
            var triangles = new List<Triangle>();
            
            if (points.Count < 3)
                return triangles;
            
            // 简化的Bowyer-Watson算法实现
            // 这里使用一个简单的贪心方法作为示例
            for (int i = 0; i < points.Count - 2; i++)
            {
                for (int j = i + 1; j < points.Count - 1; j++)
                {
                    for (int k = j + 1; k < points.Count; k++)
                    {
                        var triangle = new Triangle(points[i], points[j], points[k]);
                        if (triangle.Area > 1e-10) // 避免退化三角形
                        {
                            triangles.Add(triangle);
                        }
                    }
                }
            }
            
            return triangles;
        }
        
        /// <summary>
        /// 细化三角形
        /// </summary>
        private static List<Triangle> RefineTriangle(Triangle triangle)
        {
            var center = new Point2D(
                (triangle.A.X + triangle.B.X + triangle.C.X) / 3,
                (triangle.A.Y + triangle.B.Y + triangle.C.Y) / 3
            );
            
            return new List<Triangle>
            {
                new Triangle(triangle.A, triangle.B, center),
                new Triangle(triangle.B, triangle.C, center),
                new Triangle(triangle.C, triangle.A, center)
            };
        }
        
        /// <summary>
        /// 在形状内生成点
        /// </summary>
        private static List<Point2D> GeneratePointsInShape(ROIShape shape, MeshGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = GetShapeBounds(shape);
            var random = new Random(parameters.RandomSeed);
            var numPoints = (int)(bounds.Width * bounds.Height * parameters.Density);
            
            for (int i = 0; i < numPoints; i++)
            {
                var x = bounds.MinX + random.NextDouble() * bounds.Width;
                var y = bounds.MinY + random.NextDouble() * bounds.Height;
                var point = new Point2D(x, y);
                
                if (IsPointInShape(point, shape))
                {
                    points.Add(point);
                }
            }
            
            return points;
        }
        
        /// <summary>
        /// 获取形状边界点
        /// </summary>
        private static List<Point2D> GetBoundaryPoints(ROIShape shape)
        {
            return shape switch
            {
                PolygonROI polygon => polygon.Vertices.ToList(),
                RectangleROI rectangle => new List<Point2D>
                {
                    new Point2D(rectangle.X, rectangle.Y),
                    new Point2D(rectangle.X + rectangle.Width, rectangle.Y),
                    new Point2D(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height),
                    new Point2D(rectangle.X, rectangle.Y + rectangle.Height)
                },
                _ => new List<Point2D>()
            };
        }
        
        /// <summary>
        /// 获取形状边界框
        /// </summary>
        private static (double MinX, double MinY, double MaxX, double MaxY, double Width, double Height) GetShapeBounds(ROIShape shape)
        {
            return shape switch
            {
                PointROI point => (point.X - 1, point.Y - 1, point.X + 1, point.Y + 1, 2, 2),
                LineROI line => (Math.Min(line.StartX, line.EndX) - 1, Math.Min(line.StartY, line.EndY) - 1,
                               Math.Max(line.StartX, line.EndX) + 1, Math.Max(line.StartY, line.EndY) + 1,
                               Math.Abs(line.EndX - line.StartX) + 2, Math.Abs(line.EndY - line.StartY) + 2),
                RectangleROI rect => (rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height, rect.Width, rect.Height),
                CircleROI circle => (circle.CenterX - circle.Radius, circle.CenterY - circle.Radius,
                                   circle.CenterX + circle.Radius, circle.CenterY + circle.Radius,
                                   circle.Radius * 2, circle.Radius * 2),
                PolygonROI polygon => (
                    polygon.Vertices.Min(p => p.X), polygon.Vertices.Min(p => p.Y),
                    polygon.Vertices.Max(p => p.X), polygon.Vertices.Max(p => p.Y),
                    polygon.Vertices.Max(p => p.X) - polygon.Vertices.Min(p => p.X),
                    polygon.Vertices.Max(p => p.Y) - polygon.Vertices.Min(p => p.Y)
                ),
                _ => (0, 0, 100, 100, 100, 100)
            };
        }
        
        /// <summary>
        /// 判断点是否在形状内
        /// </summary>
        private static bool IsPointInShape(Point2D point, ROIShape shape)
        {
            return shape switch
            {
                PointROI p => Math.Abs(point.X - p.X) < 1 && Math.Abs(point.Y - p.Y) < 1,
                RectangleROI rect => point.X >= rect.X && point.X <= rect.X + rect.Width &&
                                   point.Y >= rect.Y && point.Y <= rect.Y + rect.Height,
                CircleROI circle => Math.Pow(point.X - circle.CenterX, 2) + Math.Pow(point.Y - circle.CenterY, 2) <= Math.Pow(circle.Radius, 2),
                PolygonROI polygon => IsPointInPolygon(point, polygon.Vertices),
                _ => false
            };
        }
        
        /// <summary>
        /// 判断三角形是否在形状内
        /// </summary>
        private static bool IsTriangleInShape(Triangle triangle, ROIShape shape)
        {
            return IsPointInShape(triangle.A, shape) && 
                   IsPointInShape(triangle.B, shape) && 
                   IsPointInShape(triangle.C, shape);
        }
        
        /// <summary>
        /// 判断四边形是否在形状内
        /// </summary>
        private static bool IsQuadrilateralInShape(Quadrilateral quad, ROIShape shape)
        {
            return IsPointInShape(quad.A, shape) && 
                   IsPointInShape(quad.B, shape) && 
                   IsPointInShape(quad.C, shape) && 
                   IsPointInShape(quad.D, shape);
        }
        
        /// <summary>
        /// 判断点是否在多边形内（射线法）
        /// </summary>
        private static bool IsPointInPolygon(Point2D point, IList<Point2D> vertices)
        {
            bool inside = false;
            int j = vertices.Count - 1;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                if (((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                    (point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                {
                    inside = !inside;
                }
                j = i;
            }
            
            return inside;
        }
    }
}
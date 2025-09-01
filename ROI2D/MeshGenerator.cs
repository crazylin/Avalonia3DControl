using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 面片类型
    /// </summary>
    public enum MeshType
    {
        Triangle,   // 三角面
        Quad,       // 四边形面
        Mixed       // 混合面片
    }



    /// <summary>
    /// 四边形面
    /// </summary>
    public struct Quad
    {
        public Point2D A { get; set; }
        public Point2D B { get; set; }
        public Point2D C { get; set; }
        public Point2D D { get; set; }
        
        public Quad(Point2D a, Point2D b, Point2D c, Point2D d)
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
                return Math.Abs((A.X * B.Y - B.X * A.Y) + 
                               (B.X * C.Y - C.X * B.Y) + 
                               (C.X * D.Y - D.X * C.Y) + 
                               (D.X * A.Y - A.X * D.Y)) / 2.0;
            }
        }
        
        /// <summary>
        /// 计算四边形重心
        /// </summary>
        public Point2D Centroid
        {
            get
            {
                return new Point2D((A.X + B.X + C.X + D.X) / 4.0, (A.Y + B.Y + C.Y + D.Y) / 4.0);
            }
        }
        
        /// <summary>
        /// 转换为两个三角形
        /// </summary>
        public (Triangle, Triangle) ToTriangles()
        {
            return (new Triangle(A, B, C), new Triangle(A, C, D));
        }
        
        /// <summary>
        /// 检查点是否在四边形内
        /// </summary>
        public bool Contains(Point2D point)
        {
            var (tri1, tri2) = ToTriangles();
            return tri1.Contains(point) || tri2.Contains(point);
        }
    }

    // MeshGenerationParameters类已在MeshGeneration.cs中定义

    /// <summary>
    /// 网格生成结果
    /// </summary>
    public class MeshGenerationResult
    {
        /// <summary>
        /// 三角形列表
        /// </summary>
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        
        /// <summary>
        /// 四边形列表
        /// </summary>
        public List<Quad> Quads { get; set; } = new List<Quad>();
        
        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<Point2D> Vertices { get; set; } = new List<Point2D>();
        
        /// <summary>
        /// 边列表
        /// </summary>
        public List<(int, int)> Edges { get; set; } = new List<(int, int)>();
        
        /// <summary>
        /// 生成是否成功
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 生成耗时（毫秒）
        /// </summary>
        public double GenerationTime { get; set; }
        
        /// <summary>
        /// 网格质量统计
        /// </summary>
        public Dictionary<string, object> QualityMetrics { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 总面积
        /// </summary>
        public double TotalArea
        {
            get
            {
                return Triangles.Sum(t => t.Area) + Quads.Sum(q => q.Area);
            }
        }
        
        /// <summary>
        /// 面片总数
        /// </summary>
        public int TotalFaceCount => Triangles.Count + Quads.Count;
    }

    /// <summary>
    /// 网格生成器
    /// </summary>
    public static class MeshGenerator
    {
        /// <summary>
        /// 为ROI形状生成网格
        /// </summary>
        public static MeshGenerationResult GenerateMesh(ROIShape shape, MeshGenerationParameters parameters = null)
        {
            if (shape == null)
            {
                return new MeshGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Shape cannot be null"
                };
            }

            parameters ??= new MeshGenerationParameters();
            var startTime = DateTime.Now;
            var result = new MeshGenerationResult();

            try
            {
                switch (parameters.MeshType)
                {
                    case MeshType.Triangle:
                        result = GenerateTriangleMesh(shape, parameters);
                        break;
                    case MeshType.Quad:
                        result = GenerateQuadMesh(shape, parameters);
                        break;
                    case MeshType.Mixed:
                        result = GenerateMixedMesh(shape, parameters);
                        break;
                }

                if (result.Success && parameters.OptimizeQuality)
                {
                    OptimizeMeshQuality(result, parameters);
                }

                CalculateQualityMetrics(result);
                result.GenerationTime = (DateTime.Now - startTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.GenerationTime = (DateTime.Now - startTime).TotalMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 生成三角网格
        /// </summary>
        private static MeshGenerationResult GenerateTriangleMesh(ROIShape shape, MeshGenerationParameters parameters)
        {
            var result = new MeshGenerationResult();
            
            // 获取边界点
            var boundaryPoints = GetBoundaryPoints(shape, parameters.BoundaryDensity);
            result.Vertices.AddRange(boundaryPoints);
            
            // 生成内部点
            if (parameters.GenerateInteriorPoints)
            {
                var interiorPoints = GenerateInteriorPoints(shape, parameters);
                result.Vertices.AddRange(interiorPoints);
            }
            
            // Delaunay三角剖分
            result.Triangles = DelaunayTriangulation(result.Vertices, shape);
            
            // 过滤不符合条件的三角形
            result.Triangles = FilterTriangles(result.Triangles, parameters);
            
            return result;
        }

        /// <summary>
        /// 生成四边形网格
        /// </summary>
        private static MeshGenerationResult GenerateQuadMesh(ROIShape shape, MeshGenerationParameters parameters)
        {
            var result = new MeshGenerationResult();
            
            // 先生成三角网格
            var triangleResult = GenerateTriangleMesh(shape, parameters);
            
            if (!triangleResult.Success)
            {
                return triangleResult;
            }
            
            // 将三角形合并为四边形
            result.Quads = CombineTrianglesToQuads(triangleResult.Triangles);
            result.Vertices = triangleResult.Vertices;
            
            return result;
        }

        /// <summary>
        /// 生成混合网格
        /// </summary>
        private static MeshGenerationResult GenerateMixedMesh(ROIShape shape, MeshGenerationParameters parameters)
        {
            var result = new MeshGenerationResult();
            
            // 先生成三角网格
            var triangleResult = GenerateTriangleMesh(shape, parameters);
            
            if (!triangleResult.Success)
            {
                return triangleResult;
            }
            
            // 部分三角形合并为四边形，保留其他三角形
            var (quads, remainingTriangles) = PartialCombineTrianglesToQuads(triangleResult.Triangles);
            
            result.Quads = quads;
            result.Triangles = remainingTriangles;
            result.Vertices = triangleResult.Vertices;
            
            return result;
        }

        /// <summary>
        /// 获取边界点
        /// </summary>
        private static List<Point2D> GetBoundaryPoints(ROIShape shape, double density)
        {
            var points = new List<Point2D>();
            
            switch (shape)
            {
                case PolygonROI polygon:
                    points.AddRange(GetPolygonBoundaryPoints(polygon, density));
                    break;
                case CircleROI circle:
                    points.AddRange(GetCircleBoundaryPoints(circle, density));
                    break;
                case RectangleROI rectangle:
                    points.AddRange(GetRectangleBoundaryPoints(rectangle, density));
                    break;
                case LineROI line when line.IsClosed:
                    points.AddRange(GetLineBoundaryPoints(line, density));
                    break;
                default:
                    // 转换为多边形后获取边界点
                    var bounds = shape.GetBounds();
                    points.AddRange(GetRectangleBoundaryPoints(
                        new RectangleROI(bounds.Center, bounds.Width, bounds.Height), density));
                    break;
            }
            
            return points;
        }

        /// <summary>
        /// 获取多边形边界点
        /// </summary>
        private static List<Point2D> GetPolygonBoundaryPoints(PolygonROI polygon, double density)
        {
            var points = new List<Point2D>();
            var vertices = polygon.Vertices;
            
            if (vertices.Count < 3) return points;
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var start = vertices[i];
                var end = vertices[(i + 1) % vertices.Count];
                
                points.Add(start);
                
                double distance = start.Distance(end);
                int segments = Math.Max(1, (int)Math.Ceiling(distance / density));
                
                for (int j = 1; j < segments; j++)
                {
                    double t = (double)j / segments;
                    var point = new Point2D(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                    points.Add(point);
                }
            }
            
            return points;
        }

        /// <summary>
        /// 获取圆形边界点
        /// </summary>
        private static List<Point2D> GetCircleBoundaryPoints(CircleROI circle, double density)
        {
            var points = new List<Point2D>();
            
            double circumference = 2 * Math.PI * circle.Radius;
            int segments = Math.Max(8, (int)Math.Ceiling(circumference / density));
            
            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = circle.Center.X + circle.Radius * Math.Cos(angle);
                double y = circle.Center.Y + circle.Radius * Math.Sin(angle);
                points.Add(new Point2D(x, y));
            }
            
            return points;
        }

        /// <summary>
        /// 获取矩形边界点
        /// </summary>
        private static List<Point2D> GetRectangleBoundaryPoints(RectangleROI rectangle, double density)
        {
            var points = new List<Point2D>();
            var corners = rectangle.GetCorners();
            
            for (int i = 0; i < corners.Count; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % corners.Count];
                
                points.Add(start);
                
                double distance = start.Distance(end);
                int segments = Math.Max(1, (int)Math.Ceiling(distance / density));
                
                for (int j = 1; j < segments; j++)
                {
                    double t = (double)j / segments;
                    var point = new Point2D(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                    points.Add(point);
                }
            }
            
            return points;
        }

        /// <summary>
        /// 获取线条边界点
        /// </summary>
        private static List<Point2D> GetLineBoundaryPoints(LineROI line, double density)
        {
            var points = new List<Point2D>();
            
            for (int i = 0; i < line.Points.Count - 1; i++)
            {
                var start = line.Points[i];
                var end = line.Points[i + 1];
                
                points.Add(start);
                
                double distance = start.Distance(end);
                int segments = Math.Max(1, (int)Math.Ceiling(distance / density));
                
                for (int j = 1; j < segments; j++)
                {
                    double t = (double)j / segments;
                    var point = new Point2D(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                    points.Add(point);
                }
            }
            
            if (line.IsClosed && line.Points.Count > 2)
            {
                points.Add(line.Points[line.Points.Count - 1]);
            }
            
            return points;
        }

        /// <summary>
        /// 生成内部点
        /// </summary>
        private static List<Point2D> GenerateInteriorPoints(ROIShape shape, MeshGenerationParameters parameters)
        {
            var pointParams = new PointGenerationParameters
            {
                Mode = PointGenerationMode.Density,
                Density = 1.0 / parameters.MaxTriangleArea,
                BorderOffset = parameters.BoundaryDensity / 2
            };
            
            var result = PointGenerator.GeneratePoints(shape, pointParams);
            return result.Success ? result.Points : new List<Point2D>();
        }

        /// <summary>
        /// Delaunay三角剖分（简化实现）
        /// </summary>
        private static List<Triangle> DelaunayTriangulation(List<Point2D> points, ROIShape shape)
        {
            var triangles = new List<Triangle>();
            
            if (points.Count < 3) return triangles;
            
            // 简化的Bowyer-Watson算法实现
            // 这里使用一个简单的扇形三角剖分作为示例
            
            // 找到凸包
            var hull = FindConvexHull(points);
            
            if (hull.Count < 3) return triangles;
            
            // 从凸包的第一个点开始，创建扇形三角形
            var center = hull[0];
            
            for (int i = 1; i < hull.Count - 1; i++)
            {
                var triangle = new Triangle(center, hull[i], hull[i + 1]);
                
                // 检查三角形是否在形状内
                if (shape.Contains(triangle.Centroid))
                {
                    triangles.Add(triangle);
                }
            }
            
            // 处理内部点
            var interiorPoints = points.Except(hull).ToList();
            
            foreach (var point in interiorPoints)
            {
                if (shape.Contains(point))
                {
                    // 找到包含该点的三角形并细分
                    for (int i = triangles.Count - 1; i >= 0; i--)
                    {
                        if (triangles[i].Contains(point))
                        {
                            var oldTriangle = triangles[i];
                            triangles.RemoveAt(i);
                            
                            // 创建三个新三角形
                            triangles.Add(new Triangle(point, oldTriangle.A, oldTriangle.B));
                            triangles.Add(new Triangle(point, oldTriangle.B, oldTriangle.C));
                            triangles.Add(new Triangle(point, oldTriangle.C, oldTriangle.A));
                            break;
                        }
                    }
                }
            }
            
            return triangles;
        }

        /// <summary>
        /// 寻找凸包
        /// </summary>
        private static List<Point2D> FindConvexHull(List<Point2D> points)
        {
            if (points.Count < 3) return points.ToList();
            
            // Graham扫描算法
            var start = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            
            var sorted = points.Where(p => p != start)
                              .OrderBy(p => Math.Atan2(p.Y - start.Y, p.X - start.X))
                              .ToList();
            
            var hull = new List<Point2D> { start };
            
            foreach (var point in sorted)
            {
                while (hull.Count > 1 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }
            
            return hull;
        }

        /// <summary>
        /// 计算叉积
        /// </summary>
        private static double CrossProduct(Point2D o, Point2D a, Point2D b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        /// <summary>
        /// 过滤三角形
        /// </summary>
        private static List<Triangle> FilterTriangles(List<Triangle> triangles, MeshGenerationParameters parameters)
        {
            return triangles.Where(t => 
                t.Area <= parameters.MaxTriangleArea && 
                t.IsWellShaped
            ).ToList();
        }

        /// <summary>
        /// 将三角形合并为四边形
        /// </summary>
        private static List<Quad> CombineTrianglesToQuads(List<Triangle> triangles)
        {
            var quads = new List<Quad>();
            var used = new HashSet<int>();
            
            for (int i = 0; i < triangles.Count; i++)
            {
                if (used.Contains(i)) continue;
                
                for (int j = i + 1; j < triangles.Count; j++)
                {
                    if (used.Contains(j)) continue;
                    
                    var quad = TryMergeTriangles(triangles[i], triangles[j]);
                    if (quad.HasValue)
                    {
                        quads.Add(quad.Value);
                        used.Add(i);
                        used.Add(j);
                        break;
                    }
                }
            }
            
            return quads;
        }

        /// <summary>
        /// 部分合并三角形为四边形
        /// </summary>
        private static (List<Quad>, List<Triangle>) PartialCombineTrianglesToQuads(List<Triangle> triangles)
        {
            var quads = new List<Quad>();
            var remainingTriangles = new List<Triangle>();
            var used = new HashSet<int>();
            
            // 尝试合并相邻的三角形
            for (int i = 0; i < triangles.Count; i++)
            {
                if (used.Contains(i)) continue;
                
                bool merged = false;
                for (int j = i + 1; j < triangles.Count; j++)
                {
                    if (used.Contains(j)) continue;
                    
                    var quad = TryMergeTriangles(triangles[i], triangles[j]);
                    if (quad.HasValue)
                    {
                        quads.Add(quad.Value);
                        used.Add(i);
                        used.Add(j);
                        merged = true;
                        break;
                    }
                }
                
                if (!merged)
                {
                    remainingTriangles.Add(triangles[i]);
                }
            }
            
            return (quads, remainingTriangles);
        }

        /// <summary>
        /// 尝试合并两个三角形为四边形
        /// </summary>
        private static Quad? TryMergeTriangles(Triangle tri1, Triangle tri2)
        {
            // 检查是否有共享边
            var points1 = new[] { tri1.A, tri1.B, tri1.C };
            var points2 = new[] { tri2.A, tri2.B, tri2.C };
            
            var sharedPoints = points1.Intersect(points2).ToList();
            
            if (sharedPoints.Count == 2)
            {
                // 找到非共享点
                var unique1 = points1.Except(sharedPoints).First();
                var unique2 = points2.Except(sharedPoints).First();
                
                // 构造四边形（需要正确的顶点顺序）
                var quad = new Quad(unique1, sharedPoints[0], unique2, sharedPoints[1]);
                return quad;
            }
            
            return null;
        }

        /// <summary>
        /// 优化网格质量
        /// </summary>
        private static void OptimizeMeshQuality(MeshGenerationResult result, MeshGenerationParameters parameters)
        {
            // 拉普拉斯平滑
            for (int iter = 0; iter < parameters.MaxIterations; iter++)
            {
                var newVertices = new List<Point2D>(result.Vertices);
                
                for (int i = 0; i < result.Vertices.Count; i++)
                {
                    var neighbors = GetVertexNeighbors(i, result);
                    if (neighbors.Count > 0)
                    {
                        var avgX = neighbors.Average(n => result.Vertices[n].X);
                        var avgY = neighbors.Average(n => result.Vertices[n].Y);
                        
                        var smoothed = new Point2D(
                            result.Vertices[i].X + parameters.SmoothingStrength * (avgX - result.Vertices[i].X),
                            result.Vertices[i].Y + parameters.SmoothingStrength * (avgY - result.Vertices[i].Y)
                        );
                        
                        newVertices[i] = smoothed;
                    }
                }
                
                result.Vertices = newVertices;
            }
        }

        /// <summary>
        /// 获取顶点的邻居
        /// </summary>
        private static List<int> GetVertexNeighbors(int vertexIndex, MeshGenerationResult result)
        {
            var neighbors = new HashSet<int>();
            
            // 从三角形中查找邻居
            foreach (var triangle in result.Triangles)
            {
                var vertices = new[] { triangle.A, triangle.B, triangle.C };
                var indices = vertices.Select(v => result.Vertices.IndexOf(v)).ToArray();
                
                int pos = Array.IndexOf(indices, vertexIndex);
                if (pos >= 0)
                {
                    neighbors.Add(indices[(pos + 1) % 3]);
                    neighbors.Add(indices[(pos + 2) % 3]);
                }
            }
            
            // 从四边形中查找邻居
            foreach (var quad in result.Quads)
            {
                var vertices = new[] { quad.A, quad.B, quad.C, quad.D };
                var indices = vertices.Select(v => result.Vertices.IndexOf(v)).ToArray();
                
                int pos = Array.IndexOf(indices, vertexIndex);
                if (pos >= 0)
                {
                    neighbors.Add(indices[(pos + 1) % 4]);
                    neighbors.Add(indices[(pos + 3) % 4]);
                }
            }
            
            return neighbors.ToList();
        }

        /// <summary>
        /// 计算网格质量指标
        /// </summary>
        private static void CalculateQualityMetrics(MeshGenerationResult result)
        {
            if (result.Triangles.Count > 0)
            {
                var areas = result.Triangles.Select(t => t.Area).ToList();
                var angles = result.Triangles.SelectMany(t => new[]
                {
                    t.GetAngle(t.A, t.B, t.C),
                    t.GetAngle(t.B, t.C, t.A),
                    t.GetAngle(t.C, t.A, t.B)
                }).ToList();
                
                result.QualityMetrics["MinTriangleArea"] = areas.Min();
                result.QualityMetrics["MaxTriangleArea"] = areas.Max();
                result.QualityMetrics["AvgTriangleArea"] = areas.Average();
                result.QualityMetrics["MinAngle"] = angles.Min();
                result.QualityMetrics["MaxAngle"] = angles.Max();
                result.QualityMetrics["AvgAngle"] = angles.Average();
            }
            
            if (result.Quads.Count > 0)
            {
                var quadAreas = result.Quads.Select(q => q.Area).ToList();
                result.QualityMetrics["MinQuadArea"] = quadAreas.Min();
                result.QualityMetrics["MaxQuadArea"] = quadAreas.Max();
                result.QualityMetrics["AvgQuadArea"] = quadAreas.Average();
            }
            
            result.QualityMetrics["TriangleCount"] = result.Triangles.Count;
            result.QualityMetrics["QuadCount"] = result.Quads.Count;
            result.QualityMetrics["VertexCount"] = result.Vertices.Count;
        }
    }
}
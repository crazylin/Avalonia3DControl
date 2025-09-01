using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    // PointGenerationParameters类已在PointGeneration.cs中定义

    /// <summary>
    /// 布点结果
    /// </summary>
    public class PointGenerationResult
    {
        /// <summary>
        /// 生成的点列表
        /// </summary>
        public List<Point2D> Points { get; set; } = new List<Point2D>();
        
        /// <summary>
        /// 实际生成的点数量
        /// </summary>
        public int ActualCount => Points.Count;
        
        /// <summary>
        /// 预期生成的点数量
        /// </summary>
        public int ExpectedCount { get; set; }
        
        /// <summary>
        /// 生成耗时（毫秒）
        /// </summary>
        public double GenerationTime { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 统计信息
        /// </summary>
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 智能布点生成器
    /// </summary>
    public static class PointGenerator
    {
        /// <summary>
        /// 在ROI形状内生成点
        /// </summary>
        public static PointGenerationResult GeneratePoints(ROIShape shape, PointGenerationParameters parameters)
        {
            if (shape == null || parameters == null)
            {
                return new PointGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Shape or parameters cannot be null"
                };
            }

            var startTime = DateTime.Now;
            var result = new PointGenerationResult();

            try
            {
                switch (parameters.Mode)
                {
                    case PointGenerationMode.Density:
                        result = GenerateByDensity(shape, parameters);
                        break;
                    case PointGenerationMode.RectangleGrid:
                        result = GenerateRectangleGrid(shape, parameters);
                        break;
                    case PointGenerationMode.CircularGrid:
                        result = GenerateCircularGrid(shape, parameters);
                        break;
                    case PointGenerationMode.Random:
                        result = GenerateRandom(shape, parameters);
                        break;
                    case PointGenerationMode.Hexagonal:
                        result = GenerateHexagonal(shape, parameters);
                        break;
                    default:
                        result.Success = false;
                        result.ErrorMessage = $"Unsupported generation mode: {parameters.Mode}";
                        break;
                }

                // 应用抖动
                if (result.Success && parameters.JitterStrength > 0)
                {
                    ApplyJitter(result.Points, parameters.JitterStrength, parameters.RandomSeed);
                }

                // 应用最小间距过滤
                if (result.Success && parameters.MinimumSpacing > 0)
                {
                    result.Points = FilterByMinimumSpacing(result.Points, parameters.MinimumSpacing);
                }

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
        /// 按密度生成点
        /// </summary>
        private static PointGenerationResult GenerateByDensity(ROIShape shape, PointGenerationParameters parameters)
        {
            var result = new PointGenerationResult();
            var bounds = shape.GetBounds();
            var area = shape.GetArea();
            
            if (area <= 0)
            {
                result.Success = false;
                result.ErrorMessage = "Shape area is zero or negative";
                return result;
            }

            int expectedCount = (int)Math.Ceiling(area * parameters.Density);
            result.ExpectedCount = expectedCount;

            // 计算网格间距
            double spacing = Math.Sqrt(area / expectedCount);
            int gridCols = (int)Math.Ceiling(bounds.Width / spacing);
            int gridRows = (int)Math.Ceiling(bounds.Height / spacing);

            var random = new Random(parameters.RandomSeed);

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    double x = bounds.X + (col + 0.5) * spacing;
                    double y = bounds.Y + (row + 0.5) * spacing;
                    
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        // 添加一些随机偏移
                        if (parameters.JitterStrength > 0)
                        {
                            double jitterX = (random.NextDouble() - 0.5) * spacing * parameters.JitterStrength;
                            double jitterY = (random.NextDouble() - 0.5) * spacing * parameters.JitterStrength;
                            point = new Point2D(x + jitterX, y + jitterY);
                            
                            // 确保抖动后的点仍在形状内
                            if (shape.Contains(point))
                            {
                                result.Points.Add(point);
                            }
                        }
                        else
                        {
                            result.Points.Add(point);
                        }
                    }
                }
            }

            result.Statistics["GridColumns"] = gridCols;
            result.Statistics["GridRows"] = gridRows;
            result.Statistics["Spacing"] = spacing;
            
            return result;
        }

        /// <summary>
        /// 生成矩形网格点
        /// </summary>
        private static PointGenerationResult GenerateRectangleGrid(ROIShape shape, PointGenerationParameters parameters)
        {
            var result = new PointGenerationResult();
            var bounds = shape.GetBounds();
            
            result.ExpectedCount = parameters.GridColumns * parameters.GridRows;

            double stepX = bounds.Width / (parameters.GridColumns + 1);
            double stepY = bounds.Height / (parameters.GridRows + 1);

            for (int row = 1; row <= parameters.GridRows; row++)
            {
                for (int col = 1; col <= parameters.GridColumns; col++)
                {
                    double x = bounds.X + col * stepX;
                    double y = bounds.Y + row * stepY;
                    
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        result.Points.Add(point);
                    }
                }
            }

            // 如果需要包含边界点
            if (parameters.IncludeBoundary)
            {
                AddBoundaryPoints(shape, result.Points, parameters);
            }

            result.Statistics["StepX"] = stepX;
            result.Statistics["StepY"] = stepY;
            
            return result;
        }

        /// <summary>
        /// 生成圆形网格点
        /// </summary>
        private static PointGenerationResult GenerateCircularGrid(ROIShape shape, PointGenerationParameters parameters)
        {
            var result = new PointGenerationResult();
            var bounds = shape.GetBounds();
            var center = bounds.Center;
            
            // 估算最大半径
            double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2;
            
            result.ExpectedCount = parameters.RadialLayers * parameters.AngularDivisions;

            // 中心点
            if (shape.Contains(center))
            {
                result.Points.Add(center);
            }

            double angleRange = parameters.EndAngle - parameters.StartAngle;
            
            for (int layer = 1; layer <= parameters.RadialLayers; layer++)
            {
                double radius = (double)layer / parameters.RadialLayers * maxRadius;
                int divisionsForLayer = (int)(parameters.AngularDivisions * Math.Pow(layer, 0.5)); // 外层更多点
                
                for (int i = 0; i < divisionsForLayer; i++)
                {
                    double angle = parameters.StartAngle + (double)i / divisionsForLayer * angleRange;
                    
                    double x = center.X + radius * Math.Cos(angle);
                    double y = center.Y + radius * Math.Sin(angle);
                    
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        result.Points.Add(point);
                    }
                }
            }

            result.Statistics["MaxRadius"] = maxRadius;
            result.Statistics["AngleRange"] = angleRange;
            
            return result;
        }

        /// <summary>
        /// 生成随机分布点
        /// </summary>
        private static PointGenerationResult GenerateRandom(ROIShape shape, PointGenerationParameters parameters)
        {
            var result = new PointGenerationResult();
            var bounds = shape.GetBounds();
            var random = new Random(parameters.RandomSeed);
            
            result.ExpectedCount = parameters.RandomCount;
            
            int attempts = 0;
            int maxAttempts = parameters.RandomCount * 10; // 防止无限循环
            
            while (result.Points.Count < parameters.RandomCount && attempts < maxAttempts)
            {
                double x = bounds.X + random.NextDouble() * bounds.Width;
                double y = bounds.Y + random.NextDouble() * bounds.Height;
                
                var point = new Point2D(x, y);
                
                if (shape.Contains(point))
                {
                    // 检查最小间距
                    if (parameters.MinimumSpacing <= 0 || 
                        !result.Points.Any(p => p.Distance(point) < parameters.MinimumSpacing))
                    {
                        result.Points.Add(point);
                    }
                }
                
                attempts++;
            }

            result.Statistics["Attempts"] = attempts;
            result.Statistics["SuccessRate"] = (double)result.Points.Count / attempts;
            
            return result;
        }

        /// <summary>
        /// 生成六边形网格点
        /// </summary>
        private static PointGenerationResult GenerateHexagonal(ROIShape shape, PointGenerationParameters parameters)
        {
            var result = new PointGenerationResult();
            var bounds = shape.GetBounds();
            
            // 六边形网格的间距
            double spacing = Math.Sqrt(bounds.Width * bounds.Height / (parameters.GridColumns * parameters.GridRows));
            double rowHeight = spacing * Math.Sqrt(3) / 2;
            
            int rows = (int)Math.Ceiling(bounds.Height / rowHeight);
            int cols = (int)Math.Ceiling(bounds.Width / spacing);
            
            result.ExpectedCount = rows * cols;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    double x = bounds.X + col * spacing;
                    double y = bounds.Y + row * rowHeight;
                    
                    // 奇数行偏移半个间距
                    if (row % 2 == 1)
                    {
                        x += spacing / 2;
                    }
                    
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        result.Points.Add(point);
                    }
                }
            }

            result.Statistics["Spacing"] = spacing;
            result.Statistics["RowHeight"] = rowHeight;
            result.Statistics["Rows"] = rows;
            result.Statistics["Columns"] = cols;
            
            return result;
        }

        /// <summary>
        /// 添加边界点
        /// </summary>
        private static void AddBoundaryPoints(ROIShape shape, List<Point2D> points, PointGenerationParameters parameters)
        {
            var controlPoints = shape.GetControlPoints();
            
            foreach (var point in controlPoints)
            {
                if (!points.Any(p => p.Distance(point) < 1e-6))
                {
                    points.Add(point);
                }
            }
            
            // 对于多边形和线条，添加边上的点
            if (shape is PolygonROI polygon)
            {
                AddPolygonBoundaryPoints(polygon, points, parameters);
            }
            else if (shape is LineROI line)
            {
                AddLineBoundaryPoints(line, points, parameters);
            }
        }

        /// <summary>
        /// 添加多边形边界点
        /// </summary>
        private static void AddPolygonBoundaryPoints(PolygonROI polygon, List<Point2D> points, PointGenerationParameters parameters)
        {
            var vertices = polygon.Vertices;
            if (vertices.Count < 2) return;
            
            double spacing = 10.0; // 边界点间距
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var start = vertices[i];
                var end = vertices[(i + 1) % vertices.Count];
                
                double distance = start.Distance(end);
                int segments = (int)Math.Ceiling(distance / spacing);
                
                for (int j = 1; j < segments; j++)
                {
                    double t = (double)j / segments;
                    var point = new Point2D(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                    
                    if (!points.Any(p => p.Distance(point) < 1e-6))
                    {
                        points.Add(point);
                    }
                }
            }
        }

        /// <summary>
        /// 添加线条边界点
        /// </summary>
        private static void AddLineBoundaryPoints(LineROI line, List<Point2D> points, PointGenerationParameters parameters)
        {
            if (line.Points.Count < 2) return;
            
            double spacing = 10.0; // 边界点间距
            
            for (int i = 0; i < line.Points.Count - 1; i++)
            {
                var start = line.Points[i];
                var end = line.Points[i + 1];
                
                double distance = start.Distance(end);
                int segments = (int)Math.Ceiling(distance / spacing);
                
                for (int j = 1; j < segments; j++)
                {
                    double t = (double)j / segments;
                    var point = new Point2D(
                        start.X + t * (end.X - start.X),
                        start.Y + t * (end.Y - start.Y)
                    );
                    
                    if (!points.Any(p => p.Distance(point) < 1e-6))
                    {
                        points.Add(point);
                    }
                }
            }
        }

        /// <summary>
        /// 应用抖动
        /// </summary>
        private static void ApplyJitter(List<Point2D> points, double jitterStrength, int seed)
        {
            var random = new Random(seed);
            
            for (int i = 0; i < points.Count; i++)
            {
                double jitterX = (random.NextDouble() - 0.5) * jitterStrength * 10;
                double jitterY = (random.NextDouble() - 0.5) * jitterStrength * 10;
                
                points[i] = new Point2D(
                    points[i].X + jitterX,
                    points[i].Y + jitterY
                );
            }
        }

        /// <summary>
        /// 按最小间距过滤点
        /// </summary>
        private static List<Point2D> FilterByMinimumSpacing(List<Point2D> points, double minimumSpacing)
        {
            var filtered = new List<Point2D>();
            
            foreach (var point in points)
            {
                bool tooClose = filtered.Any(p => p.Distance(point) < minimumSpacing);
                
                if (!tooClose)
                {
                    filtered.Add(point);
                }
            }
            
            return filtered;
        }

        /// <summary>
        /// 在多个ROI形状的并集内生成点
        /// </summary>
        public static PointGenerationResult GeneratePointsInUnion(List<ROIShape> shapes, PointGenerationParameters parameters)
        {
            if (shapes == null || shapes.Count == 0)
            {
                return new PointGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No shapes provided"
                };
            }

            var allPoints = new List<Point2D>();
            var totalExpected = 0;
            var startTime = DateTime.Now;

            foreach (var shape in shapes)
            {
                var shapeResult = GeneratePoints(shape, parameters);
                if (shapeResult.Success)
                {
                    allPoints.AddRange(shapeResult.Points);
                    totalExpected += shapeResult.ExpectedCount;
                }
            }

            // 去除重复点
            var uniquePoints = new List<Point2D>();
            foreach (var point in allPoints)
            {
                bool isDuplicate = uniquePoints.Any(p => p.Distance(point) < 1e-6);
                if (!isDuplicate)
                {
                    uniquePoints.Add(point);
                }
            }

            return new PointGenerationResult
            {
                Points = uniquePoints,
                ExpectedCount = totalExpected,
                GenerationTime = (DateTime.Now - startTime).TotalMilliseconds,
                Success = true,
                Statistics = new Dictionary<string, object>
                {
                    ["ShapeCount"] = shapes.Count,
                    ["TotalPointsBeforeDeduplication"] = allPoints.Count,
                    ["DuplicatesRemoved"] = allPoints.Count - uniquePoints.Count
                }
            };
        }

        /// <summary>
        /// 优化点分布（移除过密的点，补充稀疏区域）
        /// </summary>
        public static List<Point2D> OptimizePointDistribution(List<Point2D> points, ROIShape shape, double targetSpacing)
        {
            if (points == null || points.Count == 0 || shape == null)
                return new List<Point2D>();

            var optimized = new List<Point2D>(points);
            var bounds = shape.GetBounds();
            var random = new Random();

            // 第一步：移除过密的点
            for (int i = optimized.Count - 1; i >= 0; i--)
            {
                var currentPoint = optimized[i];
                int nearbyCount = 0;
                
                for (int j = 0; j < optimized.Count; j++)
                {
                    if (i != j && optimized[j].Distance(currentPoint) < targetSpacing)
                    {
                        nearbyCount++;
                    }
                }
                
                // 如果附近点太多，移除当前点
                if (nearbyCount > 3)
                {
                    optimized.RemoveAt(i);
                }
            }

            // 第二步：在稀疏区域补充点
            int gridSize = (int)Math.Ceiling(Math.Max(bounds.Width, bounds.Height) / targetSpacing);
            
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    double x = bounds.X + (i + 0.5) * bounds.Width / gridSize;
                    double y = bounds.Y + (j + 0.5) * bounds.Height / gridSize;
                    var candidate = new Point2D(x, y);
                    
                    if (shape.Contains(candidate))
                    {
                        // 检查是否在稀疏区域
                        bool isSparse = !optimized.Any(p => p.Distance(candidate) < targetSpacing);
                        
                        if (isSparse)
                        {
                            optimized.Add(candidate);
                        }
                    }
                }
            }

            return optimized;
        }
    }
}
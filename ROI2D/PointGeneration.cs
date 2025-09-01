using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 布点模式
    /// </summary>
    public enum PointGenerationMode
    {
        Grid,           // 网格布点
        RandomDensity,  // 随机密度填充
        CircularGrid,   // 圆形网格
        HexagonalGrid,  // 六边形网格
        ConcentricCircles, // 同心圆
        Spiral,         // 螺旋线
        Poisson,        // 泊松分布
        Density,        // 密度布点
        RectangleGrid,  // 矩形网格
        Random,         // 随机布点
        Hexagonal       // 六边形布点
    }

    /// <summary>
    /// 布点参数
    /// </summary>
    public class PointGenerationParameters
    {
        /// <summary>
        /// 布点模式
        /// </summary>
        public PointGenerationMode Mode { get; set; } = PointGenerationMode.Grid;
        
        /// <summary>
        /// 点间距
        /// </summary>
        public double Spacing { get; set; } = 10.0;
        
        /// <summary>
        /// 点密度（点/单位面积）
        /// </summary>
        public double Density { get; set; } = 0.1;
        
        /// <summary>
        /// 随机种子
        /// </summary>
        public int RandomSeed { get; set; } = 42;
        
        /// <summary>
        /// 最小距离（用于泊松分布）
        /// </summary>
        public double MinDistance { get; set; } = 5.0;
        
        /// <summary>
        /// 最大尝试次数
        /// </summary>
        public int MaxAttempts { get; set; } = 30;
        
        /// <summary>
        /// 边界偏移
        /// </summary>
        public double BorderOffset { get; set; } = 0.0;
        
        /// <summary>
        /// 是否包含边界点
        /// </summary>
        public bool IncludeBoundary { get; set; } = true;
        
        /// <summary>
        /// 角度偏移（用于旋转网格）
        /// </summary>
        public double AngleOffset { get; set; } = 0.0;
        
        /// <summary>
        /// 抖动强度（用于添加随机偏移）
        /// </summary>
        public double JitterStrength { get; set; } = 0.0;
        
        /// <summary>
        /// 最小间距（用于避免点过于密集）
        /// </summary>
        public double MinimumSpacing { get; set; } = 1.0;
        
        /// <summary>
        /// 网格行数
        /// </summary>
        public int GridRows { get; set; } = 10;
        
        /// <summary>
        /// 网格列数
        /// </summary>
        public int GridColumns { get; set; } = 10;
        
        /// <summary>
        /// 径向层数
        /// </summary>
        public int RadialLayers { get; set; } = 5;
        
        /// <summary>
        /// 角度分割数
        /// </summary>
        public int AngularDivisions { get; set; } = 8;
        public double StartAngle { get; set; } = 0.0;
        public double EndAngle { get; set; } = 2 * Math.PI;
        public int RandomCount { get; set; } = 100;
    }

    /// <summary>
    /// 智能布点系统
    /// </summary>
    public static class PointGeneration
    {
        /// <summary>
        /// 在ROI形状内生成点
        /// </summary>
        public static List<Point2D> GeneratePoints(ROIShape shape, PointGenerationParameters parameters)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            return parameters.Mode switch
            {
                PointGenerationMode.Grid => GenerateGridPoints(shape, parameters),
                PointGenerationMode.RandomDensity => GenerateRandomPoints(shape, parameters),
                PointGenerationMode.CircularGrid => GenerateCircularGridPoints(shape, parameters),
                PointGenerationMode.HexagonalGrid => GenerateHexagonalGridPoints(shape, parameters),
                PointGenerationMode.ConcentricCircles => GenerateConcentricCirclePoints(shape, parameters),
                PointGenerationMode.Spiral => GenerateSpiralPoints(shape, parameters),
                PointGenerationMode.Poisson => GeneratePoissonPoints(shape, parameters),
                _ => throw new ArgumentException($"不支持的布点模式: {parameters.Mode}")
            };
        }

        #region 网格布点

        /// <summary>
        /// 生成网格点
        /// </summary>
        private static List<Point2D> GenerateGridPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            
            // 应用边界偏移
            var minX = bounds.Left + parameters.BorderOffset;
            var maxX = bounds.Right - parameters.BorderOffset;
            var minY = bounds.Top + parameters.BorderOffset;
            var maxY = bounds.Bottom - parameters.BorderOffset;
            
            var spacing = parameters.Spacing;
            var angle = parameters.AngleOffset * Math.PI / 180;
            
            // 计算网格范围
            var cols = (int)Math.Ceiling((maxX - minX) / spacing) + 1;
            var rows = (int)Math.Ceiling((maxY - minY) / spacing) + 1;
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var x = minX + col * spacing;
                    var y = minY + row * spacing;
                    
                    // 应用旋转
                    if (angle != 0)
                    {
                        var center = new Point2D((minX + maxX) / 2, (minY + maxY) / 2);
                        var rotated = RotatePoint(new Point2D(x, y), center, angle);
                        x = rotated.X;
                        y = rotated.Y;
                    }
                    
                    var point = new Point2D(x, y);
                    
                    // 检查点是否在形状内
                    if (shape.Contains(point))
                    {
                        points.Add(point);
                    }
                }
            }
            
            return points;
        }

        #endregion

        #region 随机密度填充

        /// <summary>
        /// 生成随机密度点
        /// </summary>
        private static List<Point2D> GenerateRandomPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var random = new Random(parameters.RandomSeed);
            
            // 计算需要生成的点数
            var area = bounds.Width * bounds.Height;
            var targetCount = (int)(area * parameters.Density);
            
            var attempts = 0;
            var maxAttempts = targetCount * 10; // 防止无限循环
            
            while (points.Count < targetCount && attempts < maxAttempts)
            {
                var x = bounds.Left + random.NextDouble() * bounds.Width;
                var y = bounds.Top + random.NextDouble() * bounds.Height;
                var point = new Point2D(x, y);
                
                if (shape.Contains(point))
                {
                    points.Add(point);
                }
                
                attempts++;
            }
            
            return points;
        }

        #endregion

        #region 圆形网格

        /// <summary>
        /// 生成圆形网格点
        /// </summary>
        private static List<Point2D> GenerateCircularGridPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var center = new Point2D((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
            
            var maxRadius = Math.Max(bounds.Width, bounds.Height) / 2;
            var radialSpacing = parameters.Spacing;
            var angularSpacing = parameters.Spacing;
            
            // 添加中心点
            if (shape.Contains(center))
            {
                points.Add(center);
            }
            
            for (double radius = radialSpacing; radius <= maxRadius; radius += radialSpacing)
            {
                var circumference = 2 * Math.PI * radius;
                var pointCount = Math.Max(1, (int)(circumference / angularSpacing));
                var angleStep = 2 * Math.PI / pointCount;
                
                for (int i = 0; i < pointCount; i++)
                {
                    var angle = i * angleStep + parameters.AngleOffset * Math.PI / 180;
                    var x = center.X + radius * Math.Cos(angle);
                    var y = center.Y + radius * Math.Sin(angle);
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        points.Add(point);
                    }
                }
            }
            
            return points;
        }

        #endregion

        #region 六边形网格

        /// <summary>
        /// 生成六边形网格点
        /// </summary>
        private static List<Point2D> GenerateHexagonalGridPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var spacing = parameters.Spacing;
            
            // 六边形网格的行间距
            var rowSpacing = spacing * Math.Sqrt(3) / 2;
            var colSpacing = spacing;
            
            var minX = bounds.Left - spacing;
            var maxX = bounds.Right + spacing;
            var minY = bounds.Top - spacing;
            var maxY = bounds.Bottom + spacing;
            
            var rows = (int)Math.Ceiling((maxY - minY) / rowSpacing);
            var cols = (int)Math.Ceiling((maxX - minX) / colSpacing);
            
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var x = minX + col * colSpacing;
                    var y = minY + row * rowSpacing;
                    
                    // 奇数行偏移半个间距
                    if (row % 2 == 1)
                    {
                        x += colSpacing / 2;
                    }
                    
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        points.Add(point);
                    }
                }
            }
            
            return points;
        }

        #endregion

        #region 同心圆

        /// <summary>
        /// 生成同心圆点
        /// </summary>
        private static List<Point2D> GenerateConcentricCirclePoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var center = new Point2D((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
            
            var maxRadius = Math.Max(bounds.Width, bounds.Height) / 2;
            var radialSpacing = parameters.Spacing;
            
            // 添加中心点
            if (shape.Contains(center))
            {
                points.Add(center);
            }
            
            for (double radius = radialSpacing; radius <= maxRadius; radius += radialSpacing)
            {
                var circumference = 2 * Math.PI * radius;
                var pointCount = Math.Max(8, (int)(circumference / parameters.Spacing));
                var angleStep = 2 * Math.PI / pointCount;
                
                for (int i = 0; i < pointCount; i++)
                {
                    var angle = i * angleStep;
                    var x = center.X + radius * Math.Cos(angle);
                    var y = center.Y + radius * Math.Sin(angle);
                    var point = new Point2D(x, y);
                    
                    if (shape.Contains(point))
                    {
                        points.Add(point);
                    }
                }
            }
            
            return points;
        }

        #endregion

        #region 螺旋线

        /// <summary>
        /// 生成螺旋线点
        /// </summary>
        private static List<Point2D> GenerateSpiralPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var center = new Point2D((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
            
            var maxRadius = Math.Max(bounds.Width, bounds.Height) / 2;
            var spacing = parameters.Spacing;
            var angleStep = spacing / maxRadius; // 角度步长
            
            double angle = 0;
            double radius = 0;
            
            while (radius <= maxRadius)
            {
                var x = center.X + radius * Math.Cos(angle);
                var y = center.Y + radius * Math.Sin(angle);
                var point = new Point2D(x, y);
                
                if (shape.Contains(point))
                {
                    points.Add(point);
                }
                
                angle += angleStep;
                radius = angle * spacing / (2 * Math.PI);
            }
            
            return points;
        }

        #endregion

        #region 泊松分布

        /// <summary>
        /// 生成泊松分布点（Bridson算法）
        /// </summary>
        private static List<Point2D> GeneratePoissonPoints(ROIShape shape, PointGenerationParameters parameters)
        {
            var points = new List<Point2D>();
            var bounds = shape.GetBounds();
            var minDistance = parameters.MinDistance;
            var maxAttempts = parameters.MaxAttempts;
            var random = new Random(parameters.RandomSeed);
            
            // 网格大小
            var cellSize = minDistance / Math.Sqrt(2);
            var gridWidth = (int)Math.Ceiling(bounds.Width / cellSize);
            var gridHeight = (int)Math.Ceiling(bounds.Height / cellSize);
            
            // 网格存储点的索引
            var grid = new int[gridWidth, gridHeight];
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    grid[i, j] = -1;
                }
            }
            
            var activeList = new List<int>();
            
            // 生成第一个点
            var firstPoint = new Point2D(
                bounds.Left + random.NextDouble() * bounds.Width,
                bounds.Top + random.NextDouble() * bounds.Height
            );
            
            if (shape.Contains(firstPoint))
            {
                points.Add(firstPoint);
                var gridX = (int)((firstPoint.X - bounds.Left) / cellSize);
                var gridY = (int)((firstPoint.Y - bounds.Top) / cellSize);
                grid[gridX, gridY] = 0;
                activeList.Add(0);
            }
            
            while (activeList.Count > 0)
            {
                var randomIndex = random.Next(activeList.Count);
                var pointIndex = activeList[randomIndex];
                var point = points[pointIndex];
                
                bool found = false;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // 在环形区域内生成候选点
                    var angle = random.NextDouble() * 2 * Math.PI;
                    var radius = minDistance + random.NextDouble() * minDistance;
                    
                    var candidate = new Point2D(
                        point.X + radius * Math.Cos(angle),
                        point.Y + radius * Math.Sin(angle)
                    );
                    
                    // 检查候选点是否在边界内
                    if (candidate.X < bounds.Left || candidate.X >= bounds.Right ||
                        candidate.Y < bounds.Top || candidate.Y >= bounds.Bottom)
                        continue;
                    
                    // 检查候选点是否在形状内
                    if (!shape.Contains(candidate))
                        continue;
                    
                    // 检查与现有点的距离
                    var gridX = (int)((candidate.X - bounds.Left) / cellSize);
                    var gridY = (int)((candidate.Y - bounds.Top) / cellSize);
                    
                    bool tooClose = false;
                    
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            var checkX = gridX + dx;
                            var checkY = gridY + dy;
                            
                            if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                            {
                                var neighborIndex = grid[checkX, checkY];
                                if (neighborIndex >= 0)
                                {
                                    var neighbor = points[neighborIndex];
                                    if (candidate.DistanceTo(neighbor) < minDistance)
                                    {
                                        tooClose = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (tooClose) break;
                    }
                    
                    if (!tooClose)
                    {
                        points.Add(candidate);
                        grid[gridX, gridY] = points.Count - 1;
                        activeList.Add(points.Count - 1);
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    activeList.RemoveAt(randomIndex);
                }
            }
            
            return points;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 旋转点
        /// </summary>
        private static Point2D RotatePoint(Point2D point, Point2D center, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            
            return new Point2D(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos
            );
        }

        /// <summary>
        /// 创建点ROI列表
        /// </summary>
        public static List<PointROI> CreatePointROIs(List<Point2D> points, double radius = 2.0)
        {
            return points.Select(p => new PointROI
            {
                Center = p,
                Radius = radius,
                Name = $"Point_{points.IndexOf(p)}"
            }).ToList();
        }

        /// <summary>
        /// 批量添加生成的点到ROI图层
        /// </summary>
        public static void AddGeneratedPointsToLayer(ROI2DOverlay overlay, List<Point2D> points, double radius = 2.0)
        {
            if (overlay == null || points == null) return;
            
            var pointROIs = CreatePointROIs(points, radius);
            
            foreach (var pointROI in pointROIs)
            {
                overlay.AddShape(pointROI);
            }
        }

        #endregion
    }
}
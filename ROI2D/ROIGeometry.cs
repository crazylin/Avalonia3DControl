using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// ROI几何运算工具类
    /// </summary>
    public static class ROIGeometry
    {
        /// <summary>
        /// 执行两个ROI形状的几何运算
        /// </summary>
        public static List<ROIShape> PerformOperation(ROIShape shape1, ROIShape shape2, GeometryOperation operation)
        {
            if (shape1 == null || shape2 == null)
                return new List<ROIShape>();

            // 根据形状类型选择合适的算法
            if (shape1 is PolygonROI poly1 && shape2 is PolygonROI poly2)
            {
                return PerformPolygonOperation(poly1, poly2, operation);
            }
            else if (shape1 is CircleROI circle1 && shape2 is CircleROI circle2)
            {
                return PerformCircleOperation(circle1, circle2, operation);
            }
            else if (shape1 is RectangleROI rect1 && shape2 is RectangleROI rect2)
            {
                return PerformRectangleOperation(rect1, rect2, operation);
            }
            else
            {
                // 对于不同类型的形状，转换为多边形后进行运算
                var poly1Converted = ConvertToPolygon(shape1);
                var poly2Converted = ConvertToPolygon(shape2);
                return PerformPolygonOperation(poly1Converted, poly2Converted, operation);
            }
        }

        /// <summary>
        /// 多个ROI形状的批量几何运算
        /// </summary>
        public static List<ROIShape> PerformBatchOperation(List<ROIShape> shapes, GeometryOperation operation)
        {
            if (shapes == null || shapes.Count == 0)
                return new List<ROIShape>();

            if (shapes.Count == 1)
                return new List<ROIShape> { shapes[0].Clone() };

            var result = new List<ROIShape> { shapes[0].Clone() };

            for (int i = 1; i < shapes.Count; i++)
            {
                var newResult = new List<ROIShape>();
                foreach (var resultShape in result)
                {
                    var operationResult = PerformOperation(resultShape, shapes[i], operation);
                    newResult.AddRange(operationResult);
                }
                result = newResult;
            }

            return result;
        }

        /// <summary>
        /// 多边形几何运算
        /// </summary>
        private static List<ROIShape> PerformPolygonOperation(PolygonROI poly1, PolygonROI poly2, GeometryOperation operation)
        {
            var result = new List<ROIShape>();

            switch (operation)
            {
                case GeometryOperation.Union:
                    result.AddRange(PolygonUnion(poly1, poly2));
                    break;
                case GeometryOperation.Intersect:
                    result.AddRange(PolygonIntersection(poly1, poly2));
                    break;
                case GeometryOperation.Subtract:
                    result.AddRange(PolygonSubtraction(poly1, poly2));
                    break;
                case GeometryOperation.Clip:
                    result.AddRange(PolygonClip(poly1, poly2));
                    break;
            }

            return result;
        }

        /// <summary>
        /// 圆形几何运算
        /// </summary>
        private static List<ROIShape> PerformCircleOperation(CircleROI circle1, CircleROI circle2, GeometryOperation operation)
        {
            var result = new List<ROIShape>();
            double distance = circle1.Center.Distance(circle2.Center);

            switch (operation)
            {
                case GeometryOperation.Union:
                    if (distance >= circle1.Radius + circle2.Radius)
                    {
                        // 两圆不相交，返回两个圆
                        result.Add(circle1.Clone());
                        result.Add(circle2.Clone());
                    }
                    else if (distance <= Math.Abs(circle1.Radius - circle2.Radius))
                    {
                        // 一个圆包含另一个圆，返回较大的圆
                        result.Add(circle1.Radius >= circle2.Radius ? circle1.Clone() : circle2.Clone());
                    }
                    else
                    {
                        // 两圆相交，转换为多边形进行运算
                        var poly1 = CircleToPolygon(circle1);
                        var poly2 = CircleToPolygon(circle2);
                        result.AddRange(PolygonUnion(poly1, poly2));
                    }
                    break;

                case GeometryOperation.Intersect:
                    if (distance >= circle1.Radius + circle2.Radius)
                    {
                        // 两圆不相交，无交集
                    }
                    else if (distance <= Math.Abs(circle1.Radius - circle2.Radius))
                    {
                        // 一个圆包含另一个圆，返回较小的圆
                        result.Add(circle1.Radius <= circle2.Radius ? circle1.Clone() : circle2.Clone());
                    }
                    else
                    {
                        // 两圆相交，转换为多边形进行运算
                        var poly1 = CircleToPolygon(circle1);
                        var poly2 = CircleToPolygon(circle2);
                        result.AddRange(PolygonIntersection(poly1, poly2));
                    }
                    break;

                case GeometryOperation.Subtract:
                    if (distance >= circle1.Radius + circle2.Radius)
                    {
                        // 两圆不相交，返回第一个圆
                        result.Add(circle1.Clone());
                    }
                    else
                    {
                        // 转换为多边形进行运算
                        var poly1 = CircleToPolygon(circle1);
                        var poly2 = CircleToPolygon(circle2);
                        result.AddRange(PolygonSubtraction(poly1, poly2));
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 矩形几何运算
        /// </summary>
        private static List<ROIShape> PerformRectangleOperation(RectangleROI rect1, RectangleROI rect2, GeometryOperation operation)
        {
            // 如果矩形有旋转，转换为多边形进行运算
            if (Math.Abs(rect1.Angle) > 1e-10 || Math.Abs(rect2.Angle) > 1e-10)
            {
                var poly1 = RectangleToPolygon(rect1);
                var poly2 = RectangleToPolygon(rect2);
                return PerformPolygonOperation(poly1, poly2, operation);
            }

            // 轴对齐矩形的快速运算
            var result = new List<ROIShape>();
            var bounds1 = rect1.GetBounds();
            var bounds2 = rect2.GetBounds();

            switch (operation)
            {
                case GeometryOperation.Union:
                    result.AddRange(RectangleUnion(bounds1, bounds2));
                    break;
                case GeometryOperation.Intersect:
                    var intersection = RectangleIntersection(bounds1, bounds2);
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        result.Add(new RectangleROI(intersection.Center, intersection.Width, intersection.Height));
                    }
                    break;
                case GeometryOperation.Subtract:
                    var subtraction = RectangleSubtraction(bounds1, bounds2);
                    foreach (var rect in subtraction)
                    {
                        result.Add(new RectangleROI(rect.Center, rect.Width, rect.Height));
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 将形状转换为多边形
        /// </summary>
        private static PolygonROI ConvertToPolygon(ROIShape shape)
        {
            switch (shape)
            {
                case PolygonROI polygon:
                    return polygon;
                case CircleROI circle:
                    return CircleToPolygon(circle);
                case RectangleROI rectangle:
                    return RectangleToPolygon(rectangle);
                case LineROI line when line.IsClosed:
                    return new PolygonROI(line.Points);
                default:
                    throw new NotSupportedException($"Cannot convert {shape.GetType().Name} to polygon");
            }
        }

        /// <summary>
        /// 圆形转多边形
        /// </summary>
        private static PolygonROI CircleToPolygon(CircleROI circle, int segments = 32)
        {
            var vertices = new List<Point2D>();
            double angleStep = 2 * Math.PI / segments;

            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = circle.Center.X + circle.Radius * Math.Cos(angle);
                double y = circle.Center.Y + circle.Radius * Math.Sin(angle);
                vertices.Add(new Point2D(x, y));
            }

            return new PolygonROI(vertices)
            {
                Name = circle.Name,
                Color = circle.Color,
                LineWidth = circle.LineWidth,
                IsFilled = circle.IsFilled,
                FillColor = circle.FillColor,
                Opacity = circle.Opacity
            };
        }

        /// <summary>
        /// 矩形转多边形
        /// </summary>
        private static PolygonROI RectangleToPolygon(RectangleROI rectangle)
        {
            var corners = rectangle.GetCorners();
            return new PolygonROI(corners)
            {
                Name = rectangle.Name,
                Color = rectangle.Color,
                LineWidth = rectangle.LineWidth,
                IsFilled = rectangle.IsFilled,
                FillColor = rectangle.FillColor,
                Opacity = rectangle.Opacity
            };
        }

        #region 多边形运算算法

        /// <summary>
        /// 多边形并集运算（简化实现）
        /// </summary>
        private static List<PolygonROI> PolygonUnion(PolygonROI poly1, PolygonROI poly2)
        {
            // 这里使用简化的Sutherland-Hodgman算法
            // 实际项目中建议使用专业的几何库如Clipper
            var result = new List<PolygonROI>();

            if (!poly1.GetBounds().Intersects(poly2.GetBounds()))
            {
                // 边界不相交，直接返回两个多边形
                result.Add((PolygonROI)poly1.Clone());
                result.Add((PolygonROI)poly2.Clone());
            }
            else
            {
                // 简化实现：返回包围盒的并集
                var bounds1 = poly1.GetBounds();
                var bounds2 = poly2.GetBounds();
                var unionBounds = RectangleUnion(bounds1, bounds2).FirstOrDefault();
                if (unionBounds != null)
                {
                    result.Add(new PolygonROI(new[]
                    {
                        unionBounds.TopLeft,
                        unionBounds.TopRight,
                        unionBounds.BottomRight,
                        unionBounds.BottomLeft
                    }));
                }
            }

            return result;
        }

        /// <summary>
        /// 多边形交集运算（简化实现）
        /// </summary>
        private static List<PolygonROI> PolygonIntersection(PolygonROI poly1, PolygonROI poly2)
        {
            var result = new List<PolygonROI>();

            var bounds1 = poly1.GetBounds();
            var bounds2 = poly2.GetBounds();

            if (bounds1.Intersects(bounds2))
            {
                var intersection = RectangleIntersection(bounds1, bounds2);
                if (intersection.Width > 0 && intersection.Height > 0)
                {
                    result.Add(new PolygonROI(new[]
                    {
                        intersection.TopLeft,
                        intersection.TopRight,
                        intersection.BottomRight,
                        intersection.BottomLeft
                    }));
                }
            }

            return result;
        }

        /// <summary>
        /// 多边形差集运算（简化实现）
        /// </summary>
        private static List<PolygonROI> PolygonSubtraction(PolygonROI poly1, PolygonROI poly2)
        {
            var result = new List<PolygonROI>();

            var bounds1 = poly1.GetBounds();
            var bounds2 = poly2.GetBounds();

            if (!bounds1.Intersects(bounds2))
            {
                // 不相交，返回原多边形
                result.Add((PolygonROI)poly1.Clone());
            }
            else
            {
                // 简化实现：返回差集的近似结果
                var subtraction = RectangleSubtraction(bounds1, bounds2);
                foreach (var rect in subtraction)
                {
                    result.Add(new PolygonROI(new[]
                    {
                        rect.TopLeft,
                        rect.TopRight,
                        rect.BottomRight,
                        rect.BottomLeft
                    }));
                }
            }

            return result;
        }

        /// <summary>
        /// 多边形裁剪运算
        /// </summary>
        private static List<PolygonROI> PolygonClip(PolygonROI subject, PolygonROI clip)
        {
            // 使用Sutherland-Hodgman裁剪算法的简化版本
            return PolygonIntersection(subject, clip);
        }

        #endregion

        #region 矩形运算算法

        /// <summary>
        /// 矩形并集运算
        /// </summary>
        private static List<RectangleROI> RectangleUnion(Rectangle2D rect1, Rectangle2D rect2)
        {
            var result = new List<RectangleROI>();

            if (!rect1.Intersects(rect2))
            {
                // 不相交，返回两个矩形
                result.Add(new RectangleROI(rect1.Center, rect1.Width, rect1.Height));
                result.Add(new RectangleROI(rect2.Center, rect2.Width, rect2.Height));
            }
            else
            {
                // 计算包围矩形
                double minX = Math.Min(rect1.X, rect2.X);
                double minY = Math.Min(rect1.Y, rect2.Y);
                double maxX = Math.Max(rect1.X + rect1.Width, rect2.X + rect2.Width);
                double maxY = Math.Max(rect1.Y + rect1.Height, rect2.Y + rect2.Height);

                var union = new Rectangle2D(minX, minY, maxX - minX, maxY - minY);
                result.Add(new RectangleROI(union.Center, union.Width, union.Height));
            }

            return result;
        }

        /// <summary>
        /// 矩形交集运算
        /// </summary>
        private static Rectangle2D RectangleIntersection(Rectangle2D rect1, Rectangle2D rect2)
        {
            double left = Math.Max(rect1.X, rect2.X);
            double top = Math.Max(rect1.Y, rect2.Y);
            double right = Math.Min(rect1.X + rect1.Width, rect2.X + rect2.Width);
            double bottom = Math.Min(rect1.Y + rect1.Height, rect2.Y + rect2.Height);

            if (left < right && top < bottom)
            {
                return new Rectangle2D(left, top, right - left, bottom - top);
            }

            return new Rectangle2D(); // 空矩形
        }

        /// <summary>
        /// 矩形差集运算
        /// </summary>
        private static List<Rectangle2D> RectangleSubtraction(Rectangle2D rect1, Rectangle2D rect2)
        {
            var result = new List<Rectangle2D>();

            if (!rect1.Intersects(rect2))
            {
                result.Add(rect1);
                return result;
            }

            var intersection = RectangleIntersection(rect1, rect2);
            if (intersection.Width <= 0 || intersection.Height <= 0)
            {
                result.Add(rect1);
                return result;
            }

            // 计算差集的矩形片段
            double left = rect1.X;
            double top = rect1.Y;
            double right = rect1.X + rect1.Width;
            double bottom = rect1.Y + rect1.Height;

            double clipLeft = intersection.X;
            double clipTop = intersection.Y;
            double clipRight = intersection.X + intersection.Width;
            double clipBottom = intersection.Y + intersection.Height;

            // 左侧矩形
            if (left < clipLeft)
            {
                result.Add(new Rectangle2D(left, top, clipLeft - left, bottom - top));
            }

            // 右侧矩形
            if (right > clipRight)
            {
                result.Add(new Rectangle2D(clipRight, top, right - clipRight, bottom - top));
            }

            // 上侧矩形
            if (top < clipTop)
            {
                result.Add(new Rectangle2D(clipLeft, top, clipRight - clipLeft, clipTop - top));
            }

            // 下侧矩形
            if (bottom > clipBottom)
            {
                result.Add(new Rectangle2D(clipLeft, clipBottom, clipRight - clipLeft, bottom - clipBottom));
            }

            return result;
        }

        #endregion

        /// <summary>
        /// 计算两个形状的最小距离
        /// </summary>
        public static double CalculateDistance(ROIShape shape1, ROIShape shape2)
        {
            if (shape1 == null || shape2 == null)
                return double.MaxValue;

            // 简化实现：使用边界框中心点距离
            var bounds1 = shape1.GetBounds();
            var bounds2 = shape2.GetBounds();

            return bounds1.Center.Distance(bounds2.Center);
        }

        /// <summary>
        /// 检查点是否在多个形状的并集内
        /// </summary>
        public static bool IsPointInUnion(Point2D point, List<ROIShape> shapes)
        {
            return shapes?.Any(shape => shape.Contains(point)) ?? false;
        }

        /// <summary>
        /// 检查点是否在多个形状的交集内
        /// </summary>
        public static bool IsPointInIntersection(Point2D point, List<ROIShape> shapes)
        {
            return shapes?.All(shape => shape.Contains(point)) ?? false;
        }

        /// <summary>
        /// 计算形状的凸包
        /// </summary>
        public static PolygonROI CalculateConvexHull(List<Point2D> points)
        {
            if (points == null || points.Count < 3)
                return new PolygonROI();

            // Graham扫描算法计算凸包
            var hull = GrahamScan(points.ToList());
            return new PolygonROI(hull);
        }

        /// <summary>
        /// Graham扫描算法
        /// </summary>
        private static List<Point2D> GrahamScan(List<Point2D> points)
        {
            if (points.Count < 3) return points;

            // 找到最下方的点（y最小，如果相同则x最小）
            var start = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

            // 按极角排序
            var sorted = points.Where(p => p != start)
                              .OrderBy(p => Math.Atan2(p.Y - start.Y, p.X - start.X))
                              .ToList();

            var hull = new List<Point2D> { start };

            foreach (var point in sorted)
            {
                // 移除不在凸包上的点
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
    }
}
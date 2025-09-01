using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Avalonia3DControl.ROI2D
{
    /// <summary>
    /// 几何运算操作类型
    /// </summary>
    public enum GeometryOperation
    {
        Union,      // 并集
        Intersection, // 交集
        Intersect = Intersection, // 交集别名
        Difference,   // 差集
        Subtract = Difference,    // 差集别名
        XOR,          // 异或
        Clip          // 裁剪
    }

    /// <summary>
    /// 几何运算工具类
    /// </summary>
    public static class GeometryOperations
    {
        /// <summary>
        /// 执行两个ROI形状的几何运算
        /// </summary>
        public static ROIShape PerformOperation(ROIShape shape1, ROIShape shape2, GeometryOperation operation)
        {
            if (shape1 == null || shape2 == null)
                throw new ArgumentNullException("输入形状不能为空");

            // 根据形状类型选择合适的运算方法
            return operation switch
            {
                GeometryOperation.Union => Union(shape1, shape2),
                GeometryOperation.Intersection or GeometryOperation.Intersect => Intersection(shape1, shape2),
                GeometryOperation.Difference or GeometryOperation.Subtract => Difference(shape1, shape2),
                GeometryOperation.XOR => XOR(shape1, shape2),
                GeometryOperation.Clip => Clip(shape1, shape2),
                _ => throw new ArgumentException($"不支持的几何运算类型: {operation}")
            };
        }

        /// <summary>
        /// 计算两个形状的并集
        /// </summary>
        private static ROIShape Union(ROIShape shape1, ROIShape shape2)
        {
            // 对于简单形状，使用包围盒合并
            if (shape1 is RectangleROI rect1 && shape2 is RectangleROI rect2)
            {
                return UnionRectangles(rect1, rect2);
            }
            else if (shape1 is CircleROI circle1 && shape2 is CircleROI circle2)
            {
                return UnionCircles(circle1, circle2);
            }
            else if (shape1 is PolygonROI poly1 && shape2 is PolygonROI poly2)
            {
                return UnionPolygons(poly1, poly2);
            }

            // 对于不同类型的形状，转换为多边形后进行运算
            var polygon1 = ConvertToPolygon(shape1);
            var polygon2 = ConvertToPolygon(shape2);
            return UnionPolygons(polygon1, polygon2);
        }

        /// <summary>
        /// 计算两个形状的交集
        /// </summary>
        private static ROIShape Intersection(ROIShape shape1, ROIShape shape2)
        {
            if (shape1 is RectangleROI rect1 && shape2 is RectangleROI rect2)
            {
                return IntersectionRectangles(rect1, rect2);
            }
            else if (shape1 is CircleROI circle1 && shape2 is CircleROI circle2)
            {
                return IntersectionCircles(circle1, circle2);
            }

            // 对于复杂形状，转换为多边形后进行运算
            var polygon1 = ConvertToPolygon(shape1);
            var polygon2 = ConvertToPolygon(shape2);
            return IntersectionPolygons(polygon1, polygon2);
        }

        /// <summary>
        /// 计算两个形状的差集
        /// </summary>
        private static ROIShape Difference(ROIShape shape1, ROIShape shape2)
        {
            // 转换为多边形后进行差集运算
            var polygon1 = ConvertToPolygon(shape1);
            var polygon2 = ConvertToPolygon(shape2);
            return DifferencePolygons(polygon1, polygon2);
        }

        /// <summary>
        /// 计算两个形状的异或
        /// </summary>
        private static ROIShape XOR(ROIShape shape1, ROIShape shape2)
        {
            // XOR = (A ∪ B) - (A ∩ B)
            var union = Union(shape1, shape2);
            var intersection = Intersection(shape1, shape2);
            return Difference(union, intersection);
        }

        #region 矩形运算
        
        /// <summary>
        /// 矩形并集
        /// </summary>
        private static RectangleROI UnionRectangles(RectangleROI rect1, RectangleROI rect2)
        {
            var bounds1 = rect1.GetBounds();
            var bounds2 = rect2.GetBounds();

            double left = Math.Min(bounds1.Left, bounds2.Left);
            double top = Math.Min(bounds1.Top, bounds2.Top);
            double right = Math.Max(bounds1.Right, bounds2.Right);
            double bottom = Math.Max(bounds1.Bottom, bounds2.Bottom);

            return new RectangleROI
            {
                Center = new Point2D((left + right) / 2, (top + bottom) / 2),
                Width = right - left,
                Height = bottom - top,
                Angle = 0
            };
        }

        /// <summary>
        /// 矩形交集
        /// </summary>
        private static RectangleROI IntersectionRectangles(RectangleROI rect1, RectangleROI rect2)
        {
            var bounds1 = rect1.GetBounds();
            var bounds2 = rect2.GetBounds();

            double left = Math.Max(bounds1.Left, bounds2.Left);
            double top = Math.Max(bounds1.Top, bounds2.Top);
            double right = Math.Min(bounds1.Right, bounds2.Right);
            double bottom = Math.Min(bounds1.Bottom, bounds2.Bottom);

            if (left >= right || top >= bottom)
                return null; // 无交集

            return new RectangleROI
            {
                Center = new Point2D((left + right) / 2, (top + bottom) / 2),
                Width = right - left,
                Height = bottom - top,
                Angle = 0
            };
        }

        #endregion

        #region 圆形运算

        /// <summary>
        /// 圆形并集（简化为包围圆）
        /// </summary>
        private static CircleROI UnionCircles(CircleROI circle1, CircleROI circle2)
        {
            var center1 = circle1.Center;
            var center2 = circle2.Center;
            var radius1 = circle1.Radius;
            var radius2 = circle2.Radius;

            double distance = center1.DistanceTo(center2);

            // 如果一个圆包含另一个圆
            if (distance + radius2 <= radius1)
                return circle1.Clone() as CircleROI;
            if (distance + radius1 <= radius2)
                return circle2.Clone() as CircleROI;

            // 计算包围圆
            double newRadius = (distance + radius1 + radius2) / 2;
            double t = (newRadius - radius1) / distance;
            var newCenter = new Point2D(
                center1.X + t * (center2.X - center1.X),
                center1.Y + t * (center2.Y - center1.Y)
            );

            return new CircleROI
            {
                Center = newCenter,
                Radius = newRadius
            };
        }

        /// <summary>
        /// 圆形交集
        /// </summary>
        private static ROIShape IntersectionCircles(CircleROI circle1, CircleROI circle2)
        {
            var center1 = circle1.Center;
            var center2 = circle2.Center;
            var radius1 = circle1.Radius;
            var radius2 = circle2.Radius;

            double distance = center1.DistanceTo(center2);

            // 无交集
            if (distance > radius1 + radius2)
                return null;

            // 一个圆包含另一个圆
            if (distance + radius2 <= radius1)
                return circle2.Clone();
            if (distance + radius1 <= radius2)
                return circle1.Clone();

            // 有交集但不完全包含，返回交集区域的近似多边形
            return CreateCircleIntersectionPolygon(circle1, circle2);
        }

        #endregion

        #region 多边形运算

        /// <summary>
        /// 多边形并集（简化实现）
        /// </summary>
        private static PolygonROI UnionPolygons(PolygonROI poly1, PolygonROI poly2)
        {
            // 简化实现：计算凸包
            var allVertices = new List<Point2D>();
            allVertices.AddRange(poly1.Vertices);
            allVertices.AddRange(poly2.Vertices);

            var convexHull = ComputeConvexHull(allVertices);
            return new PolygonROI { Vertices = convexHull };
        }

        /// <summary>
        /// 多边形交集（简化实现）
        /// </summary>
        private static PolygonROI IntersectionPolygons(PolygonROI poly1, PolygonROI poly2)
        {
            // 使用Sutherland-Hodgman裁剪算法的简化版本
            var result = ClipPolygon(poly1.Vertices, poly2.Vertices);
            return result.Count >= 3 ? new PolygonROI { Vertices = result } : null;
        }

        /// <summary>
        /// 多边形差集（简化实现）
        /// </summary>
        private static PolygonROI DifferencePolygons(PolygonROI poly1, PolygonROI poly2)
        {
            // 简化实现：返回第一个多边形（实际应该实现复杂的布尔运算）
            return poly1.Clone() as PolygonROI;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 将形状转换为多边形
        /// </summary>
        private static PolygonROI ConvertToPolygon(ROIShape shape)
        {
            return shape switch
            {
                PolygonROI polygon => polygon,
                RectangleROI rectangle => RectangleToPolygon(rectangle),
                CircleROI circle => CircleToPolygon(circle, 32),
                PointROI point => PointToPolygon(point),
                LineROI line => LineToPolygon(line),
                _ => throw new ArgumentException($"不支持的形状类型: {shape.GetType()}")
            };
        }

        /// <summary>
        /// 矩形转多边形
        /// </summary>
        private static PolygonROI RectangleToPolygon(RectangleROI rectangle)
        {
            var center = rectangle.Center;
            var halfWidth = rectangle.Width / 2;
            var halfHeight = rectangle.Height / 2;
            var angle = rectangle.Angle * Math.PI / 180;

            var vertices = new List<Point2D>
            {
                RotatePoint(new Point2D(center.X - halfWidth, center.Y - halfHeight), center, angle),
                RotatePoint(new Point2D(center.X + halfWidth, center.Y - halfHeight), center, angle),
                RotatePoint(new Point2D(center.X + halfWidth, center.Y + halfHeight), center, angle),
                RotatePoint(new Point2D(center.X - halfWidth, center.Y + halfHeight), center, angle)
            };

            return new PolygonROI { Vertices = vertices };
        }

        /// <summary>
        /// 圆形转多边形
        /// </summary>
        private static PolygonROI CircleToPolygon(CircleROI circle, int segments = 32)
        {
            var vertices = new List<Point2D>();
            var center = circle.Center;
            var radius = circle.Radius;

            for (int i = 0; i < segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                vertices.Add(new Point2D(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle)
                ));
            }

            return new PolygonROI { Vertices = vertices };
        }

        /// <summary>
        /// 点转多边形（小正方形）
        /// </summary>
        private static PolygonROI PointToPolygon(PointROI point)
        {
            var center = point.Center;
            var size = point.Radius;

            var vertices = new List<Point2D>
            {
                new Point2D(center.X - size, center.Y - size),
                new Point2D(center.X + size, center.Y - size),
                new Point2D(center.X + size, center.Y + size),
                new Point2D(center.X - size, center.Y + size)
            };

            return new PolygonROI { Vertices = vertices };
        }

        /// <summary>
        /// 线转多边形（带宽度的矩形）
        /// </summary>
        private static PolygonROI LineToPolygon(LineROI line)
        {
            if (line.Points.Count < 2)
                return new PolygonROI { Vertices = new List<Point2D>() };

            // 简化：只处理两点线段
            var p1 = line.Points[0];
            var p2 = line.Points[1];
            var width = 2.0; // 默认线宽

            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length == 0)
                return PointToPolygon(new PointROI { Center = p1, Radius = width / 2 });

            var nx = -dy / length * width / 2;
            var ny = dx / length * width / 2;

            var vertices = new List<Point2D>
            {
                new Point2D(p1.X + nx, p1.Y + ny),
                new Point2D(p2.X + nx, p2.Y + ny),
                new Point2D(p2.X - nx, p2.Y - ny),
                new Point2D(p1.X - nx, p1.Y - ny)
            };

            return new PolygonROI { Vertices = vertices };
        }

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
        /// 计算凸包（Graham扫描算法）
        /// </summary>
        private static List<Point2D> ComputeConvexHull(List<Point2D> points)
        {
            if (points.Count < 3)
                return points.ToList();

            // 找到最下方的点（y最小，如果相同则x最小）
            var start = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();

            // 按极角排序
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
        private static double CrossProduct(Point2D a, Point2D b, Point2D c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }

        /// <summary>
        /// 多边形裁剪（Sutherland-Hodgman算法简化版）
        /// </summary>
        private static List<Point2D> ClipPolygon(List<Point2D> subject, List<Point2D> clip)
        {
            var result = subject.ToList();

            for (int i = 0; i < clip.Count; i++)
            {
                if (result.Count == 0) break;

                var clipVertex1 = clip[i];
                var clipVertex2 = clip[(i + 1) % clip.Count];

                var input = result.ToList();
                result.Clear();

                if (input.Count == 0) continue;

                var s = input[input.Count - 1];

                foreach (var e in input)
                {
                    if (IsInside(e, clipVertex1, clipVertex2))
                    {
                        if (!IsInside(s, clipVertex1, clipVertex2))
                        {
                            var intersection = GetIntersection(s, e, clipVertex1, clipVertex2);
                            if (intersection.HasValue)
                                result.Add(intersection.Value);
                        }
                        result.Add(e);
                    }
                    else if (IsInside(s, clipVertex1, clipVertex2))
                    {
                        var intersection = GetIntersection(s, e, clipVertex1, clipVertex2);
                        if (intersection.HasValue)
                            result.Add(intersection.Value);
                    }
                    s = e;
                }
            }

            return result;
        }

        /// <summary>
        /// 判断点是否在线段内侧
        /// </summary>
        private static bool IsInside(Point2D point, Point2D lineStart, Point2D lineEnd)
        {
            return CrossProduct(lineStart, lineEnd, point) >= 0;
        }

        /// <summary>
        /// 计算两线段交点
        /// </summary>
        private static Point2D? GetIntersection(Point2D p1, Point2D p2, Point2D p3, Point2D p4)
        {
            var denom = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(denom) < 1e-10) return null;

            var t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / denom;

            return new Point2D(
                p1.X + t * (p2.X - p1.X),
                p1.Y + t * (p2.Y - p1.Y)
            );
        }

        /// <summary>
        /// 创建圆形交集的近似多边形
        /// </summary>
        private static PolygonROI CreateCircleIntersectionPolygon(CircleROI circle1, CircleROI circle2)
        {
            // 简化实现：返回较小圆的多边形近似
            var smallerCircle = circle1.Radius <= circle2.Radius ? circle1 : circle2;
            return CircleToPolygon(smallerCircle, 16);
        }

        /// <summary>
        /// 裁剪操作（简化实现）
        /// </summary>
        private static ROIShape Clip(ROIShape shape1, ROIShape shape2)
        {
            // 简化实现：返回两个形状的交集
            return Intersection(shape1, shape2);
        }

        #endregion
    }
}
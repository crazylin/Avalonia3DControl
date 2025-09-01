# 2D ROI布点功能开发文档

## 1. 项目概述

### 1.1 功能目标
在现有的Avalonia 3D控件基础上，集成2D ROI（Region of Interest）布点功能，支持在3D场景的2D投影平面上进行交互式ROI绘制、编辑和智能布点操作。

### 1.2 核心特性
- **多种ROI类型**：点、线、矩形、圆形、多边形
- **几何运算**：并集、交集、差集、裁剪操作
- **智能布点**：密度填充、矩形网格、圆形填充
- **面片生成**：三角面、四边形面生成
- **坐标映射**：3D世界坐标与2D屏幕坐标双向转换
- **视频背景**：支持视频帧或静态图片作为背景
- **交互操作**：选择、移动、缩放、旋转、复制粘贴
- **历史管理**：完整的撤销/重做功能

## 2. 系统架构设计

### 2.1 整体架构
```
┌─────────────────────────────────────────────────────────────┐
│                    OpenGL3DControl                         │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   ROI2DOverlay  │  │ CoordinateMapper│  │VideoTexture │ │
│  │                 │  │                 │  │Manager      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │  ROI2DRenderer  │  │   InputTool     │  │HistoryMgr   │ │
│  │                 │  │   System        │  │             │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Shape Layer   │  │ Point Generator │  │ Mesh        │ │
│  │   Management    │  │                 │  │ Generator   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心组件

#### 2.2.1 ROI2DOverlay
- **职责**：2D ROI交互层的主控制器
- **功能**：管理ROI绘制、编辑、选择状态
- **接口**：与OpenGL3DControl集成的主要入口

#### 2.2.2 CoordinateMapper
- **职责**：坐标系统转换
- **功能**：3D世界坐标↔2D屏幕坐标双向映射
- **算法**：基于投影矩阵和视图矩阵的数学转换

#### 2.2.3 ROI2DRenderer
- **职责**：ROI图形的OpenGL渲染
- **功能**：高性能的2D图形渲染，支持抗锯齿
- **优化**：批量渲染、顶点缓冲优化

#### 2.2.4 VideoTextureManager
- **职责**：视频纹理管理
- **功能**：视频帧解码、纹理上传、内存管理
- **格式**：支持常见视频格式和静态图片

## 3. 功能模块分解

### 3.1 ROI绘制模块

#### 3.1.1 基础图形绘制
- **点绘制**：支持可调节大小的圆点
- **线绘制**：支持直线和折线
- **矩形绘制**：支持轴对齐和旋转矩形
- **圆形绘制**：支持圆和椭圆
- **多边形绘制**：支持任意多边形和贝塞尔曲线

#### 3.1.2 交互功能
- **实时预览**：绘制过程中的实时反馈
- **控制点显示**：选中图形的控制点可视化
- **吸附功能**：网格吸附、对象吸附
- **约束绘制**：按住Shift键的约束模式

### 3.2 几何运算模块

#### 3.2.1 布尔运算
- **并集运算**：多个ROI区域的合并
- **交集运算**：多个ROI区域的重叠部分
- **差集运算**：从一个ROI中减去另一个ROI
- **异或运算**：两个ROI的非重叠部分

#### 3.2.2 几何变换
- **平移变换**：ROI区域的位置移动
- **缩放变换**：ROI区域的大小调整
- **旋转变换**：ROI区域的角度旋转
- **镜像变换**：水平/垂直镜像

### 3.3 智能布点模块

#### 3.3.1 密度填充算法
```csharp
public class DensityFillAlgorithm
{
    public List<Point2D> GeneratePoints(ROIShape roi, double density)
    {
        // 基于泊松圆盘采样的均匀分布算法
        // 确保点之间的最小距离
    }
}
```

#### 3.3.2 矩形网格填充
```csharp
public class GridFillAlgorithm
{
    public List<Point2D> GenerateGrid(ROIShape roi, int rows, int cols)
    {
        // 在ROI区域内生成规则网格点
        // 支持边界裁剪
    }
}
```

#### 3.3.3 圆形填充算法
```csharp
public class CircularFillAlgorithm
{
    public List<Point2D> GenerateCircularPoints(ROIShape roi, int count, double angleStep)
    {
        // 基于极坐标的圆形分布算法
        // 支持螺旋和同心圆模式
    }
}
```

### 3.4 面片生成模块

#### 3.4.1 三角面生成
- **Delaunay三角剖分**：基于点云生成三角网格
- **约束三角剖分**：考虑边界约束的三角化
- **质量优化**：最小角度最大化、长宽比优化

#### 3.4.2 四边形面生成
- **四边形网格**：规则四边形网格生成
- **自适应细分**：基于曲率的自适应网格
- **边界对齐**：与ROI边界对齐的网格

## 4. 数据结构设计

### 4.1 核心数据类型

```csharp
// 2D点结构
public struct Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
    public Point2D(double x, double y) { X = x; Y = y; }
}

// 3D点结构
public struct Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
}

// ROI形状基类
public abstract class ROIShape
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsSelected { get; set; }
    public bool IsVisible { get; set; }
    public Color Color { get; set; }
    public double LineWidth { get; set; }
    
    public abstract bool Contains(Point2D point);
    public abstract Rectangle2D GetBounds();
    public abstract void Transform(Matrix3x3 matrix);
    public abstract ROIShape Clone();
}

// 点ROI
public class PointROI : ROIShape
{
    public Point2D Position { get; set; }
    public double Radius { get; set; }
}

// 线ROI
public class LineROI : ROIShape
{
    public List<Point2D> Points { get; set; }
    public bool IsClosed { get; set; }
}

// 矩形ROI
public class RectangleROI : ROIShape
{
    public Point2D Center { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
}

// 圆形ROI
public class CircleROI : ROIShape
{
    public Point2D Center { get; set; }
    public double Radius { get; set; }
}

// 多边形ROI
public class PolygonROI : ROIShape
{
    public List<Point2D> Vertices { get; set; }
}
```

### 4.2 渲染数据结构

```csharp
// 顶点数据
public struct Vertex2D
{
    public Vector2 Position;
    public Vector4 Color;
    public Vector2 TexCoord;
}

// 渲染批次
public class RenderBatch
{
    public List<Vertex2D> Vertices { get; set; }
    public List<uint> Indices { get; set; }
    public PrimitiveType PrimitiveType { get; set; }
    public uint TextureId { get; set; }
}
```

## 5. 实现计划

### 5.1 第一阶段：核心框架（1-2周）
1. **创建ROI2DOverlay类**
   - 集成到OpenGL3DControl
   - 基础事件处理框架
   - 坐标转换接口

2. **实现CoordinateMapper**
   - 3D到2D投影算法
   - 2D到3D反投影算法
   - 视口变换处理

3. **基础ROI2DRenderer**
   - OpenGL渲染管线
   - 基础图形绘制
   - 着色器程序

### 5.2 第二阶段：ROI绘制（2-3周）
1. **ROI形状类实现**
   - 所有ROI形状类
   - 几何算法实现
   - 碰撞检测

2. **交互工具集成**
   - 整合SY.Modules.Figure的InputTool
   - 适配OpenGL控件
   - 事件路由机制

3. **渲染优化**
   - 批量渲染
   - 顶点缓冲优化
   - 抗锯齿处理

### 5.3 第三阶段：高级功能（2-3周）
1. **几何运算**
   - 布尔运算算法
   - 几何变换
   - 运算结果优化

2. **智能布点**
   - 三种填充算法
   - 参数配置界面
   - 性能优化

3. **面片生成**
   - 三角剖分算法
   - 四边形网格生成
   - 网格质量控制

### 5.4 第四阶段：集成测试（1-2周）
1. **视频纹理支持**
   - 视频解码集成
   - 纹理管理
   - 性能优化

2. **完整功能测试**
   - 单元测试
   - 集成测试
   - 性能测试

3. **演示应用**
   - 完整功能演示
   - 用户界面优化
   - 文档完善

## 6. 技术难点与解决方案

### 6.1 坐标系统转换
**难点**：3D世界坐标与2D屏幕坐标的精确转换
**解决方案**：
- 使用OpenGL的投影矩阵和模型视图矩阵
- 实现高精度的数值计算
- 处理边界情况和数值稳定性

### 6.2 实时渲染性能
**难点**：大量ROI对象的实时渲染性能
**解决方案**：
- 实现渲染批次合并
- 使用顶点缓冲对象(VBO)
- 实现视锥体裁剪
- 层次细节(LOD)技术

### 6.3 复杂几何运算
**难点**：多边形的布尔运算和三角剖分
**解决方案**：
- 集成成熟的几何算法库
- 实现数值稳定的算法
- 处理退化情况

### 6.4 内存管理
**难点**：大量图形数据的内存管理
**解决方案**：
- 实现对象池模式
- 智能缓存策略
- 及时释放GPU资源

## 7. 测试策略

### 7.1 单元测试
- 坐标转换算法测试
- 几何运算正确性测试
- 布点算法质量测试

### 7.2 性能测试
- 渲染性能基准测试
- 内存使用监控
- 大数据量压力测试

### 7.3 集成测试
- 与3D控件的集成测试
- 多平台兼容性测试
- 用户交互流程测试

## 8. 部署与维护

### 8.1 版本管理
- 语义化版本控制
- 向后兼容性保证
- 渐进式功能发布

### 8.2 文档维护
- API文档自动生成
- 用户手册更新
- 示例代码维护

### 8.3 性能监控
- 运行时性能监控
- 错误日志收集
- 用户反馈收集

---

**文档版本**：v1.0  
**创建日期**：2024年1月  
**最后更新**：2024年1月  
**负责人**：开发团队
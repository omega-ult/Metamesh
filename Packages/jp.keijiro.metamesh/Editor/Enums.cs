namespace Metamesh
{
    public enum Shape
    {
        Plane,
        Box,
        Sphere,
        Icosphere,
        Cylinder,
        RoundedBox,
        Ring,
        Disc,
        Teapot,
        Triangle,
        Torus,
        Cone,
        Capsule,
    }

    public enum Axis
    {
        X,
        Y,
        Z
    }

// 简化的轴向锚点位置枚举
    public enum AxisPivotPosition
    {
        Min, // 最小值
        Center, // 中心点
        Max // 最大值
    }
} // namespace Metamesh
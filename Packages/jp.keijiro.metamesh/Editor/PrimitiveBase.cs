using UnityEngine;

namespace Metamesh
{
    [System.Serializable]
    public abstract class PrimitiveBase
    {
        // 锚点设置
        public AxisPivotPosition PivotX { get; set; } = AxisPivotPosition.Center;
        public AxisPivotPosition PivotY { get; set; } = AxisPivotPosition.Center;
        public AxisPivotPosition PivotZ { get; set; } = AxisPivotPosition.Center;
        public bool NormalizeSize { get; set; } = false;

        // 抽象方法，由子类实现具体的网格生成逻辑
        protected abstract void GenerateMesh(Mesh mesh);

        // 公共方法，生成网格并应用锚点调整
        public void Generate(Mesh mesh)
        {
            // 调用子类实现的网格生成方法
            GenerateMesh(mesh);
            
            // 重新计算边界
            mesh.RecalculateBounds();
            
            // 应用锚点调整
            AdjustPivot(mesh);
        }

        // 调整网格的锚点位置
        protected void AdjustPivot(Mesh mesh)
        {
            // 如果所有轴都是中心点，不需要调整
            if (PivotX == AxisPivotPosition.Center && 
                PivotY == AxisPivotPosition.Center && 
                PivotZ == AxisPivotPosition.Center) return;
            
            Vector3 offset = Vector3.zero;
            Bounds bounds = mesh.bounds;
            
            // 计算X轴偏移
            switch (PivotX)
            {
                case AxisPivotPosition.Min:
                    offset.x = bounds.min.x;
                    break;
                case AxisPivotPosition.Max:
                    offset.x = bounds.max.x;
                    break;
                // Center不需要偏移
            }
            
            // 计算Y轴偏移
            switch (PivotY)
            {
                case AxisPivotPosition.Min:
                    offset.y = bounds.min.y;
                    break;
                case AxisPivotPosition.Max:
                    offset.y = bounds.max.y;
                    break;
                // Center不需要偏移
            }
            
            // 计算Z轴偏移
            switch (PivotZ)
            {
                case AxisPivotPosition.Min:
                    offset.z = bounds.min.z;
                    break;
                case AxisPivotPosition.Max:
                    offset.z = bounds.max.z;
                    break;
                // Center不需要偏移
            }
            
            // 应用偏移到所有顶点
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= offset;
            }
            mesh.vertices = vertices;
            
            // 更新网格边界
            mesh.RecalculateBounds();
            
            // 如果需要标准化大小
            if (NormalizeSize)
            {
                NormalizeMeshSize(mesh);
            }
        }
        
        // 将网格缩放到标准大小
        protected void NormalizeMeshSize(Mesh mesh)
        {
            Bounds bounds = mesh.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            
            if (maxDimension <= 0) return;
            
            float scale = 1.0f / maxDimension;
            
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= scale;
            }
            mesh.vertices = vertices;
            
            // 更新网格边界
            mesh.RecalculateBounds();
        }
    }
}
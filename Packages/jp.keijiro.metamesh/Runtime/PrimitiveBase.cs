using UnityEngine;
using System;

namespace Metamesh
{
    // 标记哪些属性会影响拓扑结构
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TopologyAffectingAttribute : Attribute { }

    // 标记哪些属性只影响形状尺寸
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShapeOnlyAttribute : Attribute { }

    [System.Serializable]
    public abstract class PrimitiveBase
    {
        // 锚点设置
        public AxisPivotPosition PivotX { get; set; } = AxisPivotPosition.Center;
        public AxisPivotPosition PivotY { get; set; } = AxisPivotPosition.Center;
        public AxisPivotPosition PivotZ { get; set; } = AxisPivotPosition.Center;
        public bool NormalizeSize { get; set; } = false;

        // 拓扑快照
        protected int TopologyHash { get; private set; } = 0;
        
        // 缓存的网格引用
        protected Mesh CachedMesh { get; private set; }
        
        // 缓存的顶点数据
        protected Vector3[] CachedVertices { get; private set; }
        protected Vector3[] CachedNormals { get; private set; }
        
        // 抽象方法，由子类实现具体的网格生成逻辑
        protected abstract void GenerateMesh(Mesh mesh);
        
        // 抽象方法，由子类实现形状调整逻辑
        protected abstract void ReshapeMesh();
        
        // 计算当前拓扑哈希值
        protected virtual int CalculateTopologyHash()
        {
            // 默认实现，子类应该重写此方法以计算基于拓扑影响属性的哈希值
            return 0;
        }

        // 公共方法，生成网格并应用锚点调整
        public void Generate(Mesh mesh)
        {
            // 保存网格引用
            CachedMesh = mesh;
            
            // 计算当前拓扑哈希值
            TopologyHash = CalculateTopologyHash();
            
            // 调用子类实现的网格生成方法
            GenerateMesh(mesh);
            
            // 缓存顶点数据
            CacheVertexData(mesh);
            
            // 重新计算边界
            mesh.RecalculateBounds();
            
            // 应用锚点调整
            AdjustPivot(mesh);
        }
        
        // 公共方法，仅调整形状而不改变拓扑
        public bool Reshape()
        {
            // 如果没有提供网格，使用缓存的网格
            var mesh = CachedMesh;
            
            if (mesh == null)
            {
                Debug.LogWarning("无法重塑网格：没有缓存的网格引用");
                return false;
            }
            
            // 计算新的拓扑哈希值
            int newTopologyHash = CalculateTopologyHash();
            
            // 检查拓扑是否变化
            if (newTopologyHash != TopologyHash || CachedVertices == null)
            {
                // 拓扑已变化，需要重新生成整个网格
                Generate(mesh);
                return false;
            }
            
            // 拓扑未变化，只需调整形状
            ReshapeMesh();
            
            // 重新计算边界
            mesh.RecalculateBounds();
            
            // 应用锚点调整
            AdjustPivot(mesh);

            return true;
        }
        
        // 缓存顶点数据
        protected void CacheVertexData(Mesh mesh)
        {
            CachedVertices = mesh.vertices;
            CachedNormals = mesh.normals;
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
using UnityEngine;

using Unity.Mathematics;
namespace Metamesh
{

    [System.Serializable]
    public sealed class Triangle : PrimitiveBase

    {
        [ShapeOnly]
        public float3 Vertex1 = math.float3(0, 0, 0);
        
        [ShapeOnly]
        public float3 Vertex2 = math.float3(1, 0, 0);  // 修改了顶点位置，交换了Vertex2和Vertex3
        
        [ShapeOnly]
        public float3 Vertex3 = math.float3(0, 1, 0);  // 修改了顶点位置，交换了Vertex2和Vertex3
        
        [TopologyAffecting]
        public bool DoubleSided = false;
        
        // 缓存的拓扑结构数据
        private bool _cachedDoubleSided;

        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            return DoubleSided.GetHashCode();
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 缓存拓扑参数
            _cachedDoubleSided = DoubleSided;
            
            var (v1, v2, v3) = (Vertex1, Vertex2, Vertex3);
            var uv1 = Vector3.right;
            var uv2 = Vector3.up;
            var uv3 = Vector3.forward;

            var vtx = DoubleSided ?
              new Vector3[] { v1, v2, v3, v1, v3, v2 } :
              new Vector3[] { v1, v2, v3 };

            var uvs = DoubleSided ?
              new Vector3[] { uv1, uv2, uv3, uv1, uv3, uv2 } :
              new Vector3[] { uv1, uv2, uv3 };

            var idx = DoubleSided ?
              new[] { 0, 1, 2, 3, 4, 5 } :
              new[] { 0, 1, 2 };

            mesh.SetVertices(vtx);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(idx, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
        }
        
        protected override void ReshapeMesh()
        {
            // 检查拓扑参数是否变化
            if (DoubleSided != _cachedDoubleSided)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }
            
            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            
            // 更新顶点位置
            if (DoubleSided)
            {
                // 正面三角形顶点
                vertices[0] = Vertex1;
                vertices[1] = Vertex2;
                vertices[2] = Vertex3;
                
                // 背面三角形顶点（顺序相反）
                vertices[3] = Vertex1;
                vertices[4] = Vertex3;
                vertices[5] = Vertex2;
            }
            else
            {
                // 单面三角形顶点
                vertices[0] = Vertex1;
                vertices[1] = Vertex2;
                vertices[2] = Vertex3;
            }
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
            
            // 重新计算法线
            // CachedMesh.RecalculateNormals();
        }
    }

} // namespace Metamesh

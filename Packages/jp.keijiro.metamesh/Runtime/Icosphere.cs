using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace Metamesh
{

    [System.Serializable]
    public sealed class Icosphere : PrimitiveBase
    {
        [ShapeOnly]
        public float Radius = 1;
        
        [TopologyAffecting]
        public uint Subdivision = 2;
        
        // 缓存的拓扑结构数据
        private uint _cachedSubdivision;
        
        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            return Subdivision.GetHashCode();
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 缓存拓扑参数
            _cachedSubdivision = Subdivision;
            
            var builder = new IcosphereBuilder();
            for (var i = 1; i < Subdivision; i++)
                builder = new IcosphereBuilder(builder);

            var vtx = builder.Vertices.Select(v => (Vector3)(v * Radius));
            var nrm = builder.Vertices.Select(v => (Vector3)v);
            var idx = builder.Indices;

            if (builder.VertexCount > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vtx.ToList());
            mesh.SetNormals(nrm.ToList());
            mesh.SetIndices(idx.ToList(), MeshTopology.Triangles, 0);
        }
        
        protected override void ReshapeMesh()
        {
            // 检查拓扑参数是否变化
            if (Subdivision != _cachedSubdivision)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }
            
            // 获取现有顶点和法线数组
            Vector3[] vertices = CachedMesh.vertices;
            Vector3[] normals = CachedMesh.normals;
            
            // 更新顶点位置 - 只需要缩放法线向量
            for (int i = 0; i < vertices.Length; i++)
            {
                // 法线向量是单位向量，直接乘以半径得到新的顶点位置
                vertices[i].x = normals[i].x * Radius;
                vertices[i].y = normals[i].y * Radius;
                vertices[i].z = normals[i].z * Radius;
            }
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
        }
    }

} // namespace Metamesh

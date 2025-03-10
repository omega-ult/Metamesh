using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace Metamesh
{

    [System.Serializable]
    public class Sphere : PrimitiveBase
    {
        [ShapeOnly]
        public float Radius = 0.5f;
        
        [TopologyAffecting]
        public uint Columns = 24;
        
        [TopologyAffecting]
        public uint Rows = 12;
        
        // 缓存的拓扑结构数据
        private uint _cachedColumns;
        private uint _cachedRows;
        
        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            int hash = Columns.GetHashCode();
            hash = hash * 23 + Rows.GetHashCode();
            return hash;
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 缓存拓扑参数
            _cachedColumns = Columns;
            _cachedRows = Rows;
            
            // Parameter sanitization
            var res = math.int2((int)Columns, (int)Rows);
            res = math.max(res, math.int2(3, 2));

            // Vertex array
            var vtx = new List<float3>();
            var nrm = new List<float3>();
            var uv0 = new List<float2>();

            for (var iy = 0; iy < res.y + 1; iy++)
            {
                var v = (float)iy / res.y;
                var phi = math.PI * v;
                var y = math.cos(phi);
                var r = math.sin(phi);

                for (var ix = 0; ix < res.x + 1; ix++)
                {
                    var u = (float)ix / res.x;
                    var theta = 2 * math.PI * u;
                    var x = math.sin(theta) * r;
                    var z = math.cos(theta) * r;

                    var p = math.float3(x, y, z) * Radius;
                    var n = math.normalize(p);

                    vtx.Add(p);
                    nrm.Add(n);
                    uv0.Add(math.float2(u, v));
                }
            }

            // Index array
            var idx = new List<int>();
            var i = 0;

            for (var iy = 0; iy < res.y; iy++, i++)
            {
                for (var ix = 0; ix < res.x; ix++, i++)
                {
                    idx.Add(i);
                    idx.Add(i + res.x + 1);
                    idx.Add(i + 1);

                    idx.Add(i + 1);
                    idx.Add(i + res.x + 1);
                    idx.Add(i + res.x + 2);
                }
            }

            // Mesh object construction
            if (vtx.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vtx.Select(v => (Vector3)v).ToList());
            mesh.SetNormals(nrm.Select(v => (Vector3)v).ToList());
            mesh.SetUVs(0, uv0.Select(v => (Vector2)v).ToList());
            mesh.SetIndices(idx, MeshTopology.Triangles, 0);
        }
        
        protected override void ReshapeMesh()
        {
            // 检查拓扑参数是否变化
            if (Columns != _cachedColumns || Rows != _cachedRows)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }
            
            // Parameter sanitization
            var res = math.int2((int)Columns, (int)Rows);
            res = math.max(res, math.int2(3, 2));
            
            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            
            // 更新顶点位置 - 只需要更新位置，法线方向不变
            int vertexIndex = 0;
            
            for (var iy = 0; iy < res.y + 1; iy++)
            {
                var v = (float)iy / res.y;
                var phi = math.PI * v;
                var y = math.cos(phi);
                var r = math.sin(phi);

                for (var ix = 0; ix < res.x + 1; ix++)
                {
                    var u = (float)ix / res.x;
                    var theta = 2 * math.PI * u;
                    var x = math.sin(theta) * r;
                    var z = math.cos(theta) * r;

                    // 计算新的顶点位置
                    float3 p = math.float3(x, y, z) * Radius;
                    
                    // 更新顶点位置
                    vertices[vertexIndex].x = p.x;
                    vertices[vertexIndex].y = p.y;
                    vertices[vertexIndex].z = p.z;
                    
                    vertexIndex++;
                }
            }
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
        }
    }

} // namespace Metamesh

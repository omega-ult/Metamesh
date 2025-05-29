using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Metamesh
{
    [System.Serializable]
    public class Plane : PrimitiveBase
    {
        [ShapeOnly]
        public float2 Size = math.float2(1, 1);
        
        [TopologyAffecting]
        public uint2 Subdivisions = math.uint2(2, 2);
        
        [TopologyAffecting]
        public Axis Axis = Axis.Y;
        
        [TopologyAffecting]
        public bool DoubleSided = false;

        // 缓存的拓扑结构数据
        private int2 _cachedSubdivisions;
        private Axis _cachedAxis;
        private bool _cachedDoubleSided;

        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            int hash = Subdivisions.GetHashCode();
            hash = hash * 23 + Axis.GetHashCode();
            hash = hash * 23 + DoubleSided.GetHashCode();
            return hash;
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // Parameter sanitization
            var res = (int2)math.max(2, Subdivisions);

            // 缓存拓扑参数
            _cachedSubdivisions = res;
            _cachedAxis = Axis;
            _cachedDoubleSided = DoubleSided;

            // X/Y vectors perpendicular to Axis
            float3 vx, vy;

            if (Axis == Axis.X)
            {
                vx = math.float3(0, 0, 1);
                vy = math.float3(0, 1, 0);
            }
            else if (Axis == Axis.Y)
            {
                vx = math.float3(1, 0, 0);
                vy = math.float3(0, 0, 1);
            }
            else // Axis.Z
            {
                vx = math.float3(-1, 0, 0);
                vy = math.float3(0, 1, 0);
            }

            vx *= Size.x;
            vy *= Size.y;

            // Vertex array
            var vtx = new List<float3>();
            var uv0 = new List<float2>();

            for (var iy = 0; iy < res.y; iy++)
            {
                for (var ix = 0; ix < res.x; ix++)
                {
                    var uv = math.float2((float)ix / (res.x - 1),
                                         (float)iy / (res.y - 1));

                    var p = math.lerp(-vx, vx, uv.x) +
                            math.lerp(-vy, vy, uv.y);

                    vtx.Add(p);
                    uv0.Add(uv);
                }
            }

            if (DoubleSided)
            {
                vtx = vtx.Concat(vtx).ToList();
                uv0 = uv0.Concat(uv0).ToList();
            }

            // Index array
            var idx = new List<int>();
            var i = 0;

            for (var iy = 0; iy < res.y - 1; iy++, i++)
            {
                for (var ix = 0; ix < res.x - 1; ix++, i++)
                {
                    idx.Add(i);
                    idx.Add(i + res.x);
                    idx.Add(i + 1);

                    idx.Add(i + 1);
                    idx.Add(i + res.x);
                    idx.Add(i + res.x + 1);
                }
            }

            if (DoubleSided)
            {
                i += res.x;

                for (var iy = 0; iy < res.y - 1; iy++, i++)
                {
                    for (var ix = 0; ix < res.x - 1; ix++, i++)
                    {
                        idx.Add(i);
                        idx.Add(i + 1);
                        idx.Add(i + res.x);

                        idx.Add(i + 1);
                        idx.Add(i + res.x + 1);
                        idx.Add(i + res.x);
                    }
                }
            }

            // Mesh object construction
            if (vtx.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vtx.Select(v => (Vector3)v).ToList());
            mesh.SetUVs(0, uv0.Select(v => (Vector2)v).ToList());
            mesh.SetIndices(idx, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
        }

        protected override void ReshapeMesh()
        {
            // 检查拓扑参数是否变化
            var res = (int2)math.max(2, Subdivisions);
            if (res.x != _cachedSubdivisions.x || 
                res.y != _cachedSubdivisions.y || 
                Axis != _cachedAxis || 
                DoubleSided != _cachedDoubleSided)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }

            // X/Y vectors perpendicular to Axis
            float3 vx, vy;

            if (Axis == Axis.X)
            {
                vx = math.float3(0, 0, 1);
                vy = math.float3(0, 1, 0);
            }
            else if (Axis == Axis.Y)
            {
                vx = math.float3(1, 0, 0);
                vy = math.float3(0, 0, 1);
            }
            else // Axis.Z
            {
                vx = math.float3(-1, 0, 0);
                vy = math.float3(0, 1, 0);
            }

            vx *= Size.x;
            vy *= Size.y;

            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            int vertexCount = res.x * res.y;
            
            // 只更新顶点位置，不改变拓扑结构
            for (var iy = 0; iy < res.y; iy++)
            {
                for (var ix = 0; ix < res.x; ix++)
                {
                    int index = iy * res.x + ix;
                    var uv = math.float2((float)ix / (res.x - 1),
                                         (float)iy / (res.y - 1));

                    var p = math.lerp(-vx, vx, uv.x) +
                            math.lerp(-vy, vy, uv.y);

                    vertices[index].x = p.x;
                    vertices[index].y = p.y;
                    vertices[index].z = p.z;
                    
                    // 如果是双面，更新背面顶点
                    if (DoubleSided && index + vertexCount < vertices.Length)
                    {
                        vertices[index + vertexCount].x = p.x;
                        vertices[index + vertexCount].y = p.y;
                        vertices[index + vertexCount].z = p.z;
                    }
                }
            }

            // 更新网格顶点
            CachedMesh.vertices = vertices;
            
            // 重新计算法线和边界
            // CachedMesh.RecalculateNormals();
        }
    }
} // namespace Metamesh

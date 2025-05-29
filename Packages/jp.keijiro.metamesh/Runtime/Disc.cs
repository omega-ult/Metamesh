using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Mathematics;

namespace Metamesh
{
    [System.Serializable]
    public class Disc : PrimitiveBase

    {
        [ShapeOnly] public float Radius = 1;

        [TopologyAffecting] [Range(0, 1)] public float Angle = 1;

        [TopologyAffecting] public uint Segments = 32;

        [TopologyAffecting] public Axis Axis = Axis.Z;

        [TopologyAffecting] public bool DoubleSided = false;

        // 缓存的拓扑结构数据
        private float _cachedAngle;
        private uint _cachedSegments;
        private Axis _cachedAxis;
        private bool _cachedDoubleSided;

        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            int hash = Angle.GetHashCode();
            hash = hash * 23 + Segments.GetHashCode();
            hash = hash * 23 + Axis.GetHashCode();
            hash = hash * 23 + DoubleSided.GetHashCode();
            return hash;
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 缓存拓扑参数
            _cachedAngle = Angle;
            _cachedSegments = Segments;
            _cachedAxis = Axis;
            _cachedDoubleSided = DoubleSided;

            // Axis selection
            var X = float3.zero;
            var Y = float3.zero;
            var ai = (int)Axis;
            X[(ai + 1) % 3] = 1;
            Y[(ai + 2) % 3] = 1;

            // Vertex array
            var vtx = new List<float3>();
            var uv0 = new List<float2>();

            for (var i = 0; i < Segments; i++)
            {
                var phi = 2 * math.PI * Angle * ((float)i / (Segments - 1) - 0.5f);
                var (x, y) = (math.cos(phi), math.sin(phi));

                vtx.Add((x * X + y * Y) * Radius);
                uv0.Add(math.float2(-x, y) / 2 + 0.5f);
            }

            vtx.Add(0);
            uv0.Add(0.5f);

            if (DoubleSided)
            {
                vtx = vtx.Concat(vtx).ToList();
                uv0 = uv0.Concat(uv0).ToList();
            }

            // Index array
            var idx = new List<int>();
            var n = (int)Segments;

            for (var i = 0; i < n - 1; i++)
            {
                idx.Add(n);
                idx.Add(i);
                idx.Add(i + 1);
            }

            if (DoubleSided)
            {
                for (var i = 0; i < n - 1; i++)
                {
                    idx.Add(n + 1 + n);
                    idx.Add(n + 1 + i + 1);
                    idx.Add(n + 1 + i);
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
            if (Angle != _cachedAngle ||
                Segments != _cachedSegments ||
                Axis != _cachedAxis ||
                DoubleSided != _cachedDoubleSided)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }

            // Axis selection
            var X = float3.zero;
            var Y = float3.zero;
            var ai = (int)Axis;
            X[(ai + 1) % 3] = 1;
            Y[(ai + 2) % 3] = 1;

            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;

            // 更新顶点位置 - 只需要更新圆周上的点，中心点保持不变
            int n = (int)Segments;

            for (var i = 0; i < n; i++)
            {
                var phi = 2 * math.PI * Angle * ((float)i / (Segments - 1) - 0.5f);
                var (x, y) = (math.cos(phi), math.sin(phi));

                float3 newPos = (x * X + y * Y) * Radius;

                // 更新顶点位置
                vertices[i].x = newPos.x;
                vertices[i].y = newPos.y;
                vertices[i].z = newPos.z;

                // 如果是双面，更新背面顶点
                if (DoubleSided && i + n + 1 < vertices.Length)
                {
                    vertices[i + n + 1].x = newPos.x;
                    vertices[i + n + 1].y = newPos.y;
                    vertices[i + n + 1].z = newPos.z;
                }
            }

            // 中心点位置保持为原点
            vertices[n].x = 0;
            vertices[n].y = 0;
            vertices[n].z = 0;

            if (DoubleSided)
            {
                vertices[n * 2 + 1].x = 0;
                vertices[n * 2 + 1].y = 0;
                vertices[n * 2 + 1].z = 0;
            }

            // 更新网格顶点
            CachedMesh.vertices = vertices;
        }
    }
} // namespace Metamesh
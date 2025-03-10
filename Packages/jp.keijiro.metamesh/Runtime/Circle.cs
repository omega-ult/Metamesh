using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Mathematics;

namespace Metamesh
{

    [System.Serializable]
    public sealed class Ring : PrimitiveBase

    {
        [ShapeOnly]
        public float Radius = 1;

        [ShapeOnly]
        public float Width = 0.1f;

        [TopologyAffecting]
        [Range(0, 1)] public float Angle = 1;

        [TopologyAffecting]
        public uint Segments = 32;

        [TopologyAffecting]
        public Axis Axis = Axis.Z;

        [TopologyAffecting]
        public bool DoubleSided = false;

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

            // Parameter sanitization
            var ext = math.min(Radius, Width / 2);

            // Axis selection
            var X = float3.zero;
            var Y = float3.zero;
            var ai = (int)Axis;
            X[(ai + 1) % 3] = 1;
            Y[(ai + 2) % 3] = 1;

            // Vertex array
            var vtx = new List<float3>();
            var uv0 = new List<float2>();

            var i_div_o = (Radius - Width / 2) / (Radius + Width / 2);

            for (var i = 0; i < Segments; i++)
            {
                var phi = 2 * math.PI * Angle * ((float)i / (Segments - 1) - 0.5f);
                var (x, y) = (math.cos(phi), math.sin(phi));

                var v = x * X + y * Y;
                vtx.Add(v * (Radius - ext));
                vtx.Add(v * (Radius + ext));
                uv0.Add(math.float2(-x, y) / 2 * i_div_o + 0.5f);
                uv0.Add(math.float2(-x, y) / 2 + 0.5f);
            }

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
                idx.Add(i * 2);
                idx.Add(i * 2 + 1);
                idx.Add(i * 2 + 2);

                idx.Add(i * 2 + 1);
                idx.Add(i * 2 + 3);
                idx.Add(i * 2 + 2);
            }

            if (DoubleSided)
            {
                for (var i = 0; i < n - 1; i++)
                {
                    idx.Add((n + i) * 2);
                    idx.Add((n + i) * 2 + 2);
                    idx.Add((n + i) * 2 + 1);

                    idx.Add((n + i) * 2 + 1);
                    idx.Add((n + i) * 2 + 2);
                    idx.Add((n + i) * 2 + 3);
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

            // Parameter sanitization
            var ext = math.min(Radius, Width / 2);

            // Axis selection
            var X = float3.zero;
            var Y = float3.zero;
            var ai = (int)Axis;
            X[(ai + 1) % 3] = 1;
            Y[(ai + 2) % 3] = 1;

            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;

            // 更新顶点位置
            int n = (int)Segments;

            for (var i = 0; i < n; i++)
            {
                var phi = 2 * math.PI * Angle * ((float)i / (Segments - 1) - 0.5f);
                var (x, y) = (math.cos(phi), math.sin(phi));

                var v = x * X + y * Y;
                float3 innerPos = v * (Radius - ext);
                float3 outerPos = v * (Radius + ext);

                // 更新内圈顶点位置
                int innerIndex = i * 2;
                vertices[innerIndex].x = innerPos.x;
                vertices[innerIndex].y = innerPos.y;
                vertices[innerIndex].z = innerPos.z;

                // 更新外圈顶点位置
                int outerIndex = i * 2 + 1;
                vertices[outerIndex].x = outerPos.x;
                vertices[outerIndex].y = outerPos.y;
                vertices[outerIndex].z = outerPos.z;

                // 如果是双面，更新背面顶点
                if (DoubleSided)
                {
                    int backInnerIndex = (n + i) * 2;
                    int backOuterIndex = (n + i) * 2 + 1;

                    if (backInnerIndex < vertices.Length && backOuterIndex < vertices.Length)
                    {
                        vertices[backInnerIndex].x = innerPos.x;
                        vertices[backInnerIndex].y = innerPos.y;
                        vertices[backInnerIndex].z = innerPos.z;

                        vertices[backOuterIndex].x = outerPos.x;
                        vertices[backOuterIndex].y = outerPos.y;
                        vertices[backOuterIndex].z = outerPos.z;
                    }
                }
            }

            // 更新网格顶点
            CachedMesh.vertices = vertices;
        }
    }

} // namespace Metamesh

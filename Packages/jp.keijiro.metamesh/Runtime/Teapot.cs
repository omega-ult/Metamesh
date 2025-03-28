using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Metamesh
{

    [System.Serializable]
    public class Teapot : PrimitiveBase

    {
        [TopologyAffecting]
        public int Subdivision = 10;
        
        // 缓存的拓扑结构数据
        private int _cachedSubdivision;
        
        // 缓存的顶点数据，用于缩放操作
        private float3[] _originalVertices;
        
        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            return Subdivision.GetHashCode();
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 缓存拓扑参数
            _cachedSubdivision = Subdivision;
            
            var P = Patches;

            // Vertex array construction
            var vtx = new List<float3>();
            var nrm = new List<float3>();
            var uv0 = new List<float2>();
            for (var offs = 0; offs < P.Length; offs += 16)
            {
                for (var col = 0; col < Subdivision; col++)
                {
                    var i = offs;
                    var u = (float)col / (Subdivision - 1);
                    var c0 = Evaluate(P[i++], P[i++], P[i++], P[i++], u);
                    var c1 = Evaluate(P[i++], P[i++], P[i++], P[i++], u);
                    var c2 = Evaluate(P[i++], P[i++], P[i++], P[i++], u);
                    var c3 = Evaluate(P[i++], P[i++], P[i++], P[i++], u);
                    for (var row = 0; row < Subdivision; row++)
                    {
                        var v = (float)row / (Subdivision - 1);
                        var p = Evaluate(c0.p, c1.p, c2.p, c3.p, v);
                        var du = p.d;
                        var dv = Bezier(c0.d, c1.d, c2.d, c3.d, v);
                        if (math.length(dv) < math.FLT_MIN_NORMAL) dv = c1.d;
                        vtx.Add(p.p);
                        nrm.Add(math.normalize(math.cross(du, dv)));
                        uv0.Add(math.float2(u, v));
                    }
                }
            }

            // 缓存原始顶点位置，用于后续缩放
            _originalVertices = vtx.ToArray();

            // Index array construction
            var idx = new List<int>();
            for (var offs = 0; offs < vtx.Count; offs += Subdivision * Subdivision)
            {
                for (var row = 0; row < Subdivision - 1; row++)
                {
                    for (var col = 0; col < Subdivision - 1; col++)
                    {
                        // Quad indices
                        var i0 = offs + row * Subdivision + col;
                        var i1 = i0 + 1;
                        var i2 = i0 + Subdivision;
                        var i3 = i0 + Subdivision + 1;
                        // First triangle
                        idx.Add(i0);
                        idx.Add(i1);
                        idx.Add(i2);
                        // Second triangle
                        idx.Add(i1);
                        idx.Add(i3);
                        idx.Add(i2);
                    }
                }
            }

            // Mesh object construction
            mesh.SetVertices(vtx.Select(v => (Vector3)v).ToList());
            mesh.SetNormals(nrm.Select(n => (Vector3)n).ToList());
            mesh.SetUVs(0, uv0.Select(t => (Vector2)t).ToList());
            mesh.SetIndices(idx, MeshTopology.Triangles, 0);
        }
        
        protected override void ReshapeMesh()
        {
            // 检查拓扑参数是否变化
            if (Subdivision != _cachedSubdivision || _originalVertices == null)
            {
                // 拓扑结构变化或者没有缓存的顶点数据，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }
            
            // 对于茶壶模型，我们可以简单地应用全局缩放
            // 这里我们假设缩放因子为1.0，如果需要可以添加一个Scale属性
            float scale = 1.0f;
            
            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            
            // 应用缩放
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x = _originalVertices[i].x * scale;
                vertices[i].y = _originalVertices[i].y * scale;
                vertices[i].z = _originalVertices[i].z * scale;
            }
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
            
            // 由于我们只是均匀缩放，法线不需要更新
        }

        // Surface evaluation function
        (float3 p, float3 d)
          Evaluate(float3 p0, float3 p1, float3 p2, float3 p3, float t)
            => (Bezier(p0, p1, p2, p3, t), BezierD(p0, p1, p2, p3, t));

        // Bezier curve function
        float3 Bezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            var mt = 1 - t;
            return mt * mt * mt * p0 +
                   3 * mt * mt * t * p1 +
                   3 * mt * t * t * p2 +
                        t * t * t * p3;
        }

        // Derivative of Bezier
        float3 BezierD(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            var mt = 1 - t;
            return 3 * mt * mt * (p1 - p0) +
                   6 * mt * t * (p2 - p1) +
                   3 * t * t * (p3 - p2);
        }

        // Utah teapot patches from CGA
        // http://www.holmes3d.net/graphics/teapot/
        static readonly float3[] Patches =
        {
        math.float3( 0.280f,  0.465f,  0.000f),
        math.float3( 0.280f,  0.465f, -0.157f),
        math.float3( 0.157f,  0.465f, -0.280f),
        math.float3( 0.000f,  0.465f, -0.280f),
        math.float3( 0.268f,  0.491f,  0.000f),
        math.float3( 0.268f,  0.491f, -0.150f),
        math.float3( 0.150f,  0.491f, -0.268f),
        math.float3( 0.000f,  0.491f, -0.268f),
        math.float3( 0.288f,  0.491f,  0.000f),
        math.float3( 0.288f,  0.491f, -0.161f),
        math.float3( 0.161f,  0.491f, -0.288f),
        math.float3( 0.000f,  0.491f, -0.288f),
        math.float3( 0.300f,  0.465f,  0.000f),
        math.float3( 0.300f,  0.465f, -0.168f),
        math.float3( 0.168f,  0.465f, -0.300f),
        math.float3( 0.000f,  0.465f, -0.300f),
        math.float3( 0.000f,  0.465f, -0.280f),
        math.float3(-0.157f,  0.465f, -0.280f),
        math.float3(-0.280f,  0.465f, -0.157f),
        math.float3(-0.280f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.491f, -0.268f),
        math.float3(-0.150f,  0.491f, -0.268f),
        math.float3(-0.268f,  0.491f, -0.150f),
        math.float3(-0.268f,  0.491f,  0.000f),
        math.float3( 0.000f,  0.491f, -0.288f),
        math.float3(-0.161f,  0.491f, -0.288f),
        math.float3(-0.288f,  0.491f, -0.161f),
        math.float3(-0.288f,  0.491f,  0.000f),
        math.float3( 0.000f,  0.465f, -0.300f),
        math.float3(-0.168f,  0.465f, -0.300f),
        math.float3(-0.300f,  0.465f, -0.168f),
        math.float3(-0.300f,  0.465f,  0.000f),
        math.float3(-0.280f,  0.465f,  0.000f),
        math.float3(-0.280f,  0.465f,  0.157f),
        math.float3(-0.157f,  0.465f,  0.280f),
        math.float3( 0.000f,  0.465f,  0.280f),
        math.float3(-0.268f,  0.491f,  0.000f),
        math.float3(-0.268f,  0.491f,  0.150f),
        math.float3(-0.150f,  0.491f,  0.268f),
        math.float3( 0.000f,  0.491f,  0.268f),
        math.float3(-0.288f,  0.491f,  0.000f),
        math.float3(-0.288f,  0.491f,  0.161f),
        math.float3(-0.161f,  0.491f,  0.288f),
        math.float3( 0.000f,  0.491f,  0.288f),
        math.float3(-0.300f,  0.465f,  0.000f),
        math.float3(-0.300f,  0.465f,  0.168f),
        math.float3(-0.168f,  0.465f,  0.300f),
        math.float3( 0.000f,  0.465f,  0.300f),
        math.float3( 0.000f,  0.465f,  0.280f),
        math.float3( 0.157f,  0.465f,  0.280f),
        math.float3( 0.280f,  0.465f,  0.157f),
        math.float3( 0.280f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.491f,  0.268f),
        math.float3( 0.150f,  0.491f,  0.268f),
        math.float3( 0.268f,  0.491f,  0.150f),
        math.float3( 0.268f,  0.491f,  0.000f),
        math.float3( 0.000f,  0.491f,  0.288f),
        math.float3( 0.161f,  0.491f,  0.288f),
        math.float3( 0.288f,  0.491f,  0.161f),
        math.float3( 0.288f,  0.491f,  0.000f),
        math.float3( 0.000f,  0.465f,  0.300f),
        math.float3( 0.168f,  0.465f,  0.300f),
        math.float3( 0.300f,  0.465f,  0.168f),
        math.float3( 0.300f,  0.465f,  0.000f),
        math.float3( 0.300f,  0.465f,  0.000f),
        math.float3( 0.300f,  0.465f, -0.168f),
        math.float3( 0.168f,  0.465f, -0.300f),
        math.float3( 0.000f,  0.465f, -0.300f),
        math.float3( 0.350f,  0.360f,  0.000f),
        math.float3( 0.350f,  0.360f, -0.196f),
        math.float3( 0.196f,  0.360f, -0.350f),
        math.float3( 0.000f,  0.360f, -0.350f),
        math.float3( 0.400f,  0.255f,  0.000f),
        math.float3( 0.400f,  0.255f, -0.224f),
        math.float3( 0.224f,  0.255f, -0.400f),
        math.float3( 0.000f,  0.255f, -0.400f),
        math.float3( 0.400f,  0.165f,  0.000f),
        math.float3( 0.400f,  0.165f, -0.224f),
        math.float3( 0.224f,  0.165f, -0.400f),
        math.float3( 0.000f,  0.165f, -0.400f),
        math.float3( 0.000f,  0.465f, -0.300f),
        math.float3(-0.168f,  0.465f, -0.300f),
        math.float3(-0.300f,  0.465f, -0.168f),
        math.float3(-0.300f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.360f, -0.350f),
        math.float3(-0.196f,  0.360f, -0.350f),
        math.float3(-0.350f,  0.360f, -0.196f),
        math.float3(-0.350f,  0.360f,  0.000f),
        math.float3( 0.000f,  0.255f, -0.400f),
        math.float3(-0.224f,  0.255f, -0.400f),
        math.float3(-0.400f,  0.255f, -0.224f),
        math.float3(-0.400f,  0.255f,  0.000f),
        math.float3( 0.000f,  0.165f, -0.400f),
        math.float3(-0.224f,  0.165f, -0.400f),
        math.float3(-0.400f,  0.165f, -0.224f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3(-0.300f,  0.465f,  0.000f),
        math.float3(-0.300f,  0.465f,  0.168f),
        math.float3(-0.168f,  0.465f,  0.300f),
        math.float3( 0.000f,  0.465f,  0.300f),
        math.float3(-0.350f,  0.360f,  0.000f),
        math.float3(-0.350f,  0.360f,  0.196f),
        math.float3(-0.196f,  0.360f,  0.350f),
        math.float3( 0.000f,  0.360f,  0.350f),
        math.float3(-0.400f,  0.255f,  0.000f),
        math.float3(-0.400f,  0.255f,  0.224f),
        math.float3(-0.224f,  0.255f,  0.400f),
        math.float3( 0.000f,  0.255f,  0.400f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3(-0.400f,  0.165f,  0.224f),
        math.float3(-0.224f,  0.165f,  0.400f),
        math.float3( 0.000f,  0.165f,  0.400f),
        math.float3( 0.000f,  0.465f,  0.300f),
        math.float3( 0.168f,  0.465f,  0.300f),
        math.float3( 0.300f,  0.465f,  0.168f),
        math.float3( 0.300f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.360f,  0.350f),
        math.float3( 0.196f,  0.360f,  0.350f),
        math.float3( 0.350f,  0.360f,  0.196f),
        math.float3( 0.350f,  0.360f,  0.000f),
        math.float3( 0.000f,  0.255f,  0.400f),
        math.float3( 0.224f,  0.255f,  0.400f),
        math.float3( 0.400f,  0.255f,  0.224f),
        math.float3( 0.400f,  0.255f,  0.000f),
        math.float3( 0.000f,  0.165f,  0.400f),
        math.float3( 0.224f,  0.165f,  0.400f),
        math.float3( 0.400f,  0.165f,  0.224f),
        math.float3( 0.400f,  0.165f,  0.000f),
        math.float3( 0.400f,  0.165f,  0.000f),
        math.float3( 0.400f,  0.165f, -0.224f),
        math.float3( 0.224f,  0.165f, -0.400f),
        math.float3( 0.000f,  0.165f, -0.400f),
        math.float3( 0.400f,  0.075f,  0.000f),
        math.float3( 0.400f,  0.075f, -0.224f),
        math.float3( 0.224f,  0.075f, -0.400f),
        math.float3( 0.000f,  0.075f, -0.400f),
        math.float3( 0.300f,  0.030f,  0.000f),
        math.float3( 0.300f,  0.030f, -0.168f),
        math.float3( 0.168f,  0.030f, -0.300f),
        math.float3( 0.000f,  0.030f, -0.300f),
        math.float3( 0.300f,  0.015f,  0.000f),
        math.float3( 0.300f,  0.015f, -0.168f),
        math.float3( 0.168f,  0.015f, -0.300f),
        math.float3( 0.000f,  0.015f, -0.300f),
        math.float3( 0.000f,  0.165f, -0.400f),
        math.float3(-0.224f,  0.165f, -0.400f),
        math.float3(-0.400f,  0.165f, -0.224f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3( 0.000f,  0.075f, -0.400f),
        math.float3(-0.224f,  0.075f, -0.400f),
        math.float3(-0.400f,  0.075f, -0.224f),
        math.float3(-0.400f,  0.075f,  0.000f),
        math.float3( 0.000f,  0.030f, -0.300f),
        math.float3(-0.168f,  0.030f, -0.300f),
        math.float3(-0.300f,  0.030f, -0.168f),
        math.float3(-0.300f,  0.030f,  0.000f),
        math.float3( 0.000f,  0.015f, -0.300f),
        math.float3(-0.168f,  0.015f, -0.300f),
        math.float3(-0.300f,  0.015f, -0.168f),
        math.float3(-0.300f,  0.015f,  0.000f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3(-0.400f,  0.165f,  0.224f),
        math.float3(-0.224f,  0.165f,  0.400f),
        math.float3( 0.000f,  0.165f,  0.400f),
        math.float3(-0.400f,  0.075f,  0.000f),
        math.float3(-0.400f,  0.075f,  0.224f),
        math.float3(-0.224f,  0.075f,  0.400f),
        math.float3( 0.000f,  0.075f,  0.400f),
        math.float3(-0.300f,  0.030f,  0.000f),
        math.float3(-0.300f,  0.030f,  0.168f),
        math.float3(-0.168f,  0.030f,  0.300f),
        math.float3( 0.000f,  0.030f,  0.300f),
        math.float3(-0.300f,  0.015f,  0.000f),
        math.float3(-0.300f,  0.015f,  0.168f),
        math.float3(-0.168f,  0.015f,  0.300f),
        math.float3( 0.000f,  0.015f,  0.300f),
        math.float3( 0.000f,  0.165f,  0.400f),
        math.float3( 0.224f,  0.165f,  0.400f),
        math.float3( 0.400f,  0.165f,  0.224f),
        math.float3( 0.400f,  0.165f,  0.000f),
        math.float3( 0.000f,  0.075f,  0.400f),
        math.float3( 0.224f,  0.075f,  0.400f),
        math.float3( 0.400f,  0.075f,  0.224f),
        math.float3( 0.400f,  0.075f,  0.000f),
        math.float3( 0.000f,  0.030f,  0.300f),
        math.float3( 0.168f,  0.030f,  0.300f),
        math.float3( 0.300f,  0.030f,  0.168f),
        math.float3( 0.300f,  0.030f,  0.000f),
        math.float3( 0.000f,  0.015f,  0.300f),
        math.float3( 0.168f,  0.015f,  0.300f),
        math.float3( 0.300f,  0.015f,  0.168f),
        math.float3( 0.300f,  0.015f,  0.000f),
        math.float3(-0.320f,  0.390f,  0.000f),
        math.float3(-0.320f,  0.390f, -0.060f),
        math.float3(-0.300f,  0.435f, -0.060f),
        math.float3(-0.300f,  0.435f,  0.000f),
        math.float3(-0.460f,  0.390f,  0.000f),
        math.float3(-0.460f,  0.390f, -0.060f),
        math.float3(-0.500f,  0.435f, -0.060f),
        math.float3(-0.500f,  0.435f,  0.000f),
        math.float3(-0.540f,  0.390f,  0.000f),
        math.float3(-0.540f,  0.390f, -0.060f),
        math.float3(-0.600f,  0.435f, -0.060f),
        math.float3(-0.600f,  0.435f,  0.000f),
        math.float3(-0.540f,  0.345f,  0.000f),
        math.float3(-0.540f,  0.345f, -0.060f),
        math.float3(-0.600f,  0.345f, -0.060f),
        math.float3(-0.600f,  0.345f,  0.000f),
        math.float3(-0.300f,  0.435f,  0.000f),
        math.float3(-0.300f,  0.435f,  0.060f),
        math.float3(-0.320f,  0.390f,  0.060f),
        math.float3(-0.320f,  0.390f,  0.000f),
        math.float3(-0.500f,  0.435f,  0.000f),
        math.float3(-0.500f,  0.435f,  0.060f),
        math.float3(-0.460f,  0.390f,  0.060f),
        math.float3(-0.460f,  0.390f,  0.000f),
        math.float3(-0.600f,  0.435f,  0.000f),
        math.float3(-0.600f,  0.435f,  0.060f),
        math.float3(-0.540f,  0.390f,  0.060f),
        math.float3(-0.540f,  0.390f,  0.000f),
        math.float3(-0.600f,  0.345f,  0.000f),
        math.float3(-0.600f,  0.345f,  0.060f),
        math.float3(-0.540f,  0.345f,  0.060f),
        math.float3(-0.540f,  0.345f,  0.000f),
        math.float3(-0.540f,  0.345f,  0.000f),
        math.float3(-0.540f,  0.345f, -0.060f),
        math.float3(-0.600f,  0.345f, -0.060f),
        math.float3(-0.600f,  0.345f,  0.000f),
        math.float3(-0.540f,  0.300f,  0.000f),
        math.float3(-0.540f,  0.300f, -0.060f),
        math.float3(-0.600f,  0.255f, -0.060f),
        math.float3(-0.600f,  0.255f,  0.000f),
        math.float3(-0.500f,  0.210f,  0.000f),
        math.float3(-0.500f,  0.210f, -0.060f),
        math.float3(-0.530f,  0.172f, -0.060f),
        math.float3(-0.530f,  0.172f,  0.000f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3(-0.400f,  0.165f, -0.060f),
        math.float3(-0.380f,  0.105f, -0.060f),
        math.float3(-0.380f,  0.105f,  0.000f),
        math.float3(-0.600f,  0.345f,  0.000f),
        math.float3(-0.600f,  0.345f,  0.060f),
        math.float3(-0.540f,  0.345f,  0.060f),
        math.float3(-0.540f,  0.345f,  0.000f),
        math.float3(-0.600f,  0.255f,  0.000f),
        math.float3(-0.600f,  0.255f,  0.060f),
        math.float3(-0.540f,  0.300f,  0.060f),
        math.float3(-0.540f,  0.300f,  0.000f),
        math.float3(-0.530f,  0.172f,  0.000f),
        math.float3(-0.530f,  0.172f,  0.060f),
        math.float3(-0.500f,  0.210f,  0.060f),
        math.float3(-0.500f,  0.210f,  0.000f),
        math.float3(-0.380f,  0.105f,  0.000f),
        math.float3(-0.380f,  0.105f,  0.060f),
        math.float3(-0.400f,  0.165f,  0.060f),
        math.float3(-0.400f,  0.165f,  0.000f),
        math.float3( 0.340f,  0.270f,  0.000f),
        math.float3( 0.340f,  0.270f, -0.132f),
        math.float3( 0.340f,  0.105f, -0.132f),
        math.float3( 0.340f,  0.105f,  0.000f),
        math.float3( 0.520f,  0.270f,  0.000f),
        math.float3( 0.520f,  0.270f, -0.132f),
        math.float3( 0.620f,  0.150f, -0.132f),
        math.float3( 0.620f,  0.150f,  0.000f),
        math.float3( 0.460f,  0.405f,  0.000f),
        math.float3( 0.460f,  0.405f, -0.050f),
        math.float3( 0.480f,  0.390f, -0.050f),
        math.float3( 0.480f,  0.390f,  0.000f),
        math.float3( 0.540f,  0.465f,  0.000f),
        math.float3( 0.540f,  0.465f, -0.050f),
        math.float3( 0.660f,  0.465f, -0.050f),
        math.float3( 0.660f,  0.465f,  0.000f),
        math.float3( 0.340f,  0.105f,  0.000f),
        math.float3( 0.340f,  0.105f,  0.132f),
        math.float3( 0.340f,  0.270f,  0.132f),
        math.float3( 0.340f,  0.270f,  0.000f),
        math.float3( 0.620f,  0.150f,  0.000f),
        math.float3( 0.620f,  0.150f,  0.132f),
        math.float3( 0.520f,  0.270f,  0.132f),
        math.float3( 0.520f,  0.270f,  0.000f),
        math.float3( 0.480f,  0.390f,  0.000f),
        math.float3( 0.480f,  0.390f,  0.050f),
        math.float3( 0.460f,  0.405f,  0.050f),
        math.float3( 0.460f,  0.405f,  0.000f),
        math.float3( 0.660f,  0.465f,  0.000f),
        math.float3( 0.660f,  0.465f,  0.050f),
        math.float3( 0.540f,  0.465f,  0.050f),
        math.float3( 0.540f,  0.465f,  0.000f),
        math.float3( 0.540f,  0.465f,  0.000f),
        math.float3( 0.540f,  0.465f, -0.050f),
        math.float3( 0.660f,  0.465f, -0.050f),
        math.float3( 0.660f,  0.465f,  0.000f),
        math.float3( 0.560f,  0.480f,  0.000f),
        math.float3( 0.560f,  0.480f, -0.050f),
        math.float3( 0.705f,  0.484f, -0.050f),
        math.float3( 0.705f,  0.484f,  0.000f),
        math.float3( 0.580f,  0.480f,  0.000f),
        math.float3( 0.580f,  0.480f, -0.030f),
        math.float3( 0.690f,  0.488f, -0.030f),
        math.float3( 0.690f,  0.488f,  0.000f),
        math.float3( 0.560f,  0.465f,  0.000f),
        math.float3( 0.560f,  0.465f, -0.030f),
        math.float3( 0.640f,  0.465f, -0.030f),
        math.float3( 0.640f,  0.465f,  0.000f),
        math.float3( 0.660f,  0.465f,  0.000f),
        math.float3( 0.660f,  0.465f,  0.050f),
        math.float3( 0.540f,  0.465f,  0.050f),
        math.float3( 0.540f,  0.465f,  0.000f),
        math.float3( 0.705f,  0.484f,  0.000f),
        math.float3( 0.705f,  0.484f,  0.050f),
        math.float3( 0.560f,  0.480f,  0.050f),
        math.float3( 0.560f,  0.480f,  0.000f),
        math.float3( 0.690f,  0.488f,  0.000f),
        math.float3( 0.690f,  0.488f,  0.030f),
        math.float3( 0.580f,  0.480f,  0.030f),
        math.float3( 0.580f,  0.480f,  0.000f),
        math.float3( 0.640f,  0.465f,  0.000f),
        math.float3( 0.640f,  0.465f,  0.030f),
        math.float3( 0.560f,  0.465f,  0.030f),
        math.float3( 0.560f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.160f,  0.615f,  0.000f),
        math.float3( 0.160f,  0.615f, -0.090f),
        math.float3( 0.090f,  0.615f, -0.160f),
        math.float3( 0.000f,  0.615f, -0.160f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.040f,  0.525f,  0.000f),
        math.float3( 0.040f,  0.525f, -0.022f),
        math.float3( 0.022f,  0.525f, -0.040f),
        math.float3( 0.000f,  0.525f, -0.040f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f, -0.160f),
        math.float3(-0.090f,  0.615f, -0.160f),
        math.float3(-0.160f,  0.615f, -0.090f),
        math.float3(-0.160f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.525f, -0.040f),
        math.float3(-0.022f,  0.525f, -0.040f),
        math.float3(-0.040f,  0.525f, -0.022f),
        math.float3(-0.040f,  0.525f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3(-0.160f,  0.615f,  0.000f),
        math.float3(-0.160f,  0.615f,  0.090f),
        math.float3(-0.090f,  0.615f,  0.160f),
        math.float3( 0.000f,  0.615f,  0.160f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3(-0.040f,  0.525f,  0.000f),
        math.float3(-0.040f,  0.525f,  0.022f),
        math.float3(-0.022f,  0.525f,  0.040f),
        math.float3( 0.000f,  0.525f,  0.040f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.615f,  0.160f),
        math.float3( 0.090f,  0.615f,  0.160f),
        math.float3( 0.160f,  0.615f,  0.090f),
        math.float3( 0.160f,  0.615f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.555f,  0.000f),
        math.float3( 0.000f,  0.525f,  0.040f),
        math.float3( 0.022f,  0.525f,  0.040f),
        math.float3( 0.040f,  0.525f,  0.022f),
        math.float3( 0.040f,  0.525f,  0.000f),
        math.float3( 0.040f,  0.525f,  0.000f),
        math.float3( 0.040f,  0.525f, -0.022f),
        math.float3( 0.022f,  0.525f, -0.040f),
        math.float3( 0.000f,  0.525f, -0.040f),
        math.float3( 0.080f,  0.495f,  0.000f),
        math.float3( 0.080f,  0.495f, -0.045f),
        math.float3( 0.045f,  0.495f, -0.080f),
        math.float3( 0.000f,  0.495f, -0.080f),
        math.float3( 0.260f,  0.495f,  0.000f),
        math.float3( 0.260f,  0.495f, -0.146f),
        math.float3( 0.146f,  0.495f, -0.260f),
        math.float3( 0.000f,  0.495f, -0.260f),
        math.float3( 0.260f,  0.465f,  0.000f),
        math.float3( 0.260f,  0.465f, -0.146f),
        math.float3( 0.146f,  0.465f, -0.260f),
        math.float3( 0.000f,  0.465f, -0.260f),
        math.float3( 0.000f,  0.525f, -0.040f),
        math.float3(-0.022f,  0.525f, -0.040f),
        math.float3(-0.040f,  0.525f, -0.022f),
        math.float3(-0.040f,  0.525f,  0.000f),
        math.float3( 0.000f,  0.495f, -0.080f),
        math.float3(-0.045f,  0.495f, -0.080f),
        math.float3(-0.080f,  0.495f, -0.045f),
        math.float3(-0.080f,  0.495f,  0.000f),
        math.float3( 0.000f,  0.495f, -0.260f),
        math.float3(-0.146f,  0.495f, -0.260f),
        math.float3(-0.260f,  0.495f, -0.146f),
        math.float3(-0.260f,  0.495f,  0.000f),
        math.float3( 0.000f,  0.465f, -0.260f),
        math.float3(-0.146f,  0.465f, -0.260f),
        math.float3(-0.260f,  0.465f, -0.146f),
        math.float3(-0.260f,  0.465f,  0.000f),
        math.float3(-0.040f,  0.525f,  0.000f),
        math.float3(-0.040f,  0.525f,  0.022f),
        math.float3(-0.022f,  0.525f,  0.040f),
        math.float3( 0.000f,  0.525f,  0.040f),
        math.float3(-0.080f,  0.495f,  0.000f),
        math.float3(-0.080f,  0.495f,  0.045f),
        math.float3(-0.045f,  0.495f,  0.080f),
        math.float3( 0.000f,  0.495f,  0.080f),
        math.float3(-0.260f,  0.495f,  0.000f),
        math.float3(-0.260f,  0.495f,  0.146f),
        math.float3(-0.146f,  0.495f,  0.260f),
        math.float3( 0.000f,  0.495f,  0.260f),
        math.float3(-0.260f,  0.465f,  0.000f),
        math.float3(-0.260f,  0.465f,  0.146f),
        math.float3(-0.146f,  0.465f,  0.260f),
        math.float3( 0.000f,  0.465f,  0.260f),
        math.float3( 0.000f,  0.525f,  0.040f),
        math.float3( 0.022f,  0.525f,  0.040f),
        math.float3( 0.040f,  0.525f,  0.022f),
        math.float3( 0.040f,  0.525f,  0.000f),
        math.float3( 0.000f,  0.495f,  0.080f),
        math.float3( 0.045f,  0.495f,  0.080f),
        math.float3( 0.080f,  0.495f,  0.045f),
        math.float3( 0.080f,  0.495f,  0.000f),
        math.float3( 0.000f,  0.495f,  0.260f),
        math.float3( 0.146f,  0.495f,  0.260f),
        math.float3( 0.260f,  0.495f,  0.146f),
        math.float3( 0.260f,  0.495f,  0.000f),
        math.float3( 0.000f,  0.465f,  0.260f),
        math.float3( 0.146f,  0.465f,  0.260f),
        math.float3( 0.260f,  0.465f,  0.146f),
        math.float3( 0.260f,  0.465f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.285f,  0.000f,  0.000f),
        math.float3( 0.285f,  0.000f,  0.160f),
        math.float3( 0.160f,  0.000f,  0.285f),
        math.float3( 0.000f,  0.000f,  0.285f),
        math.float3( 0.300f,  0.000f,  0.000f),
        math.float3( 0.300f,  0.000f,  0.168f),
        math.float3( 0.168f,  0.000f,  0.300f),
        math.float3( 0.000f,  0.000f,  0.300f),
        math.float3( 0.300f,  0.015f,  0.000f),
        math.float3( 0.300f,  0.015f,  0.168f),
        math.float3( 0.168f,  0.015f,  0.300f),
        math.float3( 0.000f,  0.015f,  0.300f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.285f),
        math.float3(-0.160f,  0.000f,  0.285f),
        math.float3(-0.285f,  0.000f,  0.160f),
        math.float3(-0.285f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.300f),
        math.float3(-0.168f,  0.000f,  0.300f),
        math.float3(-0.300f,  0.000f,  0.168f),
        math.float3(-0.300f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.015f,  0.300f),
        math.float3(-0.168f,  0.015f,  0.300f),
        math.float3(-0.300f,  0.015f,  0.168f),
        math.float3(-0.300f,  0.015f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3(-0.285f,  0.000f,  0.000f),
        math.float3(-0.285f,  0.000f, -0.160f),
        math.float3(-0.160f,  0.000f, -0.285f),
        math.float3( 0.000f,  0.000f, -0.285f),
        math.float3(-0.300f,  0.000f,  0.000f),
        math.float3(-0.300f,  0.000f, -0.168f),
        math.float3(-0.168f,  0.000f, -0.300f),
        math.float3( 0.000f,  0.000f, -0.300f),
        math.float3(-0.300f,  0.015f,  0.000f),
        math.float3(-0.300f,  0.015f, -0.168f),
        math.float3(-0.168f,  0.015f, -0.300f),
        math.float3( 0.000f,  0.015f, -0.300f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f, -0.285f),
        math.float3( 0.160f,  0.000f, -0.285f),
        math.float3( 0.285f,  0.000f, -0.160f),
        math.float3( 0.285f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.000f, -0.300f),
        math.float3( 0.168f,  0.000f, -0.300f),
        math.float3( 0.300f,  0.000f, -0.168f),
        math.float3( 0.300f,  0.000f,  0.000f),
        math.float3( 0.000f,  0.015f, -0.300f),
        math.float3( 0.168f,  0.015f, -0.300f),
        math.float3( 0.300f,  0.015f, -0.168f),
        math.float3( 0.300f,  0.015f,  0.000f)
    };
    }

} // namespace Metamesh

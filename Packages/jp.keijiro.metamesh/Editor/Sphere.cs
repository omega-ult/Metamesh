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
        public float Radius = 0.5f;
        public uint Columns = 24;
        public uint Rows = 12;

        protected override void GenerateMesh(Mesh mesh)
        {
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
    }

} // namespace Metamesh

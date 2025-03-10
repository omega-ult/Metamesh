using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Metamesh
{
    [System.Serializable]
    public sealed class Torus : PrimitiveBase
    {
        // 主半径 (环的中心到环面中心的距离)
        public float MajorRadius = 1;

        // 次半径 (环面的半径)
        public float MinorRadius = 0.25f;

        // 主圆周的分段数
        public int2 Segments = math.int2(32, 16);

        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            var segments = math.max(3, Segments);
            var majorSegments = segments.x;
            var minorSegments = segments.y;

            // 顶点和UV数组
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();

            // 生成顶点
            for (int i = 0; i <= minorSegments; i++)
            {
                float v = (float)i / minorSegments;
                float minorAngle = v * math.PI * 2;
                float cosMinor = math.cos(minorAngle);
                float sinMinor = math.sin(minorAngle);

                for (int j = 0; j <= majorSegments; j++)
                {
                    float u = (float)j / majorSegments;
                    float majorAngle = u * math.PI * 2;
                    float cosMajor = math.cos(majorAngle);
                    float sinMajor = math.sin(majorAngle);

                    // 计算顶点位置
                    float x = (MajorRadius + MinorRadius * cosMinor) * cosMajor;
                    float y = MinorRadius * sinMinor;
                    float z = (MajorRadius + MinorRadius * cosMinor) * sinMajor;

                    // 计算法线
                    float nx = cosMinor * cosMajor;
                    float ny = sinMinor;
                    float nz = cosMinor * sinMajor;

                    vertices.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(nx, ny, nz).normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }

            // 生成三角形索引
            int ringVertexCount = majorSegments + 1;
            for (int i = 0; i < minorSegments; i++)
            {
                int ringStart = i * ringVertexCount;
                int nextRingStart = (i + 1) * ringVertexCount;

                for (int j = 0; j < majorSegments; j++)
                {
                    indices.Add(ringStart + j);
                    indices.Add(nextRingStart + j);
                    indices.Add(ringStart + j + 1);

                    indices.Add(ringStart + j + 1);
                    indices.Add(nextRingStart + j);
                    indices.Add(nextRingStart + j + 1);
                }
            }

            // 设置网格数据
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }
    }
}
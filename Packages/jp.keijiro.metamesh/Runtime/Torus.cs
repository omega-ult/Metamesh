using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Metamesh
{
    [System.Serializable]
    public class Torus : PrimitiveBase
    {
        // 主半径 (环的中心到环面中心的距离)
        [ShapeOnly]
        public float MajorRadius = 1;

        // 次半径 (环面的半径)
        [ShapeOnly]
        public float MinorRadius = 0.25f;

        // 主圆周的分段数
        [TopologyAffecting]
        public Vector2Int Segments = new (32, 16);

        // 缓存的环形结构数据
        private int _cachedMajorSegments;
        private int _cachedMinorSegments;

        protected override int CalculateTopologyHash()
        {
            // 只有分段数影响拓扑
            return Segments.GetHashCode();
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            int2 mathSeg = math.int2(Segments.x, Segments.y); ;
            var segments = math.max(3, mathSeg);
            var majorSegments = segments.x;
            var minorSegments = segments.y;

            // 缓存分段数
            _cachedMajorSegments = majorSegments;
            _cachedMinorSegments = minorSegments;

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

        protected override void ReshapeMesh()
        {
            // 确保分段数没有变化
            int2 mathSeg = math.int2(Segments.x, Segments.y); ;
            var segments = math.max(3, mathSeg);
            var majorSegments = segments.x;
            var minorSegments = segments.y;
            
            if (majorSegments != _cachedMajorSegments || minorSegments != _cachedMinorSegments)
            {
                // 分段数变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }

            // 只更新顶点位置和法线，不改变拓扑结构
            Vector3[] vertices = CachedMesh.vertices;
            Vector3[] normals = CachedMesh.normals;


            int vertexIndex = 0;
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

                    vertices[vertexIndex].x = x;
                    vertices[vertexIndex].y = y;
                    vertices[vertexIndex].z = z;

                    normals[vertexIndex].x = nx;
                    normals[vertexIndex].y = ny;
                    normals[vertexIndex].z = nz;

                    vertexIndex++;
                }
            }
            CachedMesh.vertices = vertices;
            CachedMesh.normals = normals;
        }
    }
}
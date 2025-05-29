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
        
        // 起始角度 (0-360度)
        [TopologyAffecting]
        [Range(0, 360)]
        public float StartAngle = 0;
        
        // 结束角度 (0-360度)
        [TopologyAffecting]
        [Range(0, 360)]
        public float EndAngle = 360;
        
        // 是否封闭端点
        [TopologyAffecting]
        public bool CapEnds = true;
        
        // 轴向
        [TopologyAffecting]
        public Axis Axis = Axis.Y;

        // 缓存的环形结构数据
        private int _cachedMajorSegments;
        private int _cachedMinorSegments;
        private float _cachedStartAngle;
        private float _cachedEndAngle;
        private bool _cachedCapEnds;
        private Axis _cachedAxis;
        
        // 缓存端点顶点的起始索引
        private int _startCapCenterIndex;
        private int _endCapCenterIndex;

        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，包含所有影响拓扑的参数
            int hash = Segments.GetHashCode();
            hash = hash * 23 + StartAngle.GetHashCode();
            hash = hash * 23 + EndAngle.GetHashCode();
            hash = hash * 23 + CapEnds.GetHashCode();
            hash = hash * 23 + Axis.GetHashCode();
            return hash;
        }

        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            int2 mathSeg = math.int2(Segments.x, Segments.y);
            var segments = math.max(3, mathSeg);
            var majorSegments = segments.x;
            var minorSegments = segments.y;

            // 缓存分段数和角度设置
            _cachedMajorSegments = majorSegments;
            _cachedMinorSegments = minorSegments;
            _cachedStartAngle = StartAngle;
            _cachedEndAngle = EndAngle;
            _cachedCapEnds = CapEnds;
            _cachedAxis = Axis;

            // 确保起始角度小于结束角度
            float startAngle = StartAngle;
            float endAngle = EndAngle;
            if (startAngle > endAngle)
            {
                float temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }
            
            // 转换为弧度
            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;
            float angleRange = endRad - startRad;
            
            // 是否是完整圆环
            bool isFullTorus = Mathf.Approximately(angleRange, 2 * Mathf.PI) || angleRange >= 2 * Mathf.PI;

            // 顶点和UV数组
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();

            // 轴向选择
            var X = float3.zero;
            var Y = float3.zero;
            var Z = float3.zero;
            var ai = (int)Axis;
            X[(ai + 1) % 3] = 1;
            Y[(ai) % 3] = 1;
            Z[(ai + 2) % 3] = 1;

            // 生成顶点
            for (int i = 0; i <= minorSegments; i++)
            {
                float v = (float)i / minorSegments;
                float minorAngle = v * math.PI * 2;
                float cosMinor = math.cos(minorAngle);
                float sinMinor = math.sin(minorAngle);

                // 主圆周分段数调整 - 如果不是完整圆环，需要额外的顶点用于端点
                int actualMajorSegments = isFullTorus ? majorSegments : majorSegments + (CapEnds ? 0 : 1);
                
                for (int j = 0; j <= actualMajorSegments; j++)
                {
                    float u = (float)j / actualMajorSegments;
                    float majorAngle;
                    
                    if (isFullTorus)
                    {
                        majorAngle = u * math.PI * 2;
                    }
                    else
                    {
                        majorAngle = startRad + u * angleRange;
                    }
                    
                    float cosMajor = math.cos(majorAngle);
                    float sinMajor = math.sin(majorAngle);

                    // 计算顶点位置 (根据轴向调整)
                    float3 position = float3.zero;
                    float3 normal = float3.zero;
                    
                    // 基于轴向计算位置
                    float xPos = (MajorRadius + MinorRadius * cosMinor) * cosMajor;
                    float yPos = MinorRadius * sinMinor;
                    float zPos = (MajorRadius + MinorRadius * cosMinor) * sinMajor;
                    
                    if (Axis == Axis.Y) // Y轴向上
                    {
                        position = new float3(xPos, yPos, zPos);
                        normal = new float3(cosMinor * cosMajor, sinMinor, cosMinor * sinMajor);
                    }
                    else if (Axis == Axis.X) // X轴向上
                    {
                        position = new float3(yPos, xPos, zPos);
                        normal = new float3(sinMinor, cosMinor * cosMajor, cosMinor * sinMajor);
                    }
                    else // Z轴向上
                    {
                        position = new float3(xPos, zPos, yPos);
                        normal = new float3(cosMinor * cosMajor, cosMinor * sinMajor, sinMinor);
                    }

                    vertices.Add((Vector3)position);
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, v));
                }
            }

            // 生成三角形索引
            int ringVertexCount = (isFullTorus ? majorSegments : majorSegments + (CapEnds ? 0 : 1)) + 1;
            for (int i = 0; i < minorSegments; i++)
            {
                int ringStart = i * ringVertexCount;
                int nextRingStart = (i + 1) * ringVertexCount;

                for (int j = 0; j < ringVertexCount - 1; j++)
                {
                    indices.Add(ringStart + j);
                    indices.Add(nextRingStart + j);
                    indices.Add(ringStart + j + 1);

                    indices.Add(ringStart + j + 1);
                    indices.Add(nextRingStart + j);
                    indices.Add(nextRingStart + j + 1);
                }
            }
            
            // 如果不是完整圆环且需要封闭端点，添加端点三角形
            if (!isFullTorus && CapEnds)
            {
                // 记录端点中心顶点的索引
                _startCapCenterIndex = vertices.Count;
                
                // 添加起始端点的三角形
                AddCapTriangles(vertices, normals, uvs, indices, startRad, true);
                
                // 记录结束端点中心顶点的索引
                _endCapCenterIndex = vertices.Count;
                
                // 添加结束端点的三角形
                AddCapTriangles(vertices, normals, uvs, indices, endRad, false);
            }
            else
            {
                // 如果没有端点，设置为-1表示不存在
                _startCapCenterIndex = -1;
                _endCapCenterIndex = -1;
            }

            // 设置网格数据
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }
        
        // 添加端点封闭的三角形
        private void AddCapTriangles(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, float angle, bool isStart)
        {
            float cosMajor = math.cos(angle);
            float sinMajor = math.sin(angle);
            
            // 中心点索引
            int centerIndex = vertices.Count;
            
            // 添加中心点
            float3 centerPos = float3.zero;
            if (Axis == Axis.Y)
            {
                centerPos = new float3(MajorRadius * cosMajor, 0, MajorRadius * sinMajor);
            }
            else if (Axis == Axis.X)
            {
                centerPos = new float3(0, MajorRadius * cosMajor, MajorRadius * sinMajor);
            }
            else // Z轴向上
            {
                centerPos = new float3(MajorRadius * cosMajor, MajorRadius * sinMajor, 0);
            }
            
            // 计算法线方向 (指向端点外侧)
            float3 normal = float3.zero;
            if (Axis == Axis.Y)
            {
                normal = new float3(isStart ? -sinMajor : sinMajor, 0, isStart ? cosMajor : -cosMajor);
            }
            else if (Axis == Axis.X)
            {
                normal = new float3(0, isStart ? -sinMajor : sinMajor, isStart ? cosMajor : -cosMajor);
            }
            else // Z轴向上
            {
                normal = new float3(isStart ? -sinMajor : sinMajor, isStart ? cosMajor : -cosMajor, 0);
            }
            
            vertices.Add((Vector3)centerPos);
            normals.Add((Vector3)normal);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // 获取端点的顶点索引
            int ringVertexCount = _cachedMajorSegments + 1 + (CapEnds ? 0 : 1);
            int startVertex = isStart ? 0 : _cachedMajorSegments;
            
            // 添加三角形 - 连接中心点和圆周上的点
            for (int i = 0; i < _cachedMinorSegments; i++)
            {
                int current = startVertex + i * ringVertexCount;
                int next = startVertex + (i + 1) * ringVertexCount;
                
                if (isStart)
                {
                    indices.Add(centerIndex);
                    indices.Add(next);
                    indices.Add(current);
                }
                else
                {
                    indices.Add(centerIndex);
                    indices.Add(current);
                    indices.Add(next);
                }
            }
        }

        protected override void ReshapeMesh()
        {
            // 确保分段数和角度设置没有变化
            int2 mathSeg = math.int2(Segments.x, Segments.y);
            var segments = math.max(3, mathSeg);
            var majorSegments = segments.x;
            var minorSegments = segments.y;
            
            if (majorSegments != _cachedMajorSegments || 
                minorSegments != _cachedMinorSegments ||
                !Mathf.Approximately(StartAngle, _cachedStartAngle) ||
                !Mathf.Approximately(EndAngle, _cachedEndAngle) ||
                CapEnds != _cachedCapEnds ||
                Axis != _cachedAxis)
            {
                // 拓扑参数变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }

            // 确保起始角度小于结束角度
            float startAngle = StartAngle;
            float endAngle = EndAngle;
            if (startAngle > endAngle)
            {
                float temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }
            
            // 转换为弧度
            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;
            float angleRange = endRad - startRad;
            
            // 是否是完整圆环
            bool isFullTorus = Mathf.Approximately(angleRange, 2 * Mathf.PI) || angleRange >= 2 * Mathf.PI;

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

                // 主圆周分段数调整
                int actualMajorSegments = isFullTorus ? majorSegments : majorSegments + (CapEnds ? 0 : 1);
                
                for (int j = 0; j <= actualMajorSegments; j++)
                {
                    float u = (float)j / actualMajorSegments;
                    float majorAngle;
                    
                    if (isFullTorus)
                    {
                        majorAngle = u * math.PI * 2;
                    }
                    else
                    {
                        majorAngle = startRad + u * angleRange;
                    }
                    
                    float cosMajor = math.cos(majorAngle);
                    float sinMajor = math.sin(majorAngle);

                    // 计算顶点位置 (根据轴向调整)
                    float3 position = float3.zero;
                    float3 normal = float3.zero;
                    
                    // 基于轴向计算位置
                    float xPos = (MajorRadius + MinorRadius * cosMinor) * cosMajor;
                    float yPos = MinorRadius * sinMinor;
                    float zPos = (MajorRadius + MinorRadius * cosMinor) * sinMajor;
                    
                    if (Axis == Axis.Y) // Y轴向上
                    {
                        position = new float3(xPos, yPos, zPos);
                        normal = new float3(cosMinor * cosMajor, sinMinor, cosMinor * sinMajor);
                    }
                    else if (Axis == Axis.X) // X轴向上
                    {
                        position = new float3(yPos, xPos, zPos);
                        normal = new float3(sinMinor, cosMinor * cosMajor, cosMinor * sinMajor);
                    }
                    else // Z轴向上
                    {
                        position = new float3(xPos, zPos, yPos);
                        normal = new float3(cosMinor * cosMajor, cosMinor * sinMajor, sinMinor);
                    }

                    vertices[vertexIndex] = (Vector3)position;
                    normals[vertexIndex] = (Vector3)normal;
                    vertexIndex++;
                }
            }
            
            // 如果有端点封闭，更新端点的顶点
            if (!isFullTorus && CapEnds && _startCapCenterIndex >= 0 && _endCapCenterIndex >= 0)
            {
                // 更新起始端点中心点
                UpdateCapCenter(vertices, normals, startRad, true, _startCapCenterIndex);
                
                // 更新结束端点中心点
                UpdateCapCenter(vertices, normals, endRad, false, _endCapCenterIndex);
            }
            
            CachedMesh.vertices = vertices;
            CachedMesh.normals = normals;
        }
        
        // 更新端点中心点的位置和法线
        private void UpdateCapCenter(Vector3[] vertices, Vector3[] normals, float angle, bool isStart, int centerIndex)
        {
            float cosMajor = math.cos(angle);
            float sinMajor = math.sin(angle);
            
            // 更新中心点位置
            float3 centerPos = float3.zero;
            if (Axis == Axis.Y)
            {
                centerPos = new float3(MajorRadius * cosMajor, 0, MajorRadius * sinMajor);
            }
            else if (Axis == Axis.X)
            {
                centerPos = new float3(0, MajorRadius * cosMajor, MajorRadius * sinMajor);
            }
            else // Z轴向上
            {
                centerPos = new float3(MajorRadius * cosMajor, MajorRadius * sinMajor, 0);
            }
            
            // 计算法线方向 (指向端点外侧)
            float3 normal = float3.zero;
            if (Axis == Axis.Y)
            {
                normal = new float3(isStart ? -sinMajor : sinMajor, 0, isStart ? cosMajor : -cosMajor);
            }
            else if (Axis == Axis.X)
            {
                normal = new float3(0, isStart ? -sinMajor : sinMajor, isStart ? cosMajor : -cosMajor);
            }
            else // Z轴向上
            {
                normal = new float3(isStart ? -sinMajor : sinMajor, isStart ? cosMajor : -cosMajor, 0);
            }
            
            // 更新顶点和法线
            vertices[centerIndex] = (Vector3)centerPos;
            normals[centerIndex] = (Vector3)normal;
        }
    }
}
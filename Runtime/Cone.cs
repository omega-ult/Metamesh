using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Metamesh
{
    [System.Serializable]
    public class Cone : PrimitiveBase
    {
        // 底部半径
        [ShapeOnly]
        public float Radius = 0.5f;
        
        // 高度
        [ShapeOnly]
        public float Height = 1.0f;
        
        // 圆周分段数
        [TopologyAffecting]
        public int Segments = 32;
        
        // 是否生成底部
        [TopologyAffecting]
        public bool GenerateBase = true;
        
        // 是否生成UV坐标
        [TopologyAffecting]
        public bool GenerateUV = true;
        
        // 缓存的拓扑结构数据
        private int _cachedSegments;
        private bool _cachedGenerateBase;
        private bool _cachedGenerateUV;
        
        protected override int CalculateTopologyHash()
        {
            // 计算拓扑哈希值，只考虑影响拓扑的参数
            int hash = Segments.GetHashCode();
            hash = hash * 23 + GenerateBase.GetHashCode();
            hash = hash * 23 + GenerateUV.GetHashCode();
            return hash;
        }
        
        // 重写基类的抽象方法
        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            var segments = math.max(3, Segments);
            
            // 缓存拓扑参数
            _cachedSegments = segments;
            _cachedGenerateBase = GenerateBase;
            _cachedGenerateUV = GenerateUV;
            
            // 顶点和UV数组
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            
            // 添加顶点（圆锥顶点）
            var topPoint = new Vector3(0, Height, 0);
            vertices.Add(topPoint);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // 生成底部圆周顶点
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * math.PI * i / segments;
                float x = math.cos(angle) * Radius;
                float z = math.sin(angle) * Radius;
                
                Vector3 vertexPosition = new Vector3(x, 0, z);
                vertices.Add(vertexPosition);
                
                // 计算侧面法线 - 修改为平滑法线，垂直于表面
                // 计算从顶点到底部边缘点的方向向量
                Vector3 edgeDir = (vertexPosition - topPoint).normalized;
                // 计算底部圆周的切线方向
                Vector3 tangent = new Vector3(-z, 0, x).normalized;
                // 计算垂直于圆锥侧面的法线
                Vector3 sideNormal = Vector3.Cross(tangent, edgeDir).normalized;
                normals.Add(sideNormal);
                
                // UV坐标
                if (GenerateUV)
                {
                    float u = (float)i / segments;
                    uvs.Add(new Vector2(u, 0));
                }
                else
                {
                    uvs.Add(Vector2.zero);
                }
            }
            
            // 生成侧面三角形 - 修改索引顺序使面朝外
            for (int i = 0; i < segments; i++)
            {
                indices.Add(0); // 顶点
                indices.Add(i + 2);
                indices.Add(i + 1);
            }
            
            // 如果需要生成底部
            if (GenerateBase)
            {
                // 添加底部中心点 - 使用统一的底面法线
                int centerIndex = vertices.Count;
                vertices.Add(new Vector3(0, 0, 0));
                normals.Add(Vector3.down); // 底面法线统一朝下
                uvs.Add(new Vector2(0.5f, 0.5f));
                
                // 添加底部圆周顶点 - 使用统一的底面法线
                int baseStartIndex = vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = 2 * math.PI * i / segments;
                    float x = math.cos(angle) * Radius;
                    float z = math.sin(angle) * Radius;
                    
                    vertices.Add(new Vector3(x, 0, z));
                    normals.Add(Vector3.down); // 底面法线统一朝下
                    
                    if (GenerateUV)
                    {
                        float u = 0.5f + 0.5f * math.cos(angle);
                        float v = 0.5f + 0.5f * math.sin(angle);
                        uvs.Add(new Vector2(u, v));
                    }
                    else
                    {
                        uvs.Add(Vector2.zero);
                    }
                }
                
                // 生成底部三角形 - 保持索引顺序使底面朝外(朝下)
                for (int i = 0; i < segments; i++)
                {
                    indices.Add(centerIndex);
                    indices.Add(baseStartIndex + i);
                    indices.Add(baseStartIndex + i + 1);
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
            // 检查拓扑参数是否变化
            var segments = math.max(3, Segments);
            if (segments != _cachedSegments || 
                GenerateBase != _cachedGenerateBase || 
                GenerateUV != _cachedGenerateUV)
            {
                // 拓扑结构变化，需要重新生成整个网格
                GenerateMesh(CachedMesh);
                return;
            }
            
            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            
            // 更新顶点位置
            // 更新顶点（圆锥顶点）
            vertices[0].x = 0;
            vertices[0].y = Height;
            vertices[0].z = 0;
            
            // 更新底部圆周顶点
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * math.PI * i / segments;
                float x = math.cos(angle) * Radius;
                float z = math.sin(angle) * Radius;
                
                int index = i + 1; // 索引从1开始，因为0是顶点
                vertices[index].x = x;
                vertices[index].y = 0;
                vertices[index].z = z;
            }
            
            // 如果有底部
            if (GenerateBase)
            {
                // 底部中心点位置不变，只需更新底部圆周顶点
                int centerIndex = segments + 2; // 顶点 + 底部圆周顶点数量 + 1
                
                // 更新底部圆周顶点
                for (int i = 0; i <= segments; i++)
                {
                    float angle = 2 * math.PI * i / segments;
                    float x = math.cos(angle) * Radius;
                    float z = math.sin(angle) * Radius;
                    
                    int index = centerIndex + i + 1; // 中心点索引 + 1 + i
                    if (index < vertices.Length)
                    {
                        vertices[index].x = x;
                        vertices[index].y = 0;
                        vertices[index].z = z;
                    }
                }
            }
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
            
            // 更新法线
            Vector3[] normals = CachedMesh.normals;
            
            // 更新侧面法线
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * math.PI * i / segments;
                float x = math.cos(angle) * Radius;
                float z = math.sin(angle) * Radius;
                
                Vector3 vertexPosition = new Vector3(x, 0, z);
                Vector3 topPoint = new Vector3(0, Height, 0);
                
                // 计算从顶点到底部边缘点的方向向量
                Vector3 edgeDir = (vertexPosition - topPoint).normalized;
                // 计算底部圆周的切线方向
                Vector3 tangent = new Vector3(-z, 0, x).normalized;
                // 计算垂直于圆锥侧面的法线
                Vector3 sideNormal = Vector3.Cross(tangent, edgeDir).normalized;
                
                int index = i + 1; // 索引从1开始，因为0是顶点
                normals[index] = sideNormal;
            }
            
            // 更新网格法线
            CachedMesh.normals = normals;
        }
    }
}
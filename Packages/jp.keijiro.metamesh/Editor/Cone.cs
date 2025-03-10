using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Metamesh
{
    [System.Serializable]
    public sealed class Cone : PrimitiveBase
    {
        // 底部半径
        public float Radius = 0.5f;
        
        // 高度
        public float Height = 1.0f;
        
        // 圆周分段数
        public int Segments = 32;
        
        // 是否生成底部
        public bool GenerateBase = true;
        
        // 是否生成UV坐标
        public bool GenerateUV = true;
        
        // 重写基类的抽象方法
        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            var segments = math.max(3, Segments);
            
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
    }
}
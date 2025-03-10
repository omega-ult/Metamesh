using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Metamesh
{
    [System.Serializable]
    public sealed class Capsule : PrimitiveBase

    {
        // 胶囊体半径
        public float Radius = 0.5f;
        
        // 胶囊体高度（包括两个半球）
        public float Height = 2.0f;
        
        // 方向轴
        public Axis Direction = Axis.Y;
        
        // 圆周分段数
        public int RadialSegments = 24;
        
        // 高度分段数（仅圆柱部分）
        public int HeightSegments = 1;
        
        // 半球分段数
        public int CapSegments = 8;
        
        protected override void GenerateMesh(Mesh mesh)
        {
            // 参数验证
            var radialSegments = math.max(3, RadialSegments);
            var heightSegments = math.max(1, HeightSegments);
            var capSegments = math.max(1, CapSegments);
            
            // 计算圆柱体部分的高度
            float cylinderHeight = Height - 2 * Radius;
            if (cylinderHeight < 0) cylinderHeight = 0;
            
            // 顶点和UV数组
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();
            
            // 生成顶部半球
            GenerateHemisphere(vertices, normals, uvs, indices, Radius, capSegments, radialSegments, cylinderHeight / 2, true);
            
            // 生成圆柱体部分
            if (cylinderHeight > 0)
            {
                GenerateCylinder(vertices, normals, uvs, indices, Radius, cylinderHeight, heightSegments, radialSegments);
            }
            
            // 生成底部半球
            GenerateHemisphere(vertices, normals, uvs, indices, Radius, capSegments, radialSegments, -cylinderHeight / 2, false);
            
            // 根据方向轴旋转顶点
            RotateVertices(vertices, normals);
            
            // 设置网格数据
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }
        
        private void GenerateHemisphere(
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices,
            float radius, int segments, int radialSegments, float yOffset, bool top)
        {
            int startIndex = vertices.Count;
            
            // 添加顶点
            for (int y = 0; y <= segments; y++)
            {
                float yAngle = math.PI * 0.5f * y / segments * (top ? 1 : -1);
                float yPos = radius * math.sin(yAngle);
                float radiusAtHeight = radius * math.cos(yAngle);
                
                for (int x = 0; x <= radialSegments; x++)
                {
                    float xAngle = 2 * math.PI * x / radialSegments;
                    
                    // 计算顶点位置
                    float xPos = radiusAtHeight * math.sin(xAngle);
                    float zPos = radiusAtHeight * math.cos(xAngle);
                    
                    Vector3 vertex = new Vector3(xPos, yPos + yOffset, zPos);
                    Vector3 normal = new Vector3(xPos, yPos, zPos).normalized;
                    
                    vertices.Add(vertex);
                    normals.Add(normal);
                    
                    // UV坐标
                    float u = (float)x / radialSegments;
                    float v = top ? 
                        1.0f - (float)y / segments * 0.5f : 
                        0.5f - (float)y / segments * 0.5f;
                    uvs.Add(new Vector2(u, v));
                }
            }
            
            // 添加三角形
            int rings = segments + 1;
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int current = startIndex + y * (radialSegments + 1) + x;
                    int next = current + 1;
                    int nextRow = current + (radialSegments + 1);
                    int nextRowNext = nextRow + 1;
                    
                    // 添加两个三角形
                    if (top)
                    {
                        indices.Add(current);
                        indices.Add(nextRow);
                        indices.Add(next);
                        
                        indices.Add(next);
                        indices.Add(nextRow);
                        indices.Add(nextRowNext);
                    }
                    else
                    {
                        indices.Add(current);
                        indices.Add(next);
                        indices.Add(nextRow);
                        
                        indices.Add(next);
                        indices.Add(nextRowNext);
                        indices.Add(nextRow);
                    }
                }
            }
        }
        
        private void GenerateCylinder(
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices,
            float radius, float height, int heightSegments, int radialSegments)
        {
            int startIndex = vertices.Count;
            float halfHeight = height * 0.5f;
            
            // 添加顶点
            for (int y = 0; y <= heightSegments; y++)
            {
                float yPos = -halfHeight + height * y / heightSegments;
                float v = 0.5f + yPos / height;
                
                for (int x = 0; x <= radialSegments; x++)
                {
                    float xAngle = 2 * math.PI * x / radialSegments;
                    float xPos = radius * math.sin(xAngle);
                    float zPos = radius * math.cos(xAngle);
                    
                    Vector3 vertex = new Vector3(xPos, yPos, zPos);
                    Vector3 normal = new Vector3(xPos, 0, zPos).normalized;
                    
                    vertices.Add(vertex);
                    normals.Add(normal);
                    
                    // UV坐标
                    float u = (float)x / radialSegments;
                    uvs.Add(new Vector2(u, v));
                }
            }
            
            // 添加三角形
            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int current = startIndex + y * (radialSegments + 1) + x;
                    int next = current + 1;
                    int nextRow = current + (radialSegments + 1);
                    int nextRowNext = nextRow + 1;
                    
                    // 添加两个三角形
                    indices.Add(current);
                    indices.Add(next);
                    indices.Add(nextRow);
                    
                    indices.Add(next);
                    indices.Add(nextRowNext);
                    indices.Add(nextRow);
                }
            }
        }
        
        private void RotateVertices(List<Vector3> vertices, List<Vector3> normals)
        {
            if (Direction == Axis.Y) return; // Y轴是默认方向，不需要旋转
            
            Quaternion rotation;
            
            if (Direction == Axis.X)
            {
                rotation = Quaternion.Euler(0, 0, 90);
            }
            else // Axis.Z
            {
                rotation = Quaternion.Euler(90, 0, 0);
            }
            
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = rotation * vertices[i];
                normals[i] = rotation * normals[i];
            }
        }
    }
}
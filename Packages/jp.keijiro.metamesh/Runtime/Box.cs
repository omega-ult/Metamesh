using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace Metamesh
{
    [System.Serializable]
    public sealed class Box : PrimitiveBase
    {
        [ShapeOnly]
        public float Width = 1;
        
        [ShapeOnly]
        public float Height = 1;
        
        [ShapeOnly]
        public float Depth = 1;

        // 重写基类的抽象方法
        protected override void GenerateMesh(Mesh mesh)
        {
            var w = Width / 2;
            var h = Height / 2;
            var d = Depth / 2;

            var vertices = new Vector3[]
            {
                // 前面
                new Vector3(-w, -h, d), new Vector3(w, -h, d), new Vector3(w, h, d), new Vector3(-w, h, d),
                // 后面
                new Vector3(w, -h, -d), new Vector3(-w, -h, -d), new Vector3(-w, h, -d), new Vector3(w, h, -d),
                // 上面
                new Vector3(-w, h, d), new Vector3(w, h, d), new Vector3(w, h, -d), new Vector3(-w, h, -d),
                // 下面
                new Vector3(-w, -h, -d), new Vector3(w, -h, -d), new Vector3(w, -h, d), new Vector3(-w, -h, d),
                // 右面
                new Vector3(w, -h, d), new Vector3(w, -h, -d), new Vector3(w, h, -d), new Vector3(w, h, d),
                // 左面
                new Vector3(-w, -h, -d), new Vector3(-w, -h, d), new Vector3(-w, h, d), new Vector3(-w, h, -d)
            };

            var normals = new Vector3[]
            {
                // 前面
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                // 后面
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                // 上面
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                // 下面
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                // 右面
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                // 左面
                Vector3.left, Vector3.left, Vector3.left, Vector3.left
            };

            var uvs = new Vector2[]
            {
                // 前面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // 后面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // 上面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // 下面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // 右面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                // 左面
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };

            var indices = new int[]
            {
                // 前面
                0, 2, 1, 0, 3, 2,
                // 后面
                4, 6, 5, 4, 7, 6,
                // 上面
                8, 10, 9, 8, 11, 10,
                // 下面
                12, 14, 13, 12, 15, 14,
                // 右面
                16, 18, 17, 16, 19, 18,
                // 左面
                20, 22, 21, 20, 23, 22
            };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }
        
        // 计算拓扑哈希值 - 对于Box来说，拓扑结构是固定的
        protected override int CalculateTopologyHash()
        {
            // Box的拓扑结构是固定的，返回一个常量值
            return 1;
        }
        
        // 实现ReshapeMesh方法，只更新顶点位置
        protected override void ReshapeMesh()
        {
            var w = Width / 2;
            var h = Height / 2;
            var d = Depth / 2;
            
            // 获取现有顶点数组
            Vector3[] vertices = CachedMesh.vertices;
            
            // 前面
            vertices[0].x = -w; vertices[0].y = -h; vertices[0].z = d;
            vertices[1].x = w;  vertices[1].y = -h; vertices[1].z = d;
            vertices[2].x = w;  vertices[2].y = h;  vertices[2].z = d;
            vertices[3].x = -w; vertices[3].y = h;  vertices[3].z = d;
            
            // 后面
            vertices[4].x = w;  vertices[4].y = -h; vertices[4].z = -d;
            vertices[5].x = -w; vertices[5].y = -h; vertices[5].z = -d;
            vertices[6].x = -w; vertices[6].y = h;  vertices[6].z = -d;
            vertices[7].x = w;  vertices[7].y = h;  vertices[7].z = -d;
            
            // 上面
            vertices[8].x = -w;  vertices[8].y = h;  vertices[8].z = d;
            vertices[9].x = w;   vertices[9].y = h;  vertices[9].z = d;
            vertices[10].x = w;  vertices[10].y = h; vertices[10].z = -d;
            vertices[11].x = -w; vertices[11].y = h; vertices[11].z = -d;
            
            // 下面
            vertices[12].x = -w; vertices[12].y = -h; vertices[12].z = -d;
            vertices[13].x = w;  vertices[13].y = -h; vertices[13].z = -d;
            vertices[14].x = w;  vertices[14].y = -h; vertices[14].z = d;
            vertices[15].x = -w; vertices[15].y = -h; vertices[15].z = d;
            
            // 右面
            vertices[16].x = w; vertices[16].y = -h; vertices[16].z = d;
            vertices[17].x = w; vertices[17].y = -h; vertices[17].z = -d;
            vertices[18].x = w; vertices[18].y = h;  vertices[18].z = -d;
            vertices[19].x = w; vertices[19].y = h;  vertices[19].z = d;
            
            // 左面
            vertices[20].x = -w; vertices[20].y = -h; vertices[20].z = -d;
            vertices[21].x = -w; vertices[21].y = -h; vertices[21].z = d;
            vertices[22].x = -w; vertices[22].y = h;  vertices[22].z = d;
            vertices[23].x = -w; vertices[23].y = h;  vertices[23].z = -d;
            
            // 更新网格顶点
            CachedMesh.vertices = vertices;
        }
    }
} // namespace Metamesh

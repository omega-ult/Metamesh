using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace Metamesh
{

    [System.Serializable]
    public sealed class Box : PrimitiveBase
    {
        public float Width = 1;
        public float Height = 1;
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
    }

} // namespace Metamesh

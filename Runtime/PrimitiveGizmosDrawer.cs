using UnityEngine;

namespace Metamesh
{
    public static class PrimitiveGizmosDrawer
    {
        public static void DrawWireframe(PrimitiveBase primitive, Transform transform = null)
        {
            if (primitive == null) return;
            
            // 创建临时网格用于绘制
            var mesh = new Mesh();
            primitive.Generate(mesh);
            
            // 获取变换参数
            var position = transform != null ? transform.position : Vector3.zero;
            var rotation = transform != null ? transform.rotation : Quaternion.identity;
            var scale = transform != null ? transform.lossyScale : Vector3.one;
            
            // 绘制线框
            Gizmos.DrawWireMesh(mesh, position, rotation, scale);
            
            // 清理临时网格
            Object.DestroyImmediate(mesh);
        }
    }
}

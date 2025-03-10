using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Metamesh {

[ScriptedImporter(1, "metamesh")]
public sealed class MetameshImporter : ScriptedImporter
{
    #region ScriptedImporter implementation

    [SerializeField] Shape _shape = Shape.Box;
    [SerializeField] Plane _plane = null;
    [SerializeField] Box _box = new Box();
    [SerializeField] Sphere _sphere = new Sphere();
    [SerializeField] Icosphere _icosphere = new Icosphere();
    [SerializeField] Cylinder _cylinder = new Cylinder();
    [SerializeField] RoundedBox _roundedBox = new RoundedBox();
    [SerializeField] Ring _ring = new Ring();
    [SerializeField] Disc _disc = new Disc();
    [SerializeField] Teapot _teapot = new Teapot();
    [SerializeField] Triangle _triangle = new Triangle();
    [SerializeField] Torus _torus = new Torus(); 
    [SerializeField] Cone _cone = new Cone(); 
    [SerializeField] Capsule _capsule = new Capsule();
    [SerializeField] bool _generateLightmapUVs = false;
    [SerializeField] bool _readWriteMeshes = false;
    
    // 简化的锚点设置 - 分别为X、Y、Z轴设置
    [SerializeField] AxisPivotPosition _pivotX = AxisPivotPosition.Center;
    [SerializeField] AxisPivotPosition _pivotY = AxisPivotPosition.Center;
    [SerializeField] AxisPivotPosition _pivotZ = AxisPivotPosition.Center;
    [SerializeField] bool _normalizeSize = false; // 是否将模型缩放到标准大小

    public override void OnImportAsset(AssetImportContext context)
    {
        var gameObject = new GameObject();
        var mesh = ImportAsMesh(context.assetPath);

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var pipelineAsset = GraphicsSettings.currentRenderPipeline;
        var baseMaterial = pipelineAsset ? pipelineAsset.defaultMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
        
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = baseMaterial;

        context.AddObjectToAsset("prefab", gameObject);
        if (mesh != null) context.AddObjectToAsset("mesh", mesh);

        context.SetMainObject(gameObject);
    }

    #endregion

    #region Reader implementation

    // 设置各个基本体的锚点属性
    private void SetPrimitivesPivotProperties()
    {
        // 为每个继承自PrimitiveBase的基本体设置锚点属性
        SetPrimitiveBasePivot(_box as PrimitiveBase);
        SetPrimitiveBasePivot(_sphere as PrimitiveBase);
        SetPrimitiveBasePivot(_icosphere as PrimitiveBase);
        SetPrimitiveBasePivot(_cylinder as PrimitiveBase);
        SetPrimitiveBasePivot(_roundedBox as PrimitiveBase);
        SetPrimitiveBasePivot(_ring as PrimitiveBase);
        SetPrimitiveBasePivot(_disc as PrimitiveBase);
        SetPrimitiveBasePivot(_teapot as PrimitiveBase);
        SetPrimitiveBasePivot(_triangle as PrimitiveBase);
        SetPrimitiveBasePivot(_torus as PrimitiveBase);
        SetPrimitiveBasePivot(_cone as PrimitiveBase);
        SetPrimitiveBasePivot(_capsule as PrimitiveBase);
        SetPrimitiveBasePivot(_plane as PrimitiveBase);
    }
    
    // 为基本体设置锚点属性
    private void SetPrimitiveBasePivot(PrimitiveBase primitive)
    {
        if (primitive != null)
        {
            primitive.PivotX = _pivotX;
            primitive.PivotY = _pivotY;
            primitive.PivotZ = _pivotZ;
            primitive.NormalizeSize = _normalizeSize;
        }
    }

    Mesh ImportAsMesh(string path)
    {
        var mesh = new Mesh();
        mesh.name = "Mesh";

        // 设置各个基本体的锚点属性
        SetPrimitivesPivotProperties();

        switch (_shape)
        {
            case Shape.Plane      : _plane     .Generate(mesh); break;
            case Shape.Box        : _box       .Generate(mesh); break;
            case Shape.Sphere     : _sphere    .Generate(mesh); break;
            case Shape.Icosphere  : _icosphere .Generate(mesh); break;
            case Shape.Cylinder   : _cylinder  .Generate(mesh); break;
            case Shape.RoundedBox : _roundedBox.Generate(mesh); break;
            case Shape.Ring       : _ring      .Generate(mesh); break;
            case Shape.Disc       : _disc      .Generate(mesh); break;
            case Shape.Teapot     : _teapot    .Generate(mesh); break;
            case Shape.Triangle   : _triangle  .Generate(mesh); break;
            case Shape.Torus      : _torus     .Generate(mesh); break;
            case Shape.Cone       : _cone      .Generate(mesh); break;
            case Shape.Capsule    : _capsule   .Generate(mesh); break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if(_generateLightmapUVs) Unwrapping.GenerateSecondaryUVSet(mesh);
        mesh.UploadMeshData(!_readWriteMeshes);

        return mesh;
    }
    
    // 调整网格的锚点位置
    private void AdjustPivot(Mesh mesh)
    {
        // 如果所有轴都是中心点，不需要调整
        if (_pivotX == AxisPivotPosition.Center && 
            _pivotY == AxisPivotPosition.Center && 
            _pivotZ == AxisPivotPosition.Center) return;
        
        Vector3 offset = Vector3.zero;
        Bounds bounds = mesh.bounds;
        
        // 计算X轴偏移
        switch (_pivotX)
        {
            case AxisPivotPosition.Min:
                offset.x = bounds.min.x;
                break;
            case AxisPivotPosition.Max:
                offset.x = bounds.max.x;
                break;
            // Center不需要偏移
        }
        
        // 计算Y轴偏移
        switch (_pivotY)
        {
            case AxisPivotPosition.Min:
                offset.y = bounds.min.y;
                break;
            case AxisPivotPosition.Max:
                offset.y = bounds.max.y;
                break;
            // Center不需要偏移
        }
        
        // 计算Z轴偏移
        switch (_pivotZ)
        {
            case AxisPivotPosition.Min:
                offset.z = bounds.min.z;
                break;
            case AxisPivotPosition.Max:
                offset.z = bounds.max.z;
                break;
            // Center不需要偏移
        }
        
        // 应用偏移到所有顶点
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= offset;
        }
        mesh.vertices = vertices;
        
        // 更新网格边界
        mesh.RecalculateBounds();
        
        // 如果需要标准化大小
        if (_normalizeSize)
        {
            NormalizeSize(mesh);
        }
    }
    
    // 将网格缩放到标准大小
    private void NormalizeSize(Mesh mesh)
    {
        Bounds bounds = mesh.bounds;
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        
        if (maxDimension <= 0) return;
        
        float scale = 1.0f / maxDimension;
        
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] *= scale;
        }
        mesh.vertices = vertices;
        
        // 更新网格边界
        mesh.RecalculateBounds();
    }

    #endregion
}

} // namespace Metamesh

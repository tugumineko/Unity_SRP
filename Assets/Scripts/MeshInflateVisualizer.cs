#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class MeshInflateVisualizer : MonoBehaviour
{
    //don't work
    
    [Header("Mesh Settings")]
    public Mesh sourceMesh;
    [Range(-0.5f, 0.5f)] public float inflationThickness = 0.1f;
    public bool autoUpdate = true;

    [Header("Visualization Settings")]
    public bool showNormals = true;
    [Range(0.01f, 1f)] public float normalLength = 0.1f;
    public Color normalColor = Color.red;
    public bool showOpenEdges = true;
    public Color openEdgeColor = Color.green;
    
    [Header("Generated Objects")]
    public GameObject inflatedMeshObject;
    
    // 网格数据缓存
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector2> _uv0 = new List<Vector2>();
    private List<int> _triangles = new List<int>();
    private Dictionary<int, int> _referenceVertexIdMap = new Dictionary<int, int>();
    private List<MeshInflateHelper.Edge> _edges = new List<MeshInflateHelper.Edge>();
    private List<Vector3> _inflationNormals = new List<Vector3>();
    
    // 用于检测变化的缓存
    private Mesh _lastSourceMesh;
    private float _lastInflationThickness;
    
    private void OnEnable()
    {
        // 确保在编辑模式注册更新
        #if UNITY_EDITOR
        EditorApplication.update += EditorUpdate;
        #endif
        
        // 初始化缓存
        _lastSourceMesh = sourceMesh;
        _lastInflationThickness = inflationThickness;
        
        // 初始更新
        if (sourceMesh != null)
        {
            UpdateMeshData();
        }
    }
    
    private void OnDisable()
    {
        // 清理资源
        #if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
        #endif
    }

    private void EditorUpdate()
    {
        // 只在编辑模式下运行
        if (!Application.isPlaying)
        {
            // 检查是否需要更新
            bool meshChanged = sourceMesh != _lastSourceMesh;
            bool thicknessChanged = Mathf.Abs(inflationThickness - _lastInflationThickness) > 0.001f;
            
            if (autoUpdate && (meshChanged || thicknessChanged))
            {
                UpdateMeshData();
                
                // 更新缓存
                _lastSourceMesh = sourceMesh;
                _lastInflationThickness = inflationThickness;
                
                // 强制重绘场景
                SceneView.RepaintAll();
            }
        }
    }
    
    private void OnValidate()
    {
        // 当inspector值改变时更新
        if (autoUpdate && sourceMesh != null)
        {
            UpdateMeshData();
            SceneView.RepaintAll();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (sourceMesh == null || _vertices.Count == 0) return;
        
        // 可视化开放边
        if (showOpenEdges && _edges != null)
        {
            Gizmos.color = openEdgeColor;
            foreach (var edge in _edges)
            {
                if (edge.if1 == null) // 只绘制边界边
                {
                    Vector3 v0 = transform.TransformPoint(_vertices[edge.ie0]);
                    Vector3 v1 = transform.TransformPoint(_vertices[edge.ie1]);
                    Gizmos.DrawLine(v0, v1);
                    Gizmos.DrawSphere(v0, 0.005f);
                    Gizmos.DrawSphere(v1, 0.005f);
                }
            }
        }
        
        // 可视化法线
        if (showNormals && _inflationNormals != null && _inflationNormals.Count == _vertices.Count)
        {
            Gizmos.color = normalColor;
            for (int i = 0; i < _vertices.Count; i++)
            {
                Vector3 pos = transform.TransformPoint(_vertices[i]);
                Vector3 normal = _inflationNormals[i];
                
                // 确保法线有有效长度
                if (normal.sqrMagnitude > 0.0001f)
                {
                    Vector3 normalEnd = pos + transform.TransformDirection(normal).normalized * normalLength;
                    Gizmos.DrawLine(pos, normalEnd);
                    Gizmos.DrawSphere(normalEnd, 0.002f);
                }
            }
        }
    }

    [ContextMenu("Update Mesh Data")]
    public void UpdateMeshData()
    {
        if (sourceMesh == null)
        {
            Debug.LogWarning("Source Mesh is not assigned. Please assign a mesh first.");
            return;
        }
        
        // 清空旧数据
        _vertices.Clear();
        _uv0.Clear();
        _triangles.Clear();
        _referenceVertexIdMap.Clear();
        _edges.Clear();
        _inflationNormals.Clear();
        
        // 获取网格数据
        MeshInflateHelper.CopyDataToLists(sourceMesh, ref _vertices, ref _uv0, ref _triangles);
        
        // 计算边和膨胀法线
        if (MeshInflateHelper.GetEdges(_vertices, _triangles, ref _referenceVertexIdMap, ref _edges, transform, "Mesh Visualizer"))
        {
            MeshInflateHelper.GenerateInflationNormals(
                _vertices, _triangles, _edges, _referenceVertexIdMap, 
                ref _inflationNormals, inflationThickness, transform, "Mesh Visualizer");
                
            Debug.Log($"Mesh data updated. Vertices: {_vertices.Count}, Edges: {_edges.Count}, Normals: {_inflationNormals.Count}");
        }
        else
        {
            Debug.LogError("Failed to calculate mesh edges. The mesh might be non-manifold.");
        }
    }

    [ContextMenu("Generate Inflated Mesh")]
    public void GenerateInflatedMesh()
    {
        if (sourceMesh == null) 
        {
            Debug.LogError("Cannot generate inflated mesh. Source mesh is not assigned.");
            return;
        }
        
        if (_inflationNormals.Count == 0 || _inflationNormals.Count != _vertices.Count) 
        {
            Debug.LogError("Inflation normals are not calculated correctly. Update mesh data first.");
            return;
        }
        
        // 创建新网格
        Mesh inflatedMesh = new Mesh();
        inflatedMesh.name = $"{sourceMesh.name}_Inflated";
        
        // 复制原始顶点并应用膨胀
        Vector3[] inflatedVertices = new Vector3[_vertices.Count];
        for (int i = 0; i < _vertices.Count; i++)
        {
            inflatedVertices[i] = _vertices[i] + _inflationNormals[i];
        }
        
        inflatedMesh.vertices = inflatedVertices;
        inflatedMesh.uv = _uv0.ToArray();
        inflatedMesh.triangles = _triangles.ToArray();
        inflatedMesh.RecalculateNormals();
        inflatedMesh.RecalculateBounds();
        inflatedMesh.RecalculateTangents();
        
        // 创建或更新游戏对象
        if (inflatedMeshObject != null)
        {
            // 在编辑模式下安全销毁
            if (Application.isPlaying)
            {
                Destroy(inflatedMeshObject);
            }
            else
            {
                DestroyImmediate(inflatedMeshObject);
            }
        }
        
        inflatedMeshObject = new GameObject($"{sourceMesh.name}_Inflated");
        inflatedMeshObject.transform.SetParent(transform);
        inflatedMeshObject.transform.localPosition = Vector3.zero;
        inflatedMeshObject.transform.localRotation = Quaternion.identity;
        inflatedMeshObject.transform.localScale = Vector3.one;
        
        // 添加网格组件
        MeshFilter meshFilter = inflatedMeshObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = inflatedMesh;
        
        MeshRenderer renderer = inflatedMeshObject.AddComponent<MeshRenderer>();
        
        // 尝试复制原始材质
        MeshRenderer originalRenderer = GetComponent<MeshRenderer>();
        if (originalRenderer != null)
        {
            renderer.sharedMaterials = originalRenderer.sharedMaterials;
        }
        else
        {
            // 创建默认材质
            Material defaultMaterial = new Material(Shader.Find("SRPLearn/AOSA"));
            defaultMaterial.color = new Color(0.8f, 0.8f, 1f, 1f); // 淡蓝色
            renderer.sharedMaterial = defaultMaterial;
        }
        
        // 确保网格在编辑器中可见
        #if UNITY_EDITOR
        EditorUtility.SetDirty(inflatedMeshObject);
        #endif
        
        Debug.Log($"Generated inflated mesh: {inflatedMeshObject.name}");
    }

    [ContextMenu("Clear Generated Mesh")]
    public void ClearGeneratedMesh()
    {
        if (inflatedMeshObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(inflatedMeshObject);
            }
            else
            {
                DestroyImmediate(inflatedMeshObject);
            }
            inflatedMeshObject = null;
        }
    }
}
#endif
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ShowInflatedMesh : MonoBehaviour
{
    [ContextMenu("Generate ShowUV3 Mesh")]
    public void GenerateShowUV3Mesh()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Missing MeshFilter or Mesh.");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        int vertexCount = originalMesh.vertexCount;

        // 1. 获取原始数据
        Vector3[] vertices = originalMesh.vertices;
        List<Vector3> uv3 = new List<Vector3>();
        originalMesh.GetUVs(3, uv3); 

        if (uv3.Count != vertexCount)
        {
            Debug.LogWarning("UV3 missing or length mismatch.");
            return;
        }

        // 2. 生成偏移后顶点
        Vector3[] inflatedVertices = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
            inflatedVertices[i] = vertices[i] + uv3[i];

        // 3. 克隆其他数据
        Mesh inflatedMesh = new Mesh();
        inflatedMesh.name = originalMesh.name + "_ShowUV3";
        inflatedMesh.vertices = inflatedVertices;
        inflatedMesh.triangles = originalMesh.triangles;
        inflatedMesh.normals = originalMesh.normals;
        inflatedMesh.uv = originalMesh.uv;
        inflatedMesh.uv2 = originalMesh.uv2;
        inflatedMesh.SetUVs(3, uv3); 

        inflatedMesh.RecalculateBounds();
        inflatedMesh.RecalculateNormals();

        // 4. 创建 GameObject，设置同级、同位置
        GameObject newObj = new GameObject(gameObject.name + "_showUV3");
        newObj.transform.SetParent(transform.parent, false);
        newObj.transform.position = transform.position;
        newObj.transform.rotation = transform.rotation;
        newObj.transform.localScale = transform.localScale;

        var mf = newObj.AddComponent<MeshFilter>();
        var mr = newObj.AddComponent<MeshRenderer>();

        mf.sharedMesh = inflatedMesh;
        mr.sharedMaterial = GetComponent<MeshRenderer>()?.sharedMaterial;
        
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
}
#endif

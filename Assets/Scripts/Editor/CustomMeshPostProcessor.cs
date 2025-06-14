using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class CustomMeshPostProcessor : AssetPostprocessor
{
    private void OnPostprocessModel(GameObject gameObject)
    {
        if (!assetPath.StartsWith("Assets/"))
        {
            return;
        }

        string[] path = assetPath.Split('/');
        ProcessGameObject(gameObject, context, path[path.Length - 1]);
    }

    private static void ProcessGameObject(GameObject gameObject, UnityEditor.AssetImporters.AssetImportContext context,
        string meshName)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv0 = new List<Vector2>();
        List<int> triangles = new List<int>();
        Dictionary<int,int> referenceVertexIdMap = new Dictionary<int,int>();
        List<MeshInflateHelper.Edge> edges = new List<MeshInflateHelper.Edge>();
        List<Vector3> inflationNormals = new List<Vector3>();

        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < meshFilters.Length; i++)
        {
            GenerateInflationNormals(ref vertices, ref uv0, ref triangles, ref referenceVertexIdMap, ref edges, ref inflationNormals, gameObject, meshFilters[i].sharedMesh, context, meshName);
        }
    }
    
    private static void GenerateInflationNormals(ref List<Vector3> vertices,
        ref List<Vector2> uv0,
        ref List<int> triangles,
        ref Dictionary<int, int> referenceVertexIdMap,
        ref List<MeshInflateHelper.Edge> edges,
        ref List<Vector3> inflationNormals,
        GameObject gameObject,
        Mesh mesh,
        UnityEditor.AssetImporters.AssetImportContext context,
        string meshName)
    {
        string editorTitle = "Importing Mesh \"" + meshName + "\"";
        mesh.CopyDataToLists(ref vertices, ref uv0, ref triangles);

        if (!MeshInflateHelper.GetEdges(vertices, triangles, ref referenceVertexIdMap, ref edges, null, editorTitle))
        {
            Debug.LogError(string.Format("GenerateInflationNormals: Error while generating edges for {0}.",mesh.name));
            return;
        }
        
        //这里的thickness是封边厚度，后续还要进行法线膨胀过程
        MeshInflateHelper.MeshType meshType = MeshInflateHelper.TryGenerateClosedMeshFromOpenMesh(ref vertices, ref uv0, ref triangles, edges, referenceVertexIdMap, -0.001f, null, editorTitle);
        
        if (meshType == MeshInflateHelper.MeshType.Closed)
        {
            //膨胀法线
            MeshInflateHelper.GenerateInflationNormals(vertices, triangles, edges, referenceVertexIdMap, ref inflationNormals, 1.0f, null, editorTitle);
            
            mesh.SetUVs(3, inflationNormals);
        }

        else
        {
            CreateNewInflationMesh(ref vertices, ref uv0, ref triangles, ref referenceVertexIdMap, ref edges,
                ref inflationNormals, gameObject, mesh, context, editorTitle);
        }
        
    }

    private static void CreateNewInflationMesh(ref List<Vector3> vertices, ref List<Vector2> uv0,
        ref List<int> triangles, ref Dictionary<int, int> referenceVertexIdMap, ref List<MeshInflateHelper.Edge> edges,
        ref List<Vector3> inflationNormals, GameObject gameObject, Mesh mesh,
        UnityEditor.AssetImporters.AssetImportContext context, string editorTitle)
    {
        Mesh inflationMesh = new Mesh();
        inflationMesh.name = string.Format("{0}_inflation", mesh.name);
        inflationMesh.SetVertices(vertices);
        inflationMesh.SetUVs(0, uv0);
        inflationMesh.SetTriangles(triangles, 0);

        if (!MeshInflateHelper.GetEdges(vertices, triangles, ref referenceVertexIdMap, ref edges, null, editorTitle))
        {
            Debug.LogError(string.Format("CreateNewInflationMesh: Error while generating edges from custom version of {0}.",mesh.name));
            return;
        }

        MeshInflateHelper.GenerateInflationNormals(vertices, triangles, edges, referenceVertexIdMap,
            ref inflationNormals, 1.0f, null, editorTitle);
        inflationMesh.SetUVs(3, inflationNormals);
        
        context.AddObjectToAsset(GUID.Generate().ToString(), inflationMesh);
        
        GameObject inflationMeshGameObject = new GameObject(inflationMesh.name);
        inflationMeshGameObject.transform.SetParent(gameObject.transform);
        MeshFilter meshFilter = inflationMeshGameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = inflationMesh;
        MeshRenderer meshRenderer = inflationMeshGameObject.AddComponent<MeshRenderer>();
    }
    
    
}

#endif

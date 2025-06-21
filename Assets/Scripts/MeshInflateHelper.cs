using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class MeshInflateHelper
{
    public enum MeshType { Closed, SemiOpen, FullyOpen }


    
    
    /// <summary>
    /// Edge : 包含一条边的两个顶点索引，以及与该边相关的两个三角形的顶点索引。
    ///
    /// 不支持非流形网格(non-manifold meshes)
    /// </summary>
    public struct Edge
    {
        public int ie0;
        public int ie1;
        public int if0;
        public int? if1;

        public Edge(int ie0, int ie1, int if0)
        {
            this.ie0 = ie0;
            this.ie1 = ie1;
            this.if0 = if0;
            this.if1 = null;
        }
    }

    private const float HalfPI = 1.570796326794897f;

    /// <summary>
    /// 构建或维护一个网格的边结构（Edge 列表），同时检测是否存在非流形边（即被多个三角形共享的边）。
    /// 
    ///
    /// </summary>
    /// <param name="edges">需要进行操作的edges </param> 
    /// <param name="vertices">Mesh中的vertices</param> 
    /// <param name="i0">edge的第一个顶点ID</param> 
    /// <param name="i1">edge的第二个顶点ID</param>
    /// <param name="i2">用来创建三角形的第三个顶点ID</param>
    /// <returns>如果是非流形的，就提前终止处理，返回 false。</returns>
    public static bool AddToList(this List<Edge> edges, List<Vector3> vertices, int i0, int i1, int i2)
    {
        Edge edge;
        Vector3 v0, v1, edgeV0, edgeV1;
        int edgeCount = edges.Count;
        
        v0 = vertices[i0];
        v1 = vertices[i1];

        for (int i = 0; i < edgeCount; i++)
        {
            edgeV0 = vertices[edges[i].ie0];
            edgeV1 = vertices[edges[i].ie1];

            if ((edgeV0 - v0).sqrMagnitude <= math.EPSILON && (edgeV1 - v0).sqrMagnitude <= math.EPSILON ||
                (edgeV1 - v0).sqrMagnitude <= math.EPSILON && (edgeV0 - v1).sqrMagnitude <= math.EPSILON)
            {
                edge = edges[i];

                if (edge.if1 != null)
                {
                    return false;
                }

                edge.if1 = i2;
                edges[i] = edge;
                return true;
            }
        }
        
        edges.Add(new Edge(i0,i1,i2));
        return true;
    }
    
    /// <summary>
    /// 构建或维护一个网格的边结构（Edge 列表），同时检测是否存在非流形边（即被多个三角形共享的边）。
    /// 通过ID来直接判断。 
    ///
    /// </summary>
    /// <param name="edges">需要进行操作的edges </param> 
    /// <param name="i0">edge的第一个顶点ID</param> 
    /// <param name="i1">edge的第二个顶点ID</param>
    /// <param name="i2">用来创建三角形的第三个顶点ID</param>
    /// <returns>如果是非流形的，就提前终止处理，返回 false。</returns>
    public static bool AddToList(this List<Edge> edges, int i0, int i1, int i2)
    {
        Edge edge;
        int edgeCount = edges.Count;

        for (int i = 0; i < edgeCount; i++)
        {
            edge = edges[i];
            if ((edge.ie0 == i0 && edge.ie1 == i1) || (edge.ie0 == i1 && edge.ie1 == i0))
            {
                if (edge.if1 != null)
                {
                    return false;
                }
                
                edge.if1 = i2;
                edges[i] = edge;
                return true;
            }
        }
        
        edges.Add(new Edge(i0, i1, i2));
        return true;
    }

    private static Vector3 GetSurfaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        return math.normalize(math.cross(b - a, c - a));
    }
    
    //@source https://github.com/blender/blender/blob/9c0bffcc89f174f160805de042b00ae7c201c40b/source/blender/blenlib/intern/math_vector.c#L452
    private static float Angle(Vector3 a, Vector3 b)
    {
        if (math.dot(a, b) >= 0.0f)
        {
            return 2.0f * math.asin((b-a).magnitude / 2.0f);
        }
        
        return math.PI - 2.0f * math.asin((-b-a).magnitude / 2.0f);
    }

    public static void CopyDataToLists(this Mesh mesh, ref List<Vector3> vertices, ref List<Vector2> uv0,
        ref List<int> triangles)
    {
        Vector3[] meshVertices = mesh.vertices;

        if (mesh.uv == null)
        {
            Debug.LogError("CopyDataToLists: mesh.uv is null. Maybe you don't set the uv!");
        }

        Vector2[] meshUV0 = mesh.uv;
        int[] meshTriangles = mesh.triangles;
        
        int vertexCount = meshVertices.Length;
        int triangleCount = meshTriangles.Length;
        
        vertices.Clear();
        uv0.Clear();
        triangles.Clear();
        
        for (int i = 0; i < vertexCount; i++)
        {
            vertices.Add(meshVertices[i]);
            uv0.Add(meshUV0[i]);
        }

        for (int i = 0; i < triangleCount; i++)
        {
            triangles.Add(meshTriangles[i]);
        }
    }

    public static bool GetEdges(List<Vector3> vertices, List<int> triangles,
        ref Dictionary<int, int> referenceVertexIdMap, ref List<Edge> edges,
        Transform debugTransform = null, string editorTitle = null)
    {
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Get edges", "Startup...", 0.0f);
        }    
#endif
        Vector3 v0, v1;
        int i0, i1, i2;
        int vertexCount = vertices.Count;
        int triangleCount = triangles.Count;

        for (int i = 0; i < triangleCount; i+=3)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Get edges", "Verifying base data...", (float)i / triangleCount);
            }            
#endif
            i0 = triangles[i];
            i1 = triangles[i + 1];
            i2 = triangles[i + 2];
            if (i0 == i1 || i1 == i2 || i2 == i0)
            {
                Debug.LogError(string.Format("GetEdges : Mesh is weird (some triangles have twice the same index, meaning they are just a line). Triangle indices are the following: {0}; {1}; {2}.",i0,i1,i2));
                return false;
            }
        }
        
        referenceVertexIdMap.Clear();
        edges.Clear();

        for (int i = 0; i < vertexCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Get edges", "Aggregating vertices sharing the same position...", (float)i / vertexCount);
            }    
#endif
            v0 = vertices[i];
            if (referenceVertexIdMap.TryAdd(i, i))
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    v1 = vertices[j];
                    if ((v0 - v1).magnitude <= math.EPSILON)
                    {
                        referenceVertexIdMap.TryAdd(j, i);
                    }
                }
            }
        }

        for (int i = 0; i < triangleCount; i += 3)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Get edges", "Get all edges...", (float)i / triangleCount);
            }            
#endif
            if (!referenceVertexIdMap.TryGetValue(triangles[i], out i0) ||
                !referenceVertexIdMap.TryGetValue(triangles[i + 1], out i1) ||
                !referenceVertexIdMap.TryGetValue(triangles[i + 2], out i2))
            {
                Debug.LogError("GetEdges: Vertex not found in Merged Vertices Dictionary.");
                return false;
            }

            if (i0 == i1 || i0 == i2 || i1 == i2)
            {
                Debug.LogError("GetEdges: Unique vertices error (two indices are the same, it's an edge not a triangle).");
                return false;
            }

            if (!edges.AddToList(i0, i1, i2) ||
                !edges.AddToList(i1, i2, i0) ||
                !edges.AddToList(i2, i0, i1))
            {
                Debug.LogError("GetEdges: Mesh edges are invalid (more than faces for one edge, mesh is non-manifold).");
                if (debugTransform != null)
                {
                    Debug.DrawLine(debugTransform.TransformPoint(vertices[i0]),debugTransform.TransformPoint(vertices[i1]), Color.red, 15.0f);
                    Debug.DrawLine(debugTransform.TransformPoint(vertices[i0]),debugTransform.TransformPoint(vertices[i2]), Color.red, 15.0f);
                    Debug.DrawLine(debugTransform.TransformPoint(vertices[i1]),debugTransform.TransformPoint(vertices[i2]), Color.red, 15.0f);
                }
                return false;
            }
        }
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }        
#endif
        return true;
    }
    // @source https://github.com/blender/blender/blob/9c0bffcc89f174f160805de042b00ae7c201c40b/source/blender/bmesh/operators/bmo_extrude.cc#L636
    public static void GenerateInflationNormals(List<Vector3> vertices, List<int> triangles, List<Edge> edges,
        Dictionary<int, int> referenceVertexIdMap,
        ref List<Vector3> inflationNormals, float thickness = 1.0f, Transform debugTransform = null,
        string editorTitle = null)
    {
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generate inflated normals", "Startup...", 0.0f);
        }        
#endif

        Vector3 edgeNormal, f0Normal;
        Vector3? f1Normal;
        int i0;
        Edge edge;

        int vertexCount = vertices.Count;
        int edgeCount = edges.Count;
        
        inflationNormals.Clear();

        for (int i = 0; i < vertexCount; i++)
        {
            inflationNormals.Add(Vector3.zero);
        }

        for (int i = 0; i < edgeCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generate inflated normals", "Computing normals for all edges...", (float)i / edgeCount);
            }            
#endif
            edge = edges[i];
            edgeNormal = Vector3.zero;

            f0Normal = GetSurfaceNormal(vertices[edge.ie0], vertices[edge.ie1], vertices[edge.if0]);
            f1Normal = edge.if1 != null ? GetSurfaceNormal(vertices[edge.ie1], vertices[edge.ie0], vertices[edge.if1.Value]) : null;
            if (f1Normal != null)
            {
                float angle = Angle(f0Normal, f1Normal.Value);
                if (angle > 0.0f)
                {
                    //将angle作为权重
                    edgeNormal = (f0Normal + f1Normal.Value).normalized * angle;
                }
                else
                {
                    continue;
                }
            }
            
            //考虑edge只连接一个平面
            else
            {
                edgeNormal = f0Normal * HalfPI;
            }

            inflationNormals[edge.ie0] += edgeNormal;
            inflationNormals[edge.ie1] += edgeNormal;
        }

        for (int i = 0; i < vertexCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generate inflated normals", "Checking specific cases (totally flat surfaces)...",(float) i / vertexCount);
            }            
#endif
            //考虑平面（angle == 0）, 使用base normal
            if (inflationNormals[i].sqrMagnitude <= math.EPSILON)
            {
                referenceVertexIdMap.TryGetValue(i, out i0);

                for (int j = 0; j < edgeCount; j++)
                {
                    if (edges[j].ie0 == i0 || edges[j].ie1 == i0 || edges[j].if0 == i0 || edges[j].if1 == i0)
                    {
                        //注意顺序会影响法线方向
                        inflationNormals[i] = (edges[j].if1 == i0
                            ? GetSurfaceNormal(vertices[edges[j].ie1], vertices[edges[j].ie0], vertices[edges[j].if1.Value])
                            : GetSurfaceNormal(vertices[edges[j].ie0], vertices[edges[j].ie1], vertices[edges[j].if0])) * math.PI;
                    }
                }
            }
        }
        
        //传递相同的顶点的法线膨胀信息
        for (int i = 0; i < vertexCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generate inflated normals", "Copying extrusion normals to all vertices...", (float)i / vertexCount);
            }            
#endif
            if (referenceVertexIdMap.TryGetValue(i, out i0) && i != i0)
            {
                inflationNormals[i] = inflationNormals[i0];
            }
        }

        for (int i = 0; i < vertexCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generate inflated normals", "Applying thickness...", (float)i / vertexCount);
            }

            if (debugTransform != null)
            {
                Vector3 pos = debugTransform.TransformPoint(vertices[i]);
                Debug.DrawLine(pos, pos + debugTransform.TransformDirection(inflationNormals[i]) * 0.1f, Color.red, 10.0f);
            }
            
#endif
            inflationNormals[i] *= thickness;
        }
        
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }
#endif
        
    }

    /// <param name="thickness"> 封边厚度 </param>
    public static MeshType TryGenerateClosedMeshFromOpenMesh(ref List<Vector3> vertices, ref List<Vector2> uv0,
        ref List<int> triangles, List<Edge> edges,
        Dictionary<int, int> referenceVertexIdMap, float thickness = -0.001f, Transform debugTransform = null, string editorTitle = null)
    {
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generating closed mesh", "Startup...", 0.0f);
        }        
#endif
        Edge edge;
        int i, id;
        int edgeCount = edges.Count;

        List<int> maskedStartEdgesId = new List<int>(); //边界边的起点
        List<int> maskedEdgesIdOpenSet = new List<int>();
        List<int> maskedEdgesIdVisitedSet = new List<int>();
        for (i = 0; i < edgeCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Generating closed mesh", "Getting masked vertices...", (float)i / edgeCount);
            }            
#endif
            if (edges[i].if1 == null)
            {
                maskedStartEdgesId.Add(i);
                maskedEdgesIdOpenSet.Add(i);
#if UNITY_EDITOR
                if (debugTransform != null)
                {
                    Debug.DrawLine(debugTransform.TransformPoint(vertices[edges[i].ie0]),
                        debugTransform.TransformPoint(vertices[edges[i].ie1]), Color.green, 10.0f);
                }
#endif
            }
        }

        int maskedStartEdgesIdCount = maskedStartEdgesId.Count;
        if (maskedStartEdgesIdCount <= 0)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }            
#endif
            return MeshType.Closed;
        }
        
#if UNITY_EDITOR
        int processdEdges = 0;
#endif
        while (maskedEdgesIdOpenSet.Count > 0)
        {
#if UNITY_EDITOR
        if(editorTitle != null){
            ++processdEdges;
            if(UnityEditor.EditorUtility.DisplayCancelableProgressBar(editorTitle + " - Generating closed mesh", "Expanding the mask to nearby vertices..." + ((float)processdEdges / edgeCount * 100.0f).ToString("f0") + "%", (float)processdEdges / edgeCount))
            {
                Debug.LogError("TryGenerateClosedMeshFromOpenMesh: Cancelled by user.");
                break;
            }
        }
#endif
            id = maskedEdgesIdOpenSet[0];
            edge = edges[id];

            maskedEdgesIdVisitedSet.Add(id);
            maskedEdgesIdOpenSet.RemoveAt(0);
            
#if UNITY_EDITOR
            if (debugTransform != null)
            {
                Debug.DrawLine(debugTransform.TransformPoint(vertices[edge.ie0]),debugTransform.TransformPoint(vertices[edge.ie1]),Color.green, 10.0f);
                Debug.DrawLine(debugTransform.TransformPoint(vertices[edge.ie0]),debugTransform.TransformPoint(vertices[edge.ie0] + Vector3.up * 0.05f));
                Debug.DrawLine(debugTransform.TransformPoint(vertices[edge.ie1]),debugTransform.TransformPoint(vertices[edge.ie1] + Vector3.up * 0.05f));
            }
#endif
            //BFS
            for (i = 0; i < edgeCount; i++)
            {
                if (i == id)
                {
                    continue;
                }

                if (edge.ie0 == edges[i].ie0 || edge.ie1 == edges[i].ie1 || edge.ie0 == edges[i].ie1 ||
                    edge.ie1 == edges[i].ie0)
                {
                    if (maskedEdgesIdVisitedSet.Contains(i) || maskedEdgesIdOpenSet.Contains(i))
                    {
                        continue;
                    }
                    maskedEdgesIdOpenSet.Add(i);
                }
            }
        }

        List<Vector3> extrusionNormals = new List<Vector3>();
        GenerateInflationNormals(vertices, triangles, edges, referenceVertexIdMap, ref extrusionNormals, thickness, debugTransform, editorTitle);
        
        
        MaskedExtrude(ref vertices,
            ref uv0,
            ref triangles,
            edges,
            referenceVertexIdMap,
            maskedStartEdgesId,
            maskedEdgesIdVisitedSet,
            extrusionNormals,
            thickness,
            editorTitle);
        
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }        
#endif
        //semiopen means fullyopen + closed
        return maskedEdgesIdVisitedSet.Count == edges.Count ? MeshType.FullyOpen : MeshType.SemiOpen;
    }

    private static void MaskedExtrude(ref List<Vector3> vertices, ref List<Vector2> uv0, ref List<int> triangles,
        List<Edge> edges,
        Dictionary<int, int> referenceVertexIdMap, List<int> maskedStartEdgesId, List<int> maskedEdgesIdVisitedSet,
        List<Vector3> extrusionNormals, float thickness, string editorTitle = null)
    {
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Extrude vertices", "Startup...", 0.0f);
        }
#endif
        Edge edge;
        int i, j, id, i0, i1, i2, it0, it1, it2;

        int vertexCount = vertices.Count;
        int triCount = triangles.Count;
        int maskedEdgesIdVisitedSetCount = maskedEdgesIdVisitedSet.Count;

        bool[] isEdge = new bool[vertexCount];
        Dictionary<int, int> addedVerticesMap = new Dictionary<int, int>();
        int newVertexCount = vertexCount;
        for (i = 0; i < vertexCount; i++)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar(editorTitle + " - Extrude vertices", "Creating new vertices from masked vertices...", (float)i /vertexCount);
            }    
#endif
            referenceVertexIdMap.TryGetValue(i, out id);
            for (j = 0; j < maskedEdgesIdVisitedSetCount; j++)
            {
                edge = edges[maskedEdgesIdVisitedSet[j]];
                if (edge.ie0 == id || edge.ie1 == id)
                {
                    //生成厚度
                    vertices.Add(vertices[i] + extrusionNormals[i]);
                    uv0.Add(uv0[i] + Vector2.right);
                    
                    addedVerticesMap.Add(i, newVertexCount);
                    isEdge[i] = maskedStartEdgesId.Contains(maskedEdgesIdVisitedSet[j]);
                    ++newVertexCount;
                    break;
                }
            }
        }

        for (i = 0; i < triCount; i += 3)
        {
#if UNITY_EDITOR
            if (editorTitle != null)
            {
                UnityEditor.EditorUtility.DisplayCancelableProgressBar(editorTitle + " - Extrude vertices", "Bridging surfaces...", (float)i/triCount);
            }
#endif
            it0 = triangles[i];
            it1 = triangles[i + 1];
            it2 = triangles[i + 2];

            if (addedVerticesMap.TryGetValue(it0, out i0) &&
                addedVerticesMap.TryGetValue(it1, out i1) &&
                addedVerticesMap.TryGetValue(it2, out i2))
            {
                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i0);
                
                Bridge(ref vertices, ref uv0, ref triangles, referenceVertexIdMap, edges, maskedStartEdgesId, it0, it1, it2, i0, i1, i2, thickness);
            }
        }
        
#if UNITY_EDITOR
        if (editorTitle != null)
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }        
#endif
    }

    /// <param name="itx"> 原始顶点 </param>
    /// <param name="ix"> 变换后的顶点（沿法线偏移了的） </param>
    private static void Bridge(ref List<Vector3> vertices,
        ref List<Vector2> uv0,
        ref List<int> triangles,
        Dictionary<int, int> referenceVertexIdMap,
        List<Edge> edges,
        List<int> maskedStartEdgesId,
        int it0,
        int it1,
        int it2,
        int i0,
        int i1,
        int i2,
        float thickness)
    {
        referenceVertexIdMap.TryGetValue(it0, out int rit0);
        referenceVertexIdMap.TryGetValue(it1, out int rit1);
        referenceVertexIdMap.TryGetValue(it2, out int rit2);
        
        for (int i = 0; i < maskedStartEdgesId.Count; ++i)
        {
            Edge edge = edges[maskedStartEdgesId[i]];
            if (edge.ie0 == rit0 && edge.ie1 == rit1)
                CreateBridge(ref vertices, ref uv0, ref triangles, it0, it1, i0, i1, thickness);
            else if (edge.ie1 == rit0 && edge.ie0 == rit1)
                CreateBridge(ref vertices, ref uv0, ref triangles, it1, it0, i1, i0, thickness);
            else if (edge.ie0 == rit1 && edge.ie1 == rit2)
                CreateBridge(ref vertices, ref uv0, ref triangles, it1, it2, i1, i2, thickness);
            else if (edge.ie1 == rit1 && edge.ie0 == rit2)
                CreateBridge(ref vertices, ref uv0, ref triangles, it2, it1, i2, i1, thickness);
            else if (edge.ie0 == rit2 && edge.ie1 == rit0)
                CreateBridge(ref vertices, ref uv0, ref triangles, it2, it0, i2, i0, thickness);
            else if (edge.ie1 == rit2 && edge.ie0 == rit0)
                CreateBridge(ref vertices, ref uv0, ref triangles, it0, it2, i0, i2, thickness);
        }
    }
    
    private static void CreateBridge(ref List<Vector3> vertices,
        ref List<Vector2> uv0,
        ref List<int> triangles,
        int it0,
        int it1,
        int i0,
        int i1,
        float thickness)
    {
        int vertexCount = vertices.Count;

        vertices.Add(vertices[it0]);
        vertices.Add(vertices[it1]);
        vertices.Add(vertices[i0]);
        vertices.Add(vertices[i1]);

        float distX = (uv0[it0] - uv0[it1]).magnitude;
        float distY = math.abs(thickness * 10.0f);

        //存在共用uv，vertices[0]对应uv0[1]
        uv0.Add(Vector2.zero);
        uv0.Add(new Vector2(distX, 0.0f));
        uv0.Add(new Vector2(0.0f,  distY));
        uv0.Add(new Vector2(distX, distY));

        triangles.Add(vertexCount);
        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 1);

        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 3);
        triangles.Add(vertexCount + 1);
    }
}



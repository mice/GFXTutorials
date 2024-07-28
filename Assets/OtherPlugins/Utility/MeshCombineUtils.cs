using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public static class MeshCombineUtils
{
    private static List<int> indics1 = new List<int>();
    private static List<int> indics2 = new List<int>();
    public static Mesh Combine(Mesh mesh1, Mesh mesh2)
    {
        indics1.Clear();
        indics2.Clear();
        indics1.AddRange(mesh1.GetTriangles(0));
        indics2.AddRange(mesh2.GetTriangles(0));
        Mesh sharedMesh = new Mesh();

        var totalCount = mesh1.vertexCount + mesh2.vertexCount;
        var vertex = new NativeArray<Vector3>(totalCount, Allocator.Temp);
        var normals = new NativeArray<Vector3>(totalCount, Allocator.Temp);
        var uv1 = new NativeArray<Vector3>(totalCount, Allocator.Temp);
        var indics = new NativeArray<int>(indics1.Count + indics2.Count, Allocator.Temp);

        var vertex1 = mesh1.vertices;
        for (int i = 0; i < mesh1.vertexCount; i++)
        {
            vertex[i] = vertex1[i];
        }
        var tmpVertex2 = mesh2.vertices;
        var offset = mesh1.vertexCount;
        for (int i = 0; i < mesh2.vertexCount; i++)
        {
            vertex[i + offset] = tmpVertex2[i];
        }

        var normal1 = mesh1.normals;
        for (int i = 0; i < mesh1.vertexCount; i++)
        {
            normals[i] = normal1[i];
        }

        var tmpNormal2 = mesh2.normals;
        offset = mesh1.vertexCount;
        for (int i = 0; i < mesh2.vertexCount; i++)
        {
            normals[i + offset] = tmpNormal2[i];
        }

        List<Vector2> tmpUVList = new List<Vector2>();
        mesh1.GetUVs(0, tmpUVList);
        for (int i = 0; i < tmpUVList.Count; i++)
        {
            uv1[i] = tmpUVList[i];
        }
        tmpUVList.Clear();
        mesh2.GetUVs(0, tmpUVList);
        offset = mesh1.vertexCount;
        for (int i = 0; i < tmpUVList.Count; i++)
        {
            uv1[i + offset] = new Vector3(tmpUVList[i].x, tmpUVList[i].y, 1);
        }


        offset = 0;

        CombineIndics(indics, offset, mesh1.vertexCount);

        sharedMesh.SetVertices(vertex);
        sharedMesh.SetNormals(normals);
        sharedMesh.SetUVs(0, uv1);
        sharedMesh.SetTriangles(indics.ToArray(), 0);


        vertex.Dispose();
        normals.Dispose();
        uv1.Dispose();
        indics.Dispose();
        return sharedMesh;
    }

    private static void CombineIndics(NativeArray<int> indics, int offset, int mesh1VertexCount)
    {
        for (int i = 0; i < indics1.Count; i++)
        {
            indics[i + offset] = indics1[i];
        }
        offset = indics1.Count;

        var indicsOffset = mesh1VertexCount;
        for (int i = 0; i < indics2.Count; i++)
        {
            indics[i + offset] = indics2[i] + indicsOffset;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public struct SubDrawData
{
    public int index;
    public int count;
}

public class TestPrimitivesIndexedIndirect2MeshMtx : MonoBehaviour
{
    public Material material;
    private Mesh mesh;
    public Mesh mesh1;
    public Mesh mesh2;
    public MonoBehaviour DrawContainer;

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer meshMtxs;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    private Matrix4x4[] matrix4X4s;
    private SubDrawData[] subDrawDatas;

    const int commandCount = 2;

    void Start()
    {
        mesh = new Mesh();
        var meshList = new Mesh[2] {mesh1,mesh2};
        CombineMeshes(meshList,mesh);
        UpdateMatrix();

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        meshTriangles.SetData(mesh.triangles);
        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
        meshPositions.SetData(mesh.vertices);

        meshMtxs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrix4X4s.Length, sizeof(float) * 16);
        meshMtxs.SetData(matrix4X4s);

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    private void UpdateMatrix()
    {
        subDrawDatas = new SubDrawData[2];

        List<MeshFilter> meshFilters = new List<MeshFilter>();
        this.DrawContainer.GetComponentsInChildren<MeshFilter>(true, meshFilters);
        matrix4X4s = new Matrix4x4[meshFilters.Count];

        meshFilters.Sort((t1, t2) =>
        {
            if(t1.sharedMesh == t2.sharedMesh)
            {
                return 0;
            }
            else
            {
                var t1Index = GetIndex(t1);
                var t2Index = GetIndex(t2);
                return t1Index - t2Index;
            }
        });
        int index = 0;
        foreach (var filter in meshFilters)
        {
            if (filter.sharedMesh == mesh1)
            {
                subDrawDatas[0].count ++;
            }
            else if (filter.sharedMesh == mesh2)
            {
                subDrawDatas[1].count++;
            }
            //matrix4X4s[index++] = filter.transform.position;
            matrix4X4s[index++] = filter.transform.localToWorldMatrix;// Matrix4x4.TRS(filter.transform.position, Quaternion.identity, Vector3.one);
            // filter.transform.localToWorldMatrix.transpose;
        }
    }

    private int GetIndex(MeshFilter filter)
    {
        if (filter.sharedMesh == mesh1)
        {
            return 1;
        }
        else if (filter.sharedMesh == mesh2)
        {
            return 2;
        }
        return 0;
    }

    private void CombineMeshes(Mesh[] meshList,Mesh target)
    {
        CombineInstance[] combine = new CombineInstance[2];

        for (int i = 0; i < 2; i++)
        {
            combine[i].mesh = meshList[i];
            combine[i].transform = Matrix4x4.identity;
        }

        target.CombineMeshes(combine,false);
    }

    void OnDestroy()
    {
        meshTriangles?.Dispose();
        meshTriangles = null;
        meshPositions?.Dispose();
        meshPositions = null;
        meshMtxs?.Dispose();
        meshMtxs = null;

        commandBuf?.Dispose();
        commandBuf = null;

    
    }

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetBuffer("_Triangles", meshTriangles);
        rp.matProps.SetBuffer("_Positions", meshPositions);
        rp.matProps.SetBuffer("_ObjectToWorlds", meshMtxs);
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].baseVertexIndex = mesh.GetBaseVertex(0);
        commandData[0].startIndex = mesh.GetIndexStart(0);
        commandData[0].instanceCount = (uint)subDrawDatas[0].count;
        commandData[0].startInstance = 0;
        

        commandData[1].indexCountPerInstance = mesh.GetIndexCount(1);
        commandData[1].baseVertexIndex = mesh.GetBaseVertex(1);
        commandData[1].startIndex = mesh.GetIndexStart(1);
        commandData[1].instanceCount = (uint)subDrawDatas[1].count;
        commandData[1].startInstance = (uint)subDrawDatas[0].count;
        commandBuf.SetData(commandData);
        Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, meshTriangles, commandBuf, commandCount);
    }
}
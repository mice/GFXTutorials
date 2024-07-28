using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPrimitivesIndexedIndirect2Mesh : MonoBehaviour
{
   
    public Material material;
    private Mesh mesh;
    public Mesh mesh1;
    public Mesh mesh2;

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    SubDrawData[] subDrawDatas;
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
        
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    private void UpdateMatrix()
    {
        subDrawDatas = new SubDrawData[2];

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
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(-4.5f, 0, 0)));
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].baseVertexIndex = mesh.GetBaseVertex(0);
        commandData[0].startIndex = mesh.GetIndexStart(0);
        commandData[0].instanceCount = 10;

        commandData[1].indexCountPerInstance = mesh.GetIndexCount(1);
        commandData[1].baseVertexIndex = mesh.GetBaseVertex(1);
        commandData[1].startIndex = mesh.GetIndexStart(1);
        commandData[1].instanceCount = 10;
        commandBuf.SetData(commandData);
        Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, meshTriangles, commandBuf, commandCount);
    }
}
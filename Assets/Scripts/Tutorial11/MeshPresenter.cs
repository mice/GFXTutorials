using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.WebRequestMethods;

public interface IMeshPresenter:IDisposable
{
    bool IsInited { get; }
    void Init(MeshGroupData meshGroupData, Mesh mesh);
    void Update(MeshGroupData meshGroupData);
    void Render(MeshGroupData meshGroupData, MeshInfoData meshInfoData, Mesh mesh);
}

public class MeshPresenter : IMeshPresenter
{

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer uvs;
    GraphicsBuffer meshMtxs;
    GraphicsBuffer meshTextures;
    GraphicsBuffer matrixIndex;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;


    private MaterialPropertyBlock mtp;
    int TRIANGLE_ID = Shader.PropertyToID("_Triangles");
    int POSITION_ID = Shader.PropertyToID("_Positions");
    int UV_ID = Shader.PropertyToID("_UVS");
    int OBJECTMATRIX_ID = Shader.PropertyToID("_ObjectToWorlds");
    int OBJECTTEXTURE_ID = Shader.PropertyToID("_ObjectTextures");
    int MATRIXINDEX_ID = Shader.PropertyToID("_MatrixIndex");

    public bool IsInited { get; private set; }
    public MeshPresenter()
    {
        mtp = new MaterialPropertyBlock();
    }

    public void Init(MeshGroupData meshGroupData,  Mesh mesh)
    {
        IsInited = true;
        var instanceCount = meshGroupData.TotalCount;
        var commandCount = meshGroupData.MeshCount;

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        meshTriangles.SetData(mesh.triangles);

        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
        meshPositions.SetData(mesh.vertices);

        uvs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.uv.Length, 2 * sizeof(float));
        uvs.SetData(mesh.uv);

        meshMtxs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(float) * 16);
        meshMtxs.SetData(meshGroupData.matrix4X4s);

        meshTextures = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(int));
        meshTextures.SetData(meshGroupData.objectTextures);


        matrixIndex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(int));
        matrixIndex.SetData((int[])meshGroupData.matrixIndexData);


        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    public void Update(MeshGroupData meshGroupData)
    {
        matrixIndex.SetData(meshGroupData.matrixIndexData);
    }

    public unsafe void Render(MeshGroupData meshGroupData, MeshInfoData meshInfoData,Mesh mesh)
    {

        RenderParams rp = new RenderParams(meshInfoData.material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        rp.matProps = mtp;
        rp.matProps.SetBuffer(TRIANGLE_ID, meshTriangles);
        rp.matProps.SetBuffer(POSITION_ID, meshPositions);
        rp.matProps.SetBuffer(UV_ID, uvs);
        rp.matProps.SetBuffer(OBJECTMATRIX_ID, meshMtxs);
        rp.matProps.SetBuffer(OBJECTTEXTURE_ID, meshTextures);


        rp.matProps.SetBuffer(MATRIXINDEX_ID, matrixIndex);
        var subDrawDatas = meshGroupData.subDrawDatas;
        var commandCount = commandData.Length;

        //rp.matProps.SetTexture("Tex", tex2DArr);
        fixed (int* meshInstanceStartData_ptr = &meshGroupData.meshInstanceStartData.GetPinnableReference())
            for (int i = 0; i < commandCount; i++)
            {
                commandData[i].indexCountPerInstance = mesh.GetIndexCount(i);
                commandData[i].baseVertexIndex = mesh.GetBaseVertex(i);
                commandData[i].startIndex = mesh.GetIndexStart(i);
                commandData[i].instanceCount = (uint)subDrawDatas[i].count;
                commandData[i].startInstance = (uint)meshInstanceStartData_ptr[i];
            }
        commandBuf.SetData(commandData);
        Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, meshTriangles, commandBuf, commandCount);
    }

    public void Dispose()
    {
        if (IsInited)
        {
            meshTriangles?.Dispose();
            meshTriangles = null;
            meshPositions?.Dispose();
            meshPositions = null;
            uvs?.Dispose();
            uvs = null;
            meshMtxs?.Dispose();
            meshMtxs = null;

            matrixIndex?.Dispose();
            matrixIndex = null;

            meshTextures?.Dispose();
            meshTextures = null;

            commandBuf?.Dispose();
            commandBuf = null;
        }
    }
}

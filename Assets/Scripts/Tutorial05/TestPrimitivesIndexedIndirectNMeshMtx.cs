using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TestPrimitivesIndexedIndirectNMeshMtx : MonoBehaviour
{
    public Material material;
    private Mesh mesh;
    public List<Mesh> meshList = new List<Mesh>();
   
    public MonoBehaviour DrawContainer;

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer meshMtxs;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    private Matrix4x4[] matrix4X4s;
    private SubDrawData[] subDrawDatas;

     int commandCount = 2;

    void Start()
    {
        mesh = new Mesh();
        CombineMeshes(meshList,mesh);
        //commandCount = 1;
        commandCount= meshList.Count;
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
        subDrawDatas = new SubDrawData[meshList.Count];

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
            var tIndex = GetIndex(filter);
            if (tIndex != -1)
            {
                subDrawDatas[tIndex].count++;
            }
            //matrix4X4s[index++] = filter.transform.position;
            matrix4X4s[index++] = filter.transform.localToWorldMatrix;// Matrix4x4.TRS(filter.transform.position, Quaternion.identity, Vector3.one);
            // filter.transform.localToWorldMatrix.transpose;
        }
    }

    private int GetIndex(MeshFilter filter)
    {
        return meshList.IndexOf(filter.sharedMesh);
    }

    private void CombineMeshes(List<Mesh> meshList,Mesh target)
    {
        CombineInstance[] combine = new CombineInstance[meshList.Count];

        for (int i = 0; i < meshList.Count; i++)
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
        uint startInstance = 0;
        for (int i = 0; i < commandCount; i++)
        {
            commandData[i].indexCountPerInstance = mesh.GetIndexCount(i);
            commandData[i].baseVertexIndex = mesh.GetBaseVertex(i);
            commandData[i].startIndex = mesh.GetIndexStart(i);
            commandData[i].instanceCount = (uint)subDrawDatas[i].count;
            commandData[i].startInstance = startInstance;
            startInstance += (uint)subDrawDatas[i].count;
        }
        commandBuf.SetData(commandData);
        Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, meshTriangles, commandBuf, commandCount);
    }
}
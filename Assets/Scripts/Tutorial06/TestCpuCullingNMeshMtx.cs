using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Mesh;



public class TestCpuCullingNMeshMtx : MonoBehaviour
{
    public int Count;
    public int Radius;
    public Material Material;
    public List<Mesh> MeshList = new List<Mesh>();
    public MonoBehaviour DrawContainer;

    public int TotalVisibleCount = 0;

    public List<MeshInfo> MeshInfoList = new List<MeshInfo>();
    private Mesh mesh;
    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer meshMtxs;
    GraphicsBuffer matrixIndex;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    private Matrix4x4[] matrix4X4s;
    private int[] matrixIndexData;
    private int[] meshIndexData;
    private SubDrawData[] subDrawDatas;
    private List<List<int>> indexList = new List<List<int>>();
    List<MeshFilter> meshFilters;
     int commandCount = 2;

    float4[] planefloat4s = new float4[6];
    Plane[] planes = new Plane[6];
    Camera _camera;
    private MaterialPropertyBlock mtp;
    int TRIANGLE_ID = Shader.PropertyToID("_Triangles");
    int POSITION_ID = Shader.PropertyToID("_Positions");
    int OBJECTMATRIX_ID = Shader.PropertyToID("_ObjectToWorlds");
    int MATRIXINDEX_ID = Shader.PropertyToID("_MatrixIndex");
    void Start()
    {
        mtp = new MaterialPropertyBlock();
        mesh = new Mesh();
        _camera = Camera.main;
        meshFilters = new List<MeshFilter>();
        CreateMeshes(this.DrawContainer.transform);
        CombineMeshes(MeshList, mesh);
        //commandCount = 1;
        commandCount= MeshList.Count;
        UpdateMatrix();

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        meshTriangles.SetData(mesh.triangles);
        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
        meshPositions.SetData(mesh.vertices);

        meshMtxs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrix4X4s.Length, sizeof(float) * 16);
        meshMtxs.SetData(matrix4X4s);

        matrixIndex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrixIndexData.Length, sizeof(float));
        matrixIndex.SetData(matrixIndexData);
        

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    private void CreateMeshes(Transform container)
    {
        var count = Mathf.Max(1, this.Count);
        for (int i = 0; i < count; i++)
        {
            var gObj  = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gObj.transform.parent = container;
            gObj.GetComponent<MeshFilter>().mesh = this.MeshList[UnityEngine.Random.Range(0,this.MeshList.Count)];
            var tfm = gObj.transform;
            tfm.position = UnityEngine.Random.insideUnitSphere * Radius;
        }
    }

    private void UpdateMatrix()
    {
        subDrawDatas = new SubDrawData[MeshList.Count];
        
        this.DrawContainer.GetComponentsInChildren<MeshFilter>(true, meshFilters);
        matrix4X4s = new Matrix4x4[meshFilters.Count];
        matrixIndexData = new int[meshFilters.Count];
        meshIndexData = new int[meshFilters.Count];
        for (int i = 0; i < meshFilters.Count; i++)
        {
            MeshFilter filter = meshFilters[i];
            var tIndex = GetIndex(filter);
            meshIndexData[i] = tIndex;
            matrix4X4s[i] = filter.transform.localToWorldMatrix;
        }
    }
    private bool UpdateFrustum()
    {
        
        if (_camera == null || !_camera.isActiveAndEnabled)
            return false;
        GeometryUtility.CalculateFrustumPlanes(_camera, planes);
        unsafe
        {
            fixed (void* planefloat4s_ptr = &planefloat4s[0])
            {
                fixed (void* planes_ptr = &planes[0])
                {
                    UnsafeUtility.MemCpy(planefloat4s_ptr, planes_ptr, 6 * UnsafeUtility.SizeOf<float4>());
                }
            }
        }
        return true;
    }

    private int GetIndex(MeshFilter filter)
    {
        return MeshList.IndexOf(filter.sharedMesh);
    }

    private void CombineMeshes(List<Mesh> meshList,Mesh target)
    {
        CombineInstance[] combine = new CombineInstance[meshList.Count];
        MeshInfoList = new List<MeshInfo>();
        indexList = new List<List<int>>();
        var tmpVertices = new List<Vector3>();
        for (int i = 0; i < meshList.Count; i++)
        {
            combine[i].mesh = meshList[i];
            combine[i].transform = Matrix4x4.identity;
            
            meshList[i].GetVertices(tmpVertices);
         
            var meshInfo = new MeshInfo(); 
            meshInfo.MeshIndex = i; 
            (meshInfo.Center,meshInfo.Radius) = CullUtils.GetSphereBounds(tmpVertices);
            MeshInfoList.Add(meshInfo);
            indexList.Add(new List<int>());
            tmpVertices.Clear();
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

        matrixIndex?.Dispose();
        matrixIndex = null;


        commandBuf?.Dispose();
        commandBuf = null;
    }

    bool IsVisible(Vector3 position,MeshInfo meshData)
    {
        return CullUtils.FrustumCullSphere(planefloat4s, meshData.Center + position, meshData.Radius);
    }

    void UpdateIndexAndCount()
    {
        for (int i = 0; i < commandCount; i++)
        {
            subDrawDatas[i].count = 0;
            indexList[i].Clear();
        }
        TotalVisibleCount = 0;
        for (int i = 0; i < this.meshFilters.Count; i++)
        {
            var tIndex = this.GetIndex(this.meshFilters[i]);
            
            if (IsVisible(this.meshFilters[i].transform.position, MeshInfoList[tIndex]))
            {
                
                subDrawDatas[tIndex].count++;
                indexList[tIndex].Add(i);
            }
        }

        for (int i = 0, k = 0; i < this.indexList.Count; i++)
        {
            for (int j = 0; j < this.indexList[i].Count; j++)
            {
                this.matrixIndexData[k++] = this.indexList[i][j];
            }
            TotalVisibleCount += this.indexList[i].Count;
        }
        matrixIndex.SetData(this.matrixIndexData);
    }

    void Update()
    {
        UpdateFrustum();
        UpdateIndexAndCount();
        RenderParams rp = new RenderParams(Material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        rp.matProps = mtp;
        rp.matProps.SetBuffer(TRIANGLE_ID, meshTriangles);
        rp.matProps.SetBuffer(POSITION_ID, meshPositions);
        rp.matProps.SetBuffer(OBJECTMATRIX_ID, meshMtxs);
        rp.matProps.SetBuffer(MATRIXINDEX_ID, matrixIndex);
        
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
using Stella3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.InputManagerEntry;
using static UnityEngine.Mesh;

public class TestCpuCullingTutorial10 : MonoBehaviour
{
    public int Count;
    public int Radius;
    public Material Material;
    public List<Mesh> MeshList = new List<Mesh>();
    public List<int> MatIDList = new List<int>();
    public MonoBehaviour DrawContainer;
    public Texture[] textures;

    public int TotalVisibleCount = 0;
    public bool changed = false;

   
    private Mesh mesh;
    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer uvs;
    GraphicsBuffer meshMtxs;
    GraphicsBuffer meshTextures;
    GraphicsBuffer matrixIndex;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    SharedArray<float4> planefloat4s = new SharedArray<float4>(6);
    Plane[] planes = new Plane[6];//frustum.
    public SharedArray<MeshInfo> MeshInfoList;

    private Matrix4x4[] matrix4X4s;
    private SharedArray<float3> positions;
    private int[] objectTextures;
    private SharedArray<int> matrixIndexData;
    private SharedArray<int> meshIndexData;
    private int[] meshCountData;//每个mesh对应的总数量.
    private SharedArray<int> meshInstanceStartData;//每个mesh对应的总数量.
    private SubDrawData[] subDrawDatas;
    List<MeshFilter> meshFilters;
    List<int> meshIndex;
    int commandCount = 2;
    private Texture2DArray tex2DArr;

    
    Camera _camera;
    private MaterialPropertyBlock mtp;
    int TRIANGLE_ID = Shader.PropertyToID("_Triangles");
    int POSITION_ID = Shader.PropertyToID("_Positions");
    int UV_ID = Shader.PropertyToID("_UVS");
    int OBJECTMATRIX_ID = Shader.PropertyToID("_ObjectToWorlds");
    int OBJECTTEXTURE_ID = Shader.PropertyToID("_ObjectTextures");
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
        CreateTexture(textures);
        UpdateMatrix();

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        meshTriangles.SetData(mesh.triangles);

        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
        meshPositions.SetData(mesh.vertices);

        uvs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.uv.Length, 2 * sizeof(float));
        uvs.SetData(mesh.uv);

        meshMtxs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrix4X4s.Length, sizeof(float) * 16);
        meshMtxs.SetData(matrix4X4s);

        meshTextures = new GraphicsBuffer(GraphicsBuffer.Target.Structured, objectTextures.Length, sizeof(int));
        meshTextures.SetData(objectTextures);
        

        matrixIndex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matrixIndexData.Length, sizeof(int));
        matrixIndex.SetData(matrixIndexData);
        

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    private void CreateTexture(Texture[] sourceTextures)
    {
        //Create texture2DArray
        tex2DArr = new Texture2DArray(sourceTextures[0].width,
            sourceTextures[0].height, sourceTextures.Length, sourceTextures[0].graphicsFormat, TextureCreationFlags.None);

        // Apply settings
        for (int i = 0; i < sourceTextures.Length; i++)
        {
            int m = 0;
            //for (int m = 0; m < sourceTextures[i].mipmapCount; m++)
            {
                Graphics.CopyTexture(sourceTextures[i], 0, m, tex2DArr, i, m);
            }
        }
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

    private unsafe void UpdateMatrix()
    {
        subDrawDatas = new SubDrawData[MeshList.Count];
        
        this.DrawContainer.GetComponentsInChildren<MeshFilter>(true, meshFilters);
        matrix4X4s = new Matrix4x4[meshFilters.Count];
        matrixIndexData = new SharedArray<int>(meshFilters.Count);
        meshIndexData = new SharedArray<int>(meshFilters.Count);
        objectTextures = new int[meshFilters.Count];
        positions = new SharedArray<float3>(meshFilters.Count);
        var positionArray = (float3[])positions;
        meshCountData = new int[MeshList.Count];

        fixed (int* meshIndexData_ptr = &meshIndexData.GetPinnableReference())
        for (int i = 0; i < meshFilters.Count; i++)
        {
            MeshFilter filter = meshFilters[i];
            var tIndex = GetIndex(filter);
            meshIndexData_ptr[i] = tIndex;
            matrix4X4s[i] = filter.transform.localToWorldMatrix;
            positionArray[i] = filter.transform.position;
            objectTextures[i] = MatIDList[tIndex];
            meshCountData[tIndex]++;
        }

        meshInstanceStartData = new SharedArray<int>(MeshList.Count);
        var tmpCount = 0;
        fixed (int* meshInstanceStartData_ptr = &meshInstanceStartData.GetPinnableReference())
        for (int i = 0; i < MeshList.Count; i++)
        {
            meshInstanceStartData_ptr[i] = tmpCount;
            tmpCount += meshCountData[i];
        }
    }
    private bool UpdateFrustum()
    {
        
        if (_camera == null || !_camera.isActiveAndEnabled)
            return false;
        GeometryUtility.CalculateFrustumPlanes(_camera, planes);
        unsafe
        {
            fixed (void* planefloat4s_ptr = &planefloat4s.GetPinnableReference())
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
        MeshInfoList = new SharedArray<MeshInfo>(meshList.Count);
        var tmpMeshInfoArray = (MeshInfo[]) MeshInfoList;

        var tmpVertices = new List<Vector3>();
        for (int i = 0; i < meshList.Count; i++)
        {
            combine[i].mesh = meshList[i];
            combine[i].transform = Matrix4x4.identity;
            
            meshList[i].GetVertices(tmpVertices);
         
            var meshInfo = new MeshInfo(); 
            meshInfo.MeshIndex = i; 
            (meshInfo.Center,meshInfo.Radius) = CullUtils.GetSphereBounds(tmpVertices);
            tmpMeshInfoArray[i] = meshInfo;
            
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

    bool IsVisible(Vector3 position,MeshInfo meshData)
    {
        return CullUtils.FrustumCullSphere(planefloat4s, meshData.Center + position, meshData.Radius);
    }



    void UpdateIndexAndCount()
    {
        TotalVisibleCount = 0;
        NativeArray<int> tmp_subDrawDatas = new NativeArray<int>(commandCount, Allocator.TempJob);
        var cullingJob = new CullingJobs()
        {
            planefloat4s = planefloat4s,
            MeshInfoList = MeshInfoList,
            positions = positions,
            meshIndexData = meshIndexData,
            meshInstanceStartData = meshInstanceStartData,
            matrixIndexData = matrixIndexData,
            subDrawDatas = tmp_subDrawDatas
        };        //可以减少一次sort

        //cullingJob.Execute();
        cullingJob.Run(positions.Length);
        for (int i = 0; i < subDrawDatas.Length; i++)
        {
            subDrawDatas[i].count = tmp_subDrawDatas[i];
        }

        tmp_subDrawDatas.Dispose();

        matrixIndex.SetData(this.matrixIndexData);
    }

   

    unsafe void Update()
    {
        UpdateFrustum();
        if (changed)
        {
            UpdateIndexAndCount();
        }
      
        RenderParams rp = new RenderParams(Material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        rp.matProps = mtp;
        rp.matProps.SetBuffer(TRIANGLE_ID, meshTriangles);
        rp.matProps.SetBuffer(POSITION_ID, meshPositions);
        rp.matProps.SetBuffer(UV_ID, uvs);
        rp.matProps.SetBuffer(OBJECTMATRIX_ID, meshMtxs);
        rp.matProps.SetBuffer(OBJECTTEXTURE_ID, meshTextures);

       
        rp.matProps.SetBuffer(MATRIXINDEX_ID, matrixIndex);
        //rp.matProps.SetTexture("Tex", tex2DArr);
        fixed (int* meshInstanceStartData_ptr = &meshInstanceStartData.GetPinnableReference())
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
}
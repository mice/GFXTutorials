using Stella3D;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class TestCpuCullingTutorial11 : MonoBehaviour
{
    public int Count;
    public int Radius;
    public MonoBehaviour DrawContainer;

    public MeshInfoData MeshInfoData;
    public MeshGroupData meshGroupData;
    public IMeshPresenter MeshPresenter;

   
    private Mesh mesh;

    public int TotalVisibleCount = 0;
    public bool changed = false;
    
    SharedArray<float4> planefloat4s = new SharedArray<float4>(6);
    Plane[] planes = new Plane[6];//frustum.
   
    List<MeshFilter> meshFilters;
    private Texture2DArray tex2DArr;
    Camera _camera;

    void Start()
    {
        mesh = new Mesh();
        meshGroupData = new MeshGroupData();
        _camera = Camera.main;
        meshFilters = new List<MeshFilter>();
        MeshPresenter = new MeshPresenter();
        CreateMeshes(this.DrawContainer.transform, this.MeshInfoData);

        MeshInfoData.CombineMeshes(mesh);
      
        CreateTexture(MeshInfoData.textures);
        UpdateMatrix();

        MeshPresenter.Init(meshGroupData,  mesh);
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

    private void CreateMeshes(Transform container, MeshInfoData infoData)
    {
        var count = Mathf.Max(1, this.Count);
        for (int i = 0; i < count; i++)
        {
            var gObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gObj.transform.parent = container;
            gObj.GetComponent<MeshFilter>().mesh = infoData.RandomMesh();
            var tfm = gObj.transform;
            tfm.position = UnityEngine.Random.insideUnitSphere * Radius;
        }
    }

    private unsafe void UpdateMatrix()
    {
        this.DrawContainer.GetComponentsInChildren<MeshFilter>(true, meshFilters);
        meshGroupData.InitData(meshFilters, MeshInfoData);
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


    void UpdateIndexAndCount()
    {
        TotalVisibleCount = 0;
        var commandCount = meshGroupData.MeshCount;
        NativeArray<int> tmp_subDrawDatas = new NativeArray<int>(commandCount, Allocator.TempJob);
        var cullingJob = meshGroupData.ToJob(planefloat4s, tmp_subDrawDatas);

        cullingJob.Run(meshGroupData.positions.Length);
        var subDrawDatas = meshGroupData.subDrawDatas;
        for (int i = 0; i < subDrawDatas.Length; i++)
        {
            subDrawDatas[i].count = tmp_subDrawDatas[i];
        }

        tmp_subDrawDatas.Dispose();
    }


    unsafe void Update()
    {
        UpdateFrustum();
        if (changed)
        {
            UpdateIndexAndCount();
            MeshPresenter.Update(meshGroupData);
        }
        MeshPresenter.Render(meshGroupData, MeshInfoData,mesh);
    }

    void OnDestroy()
    {
        MeshPresenter?.Dispose();
        MeshPresenter = null;
    }
}
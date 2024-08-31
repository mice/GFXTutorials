using Stella3D;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class MeshGroupData
{
    public Matrix4x4[] matrix4X4s;
    public SharedArray<float3> positions;
    public int[] objectTextures;
    public SharedArray<int> matrixIndexData;
    public SharedArray<int> meshIndexData;
    public int[] meshCountData;//每个mesh对应的总数量.
    public SharedArray<int> meshInstanceStartData;//每个mesh对应的总数量.

    public SubDrawData[] subDrawDatas;
    public SharedArray<MeshInfo> MeshInfoList;
    public int TotalCount { get; private set; }
    public int MeshCount { get;private set; }
    public void Init(int totalCount, int meshCount)
    {
        this.TotalCount = totalCount;
        this.MeshCount = meshCount;
        matrix4X4s = new Matrix4x4[totalCount];
        matrixIndexData = new SharedArray<int>(totalCount);
        meshIndexData = new SharedArray<int>(totalCount);
        objectTextures = new int[totalCount];
        positions = new SharedArray<float3>(totalCount);

        meshInstanceStartData = new SharedArray<int>(meshCount);
        subDrawDatas = new SubDrawData[meshCount];
        MeshInfoList = new SharedArray<MeshInfo>(meshCount);
    }

    public unsafe void InitData(List<MeshFilter> meshFilters, MeshInfoData meshIndexData_)
    {
        var MeshList = meshIndexData_.MeshList;
        var MatIDList = meshIndexData_.MatIDList;
        var totalCount = meshFilters.Count;
        var meshCount = MeshList.Count;
        Init(totalCount, meshCount);
      
        meshIndexData_.CalcMesh(MeshInfoList);
        meshCountData = new int[meshCount];

        fixed (float3* position_Ptr = &positions.GetPinnableReference())
        fixed (int* meshIndexData_ptr = &meshIndexData.GetPinnableReference())
            for (int i = 0; i < meshFilters.Count; i++)
            {
                MeshFilter filter = meshFilters[i];
                var tIndex = MeshList.IndexOf(filter.sharedMesh);
                meshIndexData_ptr[i] = tIndex;
                matrix4X4s[i] = filter.transform.localToWorldMatrix;
                position_Ptr[i] = filter.transform.position;
                objectTextures[i] = MatIDList[tIndex];
                meshCountData[tIndex]++;
            }


        var tmpCount = 0;
        fixed (int* meshInstanceStartData_ptr = &meshInstanceStartData.GetPinnableReference())
            for (int i = 0; i < MeshList.Count; i++)
            {
                meshInstanceStartData_ptr[i] = tmpCount;
                tmpCount += meshCountData[i];
            }
    }

    public CullingJobs ToJob(SharedArray<float4> planefloat4s, NativeArray<int> tmp_subDrawDatas)
    {
        var cullingJob = new CullingJobs()
        {
            planefloat4s = planefloat4s,
            MeshInfoList = MeshInfoList,
            positions = positions,
            meshIndexData = meshIndexData,
            meshInstanceStartData = meshInstanceStartData,
            matrixIndexData = matrixIndexData,
            subDrawDatas = tmp_subDrawDatas
        };
        return cullingJob;
    }
}
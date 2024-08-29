using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Mesh;

public class CullingJobs : IJob
{
    [ReadOnly] NativeArray<float4> planefloat4s;
    [ReadOnly] NativeArray<float3> positions;
    [ReadOnly] NativeArray<MeshInfo> MeshInfoList;
    [ReadOnly] NativeArray<int2> index1List;
    [ReadOnly] NativeArray<int> meshIndexData;
    NativeArray<int> subDrawDatas;

    public void Execute()
    {
        //NÏßÐÔ
        for (int i = 0, j = 0; i < this.MeshInfoList.Length; i++)
        {
            var tIndex = meshIndexData[i];
            if (CullUtils.FrustumCullSphere2(planefloat4s, ((float3)MeshInfoList[tIndex].Center + positions[i]), MeshInfoList[tIndex].Radius))
            {
                subDrawDatas[tIndex] = subDrawDatas[tIndex] + 1;
                index1List[j++] = new int2(tIndex, i);
            }
        }

        //Sort
        //index1List((t1, t2) => t1.Item1 - t2.Item1);

        //for (int i = 0; i < index1List.Length; i++)
        //{
        //    matrixIndexData[i] = index1List[i].y;
        //}
    }

}

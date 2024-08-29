using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CullingJobs : IJobFor
{
    [ReadOnly] public NativeArray<float4> planefloat4s;//frustum;
    [ReadOnly] public NativeArray<MeshInfo> MeshInfoList;//mesh相关信息.
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<int> meshIndexData;//当前显示对象对应的mesh 的index;
    [ReadOnly] public NativeArray<int> meshInstanceStartData;//mesh 对应的数量
    public NativeArray<int> matrixIndexData;//当前显示对象对应的Matrix的index;
    public NativeArray<int> subDrawDatas;

    [BurstCompile]
    public void Execute(int i)
    {
        //N线性
        //for (int i = 0; i < this.positions.Length; i++)
        {
            var tIndex = meshIndexData[i];
            if (CullUtils.FrustumCullSphere2(planefloat4s, ((float3)MeshInfoList[tIndex].Center + positions[i]), MeshInfoList[tIndex].Radius))
            {
                matrixIndexData[meshInstanceStartData[tIndex] + subDrawDatas[tIndex]] = i;
                subDrawDatas[tIndex] = subDrawDatas[tIndex] + 1;
            }
        }
    }
}

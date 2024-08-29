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
    [ReadOnly] public NativeArray<MeshInfo> MeshInfoList;//mesh�����Ϣ.
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<int> meshIndexData;//��ǰ��ʾ�����Ӧ��mesh ��index;
    [ReadOnly] public NativeArray<int> meshInstanceStartData;//mesh ��Ӧ������
    public NativeArray<int> matrixIndexData;//��ǰ��ʾ�����Ӧ��Matrix��index;
    public NativeArray<int> subDrawDatas;

    [BurstCompile]
    public void Execute(int i)
    {
        //N����
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

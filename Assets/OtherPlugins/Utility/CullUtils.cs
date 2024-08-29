
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/**
   Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
    renderer.enabled = GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
 */
public static class CullUtils
{

    public static void FrustumCullSperes(float4[] FrustumPlanes, float4[] center, float[] radius,int count, int[] results)
    {
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                if (math.dot(FrustumPlanes[j], new float4(center[i].x, center[i].y, center[i].z, 1.0f)) > -radius[i])
                {
                    results[i] = 0;
                    break;
                }
            }
            results[i] = 1;
        }
    }


    public static bool FrustumCullSphere(float4[] FrustumPlanes, float3 center, float radius)
    {
        for (int j = 0; j < 6; j++)
        {
            float3 planeNormal = FrustumPlanes[j].xyz;
            float planeConstant = FrustumPlanes[j].w;
            if (math.dot(FrustumPlanes[j], new float4(center.x, center.y, center.z, 1.0f)) + radius < 0)
                return false;
        }
        return true;
    }

    public static bool FrustumCullSphere2(NativeArray<float4> FrustumPlanes, float3 center, float radius)
    {
        for (int j = 0; j < 6; j++)
        {
            float3 planeNormal = FrustumPlanes[j].xyz;
            float planeConstant = FrustumPlanes[j].w;
            if (math.dot(FrustumPlanes[j], new float4(center.x, center.y, center.z, 1.0f)) + radius < 0)
                return false;
        }
        return true;
    }

    public static (Vector3,float) GetSphereBounds(List<Vector3> triangle_vertices)
    {
        Vector3 center = new Vector3(0, 0, 0);

        foreach (var v in triangle_vertices)
            center += new Vector3(v.x, v.y, v.z);

        center /= (float)(triangle_vertices.Count);

        float radius = 0;

        foreach (var v in triangle_vertices)
            radius = Mathf.Max(radius, Vector3.Distance(center, new Vector3(v.x, v.y, v.z)));//sqr浪费了计算资源

        return (center, radius);
    }
}

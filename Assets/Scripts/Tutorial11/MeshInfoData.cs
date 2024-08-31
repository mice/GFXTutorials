using Stella3D;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MeshInfoData
{
    public List<Mesh> MeshList = new List<Mesh>();
    public List<int> MatIDList = new List<int>();
    public Texture[] textures;
    public Material material;

    public void CalcMesh(SharedArray<MeshInfo> MeshInfoList)
    {
        var tmpMeshInfoArray = (MeshInfo[])MeshInfoList;
        var tmpVertices = new List<Vector3>();
        for (int i = 0; i < MeshList.Count; i++)
        {
            var meshInfo = new MeshInfo();
            meshInfo.MeshIndex = i;
            MeshList[i].GetVertices(tmpVertices);
            (meshInfo.Center, meshInfo.Radius) = CullUtils.GetSphereBounds(tmpVertices);
            tmpMeshInfoArray[i] = meshInfo;
            tmpVertices.Clear();
        }
    }

    public Mesh RandomMesh()
    {
        return this.MeshList[UnityEngine.Random.Range(0, this.MeshList.Count)];
    }

    public void CombineMeshes(Mesh target)
    {
        var meshList = MeshList;
        CombineInstance[] combine = new CombineInstance[meshList.Count];
       
        for (int i = 0; i < meshList.Count; i++)
        {
            combine[i].mesh = meshList[i];
            combine[i].transform = Matrix4x4.identity;
        }

        target.CombineMeshes(combine, false);
    }
}

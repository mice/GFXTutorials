using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Texture2DArrayTest : MonoBehaviour
{
    public Texture2D t1;
    public Texture2D t2;

     /**
    t1, t2 不需要 read/write
    */
    [ContextMenu("Create Texture2DArray")]
    public void CreateCmb()
    {
        if (t1 == null || t2 ==null)
        {
            return;
        }
        Texture2D[] sourceTextures = new Texture2D[2]{
            t1,t2
        };

        //Create texture2DArray
        Texture2DArray texture2DArray = new Texture2DArray(sourceTextures[0].width,
            sourceTextures[0].height, sourceTextures.Length, sourceTextures[0].format  , true, true);

        // Apply settings
          for (int i = 0; i < sourceTextures.Length; i++)
        {
            for (int m = 0; m < sourceTextures[i].mipmapCount; m++)
            {
                Graphics.CopyTexture(sourceTextures[i], 0, m, texture2DArray, i, m);
            }
        }
        // Apply our changes
        texture2DArray.Apply(false);
        UnityEngine.Debug.LogError("CreateCmb");
        //Save 
        UnityEditor.AssetDatabase.CreateAsset(texture2DArray, "Assets/TexArray.asset");
    }

    /**
    t1, t2 需要 read/write
    */
    [ContextMenu("Create Texture2DArray32")]
    public void CreateArgb32()
    {
        if (t1 == null || t2 ==null)
        {
            return;
        }
        Texture2D[] sourceTextures = new Texture2D[2]{
            t1,t2
        };

        //Create texture2DArray
        Texture2DArray texture2DArray = new Texture2DArray(sourceTextures[0].width,
            sourceTextures[0].height, sourceTextures.Length, TextureFormat.ARGB32  , true, false);
        // Apply settings
        texture2DArray.filterMode = FilterMode.Bilinear;
        texture2DArray.wrapMode = TextureWrapMode.Repeat;

        for (int i = 0; i < sourceTextures.Length; i++)
        {
            texture2DArray.SetPixels(sourceTextures[i].GetPixels(), i, 0);
        }

        // Apply our changes
        texture2DArray.Apply(false);

        //Save 
        UnityEditor.AssetDatabase.CreateAsset(texture2DArray, "Assets/TexArray_argb.asset");
    }
}

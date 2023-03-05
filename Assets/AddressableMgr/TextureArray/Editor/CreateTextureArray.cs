using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateTextureArray
{
    public Texture2DArray CreateTexture(Texture2D[] textures, int textureSize)
    {
        var m_TextureArray = new Texture2DArray(textureSize, textureSize, textures.Length, TextureFormat.ARGB32, false);

        for (int iTexture = 0; iTexture < textures.Length; iTexture++)
        {
            m_TextureArray.SetPixels(textures[iTexture].GetPixels(0), iTexture, 0);
        }

        m_TextureArray.Apply();

        AssetDatabase.CreateAsset(m_TextureArray,"Assets/Content/Textures");

        return m_TextureArray;
    }

}

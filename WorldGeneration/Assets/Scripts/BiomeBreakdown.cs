using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeBreakdown : MonoBehaviour
{
    [HideInInspector]int mapResolution = 1000;
    Terrain terrain;
    TerrainData terData;

    private void Start()
    {
        terData = terrain.terrainData;
        float[,,] maps;
        //terData.SetAlphamaps(0, 0, maps);
    }

    public void SeparatePixels()
    {
        Texture2D tex = null;
        byte[] fileData;


        fileData = System.IO.File.ReadAllBytes("coloredPng.png");
        tex = new Texture2D(mapResolution, mapResolution);
        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

        Texture2D biomeMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false); //for test
        for (int y = 0; y < mapResolution; ++y)
        {
            for (int x = 0; x < mapResolution; ++x)
            { 
                // HUE - H are degrees
                float H, S, V;
                Color.RGBToHSV(tex.GetPixel(x, y), out H, out S, out V);
                if(H >= 0.5 && H < 0.8) // Blue
                {
                    H = 0.75f;
                    BlueBiome b = new BlueBiome(new Vector2(x, y), H, S, V);
                }
                else if(H >= 0.2 && H < 0.5) // Green
                {
                    H = 0.5f;
                    GreenBiome b = new GreenBiome(new Vector2(x, y), H, S, V);
                }
                else if(H >= 0.8 || H < 0.2) // Red
                {
                    H = 0.25f;
                    RedBiome b = new RedBiome(new Vector2(x, y), H, S, V);
                }

                //sending out - test works
                biomeMap.SetPixel(x, y, Color.HSVToRGB(H, 0.75f, 0.75f));
            }
        }
        biomeMap.Apply(); //TEST

        System.IO.File.WriteAllBytes("BiomeMap2.png", biomeMap.EncodeToPNG()); //TEST

        
    }

}

public class Pixel : MonoBehaviour
{
    public Vector2 location;
    public float H;
    public float S;
    public float V;
}

public class RedBiome : Pixel
{   
    public RedBiome(Vector2 location, float H, float S, float V)
    {
        this.location = location;
        this.H = H;
        this.S = S;
        this.V = V;
    }
}
public class GreenBiome : Pixel
{
    public GreenBiome(Vector2 location, float H, float S, float V)
    {
        this.location = location;
        this.H = H;
        this.S = S;
        this.V = V;
    }
}
public class BlueBiome : Pixel
{
    public BlueBiome(Vector2 location, float H, float S, float V)
    {
        this.location = location;
        this.H = H;
        this.S = S;
        this.V = V;
    }
}

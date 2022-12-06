using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBreakDown : MonoBehaviour
{
    public static GridBreakDown instance;
    
    public static int cellPixelSize = 100;
    public List<Cell> listCells;
    public Cell[,] Grid = new Cell[cellPixelSize, cellPixelSize];
    public int numCells;
    public Texture2D tex;
    public Texture2D redTex;
    public Texture2D greenTex;
    public Texture2D blueTex;
    public int mapResolution = 1000;

    public Terrain terrain;
    public TerrainData terData;

    public List<Vector2> redCoords;
    public List<Vector2> greenCoords;
    public List<Vector2> blueCoords;

    //public void Start()
    //{
    //    ApplyTexture();
    //    Invoke("ClearTexture", 5f);
    //}
    public void ApplyTexture()
    {
        tex = null;
        
        byte[] fileData;

        fileData = System.IO.File.ReadAllBytes("Assets\\coloredPng.png");
        tex = new Texture2D(cellPixelSize, cellPixelSize);
        tex.LoadImage(fileData);

        terData = terrain.terrainData;

        

        //Set Red Layer
        //fileData = System.IO.File.ReadAllBytes("Assets\\RedCoords.png");
        //redTex = new Texture2D(mapResolution, mapResolution);
        //redTex.LoadImage(fileData);


        //Set Green Layer
        //fileData = System.IO.File.ReadAllBytes("Assets\\GreenCoords.png");
        //greenTex = new Texture2D(mapResolution, mapResolution);
        //greenTex.LoadImage(fileData);


        //Set Blue Layer
        //fileData = System.IO.File.ReadAllBytes("Assets\\BlueCoords.png");
        //blueTex = new Texture2D(mapResolution, mapResolution);
        //blueTex.LoadImage(fileData);

        redTex = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false); //for test
        greenTex = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false); //for test
        blueTex = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false); //for test

        SeparateTexture();

        terData.terrainLayers[0].diffuseTexture = redTex;
        terData.terrainLayers[0].diffuseTexture.alphaIsTransparency = true;
        terData.terrainLayers[1].diffuseTexture = greenTex;
        terData.terrainLayers[1].diffuseTexture.alphaIsTransparency = true;
        terData.terrainLayers[2].diffuseTexture = blueTex;
        terData.terrainLayers[2].diffuseTexture.alphaIsTransparency = true;
    }

    public void SeparateTexture()
    {
        Color[] pixels = tex.GetPixels();
        Color blankColor = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < mapResolution; y++)
        {
            for (int x = 0; x < mapResolution; x++)
            {
                if (tex.GetPixel(x, y).r > .5f)
                {
                    //redCoords.Add(new Vector2(x, y));
                    redTex.SetPixel(x, y, Color.red);
                    greenTex.SetPixel(x, y, blankColor);
                    blueTex.SetPixel(x, y, blankColor);
                }
                else if (tex.GetPixel(x, y).g > .5f)
                {
                    //greenCoords.Add(new Vector2(x, y));
                    redTex.SetPixel(x, y, blankColor);
                    greenTex.SetPixel(x, y, Color.green);
                    blueTex.SetPixel(x, y, blankColor);
                }
                else if (tex.GetPixel(x, y).b > .5f)
                {
                    //blueCoords.Add(new Vector2(x, y));
                    redTex.SetPixel(x, y, blankColor);
                    greenTex.SetPixel(x, y, blankColor);
                    blueTex.SetPixel(x, y, Color.blue);
                }
                else
                {

                }
            }
        }

        redTex.Apply();
        greenTex.Apply();
        blueTex.Apply();

        System.IO.File.WriteAllBytes("Assets\\RedCoords.png", redTex.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets\\GreenCoords.png", greenTex.EncodeToPNG());
        System.IO.File.WriteAllBytes("Assets\\BlueCoords.png", blueTex.EncodeToPNG());
    }

    public enum PixelColor
    {
        None,
        Red,
        Green,
        Blue
    };
    public void CreateIndividualTextures(List<Vector2> coords, Texture2D tempTex, PixelColor pixelColor)
    {
        //Texture2D tempTex = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false);
        
        for (int y = 0; y < mapResolution; y++)
        {
            for (int x = 0; x < mapResolution; x++)
            {
                if(coords.Contains(new Vector2(x,y)))
                {
                    if(pixelColor == PixelColor.Red)
                    {
                        //print("red");
                        tempTex.SetPixel(x, y, Color.red);
                    }
                    else if(pixelColor == PixelColor.Green)
                    {
                        //print("green");
                        tempTex.SetPixel(x, y, Color.green);
                    }
                    else if(pixelColor == PixelColor.Blue)
                    {
                        //print("blue");
                        tempTex.SetPixel(x, y, Color.blue);
                    }
                    else
                    {
                        tempTex.SetPixel(x, y, Color.black);
                    }
                }
                else
                {
                    tempTex.SetPixel(x, y, Color.black);
                    //print("black");
                }
            }
        }

        tempTex.Apply();
        System.IO.File.WriteAllBytes($"Assets\\{pixelColor}Coords.png", tempTex.EncodeToPNG()); //TEST
        print(tempTex.name);
    }

    public void ClearTexture()
    {
        Color[] resetColors = tex.GetPixels();

        for (int i = 0; i < resetColors.Length; i++)
        {
            resetColors[i] = Color.white;
        }

        tex.SetPixels(resetColors);
        tex.Apply();
    }

    private void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

//#if UNITY_EDITOR
    public void GenerateGrid()
    {
        Texture2D tex = null;
        byte[] fileData;

        fileData = System.IO.File.ReadAllBytes("coloredPng.png");
        tex = new Texture2D(cellPixelSize, cellPixelSize);
        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.


        numCells = 1000 / cellPixelSize;
        //float[,] cellPixels = new float[cellPixelSize, cellPixelSize];

        for (int row = 0; row < numCells; row++)
        {
            for (int col = 0; col < numCells; col++)
            {
                print($"row: {row}  ,  col:  {col}");
                Grid[row, col] = new Cell(row, col);
                Grid[row, col].cellPixels = GetCellPixels(row, col, tex);
                listCells.Add(Grid[row, col]);
            }
        }

        foreach(Cell c in listCells)
        {
            float sum = 0;
            for (int x = 0; x < cellPixelSize; x++)
            {
                for (int y = 0; y < cellPixelSize; y++)
                {
                    sum += c.cellPixels[x,y];

                }

            }
            c.cellHue = sum / (cellPixelSize * cellPixelSize);
            print(c.cellHue);
        }
        
    }

    public float[,] GetCellPixels(int row, int col, Texture2D tex)
    {
        int rowMod = row * cellPixelSize;
        int colMod = col * cellPixelSize;

        float[,] cellPixels = new float[cellPixelSize, cellPixelSize];

        for (int y = colMod; y < colMod + cellPixelSize; y++)
        {
            for (int x = rowMod; x < rowMod + cellPixelSize; x++)
            {
                // HUE - H are degrees
                float H, S, V;
                Color.RGBToHSV(tex.GetPixel(x, y), out H, out S, out V);

                if (x != 0 && y != 0)
                {
                    cellPixels[x - rowMod, y - colMod] = H;
                }
                else if (x == 0 && y != 0)
                {
                    cellPixels[0, y-colMod] = H;
                }
                else if (x != 0 && y == 0)
                {
                    cellPixels[x-rowMod, 0] = H;
                }
                else
                {
                    cellPixels[0, 0] = H;
                }

            }
        }

        return cellPixels;
    }
//#endif
}

public class Cell : MonoBehaviour
{
    
    public float[,] cellPixels = new float[GridBreakDown.cellPixelSize, GridBreakDown.cellPixelSize];
    public int row;
    public int col;
    public float cellHue;
    //public string name;

    public Cell(int row, int col)
    {
        this.row = row;
        this.col = col;
        //ame = $"{row}{col}";
        //gameObject.name = $"{row}{col}";
    }
    /*
    if (H >= 0.5 && H< 0.8) // Blue
                {
                    H = 0.75f;
                }
                else if (H >= 0.2 && H < 0.5) // Green
{
    H = 0.5f;
}
else if (H >= 0.8 || H < 0.2) // Red
{
    H = 0.25f;
}
    
    */
}


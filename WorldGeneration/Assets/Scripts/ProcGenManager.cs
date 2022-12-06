using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

public class ProcGenManager : MonoBehaviour
{
    [SerializeField] ProcGenConfigSO Config;
    [SerializeField] Terrain TargetTerrain;


    Dictionary<string, int> BiomeTextureToTerrainLayerIndex = new Dictionary<string, int>();

    public Texture2D map;

#if UNITY_EDITOR
    byte[,] BiomeMap_LowResolution;
    float[,] BiomeStrengths_LowResolution;
    byte[,] Biome_Generate;
    byte[,] BiomeMap;
    float[,] BiomeStrengths;
#endif // UNITY_EDITOR


    void Start()
    {
        //GetComponent<GridBreakDown>().GenerateGrid();
    }


#if UNITY_EDITOR
    public void PaintConverter(int mapResolution)
    {
        Texture2D tex = null;
        byte[] fileData;


        fileData = System.IO.File.ReadAllBytes("coloredPng.png");
        tex = new Texture2D(mapResolution, mapResolution);
        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

        print(map.name);
        //grid break down script
        //GetComponent<GridBreakDown>().GenerateGrid();

        /*
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
                }
                else if(H >= 0.2 && H < 0.5) // Green
                {
                    H = 0.5f;
                }
                else if(H >= 0.8 || H < 0.2) // Red
                {
                    H = 0.25f;
                }

                //sending out - test works
                biomeMap.SetPixel(x, y, Color.HSVToRGB(H, 0.75f, 0.75f));
            }
        }
        biomeMap.Apply(); //TEST

        System.IO.File.WriteAllBytes("BiomeMap2.png", biomeMap.EncodeToPNG()); //TEST

        */
    }

    public void RegenerateTextures()
    {
        Perform_LayerSetup();
    }

    public void RegenerateWorld()
    {
        // cache the map resolution
        //int mapResolution = TargetTerrain.terrainData.heightmapResolution;



        int mapResolution = 1000;
        int alphaMapResolution = TargetTerrain.terrainData.alphamapResolution;

        //generate texture map
        Perform_GenerateTextureMapping();

        //Perform_BiomeGeneration(mapResolution);

        // generate the low resolution biome map
        Perform_BiomeGeneration_LowResolution((int)Config.BiomeMapResolution);
        // generate the high resolution biome map
        Perform_BiomeGeneration_HighResolution((int)Config.BiomeMapResolution, mapResolution);
        // place holder for changing the height

        // paint the terrain
        Perform_TerrainPainting(mapResolution, alphaMapResolution);
    }

    void Perform_GenerateTextureMapping()
    {
        BiomeTextureToTerrainLayerIndex.Clear();

        // iterate over biomes
        int layerIndex = 0;
        foreach (var biomeMetadata in Config.Biomes)
        {
            var biome = biomeMetadata.Biome;

            // iterate over textures
            foreach (var biomeTexture in biome.Textures)
            {
                // add to layer map
                BiomeTextureToTerrainLayerIndex[biomeTexture.UniqueID] = layerIndex;
                layerIndex++;
            }
        }
    }
    void Perform_BiomeGeneration_LowResolution(int mapResolution)
    {
        // allocate the biome map and strength map
        BiomeMap_LowResolution = new byte[mapResolution, mapResolution];
        BiomeStrengths_LowResolution = new float[mapResolution, mapResolution];
        // setup space for the seed points
        int numSeedPoints = Mathf.FloorToInt(mapResolution * mapResolution * Config.BiomeSeedPointDensity);
        List<byte> biomesToSpawn = new List<byte>(numSeedPoints);
        // populate the biomes to spawn based on weightings
        float totalBiomeWeighting = Config.TotalWeighting;
        for (int biomeIndex = 0; biomeIndex < Config.NumBiomes; ++biomeIndex)
        {
            int numEntries = Mathf.RoundToInt(numSeedPoints * Config.Biomes[biomeIndex].Weighting / totalBiomeWeighting);
            //Debug.Log("Will spawn " + numEntries + " seedpoints for " + Config.Biomes[biomeIndex].Biome.Name);
            for (int entryIndex = 0; entryIndex < numEntries; ++entryIndex)
            {
                biomesToSpawn.Add((byte)biomeIndex);
            }
        }
        // spawn the individual biomes
        while (biomesToSpawn.Count > 0)
        {
            // pick a random seed point
            int seedPointIndex = Random.Range(0, biomesToSpawn.Count);
            // extract the biome index
            byte biomeIndex = biomesToSpawn[seedPointIndex];
            // remove seed point
            biomesToSpawn.RemoveAt(seedPointIndex);
            Perform_SpawnIndividualBiome(biomeIndex, mapResolution);
        }
        // save out the biome map
        Texture2D biomeMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false);
        for (int y = 0; y < mapResolution; ++y)
        {
            for (int x = 0; x < mapResolution; ++x)
            {
                float hue = ((float)BiomeMap_LowResolution[x, y] / (float)Config.NumBiomes);
                biomeMap.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
            }
        }
        biomeMap.Apply();
        System.IO.File.WriteAllBytes("BiomeMap_LowResolution.png", biomeMap.EncodeToPNG());
    }
    void Perform_LayerSetup()
    {
        
        // delete any existing layers
        if (TargetTerrain.terrainData.terrainLayers != null || TargetTerrain.terrainData.terrainLayers.Length > 0)
        {
            Undo.RecordObject(TargetTerrain, "Clearing previous layers");

            // build up list of asset paths for each layer
            List<string> layersToDelete = new List<string>();
            foreach (var layer in TargetTerrain.terrainData.terrainLayers)
            {
                if (layer == null)
                {
                    continue;                  
                }
                layersToDelete.Add(AssetDatabase.GetAssetPath(layer.GetInstanceID()));
            }

            // remove all links to layers
            TargetTerrain.terrainData.terrainLayers = null;

            // delete each layer
            foreach (var layerFile in layersToDelete)
            {
                if (string.IsNullOrEmpty(layerFile))
                {
                    continue;
                }
                AssetDatabase.DeleteAsset(layerFile);
            }
            Undo.FlushUndoRecordObjects();
        }

        string scenePath = System.IO.Path.GetDirectoryName(SceneManager.GetActiveScene().path);

        // iterate over biomes
        List<TerrainLayer> newLayers = new List<TerrainLayer>();
        foreach (var biomeMetadata in Config.Biomes)
        {
            var biome = biomeMetadata.Biome;

            // iterate over textures
            foreach (var biomeTexture in biome.Textures)
            {
                //create the layer
                TerrainLayer textureLayer = new TerrainLayer();
                textureLayer.diffuseTexture = biomeTexture.Diffuse;
                textureLayer.normalMapTexture = biomeTexture.NormalMap;

                // save as asset
                string layerPath = System.IO.Path.Combine(scenePath, "Layer_" + biome.Name + "_" + biomeTexture.UniqueID);
                //AssetDatabase.CreateAsset(textureLayer, layerPath); // old
                AssetDatabase.CreateAsset(textureLayer, $"{layerPath}.asset");


                // add to layer map
                BiomeTextureToTerrainLayerIndex[biomeTexture.UniqueID] = newLayers.Count;
                newLayers.Add(textureLayer);
            }
        }

        Undo.RecordObject(TargetTerrain.terrainData, "Updating terrain layers");
        TargetTerrain.terrainData.terrainLayers = newLayers.ToArray();
    } 
    void Perform_BiomeGeneration(int mapResolution)
    {
        // allocate the biome map and strength map
        BiomeMap = new byte[mapResolution, mapResolution];
        BiomeStrengths = new float[mapResolution, mapResolution];

        // setup space for the seed points
        int numSeedPoints = Mathf.FloorToInt(mapResolution * mapResolution * Config.BiomeSeedPointDensity);
        List<byte> biomesToSpawn = new List<byte>(numSeedPoints);

        // populate the biomes to spawn based on weightings
        float totalBiomeWeighting = Config.TotalWeighting;
        for (int biomeIndex = 0; biomeIndex < Config.NumBiomes; ++biomeIndex)
        {
            int numEntries = Mathf.RoundToInt(numSeedPoints * Config.Biomes[biomeIndex].Weighting / totalBiomeWeighting);
            Debug.Log("Will spawn " + numEntries + " seedpoints for " + Config.Biomes[biomeIndex].Biome.Name);

            for (int entryIndex = 0; entryIndex < numEntries; ++entryIndex)
            {
                biomesToSpawn.Add((byte)biomeIndex);
            }
        }

        // spawn the individual biomes
        while (biomesToSpawn.Count > 0)
        {
            // pick a random seed point
            int seedPointIndex = Random.Range(0, biomesToSpawn.Count);

            // extract the biome index
            byte biomeIndex = biomesToSpawn[seedPointIndex];

            // remove seed point
            biomesToSpawn.RemoveAt(seedPointIndex);

            //Perform_SpawnIndividualBiome(biomeIndex, mapResolution);    //Old

        }
        
        PaintConverter(mapResolution);
        /*
        Texture2D biomeMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false);
        for (int y = 0; y < mapResolution; ++y)
        {
            for (int x = 0; x < mapResolution; ++x)
            {
                float hue = ((float)BiomeMap[x, y] / (float)Config.NumBiomes);

                biomeMap.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
            }
        }
        biomeMap.Apply();

        System.IO.File.WriteAllBytes("BiomeMap.png", biomeMap.EncodeToPNG());
        */
    }

    Vector2Int[] NeighbourOffsets = new Vector2Int[] {
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
    };

    //------------------------------------------------------------------------------------------------------------------
    /*
    Use Ooze based generation from here: https://www.procjam.com/tutorials/en/ooze/
    */
    void Perform_SpawnIndividualBiome(byte biomeIndex, int mapResolution)
    {
        // cache biome config
        BiomeConfigSO biomeConfig = Config.Biomes[biomeIndex].Biome;
        // pick spawn location
        Vector2Int spawnLocation = new Vector2Int(Random.Range(0, mapResolution), Random.Range(0, mapResolution));
        // pick the starting intensity
        float startIntensity = Random.Range(biomeConfig.MinIntensity, biomeConfig.MaxIntensity);
        // setup working list
        Queue<Vector2Int> workingList = new Queue<Vector2Int>();
        workingList.Enqueue(spawnLocation);
        // setup the visted map and target intensity map
        bool[,] visited = new bool[mapResolution, mapResolution];
        float[,] targetIntensity = new float[mapResolution, mapResolution];
        // set the starting intensity
        targetIntensity[spawnLocation.x, spawnLocation.y] = startIntensity;
        // let the oozing begin
        while (workingList.Count > 0)
        {
            Vector2Int workingLocation = workingList.Dequeue();
            // set the biome
            BiomeMap_LowResolution[workingLocation.x, workingLocation.y] = biomeIndex;
            visited[workingLocation.x, workingLocation.y] = true;
            BiomeStrengths_LowResolution[workingLocation.x, workingLocation.y] = targetIntensity[workingLocation.x, workingLocation.y];
            // traverse the neighbours
            for (int neighbourIndex = 0; neighbourIndex < NeighbourOffsets.Length; ++neighbourIndex)
            {
                Vector2Int neighbourLocation = workingLocation + NeighbourOffsets[neighbourIndex];
                // skip if invalid
                if (neighbourLocation.x < 0 || neighbourLocation.y < 0 || neighbourLocation.x >= mapResolution || neighbourLocation.y >= mapResolution)
                    continue;
                // skip if visited
                if (visited[neighbourLocation.x, neighbourLocation.y])
                    continue;
                // flag as visited
                visited[neighbourLocation.x, neighbourLocation.y] = true;
                // work out and store neighbour strength;
                float decayAmount = Random.Range(biomeConfig.MinDecayRate, biomeConfig.MaxDecayRate) * NeighbourOffsets[neighbourIndex].magnitude;
                float neighbourStrength = targetIntensity[workingLocation.x, workingLocation.y] - decayAmount;
                targetIntensity[neighbourLocation.x, neighbourLocation.y] = neighbourStrength;
                // if the strength is too low - stop
                if (neighbourStrength <= 0)
                {
                    continue;
                }
                workingList.Enqueue(neighbourLocation);
            }
            //// cache biome config
            ////BiomeConfigSO biomeConfig = Config.Biomes[biomeIndex].Biome;
            //// pick spawn location
            ////Vector2Int spawnLocation = new Vector2Int(Random.Range(0, mapResolution), Random.Range(0, mapResolution));
            //// pick the starting intensity
            ////float startIntensity = Random.Range(biomeConfig.MinIntensity, biomeConfig.MaxIntensity);
            //// setup working list
            //Queue<Vector2Int> workingList = new Queue<Vector2Int>();
            ////workingList.Enqueue(spawnLocation);
            //// setup the visted map and target intensity map
            ////bool[,] visited = new bool[mapResolution, mapResolution];
            ////float[,] targetIntensity = new float[mapResolution, mapResolution];
            //// set the starting intensity
            ////targetIntensity[spawnLocation.x, spawnLocation.y] = startIntensity;
            //// let the oozing begin
            //while (workingList.Count > 0)
            //{
            //    Vector2Int workingLocation = workingList.Dequeue();
            //    // set the biome
            //    //BiomeMap_LowResolution[workingLocation.x, workingLocation.y] = biomeIndex;
            //    Biome_Generate[workingLocation.x, workingLocation.y] = biomeIndex;

            //    //visited[workingLocation.x, workingLocation.y] = true;
            //    //BiomeStrengths_LowResolution[workingLocation.x, workingLocation.y] = targetIntensity[workingLocation.x, workingLocation.y];
            //    // traverse the neighbours
            //    //for (int neighbourIndex = 0; neighbourIndex < NeighbourOffsets.Length; ++neighbourIndex)
            //    //{
            //    //    Vector2Int neighbourLocation = workingLocation + NeighbourOffsets[neighbourIndex];
            //    //    // skip if invalid
            //    //    if (neighbourLocation.x < 0 || neighbourLocation.y < 0 || neighbourLocation.x >= mapResolution || neighbourLocation.y >= mapResolution)
            //    //        continue;
            //    //    // skip if visited
            //    //    if (visited[neighbourLocation.x, neighbourLocation.y])
            //    //        continue;
            //    //    // flag as visited
            //    //    visited[neighbourLocation.x, neighbourLocation.y] = true;
            //    //    // work out and store neighbour strength;
            //    //    float decayAmount = Random.Range(biomeConfig.MinDecayRate, biomeConfig.MaxDecayRate) * NeighbourOffsets[neighbourIndex].magnitude;
            //    //    float neighbourStrength = targetIntensity[workingLocation.x, workingLocation.y] - decayAmount;
            //    //    targetIntensity[neighbourLocation.x, neighbourLocation.y] = neighbourStrength;
            //    //    // if the strength is too low - stop
            //    //    if (neighbourStrength <= 0)
            //    //    {
            //    //        continue;
            //    //    }
            //    //    workingList.Enqueue(neighbourLocation);
            //    //}
            //}
        }
    }
    byte CalculateHighResBiomeIndex(int lowResMapSize, int lowResX, int lowResY, float fractionX, float fractionY)
    {
        float A = BiomeMap_LowResolution[lowResX, lowResY];
        float B = (lowResX + 1) < lowResMapSize ? BiomeMap_LowResolution[lowResX + 1, lowResY] : A;
        float C = (lowResY + 1) < lowResMapSize ? BiomeMap_LowResolution[lowResX, lowResY + 1] : A;
        float D = 0;
        if ((lowResX + 1) >= lowResMapSize)
            D = C;
        else if ((lowResY + 1) >= lowResMapSize)
            D = B;
        else
            D = BiomeMap_LowResolution[lowResX + 1, lowResY + 1];
        // perform bilinear filtering
        float filteredIndex = A * (1 - fractionX) * (1 - fractionY) + B * fractionX * (1 - fractionY) *
                              C * fractionY * (1 - fractionX) + D * fractionX * fractionY;
        // build an array of the possible biomes based on the values used to interpolate
        float[] candidateBiomes = new float[] { A, B, C, D };
        // find the neighbouring biome closest to the interpolated biome
        float bestBiome = -1f;
        float bestDelta = float.MaxValue;
        for (int biomeIndex = 0; biomeIndex < candidateBiomes.Length; ++biomeIndex)
        {
            float delta = Mathf.Abs(filteredIndex - candidateBiomes[biomeIndex]);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestBiome = candidateBiomes[biomeIndex];
            }
        }
        return (byte)Mathf.RoundToInt(bestBiome);
    }
    void Perform_BiomeGeneration_HighResolution(int lowResMapSize, int highResMapSize)
    {
        // allocate the biome map and strength map
        BiomeMap = new byte[highResMapSize, highResMapSize];
        BiomeStrengths = new float[highResMapSize, highResMapSize];
        // calculate map scale
        float mapScale = (float)lowResMapSize / (float)highResMapSize;
        // calculate the high res map
        for (int y = 0; y < highResMapSize; ++y)
        {
            int lowResY = Mathf.FloorToInt(y * mapScale);
            float yFraction = y * mapScale - lowResY;
            for (int x = 0; x < highResMapSize; ++x)
            {
                int lowResX = Mathf.FloorToInt(x * mapScale);
                float xFraction = x * mapScale - lowResX;
                BiomeMap[x, y] = CalculateHighResBiomeIndex(lowResMapSize, lowResX, lowResY, xFraction, yFraction);
                // this would do no interpolation - ie. point based
                //BiomeMap[x, y] = BiomeMap_LowResolution[lowResX, lowResY];
            }
        }
        // save out the biome map
        Texture2D biomeMap = new Texture2D(highResMapSize, highResMapSize, TextureFormat.RGB24, false);
        for (int y = 0; y < highResMapSize; ++y)
        {
            for (int x = 0; x < highResMapSize; ++x)
            {
                float hue = ((float)BiomeMap[x, y] / (float)Config.NumBiomes);
                biomeMap.SetPixel(x, y, Color.HSVToRGB(hue, 0.75f, 0.75f));
            }
        }
        biomeMap.Apply();
        System.IO.File.WriteAllBytes("BiomeMap_HighResolution.png", biomeMap.EncodeToPNG());
    }
    //------------------------------------------------------------------------------------------------------------------

    byte CalculateBiomeIndex(int mapSize, int mapX, int mapY, float fractionX, float fractionY)
    {
        // the corners
        float A = Biome_Generate[mapX, mapY];
        float B = (mapX + 1) < mapSize ? Biome_Generate[mapX + 1, mapY] : A;
        float C = (mapY + 1) < mapSize ? Biome_Generate[mapX, mapY + 1] : A;
        float D = 0;
        if ((mapX + 1) >= mapSize)
            D = C;
        else if ((mapY + 1) >= mapSize)
            D = B;
        else
            D = Biome_Generate[mapX + 1, mapY + 1];
        // perform bilinear filtering
        float filteredIndex = A * (1 - fractionX) * (1 - fractionY) + B * fractionX * (1 - fractionY) *
                              C * fractionY * (1 - fractionX) + D * fractionX * fractionY;
        // build an array of the possible biomes based on the values used to interpolate
        float[] candidateBiomes = new float[] { A, B, C, D };
        // find the neighbouring biome closest to the interpolated biome
        float bestBiome = -1f;
        float bestDelta = float.MaxValue;
        for (int biomeIndex = 0; biomeIndex < candidateBiomes.Length; ++biomeIndex)
        {
            float delta = Mathf.Abs(filteredIndex - candidateBiomes[biomeIndex]);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestBiome = candidateBiomes[biomeIndex];
            }
        }
        return (byte)Mathf.RoundToInt(bestBiome);
    }
    void Perform_BiomeGeneration_WithSetPoints(int mapResolution)
    {
        // paiges testing
        Biome_Generate = new byte[mapResolution,mapResolution];

        Texture2D tex = null;
        byte[] fileData;
        BiomeMap = new byte[mapResolution, mapResolution];

        fileData = System.IO.File.ReadAllBytes("coloredPng.png");
        tex = new Texture2D(mapResolution, mapResolution);
        tex.LoadImage(fileData);

        Texture2D biomeMap = new Texture2D(mapResolution, mapResolution, TextureFormat.RGB24, false); //for test

        for (int y = 0; y < mapResolution; ++y)
        {
            for (int x = 0; x < mapResolution; ++x)
            {
                // HUE - H are degrees
                float H, S, V;
                float hue = ((float)BiomeMap[x, y] / (float)Config.NumBiomes);
                Color.RGBToHSV(tex.GetPixel(x, y), out H, out S, out V);
                if (H >= 0.5 && H < 0.8) // Blue
                {
                    H = 0.75f;
                    //BlueBiome b = new BlueBiome(new Vector2(x, y), H, S, V);
                }
                else if (H >= 0.2 && H < 0.5) // Green
                {
                    H = 0.5f;
                    //GreenBiome b = new GreenBiome(new Vector2(x, y), H, S, V);
                }
                else if (H >= 0.8 || H < 0.2) // Red
                {
                    H = 0.25f;
                    //RedBiome b = new RedBiome(new Vector2(x, y), H, S, V);
                }
                //sending out - test works
                biomeMap.SetPixel(x, y, Color.HSVToRGB(H, 0.75f, 0.75f));
                
                // seting the bytes to generate
                //BiomeMap[x, y] = Biome_Generate[x, y];
                Biome_Generate[x, y] = BiomeMap[x, y];

            }
        }
        //save out
        biomeMap.Apply();
        System.IO.File.WriteAllBytes("BiomeMap_Generated.png", biomeMap.EncodeToPNG());
    }



    public int GetLayerForTexture(string uniqueID)
    {
        return BiomeTextureToTerrainLayerIndex[uniqueID];
    }


    void Perform_TerrainPainting(int mapResolution, int alphaMapResolution)
    {
        float[,] heightMap = TargetTerrain.terrainData.GetHeights(0, 0, mapResolution, mapResolution);
        float[,,] alphaMaps = TargetTerrain.terrainData.GetAlphamaps(0, 0, alphaMapResolution, alphaMapResolution);

        float[,] slopeMap = new float[alphaMapResolution, alphaMapResolution];

        // generate the slope map
        for (int y = 0; y < alphaMapResolution; ++y)
        {
            for (int x = 0; x < alphaMapResolution; ++x)
            {
                slopeMap[x, y] = TargetTerrain.terrainData.GetInterpolatedNormal((float)x / alphaMapResolution, (float)y / alphaMapResolution).y;
            }
        }

        // zero out all layers
        for (int y = 0; y < alphaMapResolution; ++y)
        {
            for (int x = 0; x < alphaMapResolution; ++x)
            {
                for (int layerIndex = 0; layerIndex < TargetTerrain.terrainData.alphamapLayers; ++layerIndex)
                {
                    alphaMaps[x, y, layerIndex] = 0;
                }
            }
        }

        // run terrain painting for each biome
        for (int biomeIndex = 0; biomeIndex < Config.NumBiomes; ++biomeIndex)
        {
            var biome = Config.Biomes[biomeIndex].Biome;
            if (biome.TerrainPainter == null)
                continue;

            BaseTexturePainter[] modifiers = biome.TerrainPainter.GetComponents<BaseTexturePainter>();

            foreach (var modifier in modifiers)
            {
                //execute for assigning texture to paint*** maybe get rid of height stuff for now 
                modifier.Execute(this, mapResolution, heightMap, TargetTerrain.terrainData.heightmapScale, slopeMap, alphaMaps, alphaMapResolution, BiomeMap, biomeIndex, biome);
            }
        }

        /* //look at later
        // run texture post processing
        if (Config.PaintingPostProcessingModifier != null)
        {
            BaseTexturePainter[] modifiers = Config.PaintingPostProcessingModifier.GetComponents<BaseTexturePainter>();

            foreach (var modifier in modifiers)
            {
                modifier.Execute(this, mapResolution, heightMap, TargetTerrain.terrainData.heightmapScale, slopeMap, alphaMaps, alphaMapResolution);
            }
        }

        */
        TargetTerrain.terrainData.SetAlphamaps(0, 0, alphaMaps);
    }

#endif // UNITY_EDITOR
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainVisualization
{
    Height,
    Heat,
    Moisture,
    Biome
}

public class TileGenerator : MonoBehaviour
{
    [Header("Parameters")]
    public int noiseSampleSize;
    public float scale;
    public float maxHeight = 1.0f;
    public int textureResolution = 1;
    public TerrainVisualization visualizationType;

    [HideInInspector]
    public Vector2 offset;

    [Header("Terrain Types")]
    public TerrainType[] heightTerrainTypes;
    public TerrainType[] heatTerrainTypes;
    public TerrainType[] moistureTerrainTypes;

    [Header("Waves")]
    public Wave[] waves;
    public Wave[] heatWaves;
    public Wave[] moistureWaves;

    [Header("Curves")]
    public AnimationCurve heightCurve;

    private MeshRenderer tileMeshRenderer;
    private MeshFilter tileMeshFilter;
    private MeshCollider tileMeshCollider;

    private MeshGenerator meshGenerator;
    private MapGenerator mapGenerator;

    private TerrainData[,] dataMap;

    void Start()
    {
        // get the tile components
        tileMeshRenderer = GetComponent<MeshRenderer>();    // what the actual mesh looks like, applies the material, lightning for the mesh (VISUAL)
        tileMeshFilter = GetComponent<MeshFilter>();        // what the mesh is, determines the vertices, the normal points, the faces (TECHNICAL)
        tileMeshCollider = GetComponent<MeshCollider>();    // allows the interactable actions (behaviours) like collisions, physics (PHYSICS)

        meshGenerator = GetComponent<MeshGenerator>();
        mapGenerator = FindObjectOfType<MapGenerator>();

        GenerateTile();
    }

    void GenerateTile()
    {
        // creates a new height map in the array that is going to be added into the mesh
        // horizontal map
        float[,] heightMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, waves, offset);

        float[,] hdHeightMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize - 1, scale, waves, offset, textureResolution);

        Vector3[] verts = tileMeshFilter.mesh.vertices; // adds the vertices, so we can manipulate them verticaly later

        for(int x = 0; x < noiseSampleSize; x++)
        {
            for(int z = 0; z < noiseSampleSize; z++)
            {
                // next index 
                int index = (x * noiseSampleSize) + z;

                verts[index].y = heightCurve.Evaluate(heightMap[x, z]) * maxHeight;
            }
        }

        tileMeshFilter.mesh.vertices = verts;
        tileMeshFilter.mesh.RecalculateBounds();
        tileMeshFilter.mesh.RecalculateNormals();

        // update mesh collider
        tileMeshCollider.sharedMesh = tileMeshFilter.mesh;

        // creates the height map texture
        Texture2D heightMapTexture = TextureBuilder.BuildTexture(hdHeightMap, heightTerrainTypes);

        float[,] heatMap = GenerateHeatMap(heightMap);
        float[,] moistureMap = GenerateMoistureMap(heightMap);

        TerrainType[,] heatTerrainTypeMap = TextureBuilder.CreateTerrainTypeMap(heatMap, heatTerrainTypes);
        TerrainType[,] moistureTerrainTypeMap = TextureBuilder.CreateTerrainTypeMap(moistureMap, moistureTerrainTypes);

        switch(visualizationType)
        {
            case TerrainVisualization.Height:
                tileMeshRenderer.material.mainTexture = TextureBuilder.BuildTexture(hdHeightMap, heightTerrainTypes);
                break;
            case TerrainVisualization.Heat:
                tileMeshRenderer.material.mainTexture = TextureBuilder.BuildTexture(heatMap, heatTerrainTypes);
                break;
            case TerrainVisualization.Moisture:
                tileMeshRenderer.material.mainTexture = TextureBuilder.BuildTexture(moistureMap, moistureTerrainTypes);
                break;
            case TerrainVisualization.Biome:
                tileMeshRenderer.material.mainTexture = BiomeBuilder.instance.BuildTexture(heatTerrainTypeMap, moistureTerrainTypeMap);
                break;
        }

        CreateDataMap(heatTerrainTypeMap, moistureTerrainTypeMap);

        TreeSpawner.instance.Spawn(dataMap);

        /* older code for each case
        apply the height map texture to the MeshRenderer
        tileMeshRenderer.material.mainTexture = heightMapTexture;*/

        /*float[,] heatMap = GenerateHeatMap(heightMap);
        tileMeshRenderer.material.mainTexture = TextureBuilder.BuildTexture(heightMap, heatTerrainTypes); used for the heat map */

        /*float[,] moistureMap = GenerateMoistureMap(heightMap);
        tileMeshRenderer.material.mainTexture = TextureBuilder.BuildTexture(moistureMap, moistureTerrainTypes); // used for the moisture map*/
    }

    void CreateDataMap(TerrainType[,] heatTerrainTypeMap, TerrainType[,] moistureTerrainTypeMap)
    {
        dataMap = new TerrainData[noiseSampleSize, noiseSampleSize];
        Vector3[] verts = tileMeshFilter.mesh.vertices;

        for(int x = 0; x < noiseSampleSize; x++)
        {
            for(int z = 0; z < noiseSampleSize; z++)
            {
                TerrainData data = new TerrainData();
                data.position = transform.position + verts[(x * noiseSampleSize) + z];
                data.heatTerrainType = heatTerrainTypeMap[x,z];
                data.moistureTerrainType = moistureTerrainTypeMap[x,z];
                data.biome = BiomeBuilder.instance.GetBiome(data.heatTerrainType, data.moistureTerrainType);

                dataMap[x,z] = data;
            }
        }
    }

    // generates a new heat map
    float[,] GenerateHeatMap(float[,] heightMap)
    {
        float[,] uniformHeatMap = NoiseGenerator.GenerateUniformNoiseMap(noiseSampleSize, transform.position.z * (noiseSampleSize / meshGenerator.xSize), (noiseSampleSize / 2 * mapGenerator.numX) + 1);
        float[,] randomHeatMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, heatWaves, offset);

        float[,] heatMap = new float[noiseSampleSize, noiseSampleSize];

        for(int x = 0; x < noiseSampleSize; x++)
        {
            for(int z = 0; z < noiseSampleSize; z++)
            {
                heatMap[x,z] = randomHeatMap[x,z] * uniformHeatMap[x,z];
                heatMap[x,z] += 0.5f * heightMap[x,z];

                heatMap[x,z] = Mathf.Clamp(heatMap[x,z], 0.0f, 0.99f);
            }
        }

        return heatMap;
    }

    // generates a new moisture map
    float[,] GenerateMoistureMap(float[,] heightMap)
    {
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(noiseSampleSize, scale, moistureWaves, offset);

        for(int x = 0; x < noiseSampleSize; x++)
        {
            for(int z = 0; z < noiseSampleSize; z++)
            {
                //moistureMap[x,z] += heightMap[x,z] * heightMap[x,z];
                moistureMap[x,z] -= 0.1f * heightMap[x,z];
            }
        }

        return moistureMap;
    }
}

[System.Serializable]
public class TerrainType
{
    public int index;
    [Range(0.0f, 1.0f)]
    public float threshold;
    // public Color color; used a solid color before
    public Gradient colorGradient;
}

public class TerrainData
{
    public Vector3 position;
    public TerrainType heatTerrainType;
    public TerrainType moistureTerrainType;
    public Biome biome;
}
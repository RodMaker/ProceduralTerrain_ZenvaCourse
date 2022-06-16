using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public TileGenerator tilePrefab;
    public int numX = 2;
    public int numZ = 2;

    void Start()
    {
        GenerateTiles();
    }

    void GenerateTiles()
    {
        // get the size of the tile
        float tileSize = tilePrefab.GetComponent<MeshGenerator>().xSize;

        for(int x = 0; x < numX; x++)
        {
            for(int z = 0; z < numZ; z++)
            {
                GameObject tileObj = Instantiate(tilePrefab.gameObject, transform);
                tileObj.transform.position = new Vector3((x - ((float)numX / 2)) * tileSize, 0, (z - ((float) numZ / 2)) * tileSize);

                // set the tile's offset, centered on the (0,0)
                float offsetRate = (tilePrefab.noiseSampleSize - 1) / tilePrefab.scale;
                tileObj.GetComponent<TileGenerator>().offset = new Vector2(x * offsetRate, z * offsetRate);
            }
        }
    }
}

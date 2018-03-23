using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class generates terrain, creates mesh and colliders and adds ability to place or remove blocks.
/// </summary>
public class VoxelFarm : MonoBehaviour {
    GameDataController gdController;
    const float WAIT_TIME = (float)1 / 30;

    public int seed = 1231;                         // generation seed
    int generationSize = 7;                         // number of blocks to show at any given time in each direction
    int maxHeight = 50;                             // maximum height of generated terrain
    float detailScale = 25;

	const int MAP_HEIGHT = 65;
    const int SAND_LEVEL = 15;
    const int SNOW_LEVEL = 35;
    const int WATER_LEVEL = 10;
    const int STONE_LEVEL = -15;

    float textureSizeInAtlas = .25f;                // size of block texture in square atlas

    bool terrainGenerated = false;
    public bool TerrainGenerated {
        get { return terrainGenerated; }
    }

    bool meshGenerated = false;
    public bool MeshGenerated {
        get { return meshGenerated; }
    }

    // mesh data
    List<Vector3> meshVertices = new List<Vector3>();
    List<int> meshTriangles = new List<int>();
    List<Vector2> meshUV = new List<Vector2>();
    int faceCount;

    // mesh related components
    MeshRenderer vfRenderer;
    
    // blocks
    SortedList<Vector3, BlockType> allBlocks = new SortedList<Vector3, BlockType>(new Vector3Comparer());
    SortedList<Vector3, BlockType> playerBlocks = new SortedList<Vector3, BlockType>(new Vector3Comparer());

    int lod = 1;            // 1 means full, 2 means invisible
    public int LOD {
        get { return lod; }
    }

    void Awake() {
        // initialize
        gdController = GameDataController.Instance;
        generationSize = (int)(MapGenerator.Instance.ChunkSize/2);
        vfRenderer = GetComponent<MeshRenderer>();

        // set lists capacity
        allBlocks.Capacity = (2 * generationSize + 1) * (2 * generationSize + 1) * MAP_HEIGHT;
        meshVertices.Capacity = 24 * (2 * generationSize + 1) * (2 * generationSize + 1) * MAP_HEIGHT;
        meshTriangles.Capacity = 6 * (2 * generationSize + 1) * (2 * generationSize + 1) * MAP_HEIGHT;
        meshUV.Capacity = 4 * (2 * generationSize + 1) * (2 * generationSize + 1) * MAP_HEIGHT;

        // load player blocks
        playerBlocks = gdController.GetPlayerBlocks(transform.position);

        for (int i = 0; i < playerBlocks.Count; i++)
            allBlocks[playerBlocks.Keys[i]] = playerBlocks.Values[i];
    }

    /// <summary>
    /// Function generates blocks which will form a terrain. 
    /// </summary>
    /// <param name="atOnce">runs in one frame if true</param>
    /// <returns></returns>
    public IEnumerator GenerateTerrainData(bool atOnce = false) {
        WaitForSeconds tekabrek = new WaitForSeconds(WAIT_TIME);

        terrainGenerated = false;
        Vector3 blockPosition = Vector3.zero;
        BlockType bt;

        int frames = 0;

        // generate map from perlin noise
        for (int i = -generationSize; i <= generationSize; ++i) {
            for (int j = -generationSize; j <= generationSize; ++j) {
                // determine altitude of the terrain at given coordinate
                int numberOfBlocks = (int)(Mathf.PerlinNoise((0.1f + seed + transform.position.x + i) / detailScale, (0.1f + seed + transform.position.z + j) / detailScale) * maxHeight);

                blockPosition = new Vector3(transform.position.x + i, numberOfBlocks, transform.position.z + j);

                for (int k = numberOfBlocks; k >= 0; --k) {
                    blockPosition = new Vector3(transform.position.x + i, k, transform.position.z + j);

                    // manually set block type based on altitude since we only have 4 block types
                    if (k < SAND_LEVEL)
                        bt = BlockType.Sand;
                    else if (k < SNOW_LEVEL)
                        bt = BlockType.Grass;
                    else
                        bt = BlockType.Snow;

                        allBlocks[blockPosition] = bt;
                }

                // fill with water
                for (int k = numberOfBlocks + 1; k < WATER_LEVEL; ++k) {
                    blockPosition = new Vector3(transform.position.x + i, k, transform.position.z + j);
                        allBlocks[blockPosition] = BlockType.Water;
                }

                // add stone for bottom
                for (int k = -1; k > STONE_LEVEL; k--) {
                    blockPosition = new Vector3(transform.position.x + i, k, transform.position.z + j);
                        allBlocks[blockPosition] = BlockType.Stone;
                }

                if (!atOnce && frames%5 == 0)
                    yield return tekabrek;

                frames++;
            }
        }

        terrainGenerated = true;
        yield break;
    }

    /// <summary>
    /// Function generates mesh data from terrain data.
    /// </summary>
    /// <param name="atOnce">runs in one frame if true</param>
    /// <param name="overwrite">overwrites current mesh if true, does nothing otherwise if mesh exists</param>
    /// <returns></returns>
    public IEnumerator GenerateMesh(bool atOnce = false, bool overwrite = false) {
        if (meshGenerated && !overwrite)
            yield break;
        meshGenerated = false;

        for (int i = 0; i < allBlocks.Count; ++i) {
            if (allBlocks.Values[i] == BlockType.None)
                continue;

            if(!allBlocks.ContainsKey(allBlocks.Keys[i].WithY(allBlocks.Keys[i].y + 1)) 
                || allBlocks[allBlocks.Keys[i].WithY(allBlocks.Keys[i].y + 1)] == BlockType.Water 
                || allBlocks[allBlocks.Keys[i].WithY(allBlocks.Keys[i].y + 1)] == BlockType.None) {
                // if there is no block above
                AddSquare(allBlocks.Keys[i], SquarePosition.top, allBlocks.Values[i]);
            }
            if (!allBlocks.ContainsKey(allBlocks.Keys[i].WithY(allBlocks.Keys[i].y - 1))
                || allBlocks[allBlocks.Keys[i].WithY(allBlocks.Keys[i].y - 1)] == BlockType.None) {
                // if there is no block below
                AddSquare(allBlocks.Keys[i], SquarePosition.bottom, allBlocks.Values[i]);
            }
            if (!allBlocks.ContainsKey(allBlocks.Keys[i].WithX(allBlocks.Keys[i].x - 1))
                || allBlocks[allBlocks.Keys[i].WithX(allBlocks.Keys[i].x - 1)] == BlockType.Water
                || allBlocks[allBlocks.Keys[i].WithX(allBlocks.Keys[i].x - 1)] == BlockType.None) {
                // if there is no block to the left
                AddSquare(allBlocks.Keys[i], SquarePosition.left, allBlocks.Values[i]);
            }
            if (!allBlocks.ContainsKey(allBlocks.Keys[i].WithX(allBlocks.Keys[i].x + 1))
                || allBlocks[allBlocks.Keys[i].WithX(allBlocks.Keys[i].x + 1)] == BlockType.Water
                || allBlocks[allBlocks.Keys[i].WithX(allBlocks.Keys[i].x + 1)] == BlockType.None) {
                // if there is no block to the right
                AddSquare(allBlocks.Keys[i], SquarePosition.right, allBlocks.Values[i]);
            }
            if (!allBlocks.ContainsKey(allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z + 1))
                || allBlocks[allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z + 1)] == BlockType.Water
                || allBlocks[allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z + 1)] == BlockType.None) {
                // if there is no block to the front
                AddSquare(allBlocks.Keys[i], SquarePosition.front, allBlocks.Values[i]);
            }
            if (!allBlocks.ContainsKey(allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z - 1))
                || allBlocks[allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z - 1)] == BlockType.Water
                || allBlocks[allBlocks.Keys[i].WithZ(allBlocks.Keys[i].z - 1)] == BlockType.None) {
                // if there is no block to the back
                AddSquare(allBlocks.Keys[i], SquarePosition.back, allBlocks.Values[i]);
            }

            if (!atOnce && i % 150 == 0)
                yield return null;//tekabrek;
        }

        UpdateMesh();
        
        meshGenerated = true;
        yield break;
    }

    /// <summary>
    /// Creates mesh and colliders from mesh data.
    /// </summary>
    void UpdateMesh() {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = meshVertices.ToArray();
        mesh.uv = meshUV.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        mesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = mesh;
        
        meshVertices.Clear();
        meshUV.Clear();
        meshTriangles.Clear();
        faceCount = 0;
    }

    /// <summary>
    /// Functions creates square at given position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="squarePosition"></param>
    /// <param name="blockType"></param>
    void AddSquare(Vector3 position, SquarePosition squarePosition, BlockType blockType) {
        switch(squarePosition) {
            case SquarePosition.top:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y + .5f, position.z - .5f)));
                break;
            case SquarePosition.bottom:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y - .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y - .5f, position.z + .5f)));
                break;
            case SquarePosition.left:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x -.5f, position.y - .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x -.5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x -.5f, position.y + .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x -.5f, position.y - .5f, position.z - .5f)));
                break;
            case SquarePosition.right:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z + .5f)));
                break;
            case SquarePosition.front:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y + .5f, position.z + .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y - .5f, position.z + .5f)));
                break;
            case SquarePosition.back:
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y - .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x - .5f, position.y + .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y + .5f, position.z - .5f)));
                meshVertices.Add(transform.InverseTransformPoint(new Vector3(position.x + .5f, position.y - .5f, position.z - .5f)));                
                break;
            default:
                return;
        }

        Vector2 texturePositionInAtlas = new Vector2((int)blockType % (1 / textureSizeInAtlas), (int)((int)blockType * textureSizeInAtlas));        // get texture position in atlas from block type (very smart)

        TextureFace(texturePositionInAtlas);
    }

    /// <summary>
    /// Function creates triangles from vertices and record in meshUV with specific texture.
    /// </summary>
    /// <param name="texturePos"></param>
    void TextureFace(Vector2 texturePos) {
        // 2 triangles for square, 4 points
        meshTriangles.Add(faceCount * 4);            // 1
        meshTriangles.Add(faceCount * 4 + 1);        // 2
        meshTriangles.Add(faceCount * 4 + 2);        // 3
        meshTriangles.Add(faceCount * 4);            // 1
        meshTriangles.Add(faceCount * 4 + 2);        // 3
        meshTriangles.Add(faceCount * 4 + 3);        // 4
        
        meshUV.Add(new Vector2(textureSizeInAtlas * texturePos.x + textureSizeInAtlas, textureSizeInAtlas * texturePos.y));
        meshUV.Add(new Vector2(textureSizeInAtlas * texturePos.x + textureSizeInAtlas, textureSizeInAtlas * texturePos.y + textureSizeInAtlas));
        meshUV.Add(new Vector2(textureSizeInAtlas * texturePos.x, textureSizeInAtlas * texturePos.y + textureSizeInAtlas));
        meshUV.Add(new Vector2(textureSizeInAtlas * texturePos.x, textureSizeInAtlas * texturePos.y));

        faceCount++;
    }

    /// <summary>
    /// Returns hardness of the block with given position.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float GetBlockHardness(Vector3 position) {
        return Block.GetHardness(allBlocks[position]);
    }
    
    /// <summary>
    /// Function adds block with given position and block type into the terrain data and generates mesh.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="blockType"></param>
    public void PlaceBlock(Vector3 position, BlockType blockType) {
        allBlocks[position] = blockType;
        playerBlocks[position] = blockType;
        StartCoroutine(GenerateMesh(overwrite: true, atOnce: true));

        gdController.PlaceBlock(transform.position, position, blockType);
    }
    
    /// <summary>
    /// Function frees resources and 'resets' the instance.
    /// Needs to be called before returning object to ObjectPool.
    /// </summary>
    public void UnloadFarm() {
        if (!terrainGenerated)
            return;

        terrainGenerated = false;
        meshGenerated = false;
        StopAllCoroutines();
        allBlocks.Clear();
        playerBlocks.Clear();
        allBlocks.Clear();
    }

    /// <summary>
    /// Function sets Level Of Detail of this vortex farm.
    /// </summary>
    /// <param name="lod">1 means show, anything else disables the mesh renderer</param>
    public void SetLOD(int lod = 1) {
        if (lod == 1)
            vfRenderer.enabled = true;
        else
            vfRenderer.enabled = false;

        this.lod = lod;
    }
}

/// <summary>
/// Extension class to allow for quick modification of Vector3
/// </summary>
public static class Vector3Extensions {
    public static Vector3 WithX(this Vector3 vector3, float x) {
        return new Vector3(x, vector3.y, vector3.z);
    }

    public static Vector3 WithY(this Vector3 vector3, float y) {
        return new Vector3(vector3.x, y, vector3.z);
    }

    public static Vector3 WithZ(this Vector3 vector3, float z) {
        return new Vector3(vector3.x, vector3.y, z);
    }
}

/// <summary>
/// Custom Vector3 comparer used to sort lists by x, z and then y (into collumns).
/// </summary>
public class Vector3Comparer : IComparer<Vector3> {
    public int Compare(Vector3 x, Vector3 y) {
        if (x.x < y.x)
            return -1;
        else if (x.x == y.x && x.z < y.z)
            return -1;
        else if (x.x == y.x && x.z == y.z && x.y < y.y)
            return -1;
        else if (x.x == y.x && x.z == y.z && x.y == y.y)
            return 0;
        return 1;
    }
}

public enum SquarePosition {
    top,
    bottom,
    left,
    right,
    front,
    back
}
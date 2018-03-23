using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class is responsible for managing vortex farms which create dynamic meshes from perlin noise generated geometry.
/// </summary>
public class MapGenerator : MonoBehaviour {
    public static MapGenerator Instance;
    const float WAIT_TIME = (float)1 / 20;
    
    public List<Transform> voxelFarms = new List<Transform>();

    const int MINIMUM_RENDER_DISTANCE = 1;      // minimum number of voxel farms (chunks) rendered in each direction, total number pow(1 + MINIMUM_RENDER_DISTANCE, 2)
    const int MAXIMUM_RENDER_DISTANCE = 10;

    [Range(MINIMUM_RENDER_DISTANCE, MAXIMUM_RENDER_DISTANCE)]
    public int renderDistance = MINIMUM_RENDER_DISTANCE;
    
    int chunkSize = 15;
    public int ChunkSize {
        get { return chunkSize; }
    }

    // player positions used for dynamically determining which voxel farms to display
    Vector3 lastPlayerPosition = Vector3.zero;
    Vector3 playerAtVoxelFarm = Vector3.zero;          // position of voxel farm of player
    public Vector3 PlayerAtVoxelFarm {
        set { playerAtVoxelFarm = value; }
    }

    bool generating = true;
    public bool Generated {
        get { return !generating; }
    }

    WaitForSeconds tekabrek;

	void Awake () {
        if(!Instance)
            Instance = this;

        tekabrek = new WaitForSeconds(WAIT_TIME);
    }

    void Update() {
        // check position of player and see if we have to rerender map
        if (playerAtVoxelFarm != lastPlayerPosition && !generating) {
            StartCoroutine(GenerateMap());
        }
    }

    /// <summary>
    /// Updates/creates voxel farms that need to be shown and destroys unused ones.
    /// </summary>
    /// <param name="atOnce"></param>
    /// <returns></returns>
    public IEnumerator GenerateMap(bool atOnce = false) {
        generating = true;

        List<Transform> updatedVoxelFarms = new List<Transform>();

        // generate chunks around player position
        for (int i = -renderDistance - 1; i <= renderDistance + 1; ++i) {
            for (int j = -renderDistance - 1; j <= renderDistance + 1; ++j) {
                Vector3 vfPosition = playerAtVoxelFarm + new Vector3(i * chunkSize, 0, j * chunkSize);
                GameObject vf = null;

                // check if farm already exists, if not, create one
                bool exists = false;
                int k = 0;
                while(k < voxelFarms.Count) {
                    if (voxelFarms[k].position == vfPosition) {
                        exists = true;
                        updatedVoxelFarms.Add(voxelFarms[k]);

                        if (i >= -renderDistance && i <= renderDistance && j >= -renderDistance && j <= renderDistance)
                            voxelFarms[k].GetComponent<VoxelFarm>().SetLOD(1);
                        else voxelFarms[k].GetComponent<VoxelFarm>().SetLOD(2);

                        voxelFarms.RemoveAt(k);
                        break;
                    }
                    k++;
                }
                if (!exists) {
                    vf = ObjectPool.Instance.InstantiateObject(PooledObjects.voxelFarm, vfPosition);
                    updatedVoxelFarms.Add(vf.transform);
                    StartCoroutine(vf.GetComponent<VoxelFarm>().GenerateTerrainData(atOnce));

                    if (i >= -renderDistance && i <= renderDistance && j >= -renderDistance && j <= renderDistance)
                        vf.GetComponent<VoxelFarm>().SetLOD(1);
                    else vf.GetComponent<VoxelFarm>().SetLOD(2);
                }
            }
        }

        // delete voxelFarms outside of render distance
        while(voxelFarms.Count > 0) {
            voxelFarms[0].GetComponent<VoxelFarm>().UnloadFarm();
            ObjectPool.Instance.Free(PooledObjects.voxelFarm, voxelFarms[0]);
            voxelFarms.RemoveAt(0);
        }
        voxelFarms = new List<Transform>(updatedVoxelFarms);
        updatedVoxelFarms.Clear();

        // sort farms so that visible ones are prioritized
        voxelFarms.Sort((t1, t2) => {
            if (t1.GetComponent<VoxelFarm>().LOD == t2.GetComponent<VoxelFarm>().LOD)
                return 0;
            if (t1.GetComponent<VoxelFarm>().LOD < t2.GetComponent<VoxelFarm>().LOD)
                return -1;
            else return 1;
        });

        // fill chunks
        for (int i = 0; i < voxelFarms.Count; ++i) { 
            VoxelFarm vf = voxelFarms[i].GetComponent<VoxelFarm>();

            while (!vf.TerrainGenerated)
                yield return tekabrek;

            vf.StartCoroutine(vf.GenerateMesh(atOnce));

            while (!vf.MeshGenerated)
                yield return tekabrek;
        }

        lastPlayerPosition = playerAtVoxelFarm;
        generating = false;
    }

    /// <summary>
    /// Function makes a call to correct voxel farm to place a block of blockType at give position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="blockType"></param>
    public void PlaceBlock(Vector3 position, BlockType blockType) {
        Vector3 farmPosition = position / chunkSize;
        farmPosition = new Vector3(Mathf.Round(farmPosition.x), 0, Mathf.Round(farmPosition.z));
        farmPosition = new Vector3(farmPosition.x * chunkSize, 0, farmPosition.z * chunkSize);

        for (int i = 0; i < voxelFarms.Count; ++i) {
            // find farm where we putting the block
            if(voxelFarms[i].position == farmPosition) {
                // call spawnblock
                voxelFarms[i].GetComponent<VoxelFarm>().PlaceBlock(position, blockType);
            }
        }
    }

    /// <summary>
    /// Returns hardness of the block with given position.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float GetBlockHardness(Vector3 position) {
        Vector3 farmPosition = position / chunkSize;
        farmPosition = new Vector3(Mathf.Round(farmPosition.x), 0, Mathf.Round(farmPosition.z));
        farmPosition = new Vector3(farmPosition.x*chunkSize, 0, farmPosition.z*chunkSize);
        
        for (int i = 0; i < voxelFarms.Count; ++i) {
            // find farm where we putting the block
            if (voxelFarms[i].position == farmPosition) {
                return voxelFarms[i].GetComponent<VoxelFarm>().GetBlockHardness(position);
            }
        }

        return Block.GetHardness(BlockType.None);
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Class used for saving/loading of game data.
/// 
/// Savefile template:
/// player.x,player.y,player.z;
/// voxelfarm1.x,voxelfarm1.y,voxelfarm1.z
/// playerblock1.x,playerblock1.y,playerblock1.z,(int)blocktype1
/// playerblock2.x,playerblock2.y,playerblock2.z,(int)blocktype2*
/// voxelfarm2.x,voxelfarm2.y,voxelfarm2.z
/// playerblock1.x,playerblock1.y,playerblock1.z,(int)blocktype1
/// ...
/// ...
/// </summary>
public class GameDataController : MonoBehaviour {
    public static GameDataController Instance;

    Vector3 playerPosition;
    SortedList<Vector3, SortedList<Vector3, BlockType>> voxelFarms = new SortedList<Vector3, SortedList<Vector3, BlockType>>(new Vector3Comparer());

    string gameFilePath;
	
	void Awake () {
        if (!Instance)
            Instance = this;

        gameFilePath = Application.persistentDataPath + "/" + "dataFile";
        playerPosition = new Vector3(0, 45, 0);
        LoadGame();
	}
	
	/// <summary>
    /// Public function that saves a block.
    /// </summary>
    /// <param name="voxelFarmPosition"></param>
    /// <param name="blockPosition"></param>
    /// <param name="blockType"></param>
	public void PlaceBlock(Vector3 voxelFarmPosition, Vector3 blockPosition, BlockType blockType) {
        if (!voxelFarms.ContainsKey(voxelFarmPosition))
            voxelFarms[voxelFarmPosition] = new SortedList<Vector3, BlockType>(new Vector3Comparer());
        voxelFarms[voxelFarmPosition][blockPosition] = blockType;
    }

    /// <summary>
    /// Returns sorted list of specific voxel farm.
    /// </summary>
    /// <param name="voxelFarmPosition"></param>
    /// <returns></returns>
    public SortedList<Vector3,BlockType> GetPlayerBlocks(Vector3 voxelFarmPosition) {
        if (voxelFarms.ContainsKey(voxelFarmPosition))
            return voxelFarms[voxelFarmPosition];
        else return new SortedList<Vector3, BlockType>(new Vector3Comparer());
    }

    /// <summary>
    /// Returns player position.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPlayerPosition() {
        return playerPosition;
    }

    /// <summary>
    /// Saves game file.
    /// </summary>
    /// <param name="playerPosition"></param>
    public void SaveGame(Vector3 playerPosition) {
        StreamWriter sw = new StreamWriter(gameFilePath);
        sw.WriteLine(playerPosition.x + "," + playerPosition.y + "," + playerPosition.z + ";");

        for(int i = 0; i < voxelFarms.Count; i++) {
            sw.WriteLine(voxelFarms.Keys[i].x + "," + voxelFarms.Keys[i].y + "," + voxelFarms.Keys[i].z);
            for (int j = 0; j < voxelFarms.Values[i].Count; j++) {
                sw.WriteLine(voxelFarms.Values[i].Keys[j].x + "," + voxelFarms.Values[i].Keys[j].y + "," + voxelFarms.Values[i].Keys[j].z + "," + (int)voxelFarms.Values[i].Values[j]/*+ ", " + additional parameters if needed in the future*/);
            }
            sw.Write("*\n");
        }
        sw.Close();
    }

    /// <summary>
    /// With all the magical powers of string.Split, reconstructs the virtual world made of blocks.
    /// </summary>
    void LoadGame() {
        if (!File.Exists(gameFilePath))
            return;

        StreamReader sr = new StreamReader(gameFilePath);
        string file = sr.ReadToEnd();
        sr.Close();

        if (file.Length == 0)
            return;

        string[] splitFile = file.Split(';');

        if (splitFile[0] != "" && splitFile[0] != null) {
            string[] playerPos = splitFile[0].Split(',');
            playerPosition = new Vector3(float.Parse(playerPos[0]), float.Parse(playerPos[1]), float.Parse(playerPos[2]));
        }

        if(splitFile[1] != "" && splitFile[1] != null) {
            string[] VFs = splitFile[1].Split('*');

            // for each saved voxel farm
            for(int i = 0; i < VFs.Length-1; i++) {
                Vector3 vfPosition = new Vector3();
                string[] currentFarm = VFs[i].Split('\n');

                for (int j = 1; j < currentFarm.Length-1; j++) {
                    string[] line = currentFarm[j].Split(',');
                    // save vf
                    if (j == 1) {
                        vfPosition = new Vector3(float.Parse(line[0]), float.Parse(line[1]), float.Parse(line[2]));
                        continue;
                    }

                    // save block
                    PlaceBlock(vfPosition, new Vector3(float.Parse(line[0]), float.Parse(line[1]), float.Parse(line[2])), (BlockType)int.Parse(line[3]));
                }
            }
        }
    }
}

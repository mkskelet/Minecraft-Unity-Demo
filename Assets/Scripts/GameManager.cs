using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Starts the game, initializes object pool, calls MapGenerator to generate map.
/// Adds save and exit functionality.
/// </summary>
public class GameManager : MonoBehaviour {
    Transform player;
    MapGenerator mg;
    public GameObject loadingUI;

    bool initialized = false;

    void Start() {
        StartCoroutine(Initialize());
    }

	IEnumerator Initialize () {
        mg = MapGenerator.Instance;
        player = GameObject.Find("Player").transform;
        player.position = GameDataController.Instance.GetPlayerPosition();

        SetPlayerPosition();

        yield return new WaitForSeconds(1);     // wait a second so player can enjoy loading screen

        // initialize object pool
        ObjectPool.Instance.Initialize(PooledObjects.cube, 1);
        ObjectPool.Instance.Initialize(PooledObjects.voxelFarm, (mg.renderDistance + 2) * (mg.renderDistance + 2) * 2);
        ObjectPool.Instance.Initialize(PooledObjects.terrainCollider, 0);
        ObjectPool.Instance.Initialize(PooledObjects.empty, (mg.renderDistance + 2) * (mg.renderDistance + 2) * 2);

        while (!ObjectPool.Instance.Initialized)
            yield return null;

        // start generating map
        StartCoroutine(mg.GenerateMap(true));

        while(!mg.Generated)
            yield return null;

        yield return new WaitForSeconds(1);     // wait a second so player can enjoy loading screen

        // disable loading UI
        loadingUI.SetActive(false);

        // enable player at start of the game
        if (player.GetComponent<Rigidbody>().isKinematic) {
            player.GetComponent<Rigidbody>().isKinematic = false;
            player.GetComponent<Collider>().enabled = true;
            player.GetComponent<PlayerController>().enabled = true;
            player.GetComponent<PlayerController>().Start();
            Cursor.visible = false;
        }

        initialized = true;
    }
	
	void Update () {
        if (!initialized)
            return;

        // update player position
        //mg.PlayerAtVoxelFarm = player.position - new Vector3(player.position.x % mg.ChunkSize, player.position.y, player.position.z % mg.ChunkSize);
        SetPlayerPosition();

        // save and exit the game
        if (Input.GetKeyDown(KeyCode.Escape)) {
            GameDataController.Instance.SaveGame(player.position);
            StartCoroutine(WaitForExit());
        }
	}

    void SetPlayerPosition() {
        Vector3 farmPosition = player.position / mg.ChunkSize;
        farmPosition = new Vector3(Mathf.Round(farmPosition.x), 0, Mathf.Round(farmPosition.z));
        farmPosition = new Vector3(farmPosition.x * mg.ChunkSize, 0, farmPosition.z * mg.ChunkSize);
        mg.PlayerAtVoxelFarm = farmPosition;
    }

    IEnumerator WaitForExit() {
        // waiting a bit to make sure game gets saved before exiting
        yield return new WaitForSeconds(1);
        Application.Quit();
    }
}

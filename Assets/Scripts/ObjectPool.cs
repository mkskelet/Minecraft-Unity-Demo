using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used for preloading and instantiating game objects.
/// </summary>
public class ObjectPool : MonoBehaviour {
    public static ObjectPool Instance;
    bool initialized = false;
    public bool Initialized {
        get {
            initialized = true;
            foreach(bool i in initializing) {
                if (i == true) initialized = false;
            }
            return initialized;
        }
    }

    List<GameObject>[] pool;

    void Awake () {
        if(!Instance)
            Instance = this;

        // initialize list for each PooledObjects type
        int numberOfPooledObjectTypes = System.Enum.GetValues(typeof(PooledObjects)).Length;
        pool = new List<GameObject>[numberOfPooledObjectTypes];
        for(int i = 0; i < numberOfPooledObjectTypes; ++i) {
            pool[i] = new List<GameObject>();
        }
    }

    string[] resourceNames = { "Blocks/Cube", "VoxelFarm", "TerrainCollider", "Empty" };
    bool[] initializing = { false, false, false, false };
	
	public void Initialize (PooledObjects objectType, int size) {
        initializing[(int)objectType] = true;
        StartCoroutine(CreateObjects(objectType, size));
	}

    IEnumerator CreateObjects(PooledObjects objectType, int count) {
        for (int i = 0; i < count; ++i) {
            GameObject g = Instantiate(Resources.Load(resourceNames[(int)objectType]), Vector3.zero, Quaternion.identity, transform) as GameObject;
            g.SetActive(false);
            pool[(int)objectType].Add(g);
        }

        initializing[(int)objectType] = false;
        yield break;
    }

    /// <summary>
    /// Function returns GameObject to object pool.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectToReturn"></param>
    public void Free (PooledObjects objectType, Transform objectToReturn) {
        objectToReturn.gameObject.SetActive(false);
        pool[(int)objectType].Add(objectToReturn.gameObject);
    }

    /// <summary>
    /// Function returns GameObject to object pool.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="objectToReturn"></param>
    public void Free(PooledObjects objectType, GameObject objectToReturn) {
        Free(objectType, objectToReturn.transform);
    }

    /// <summary>
    /// Function creates GameObject of specific objectType, if object doesn't exit in object pool, it gets instantiated.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="activate"></param>
    /// <returns></returns>
    public GameObject InstantiateObject(PooledObjects objectType, Vector3 position, Quaternion rotation, bool activate = true) {
        // if pool is empty, instantiate
        if (pool[(int)objectType].Count == 0) {
            GameObject g = Instantiate(Resources.Load(resourceNames[(int)objectType]), Vector3.zero, Quaternion.identity, transform) as GameObject;
            pool[(int)objectType].Add(g);
        }

        // return first available object
        Transform obj = pool[(int)objectType][0].transform;
        obj.position = position;
        obj.rotation = rotation;
        pool[(int)objectType].RemoveAt(0);
        obj.gameObject.SetActive(activate);

        return obj.gameObject;
    }

    /// <summary>
    /// Function creates GameObject of specific objectType, if object doesn't exit in object pool, it gets instantiated.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="position"></param>
    /// <param name="activate"></param>
    /// <returns></returns>
    public GameObject InstantiateObject(PooledObjects objectType, Vector3 position, bool activate = true) {
        return InstantiateObject(objectType, position, Quaternion.identity, activate);
    }
}

public enum PooledObjects {
    cube = 0,
    voxelFarm,
    terrainCollider,
    empty
}

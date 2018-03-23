using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class enables player placing and deleting blocks in the scene during runtime.
/// Also updates selected block on the UI.
/// </summary>
public class BlockPlacer : MonoBehaviour {
    public MapGenerator mapGenerator;

    public Transform aimCube;

    Vector3 placePosition;          // position of newly placed object
    Vector3 lastDeletePosition;     // last position of object to delete
    Vector3 deletePosition;         // position of object to delete
    float timeToDelete;
    float currentTime;

    public Color inactiveColor;     // color of the UI element when inactive
    public Color activeColor;       // color of the UI element when inactive

    // HUD
    public GameObject[] blocksUI;       // UI elements of blocks
    public BlockType[] blockTypes;      // block types of UI blocks
    public Text blockDestructionText;   // HUD text to notify player about time to destroy the object

    int selectedBlock = 1;
    public int SelectedBlock {
        get { return selectedBlock; }
        set {
            if (selectedBlock < 1 || selectedBlock > 8)
                return;

            blocksUI[selectedBlock-1].GetComponent<Image>().color = inactiveColor;
            selectedBlock = value;
            blocksUI[selectedBlock-1].GetComponent<Image>().color = activeColor;
        }
    }

    bool deleting = false;

    void Start() {
        SelectedBlock = 1;      // this is done to update UI
    }

    void Update() {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        // check if we hit anything, if not, disable aimCube 
        if (!Physics.Raycast(ray, out hit, 2.5f)) {
            aimCube.gameObject.SetActive(false);
            deleting = false;
            blockDestructionText.text = "";
            return;
        }

        // set placePosition based on hit point and normal
        placePosition = hit.point + hit.normal * 0.5f;
        placePosition = new Vector3(Mathf.Round(placePosition.x), Mathf.Round(placePosition.y), Mathf.Round(placePosition.z));

        // set deletePosition based on hit point and normal
        deletePosition = hit.point + (hit.normal * -.5f);
        deletePosition = new Vector3(Mathf.Round(deletePosition.x), Mathf.Round(deletePosition.y), Mathf.Round(deletePosition.z));

        // if we are trying to place block on player position, disable aimCube
        if (blockTypes[selectedBlock-1] != BlockType.None && placePosition == new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y - 1f), Mathf.Round(transform.position.z))) {
            aimCube.gameObject.SetActive(false);
            return;
        }

        // set correct position of aimCube
        if (blockTypes[selectedBlock-1] != BlockType.None)
            aimCube.position = placePosition;
        else aimCube.position = deletePosition;

        aimCube.rotation = Quaternion.identity;
        aimCube.gameObject.SetActive(true);

        if (Input.GetMouseButtonDown(0)) {
            // place block
            if (blockTypes[selectedBlock-1] != BlockType.None)
                mapGenerator.PlaceBlock(placePosition, blockTypes[selectedBlock - 1]);
            else {
                // get ready to delete block
                deleting = true;
                lastDeletePosition = deletePosition;
                currentTime = 0;
                timeToDelete = mapGenerator.GetBlockHardness(deletePosition);
                blockDestructionText.text = timeToDelete.ToString("F2");
            }
        }
        if (Input.GetMouseButton(0) && deleting && lastDeletePosition == deletePosition) {
            currentTime += Time.deltaTime;
            blockDestructionText.text = (timeToDelete - currentTime) > 0 ? (timeToDelete - currentTime).ToString("F2") : "";

            if (currentTime >= timeToDelete) {
                mapGenerator.PlaceBlock(deletePosition, blockTypes[selectedBlock - 1]);
                deleting = false;
                blockDestructionText.text = "";
            }
        }
        else {
            deleting = false;
            blockDestructionText.text = "";
        }
    }
}

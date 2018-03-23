using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class controls player movement and other inputs.
/// </summary>
public class PlayerController : MonoBehaviour {
    [Range(1, 20)]
    public int mouseSensitivity = 15;
    public float movementSpeed = 5;
    float horizontalPenaltyModifier = .7f;

    float distanceToGround;

    float minimumX = -360;
    float maximumX = 360;
    float minimumY = -90;
    float maximumY = 90;
    float rotationX = 0;
    float rotationY = 0;
    Quaternion playerRotation;
    Quaternion cameraRotation;

    float movementThreshold = .3f;      // minimum value of movement axis before moving player

    Rigidbody rg;
    public Transform cameraT;

    public void Start () {
        rg = GetComponent<Rigidbody>();
		distanceToGround = GetComponent<Collider>().bounds.extents.y;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        cameraT.localRotation = Quaternion.Euler(Vector3.zero);
        playerRotation = transform.localRotation;
        cameraRotation = cameraT.localRotation;
    }
	
	void Update () {
        // rotate player
        rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationX = ClampAngle(rotationX, minimumX, maximumX);
        Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation = playerRotation * xQuaternion;

        // rotate camera
        rotationY += Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY = ClampAngle(rotationY, minimumY, maximumY);
        Quaternion yQuaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
        cameraT.localRotation = cameraRotation * yQuaternion;
        
        // movement
        float xAxis = 0, zAxis = 0;
        if (Input.GetAxis("Vertical") > movementThreshold || Input.GetAxis("Vertical") < -movementThreshold)
            zAxis = Input.GetAxis("Vertical");
        if (Input.GetAxis("Horizontal") > movementThreshold || Input.GetAxis("Horizontal") < -movementThreshold)
            xAxis = Input.GetAxis("Horizontal");
        
        rg.velocity = (((transform.forward * zAxis) + (transform.right * xAxis * horizontalPenaltyModifier)) * movementSpeed * (IsGrounded() ? 1 : 0.5f) + new Vector3(0, rg.velocity.y, 0));
        
        // jump
        if (IsGrounded() && Input.GetKeyDown(KeyCode.Space)) {
            rg.AddForce(transform.up * 50, ForceMode.Impulse);
        }

        #region switch blocks
        // switch modes
        if(Input.GetKeyDown(KeyCode.Alpha1)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 3;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 4;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 5;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 6;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 7;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8)) {
            cameraT.GetComponent<BlockPlacer>().SelectedBlock = 8;
        }
        #endregion
    }

    bool IsGrounded() {
        return Physics.Raycast(transform.position, -Vector3.up, distanceToGround + .1f);
    }

    float ClampAngle(float angle, float min, float max) {
        if (angle < -360)
         angle += 360;
        if (angle > 360)
         angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}

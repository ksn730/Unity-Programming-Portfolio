using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRMove : MonoBehaviour
{
    Transform centerEyeAnchorTr;

    Transform oVRCameraRig;

    CharacterController controller;

    Vector3 moveDirection;
    Vector3 prevCenterPosition;
    Vector3 prevTrPosition; 


    float speed = 3f;

    bool snapTurning = false;

    // Start is called before the first frame update
    void Start()
    {
        oVRCameraRig = transform.GetChild(0);
        centerEyeAnchorTr = transform.GetChild(0).GetChild(0).GetChild(1);
        controller =GetComponent<CharacterController>();
        moveDirection = Vector3.zero;

    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x > 0.5f)
        {
            if (snapTurning == false)
            {
                snapTurning = true;
                StartCoroutine(SnapTurn(45f));
                //transform.rotation=Quaternion.Euler(0,0,0);
            }
            //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + 45, transform.eulerAngles.z);
        }
        else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x < -0.5f)
        {
            if (snapTurning == false)
            {
                snapTurning = true;
                StartCoroutine(SnapTurn(-45f));
                //transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y - 45, transform.eulerAngles.z);
        }
        controller.center = new Vector3(centerEyeAnchorTr.transform.position.x-transform.position.x,1f,centerEyeAnchorTr.transform.position.z-transform.position.z);

        
        if (controller.isGrounded)
        {
            Vector2 stickMove = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            moveDirection = new Vector3(stickMove.x, 0f, stickMove.y);
            moveDirection = centerEyeAnchorTr.TransformDirection(moveDirection);
            moveDirection.y = 0f;
            moveDirection.Normalize();
            if (centerEyeAnchorTr.localPosition.y < 1.3f)
            {
                speed = 2f;
            }
            else
            {
                speed = 3f;
            }
            moveDirection *= speed * Time.deltaTime;
        }
        moveDirection.y += Physics.gravity.y * Time.deltaTime;

        controller.Move(moveDirection);
    }

    IEnumerator SnapTurn(float angle)
    {


        oVRCameraRig.RotateAround(new Vector3(centerEyeAnchorTr.position.x, oVRCameraRig.position.y, centerEyeAnchorTr.position.z),Vector3.up,angle);

        yield return new WaitForSeconds(0.5f);
        snapTurning = false;
    }
}

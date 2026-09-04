using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class CameraFilter1 : MonoBehaviourPun
{
    Transform playerHeadTr;
    bool isSpectator;
    Camera mainCamera;
    public int floor=0;

    // Start is called before the first frame update
    void Start()
    {
        if (!photonView.IsMine)
        {
            isSpectator = true;
        }
        else
        {
            isSpectator = false;
            Destroy(this);
        }

        if (isSpectator)
        {
            playerHeadTr = transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0);
            mainCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSpectator)
        {
            if (playerHeadTr.position.y < 3.8f && floor!=1)
            {
                floor = 1;
                mainCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Player");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TransparentPlayerBody");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Floor1");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("CctvIgnore");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TopViewLight");
                mainCamera.transform.position = new Vector3(20, 69, 8);
                mainCamera.transform.GetChild(0).GetChild(4).GetComponent<Text>().text = "1층";
            }
            else if (playerHeadTr.position.y < 7.6f && playerHeadTr.position.y >= 3.8f && floor != 2)
            {
                floor = 2;
                mainCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Player");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TransparentPlayerBody");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Floor2");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("CctvIgnore");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TopViewLight");
                mainCamera.transform.position = new Vector3(20, 73, 8);
                mainCamera.transform.GetChild(0).GetChild(4).GetComponent<Text>().text = "2층";
            }
            else if (playerHeadTr.position.y < 10.8f && playerHeadTr.position.y >= 7.6f && floor != 3)
            {
                floor = 3;
                mainCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Player");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TransparentPlayerBody");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Floor3");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("CctvIgnore");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TopViewLight");
                mainCamera.transform.position = new Vector3(20, 77, 8);
                mainCamera.transform.GetChild(0).GetChild(4).GetComponent<Text>().text = "3층";
            }
            else if (playerHeadTr.position.y >= 10.8f && floor != 4)
            {
                floor = 4;
                mainCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Player");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TransparentPlayerBody");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Floor4");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("CctvIgnore");
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("TopViewLight");
                mainCamera.transform.position = new Vector3(20, 81, 8);
                mainCamera.transform.GetChild(0).GetChild(4).GetComponent<Text>().text = "4층";
            }

        }
    }
}

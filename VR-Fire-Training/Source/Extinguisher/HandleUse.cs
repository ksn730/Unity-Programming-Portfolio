using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleUse : MonoBehaviour
{
    Transform playerTr;

    ExtinguisherUse extinguisherUse;
    Transform extinguisherTr;
    bool grab = false;

    public GameObject nowImage;
    public GameObject nextImage;

    // Start is called before the first frame update
    void Start()
    {
        extinguisherUse = transform.root.GetChild(1).GetComponent<ExtinguisherUse>();
        extinguisherTr = transform.root;
    }

    // Update is called once per frame
    void Update()
    {
        if (grab == true)
        {
            if (extinguisherUse.grabNum == 3)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    extinguisherUse.grabNum = 4;
                    playerTr = transform.root;
                    extinguisherTr.SetParent(playerTr.GetChild(0).GetChild(0).GetChild(5),true);
                    extinguisherTr.localPosition = new Vector3(0, -0.41f, 0.07f);
                    extinguisherTr.localRotation = Quaternion.Euler(0, 90, 0);
                    nowImage.SetActive(false);
                    nextImage.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("RightHand"))
            {
                grab = true;
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("RightHand"))
            {
                grab = false;
            }

        }
    }
}

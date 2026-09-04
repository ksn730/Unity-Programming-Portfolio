using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NozzleHandle : MonoBehaviour
{
    Transform playerTr;

    ExtinguisherUse extinguisherUse;
    bool grab = false;
    public GameObject nowImage;
    public GameObject nextImage;


    // Start is called before the first frame update
    void Start()
    {
        extinguisherUse = transform.root.GetChild(1).GetComponent<ExtinguisherUse>();

    }

    // Update is called once per frame
    void Update()
    {
        if (grab == true)
        {
            if (extinguisherUse.grabNum == 4)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
                {
                    extinguisherUse.grabNum = 5;
                    playerTr = transform.root;
                    transform.parent.SetParent(playerTr.GetChild(0).GetChild(0).GetChild(4), true);
                    transform.parent.localPosition = new Vector3(0f, -0.025f, 0.01f);
                    transform.parent.localRotation = Quaternion.Euler(80f, 25f, 180f);
                    nowImage.SetActive(false);
                    nextImage.SetActive(true);
                }
            }
        }
        if (extinguisherUse.grabNum == 5)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                extinguisherUse.grabNum = 6;
                
                //transform.parent.GetChild(2).GetComponent<ParticleSystem>().Play();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("LeftHand"))
            {
                grab = true;
            }

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("LeftHand"))
            {
                grab = false;
            }

        }
    }

   
}

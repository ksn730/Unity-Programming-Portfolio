using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtinguisherUse : MonoBehaviour
{
    public int grabNum=0;
    Transform playerTr;

    public GameObject nowImage;
    public GameObject nextImage;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (grabNum == 1)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger,OVRInput.Controller.LTouch))
            {
                //이 밑으로 이어서!
                grabNum = 2;
                transform.parent.SetParent(playerTr.GetChild(0).GetChild(0).GetChild(4),true);
                transform.parent.localPosition = new Vector3(0.08f, -0.03f, 0.06f);
                transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                nowImage.SetActive(false);
                nextImage.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("LeftHand"))
            {
                Debug.Log("LeftHand");
                if (grabNum == 0)
                {
                    playerTr = other.transform.root;
                    grabNum = 1;
                }
            }
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.layer == 7)
        {
            if (other.gameObject.CompareTag("LeftHand"))
            {
                if (grabNum == 1)
                {
                    grabNum = 0;
                }
            }
            
        }
    }
}

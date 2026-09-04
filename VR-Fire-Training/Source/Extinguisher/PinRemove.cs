using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinRemove : MonoBehaviour
{
    ExtinguisherUse extinguisherUse;
    bool grab = false;
    bool pinGrab = false;
    
    public GameObject nowImage;
    public GameObject nextImage;

    // Start is called before the first frame update
    void Start()
    {
        extinguisherUse=transform.root.GetChild(1).GetComponent<ExtinguisherUse>();
    }

    // Update is called once per frame
    void Update()
    {
        if (grab == true)
        {
            if (extinguisherUse.grabNum == 2)
            {
                if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    pinGrab = true;

                    //transform.parent.parent.gameObject.SetActive(false);
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
                if (pinGrab == true)
                {
                    extinguisherUse.grabNum = 3;
                    StartCoroutine(VibrateController(0.3f, 0.5f, 0.4f, OVRInput.Controller.RTouch));
                    nowImage.SetActive(false);
                    nextImage.SetActive(true);
                }
            }

        }
    }

    IEnumerator VibrateController(float duration, float frequency, float amplitude, OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
        transform.parent.parent.gameObject.SetActive(false);
    }
}

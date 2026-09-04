using Oculus.Interaction.Input;
using OVRTouchSample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class NonVRPlayer : MonoBehaviour
{

    bool isGameOver=false;

    bool isVR;
    int cameraNum;
    Camera secondCamera;
    Camera mainCamera;
    private void Awake()
    {
        isVR = isPresent();
        cameraNum = 1;
    }
    // Start is called before the first frame update
    void Start()
    {
        
        if (!isVR)
        {
            GetComponent<PlayerState>().enabled = false;
            Destroy(transform.GetChild(0).GetComponent<OVRHeadsetEmulator>());
            Destroy(transform.GetChild(0).GetComponent<OVRManager>());
            Destroy(transform.GetChild(0).GetComponent<OVRCameraRig>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Camera>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Camera>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<OVRScreenFade>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Rigidbody>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<SphereCollider>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<AudioListener>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(2).GetComponent<Camera>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(4).GetComponent<HandSmokeFilter>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(5).GetComponent<HandSmokeFilter>());
            Destroy(transform.GetChild(1).GetComponent<Calibration>());
            Destroy(transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).gameObject);
        }
        
        

        if(SceneManager.GetActiveScene().name=="Lobby 1")
        {
            GameObject.Find("Canvas").GetComponent<Canvas>().worldCamera = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Camera>();
            GameObject.Find("UIHelpersParent").transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            secondCamera = transform.GetChild(0).GetChild(0).GetChild(6).GetComponent<Camera>();
            mainCamera = Camera.main;
        }

        if (SceneManager.GetActiveScene().name == "GameOver"|| SceneManager.GetActiveScene().name == "GameClear")
        {
            isGameOver = true;
            GetComponent<CameraFilter1>().enabled = false;
            secondCamera.gameObject.SetActive(false);
            if (isVR)
            {
                GameObject.Find("Canvas").GetComponent<Canvas>().worldCamera = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Camera>();
                GameObject.Find("UIHelperParent").transform.GetChild(0).GetChild(2).GetComponent<OVRInputModule>().rayTransform = transform.GetChild(0).GetChild(0).GetChild(5);
                GameObject.Find("UIHelperParent").transform.GetChild(0).gameObject.SetActive(true);
                
            }
            
        }
        if (SceneManager.GetActiveScene().name == "FireExtinguisherStage")
        {
            isGameOver = true;
            GetComponent<CameraFilter1>().enabled = false;
            mainCamera.enabled = false;
            if (!isVR)
            {
                secondCamera.transform.position = new Vector3(33.560009f, 6.70000076f, 30.7541142f);
                secondCamera.transform.rotation = Quaternion.Euler(20f, 225f, 0f);
                secondCamera.enabled = true;
            }
            
            if (isVR)
            {
                GameObject.Find("Canvas").GetComponent<Canvas>().worldCamera = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Camera>();
                GameObject.Find("Canvas (1)").GetComponent<Canvas>().worldCamera = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Camera>();
                GameObject.Find("UIHelperParent").transform.GetChild(0).GetChild(2).GetComponent<OVRInputModule>().rayTransform = transform.GetChild(0).GetChild(0).GetChild(5);
            }
            GameObject.Find("Canvas").GetComponent<Canvas>().enabled = false;
        }
        if(!(SceneManager.GetActiveScene().name== "MainStage 1"|| SceneManager.GetActiveScene().name == "MainStage 2"|| SceneManager.GetActiveScene().name == "MainStage 3"|| SceneManager.GetActiveScene().name == "MainStage 4"))
        {
            transform.GetChild(0).GetChild(0).GetChild(5).GetChild(1).GetChild(1).gameObject.SetActive(false);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isVR&&isGameOver==false)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (cameraNum == 1)
                {
                    cameraNum = 2;
                    mainCamera.enabled = false;
                    secondCamera.enabled = true;
                }
                else if (cameraNum == 2)
                {
                    cameraNum = 1;
                    secondCamera.enabled = false;
                    mainCamera.enabled = true;
                }
            }
        }
    }
    public static bool isPresent()
    {
        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
            {
                return true;
            }
        }
        return false;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isVR)
        {

            if (other.gameObject.tag == "CctvArea")
            {
                secondCamera.transform.position = other.transform.GetChild(0).position;
                secondCamera.transform.rotation = other.transform.GetChild(0).rotation;
            }
        }
    }
}

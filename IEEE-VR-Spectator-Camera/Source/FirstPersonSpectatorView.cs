using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonSpectatorView : MonoBehaviour
{
    Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = mainCamera.transform.position - transform.position;
        transform.position = transform.position + dir * Time.deltaTime / 3f;

        transform.rotation = Quaternion.Slerp(transform.rotation, mainCamera.transform.rotation, Time.deltaTime / 3f);
    }
}

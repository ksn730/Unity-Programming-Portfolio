using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CctvFireLayerChange : MonoBehaviour
{
    public GameObject cctv_fire;
    int originalNum;

    // Start is called before the first frame update
    void Start()
    {
        if(cctv_fire != null)
        {
            originalNum = cctv_fire.layer;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FireLayerChange()
    {
        if(cctv_fire!=null)
        {
            cctv_fire.layer = 15; //layer 15: CctvIgnore
            cctv_fire.transform.GetChild(0).gameObject.layer = 15;
            cctv_fire.transform.GetChild(1).gameObject.layer = 15;
            cctv_fire.transform.GetChild(2).gameObject.layer = 15;
            cctv_fire.transform.GetChild(3).gameObject.layer = 15;
            cctv_fire.transform.GetChild(4).gameObject.layer = 15;
        }
        
    }

    public void FireLayerRollBack()
    {
        if(cctv_fire!=null)
        {
            cctv_fire.layer = originalNum;
            cctv_fire.transform.GetChild(0).gameObject.layer = originalNum;
            cctv_fire.transform.GetChild(1).gameObject.layer = originalNum;
            cctv_fire.transform.GetChild(2).gameObject.layer = originalNum;
            cctv_fire.transform.GetChild(3).gameObject.layer = originalNum;
            cctv_fire.transform.GetChild(4).gameObject.layer = originalNum;
        }
        
    }
}

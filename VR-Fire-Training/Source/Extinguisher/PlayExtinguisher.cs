using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayExtinguisher : MonoBehaviourPunCallbacks
{
    ExtinguisherUse extinguisherUse;
    PhotonView pv;


    // Start is called before the first frame update
    void Start()
    {
        extinguisherUse = transform.root.GetChild(1).GetComponent<ExtinguisherUse>();
        pv= GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (extinguisherUse.grabNum == 6)
        {
            extinguisherUse.grabNum = 7;
            if (pv.IsMine)
            {
                pv.RPC("ParticlePlay", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void ParticlePlay()
    {
        GetComponent<ParticleSystem>().Play();
    }

}

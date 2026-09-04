using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ExtinguishFire : MonoBehaviour
{
    ParticleSystem ps;
    public Transform fireTr;
    List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();


    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnParticleTrigger()
    {
        if (fireTr.GetChild(0).localScale.x>0)
        {
            if (fireTr.GetChild(0).localScale.x < 0.25)
            {
                fireTr.GetChild(0).localScale = Vector3.zero;
                fireTr.GetChild(2).GetComponent<AudioSource>().volume = 0f;
                fireTr.GetChild(2).GetComponent<AudioSource>().enabled = false;
            }
            else
            {
                fireTr.GetChild(0).localScale -= (Vector3.one) / 200f;
                fireTr.GetChild(2).GetComponent<AudioSource>().volume -= 0.002f;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreathSmoke : MonoBehaviour
{

    ParticleSystem pt;
    List<ParticleCollisionEvent> collisionEvents;
    PlayerState playerState;

    private void OnEnable()
    {
        pt=GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other)
    {
        if(playerState==null)
        {
            playerState=other.transform.root.GetComponent<PlayerState>();
        }
        playerState.StopCoroutine("CheckNoSmoke");
        playerState.smoke_count_decrease = false;
        if (playerState.hand_smoke_filter == true)
        {
            playerState.player_smoke_count++;
        }
        else
        {
            playerState.player_smoke_count++;
            playerState.player_smoke_count++;
        }
        playerState.StartCoroutine("CheckNoSmoke");

    }
}

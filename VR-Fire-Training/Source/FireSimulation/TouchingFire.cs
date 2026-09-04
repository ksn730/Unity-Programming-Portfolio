using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchingFire : MonoBehaviour
{

    ParticleSystem part;
    List<ParticleCollisionEvent> collisionEvents;
    PlayerState playerState;

    private void OnEnable()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }


    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);
        if (playerState == null)
        {
            playerState = other.transform.root.GetComponent<PlayerState>();
        }
        playerState.StopCoroutine("CheckNoFire");
        playerState.player_fire_count++;
        playerState.StartCoroutine("CheckNoFire");
        other.transform.root.GetComponent<CharacterController>().Move(new Vector3((other.transform.position-this.transform.position).x,0,(other.transform.position - this.transform.position).z).normalized/2);
        if (Vector3.Cross(other.transform.root.GetChild(0).GetChild(0).GetChild(1).forward, collisionEvents[0].intersection - other.transform.root.GetChild(0).GetChild(0).GetChild(1).position).y > 0f)
        {
            StartCoroutine(VibrateController(1.5f, 0.5f, 1f, OVRInput.Controller.RTouch));
        }
        else if(Vector3.Cross(other.transform.root.GetChild(0).GetChild(0).GetChild(1).forward, collisionEvents[0].intersection - other.transform.root.GetChild(0).GetChild(0).GetChild(1).position).y < 0f)
        {
            StartCoroutine(VibrateController(1.5f, 0.5f, 1f, OVRInput.Controller.LTouch));
        }
    }

    IEnumerator VibrateController(float duration, float frequency, float amplitude,OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}

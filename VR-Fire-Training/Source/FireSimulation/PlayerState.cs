using OVR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerState : MonoBehaviour
{
    MeshRenderer smokeFogMeshRenderer;
    Material smokeFogMat;
    public AudioSource cough1;
    public AudioSource cough2;
    public AudioSource cough3;

    CharacterController characterController;

    public int player_smoke_count;
    public bool smoke_count_decrease;
    public int player_fire_count;
    bool cough_isWaiting;

    public bool hand_smoke_filter = false;

    bool game_over = false;

    int state;

    float tempTime;
    // Start is called before the first frame update
    void Start()
    {
        characterController=this.gameObject.GetComponent<CharacterController>();

        smokeFogMeshRenderer=transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<MeshRenderer>();
        smokeFogMat = smokeFogMeshRenderer.material;
        player_smoke_count = 0;
        smoke_count_decrease = false;
        player_fire_count = 0;
        cough_isWaiting = false;
        state = 1;

        tempTime=Time.time;

        if(SceneManager.GetActiveScene().name == "FireExtinguisherStage")
        {
            characterController.radius = 0.2f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player_smoke_count < 10)
        {
            if (state != 1)
            {
                state = 1;
                StopCoroutine("PlayCoughSound");
            }
            smokeFogMat.color = new Color(0.45f, 0.45f, 0.45f, 0f);
        }
        else if (player_smoke_count < 30)
        {
            if(state != 2)
            {
                state = 2;
                StopCoroutine("PlayCoughSound");
                StartCoroutine("PlayCoughSound", cough1);
            }
            if (cough_isWaiting == false)
            {
                cough_isWaiting = true;
                StartCoroutine("PlayCoughSound", cough1);
            }
            smokeFogMat.color = new Color(0.45f, 0.45f, 0.45f, player_smoke_count * 0.02f);
        }
        else if (player_smoke_count < 50)
        {
            if (state != 3)
            {
                state = 3;
                StopCoroutine("PlayCoughSound");
                StartCoroutine("PlayCoughSound", cough2);
            }
            if (cough_isWaiting == false)
            {
                cough_isWaiting = true;
                StartCoroutine("PlayCoughSound", cough2);
            }
            smokeFogMat.color = new Color(0.45f, 0.45f, 0.45f, player_smoke_count * 0.02f);
        }
        else if (player_smoke_count < 70)
        {
            if(state != 4)
            {
                state = 4;
                StopCoroutine("PlayCoughSound");
                StartCoroutine("PlayCoughSound", cough3);
            }
            if (cough_isWaiting == false)
            {
                cough_isWaiting = true;
                StartCoroutine("PlayCoughSound", cough3);
            }
            smokeFogMat.color = new Color(0.45f, 0.45f, 0.45f, player_smoke_count * 0.02f);
        }
        else
        {
      
            
            if (game_over == false)
            {
                game_over = true;
                smokeFogMat.color = new Color(0.45f, 0.45f, 0.45f, 1f);
                characterController.enabled = false;
                if (SceneManager.GetActiveScene().name == "MainStage 1")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager1>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "MainStage 2")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager2>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "MainStage 3")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager3>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "FireExtinguisherStage")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager4>().GameOver();
                }
            }
        }

        if (smoke_count_decrease == true)
        {
            if (player_smoke_count <= 0)
            {
                smoke_count_decrease = false;
                player_smoke_count = 0;
            }
            if (Time.time - tempTime > 0.3f)
            {
                tempTime= Time.time;
                player_smoke_count--;
            }
        }

        if (player_fire_count >= 3)
        {
            if (game_over == false)
            {
                game_over = true;
                if (SceneManager.GetActiveScene().name == "MainStage 1")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager1>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "MainStage 2")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager2>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "MainStage 3")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager3>().GameOver();
                }
                else if (SceneManager.GetActiveScene().name == "FireExtinguisherStage")
                {
                    GameObject.Find("GameManager").GetComponent<GameManager4>().GameOver();
                }
            }
        }
    }

    public IEnumerator CheckNoSmoke()
    {
        yield return new WaitForSeconds(5f);
        smoke_count_decrease = true;
    }

    public IEnumerator CheckNoFire()
    {
        yield return new WaitForSeconds(5f);
        player_fire_count = 0;
    }

    IEnumerator PlayCoughSound(AudioSource cough)
    {
        yield return new WaitForSeconds(Random.Range(3, 5));
        cough.Play();
        cough_isWaiting = false;
    }
}

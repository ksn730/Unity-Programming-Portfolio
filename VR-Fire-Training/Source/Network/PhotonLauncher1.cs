using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PhotonLauncher1 : MonoBehaviourPunCallbacks
{
    //VR 유저용 스크립트

    public byte maxPlayersPerRoom = 2;
    GameObject canvas;
    GameObject player1;
    string gameVersion = "1";


    // Start is called before the first frame update
    void Start()
    {
        canvas = GameObject.Find("Canvas");
        Connect();
    }


    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.InRoom)
            {
                PhotonNetwork.JoinOrCreateRoom("Capstone", new RoomOptions { MaxPlayers = 2 }, null);
            }
        }
        else
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by Pun");
        PhotonNetwork.JoinOrCreateRoom("Capstone", new RoomOptions { MaxPlayers = 2 },null);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("OnDisconnected() was called by Pun");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("Join Room Failed");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom called by Pun. Now in a room.");
        
        
        if (PhotonNetwork.PlayerListOthers.Length > 0)
        {
            canvas.transform.GetChild(1).GetComponent<Text>().text = "감독관 접속 여부:O";
        }
        else
        {
            canvas.transform.GetChild(1).GetComponent<Text>().text = "감독관 접속 여부:X";
        }
        
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료");
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        canvas.transform.GetChild(1).GetComponent<Text>().text = "감독관 접속 여부:O";
    }
    
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (SceneManager.GetActiveScene().name == "Lobby 1") 
        {
            canvas.transform.GetChild(1).GetComponent<Text>().text = "감독관 접속 여부:X";
        }
        else
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        
    }
    
    public void TrainingStart()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Not master client");
            return;
        }

        //테스트를 위해 잠시 주석처리
        
        int randomNumber = Random.Range(1,4);
        if(randomNumber==1)
        {
            PhotonNetwork.LoadLevel("MainStage 1");
        }
        else if (randomNumber == 2)
        {
            PhotonNetwork.LoadLevel("MainStage 2");
        }
        else if (randomNumber == 3)
        {
            PhotonNetwork.LoadLevel("MainStage 3");
        }
    }
}

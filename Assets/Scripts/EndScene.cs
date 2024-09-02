using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScene : MonoBehaviour
{

    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    public void ReturnMainMenu()
    {
      
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0); // main menu
    }
}

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class CoutnDownTimerController : MonoBehaviour
{
    public static CoutnDownTimerController instance;
    public float RemainingTime;
    public float NetworkTimer;
    private PhotonView photonView;
    private bool flag = true;
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        RemainingTime = 1000 * 60 * 3; // 3 minutes
        NetworkTimer = 0f;
        this.photonView = this.GetComponent<PhotonView>();
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            this.RemainingTime -= Time.deltaTime * 1000;
            int[] units = ConvertMillisecondsToMinutesAndSeconds(this.RemainingTime);
            int m = units[0];
            int s = units[1];
            GameUIController.instance.UpdateRemainigTimeLable(m, s);
            if (this.NetworkTimer <= 0)
            {
                photonView.RPC(nameof(SyncTime), RpcTarget.Others, m,s);
                NetworkTimer = 1f;
            }
            NetworkTimer -= Time.deltaTime * 1000;
            if (this.RemainingTime <= 0 && flag)
            {
                flag = false;
                PhotonNetwork.LoadLevel(2); // game over
            }
        }
    }
    private int[] ConvertMillisecondsToMinutesAndSeconds(float milliseconds)
    {
        // Convert milliseconds to total seconds (cast to int to ensure whole number)
        int totalSeconds = (int)(milliseconds / 1000f);

        // Calculate minutes and seconds
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // Return as an array
        return new int[] { minutes, seconds };
    }

    [PunRPC]
    public void SyncTime(int m , int s)
    {
        GameUIController.instance.UpdateRemainigTimeLable(m, s);
    }

}

using JetBrains.Annotations;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;
    private GameObject player;
    private PlayerController playerController;
    public List<GameObject> spawnPoints = new List<GameObject>();
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Spawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.healt <= 0)
        {
            Die();
            Spawn();
        }
    }

    public void Die()
    {
        PhotonNetwork.Destroy(this.player);
    }
    public void Spawn()
    {
        GameObject randomSpawnPoint = this.spawnPoints[Random.Range(0 , this.spawnPoints.Count-1)];
        this.player = PhotonNetwork.Instantiate("PlayerPrefab", randomSpawnPoint.transform.position,randomSpawnPoint.transform.rotation);
        this.playerController = player.GetComponent<PlayerController>();
    }
}

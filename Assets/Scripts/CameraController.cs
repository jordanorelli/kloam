﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public Networking networking;
    public Transform player;

    // Start is called before the first frame update
    void Start() {
        networking.Connect();
    }

    // Update is called once per frame
    void Update() {
        networking.CheckForMessages();

        if (player) {
            transform.position = new Vector3(player.position.x-2, player.position.y+2, player.position.z-10);
            transform.LookAt(player.transform.position + player.up * 4f);
        }
    }
}

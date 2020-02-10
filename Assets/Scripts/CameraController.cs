using System.Collections;
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
            transform.position = new Vector3(player.position.x-1, player.position.y+4, player.position.z-8);
            transform.LookAt(player);
        }
    }
}

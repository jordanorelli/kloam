using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoulController : MonoBehaviour {
    public string playerName = "";

    public Text nameText;
    public Camera cam;

    private Quaternion fixedRotation;

    // Start is called before the first frame update
    void Start() {
        if (cam == null) {
            cam = Camera.main;
        }
        fixedRotation = transform.rotation; 
        nameText.text = "@" + playerName;
    }

    // Update is called once per frame
    void Update() {
        transform.rotation = cam.transform.rotation * fixedRotation;
    }

    public struct PickupInfo {
        public string playerName;
        public Vector3 position;
    }
}

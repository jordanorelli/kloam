using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour {
    public Text deadText;

    private bool isDead = false;
    private float deadTime = 0f;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        if (isDead) {
            deadTime += Time.deltaTime;
        }
        deadText.text = string.Format("You have been dead for {0:F1} seconds", deadTime);
    }

    public void SetDead(bool dead) {
        isDead = dead;
        if (!dead) {
            deadTime = 0f;
        }
    }
}

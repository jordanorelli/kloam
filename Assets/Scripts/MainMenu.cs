using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public LoginInfo loginInfo;
    public Networking networking;
    public InputField usernameField;
    public InputField passwordField;

    // Start is called before the first frame update
    void Start() {
        networking.Connect();
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void Login() {
        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();
        if (username == "" || password == "") {
            return;
        }
        usernameField.interactable = false;
        passwordField.interactable = false;
        loginInfo.playerName = username;
        loginInfo.password = password;
        networking.SendLogin();
        SceneManager.LoadScene("MainLevel");
    }
}

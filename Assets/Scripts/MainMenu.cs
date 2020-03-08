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
    public Text errorText;

    // Start is called before the first frame update
    void Start() {
        loginInfo.isLoggedIn = false;
        loginInfo.sentLogin = false;
        if (!networking.isConnected()) {
            networking.Connect();
            StartCoroutine(networking.ReadMessages());
        }
    }

    // Update is called once per frame
    void Update() {
        usernameField.interactable = !loginInfo.sentLogin;
        passwordField.interactable = !loginInfo.sentLogin;
        if (loginInfo.loginError == "") {
            errorText.gameObject.SetActive(false);
        } else {
            errorText.text = loginInfo.loginError;
            errorText.gameObject.SetActive(true);
        }
        if (loginInfo.isLoggedIn) {
            SceneManager.LoadScene("LevelTwo");
        }
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
    }
}

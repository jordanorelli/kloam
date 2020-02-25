using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LoginInfo", menuName = "ScriptableObjects/LoginInfo", order = 1)]
public class LoginInfo : ScriptableObject {
    public string playerName;
    public string password;
    public Vector3 startPosition;

    // sentLogin is set true when we send the login, and set back to false when
    // we receive the result
    public bool sentLogin;

    // isLoggedIn is set to true when we receive a successful login attempt
    public bool isLoggedIn;

    // loginFailed is set to true when we fail a login
    public string loginError;
}

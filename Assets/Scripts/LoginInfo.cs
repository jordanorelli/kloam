using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LoginInfo", menuName = "ScriptableObjects/LoginInfo", order = 1)]
public class LoginInfo : ScriptableObject {
    public string playerName;
    public string password;
    public Vector3 startPosition;
}

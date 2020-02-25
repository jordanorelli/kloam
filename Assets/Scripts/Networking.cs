﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Networking", order = 1)]
public class Networking : ScriptableObject {
    public string host;
    public string path;
    public int port;
    public GameObject soulPrefab;
    public GameObject playerPrefab;
    public LoginInfo loginInfo;

    private ClientWebSocket sock;
    private Task writeTask;
    private Task<WebSocketReceiveResult> readTask;
    private ArraySegment<byte> readBuffer;
    private int seq;

    async public void Connect() {
        readBuffer = new ArraySegment<byte>(new byte[32000]);

        if (sock != null) {
            if (sock.State == WebSocketState.Open) {
                return;
            }
            sock = null;
        }

        Application.quitting += autoDisconnect;

        sock = new ClientWebSocket();
        UriBuilder b = new UriBuilder();
        b.Scheme = "ws";
        b.Host = host;
        b.Port = port;
        b.Path = path;
        Debug.LogFormat("Connecting to: {0}", b.Uri);
        await sock.ConnectAsync(b.Uri, CancellationToken.None);
        Debug.LogFormat("Finished connection task with status: {0}", sock.State);
    }

    public void SendCollectSoul(string playerName, Vector3 position) {
        seq++;
        CollectSoul info;
        info.seq = seq;
        info.cmd = "collect-soul";
        info.playerName = playerName;
        info.position = position;
        string msg = JsonUtility.ToJson(info);
        ArraySegment<byte> buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
        sock.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public void SendDeath(Vector3 position) {
        seq++;
        Death death;
        death.seq = seq;
        death.cmd = "death";
        death.position = position;
        string msg = JsonUtility.ToJson(death);
        ArraySegment<byte> buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
        sock.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public void SendLogin() {
        seq++;
        Login login;
        login.seq = seq;
        login.cmd = "login";
        login.username = loginInfo.playerName;
        login.password = loginInfo.password;
        loginInfo.sentLogin = true;
        loginInfo.loginError = "";
        string msg = JsonUtility.ToJson(login);
        ArraySegment<byte> buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
        sock.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async public void CheckForMessages() {
        if (readTask != null) {
            return;
        }
        if (sock == null) {
            return;
        }
        readTask = sock.ReceiveAsync(readBuffer, CancellationToken.None);
        WebSocketReceiveResult result = await readTask;
        readTask = null;
        string msg = Encoding.UTF8.GetString(readBuffer.Array, 0, result.Count);
        string[] parts = msg.Split(new char[]{' '}, 2);
        if (parts.Length != 2) {
            Debug.LogFormat("dunno how to handle this msg: {0}", msg);
            return;
        }

        GameObject soul;

        switch (parts[0]) {
        case "spawn-soul":
            Debug.LogFormat("spawn a soul: {0}", parts[1]);
            SpawnSoul ss = JsonUtility.FromJson<SpawnSoul>(parts[1]);
            soul = Instantiate(soulPrefab, ss.position, Quaternion.identity);
            soul.name = ss.playerName;

            GameObject allSouls = GameObject.Find("Souls");
            soul.transform.SetParent(allSouls.transform);

            SoulController sc = soul.GetComponent<SoulController>();
            sc.playerName = ss.playerName;
            break;

        case "soul-collected": 
            Debug.LogFormat("a soul was collected: {0}", parts[1]);
            CollectSoul collected = JsonUtility.FromJson<CollectSoul>(parts[1]);
            soul = GameObject.Find("Souls/"+collected.playerName);
            Destroy(soul);

            if (collected.playerName == loginInfo.playerName) {
                GameObject currentPlayer = GameObject.Find("Player");
                if (currentPlayer == null) {
                    GameObject player = Instantiate(playerPrefab, loginInfo.startPosition, Quaternion.identity);
                    Camera cam = Camera.main;
                    CameraController cc = cam.GetComponent<CameraController>();
                    cc.player = player.transform;
                }
            }
            break;

        case "login-result":
            Debug.LogFormat("received login result: {0}", parts[1]);
            loginInfo.sentLogin = false;
            LoginResult login = JsonUtility.FromJson<LoginResult>(parts[1]);
            if (login.passed) {
                loginInfo.isLoggedIn = true;
            } else {
                loginInfo.loginError = login.error;
                Debug.LogErrorFormat("failed login: {0}", login.error);
            }
            break;

        case "tick":
            break;

        default:
            Debug.LogFormat("also can't handle this one: {0} {1}", parts[0], parts[1]);
            break;
        }
    }

    private void autoDisconnect() {
        if (isConnected()) {
            Debug.Log("disconnecting websocket via autoDisconnect");
            Task task = sock.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            task.Wait();
        }
    }

    private bool isConnected() {
        return sock != null && sock.State == WebSocketState.Open;
    }

    private struct CollectSoul {
        public int seq;
        public string cmd;
        public string playerName;
        public Vector3 position;
    }

    private struct Death {
        public int seq;
        public string cmd;
        public Vector3 position;
    }

    private struct SpawnSoul {
        public string playerName;
        public Vector3 position;
    }

    private struct Login {
        public int seq;
        public string cmd;
        public string username;
        public string password;
    }

    private struct LoginResult {
        public bool passed;
        public string error;
    }
}

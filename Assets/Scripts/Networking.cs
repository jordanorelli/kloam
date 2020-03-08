using System;
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
        return;
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

    public IEnumerator ReadMessages() {
        Task<WebSocketReceiveResult> readTask = null; // = sock.ReceiveAsync(readBuffer, CancellationToken.None);

        while (true) {
            if (!isConnected()) {
                yield return null;
                continue;
            }

            if (readTask == null) {
                readTask = sock.ReceiveAsync(readBuffer, CancellationToken.None);
            }

            switch (readTask.Status) {
            case TaskStatus.Created:
            case TaskStatus.WaitingForActivation:
            case TaskStatus.WaitingToRun:
            case TaskStatus.Running:
            case TaskStatus.WaitingForChildrenToComplete:
                yield return null;
                continue;

            case TaskStatus.RanToCompletion:
                parseMessage(readTask.Result);
                break;

            case TaskStatus.Canceled:
                break;
            case TaskStatus.Faulted:
                break;
            }
            readTask = null;
            yield return null;
        }
    }

    private void parseMessage(WebSocketReceiveResult message) {
        string msg = Encoding.UTF8.GetString(readBuffer.Array, 0, message.Count);
        string[] parts = msg.Split(new char[]{' '}, 2);
        if (parts.Length != 2) {
            Debug.LogFormat("dunno how to handle this msg: {0}", msg);
            return;
        }
        switch (parts[0]) {
        case "spawn-soul":
            SpawnSoul spawned = JsonUtility.FromJson<SpawnSoul>(parts[1]);
            onSpawnSoul(spawned);
            break;

        case "soul-collected": 
            CollectSoul collected = JsonUtility.FromJson<CollectSoul>(parts[1]);
            onSoulCollected(collected);
            break;

        case "login-result":
            LoginResult login = JsonUtility.FromJson<LoginResult>(parts[1]);
            onLoginResult(login);
            break;

        case "tick":
            break;

        default:
            Debug.LogFormat("also can't handle this one: {0} {1}", parts[0], parts[1]);
            break;
        }
    }

    private void onSpawnSoul(SpawnSoul spawn) {
        Debug.LogFormat("spawn a soul: {0} at {1}", spawn.playerName, spawn.position);
        GameObject soul = Instantiate(soulPrefab, spawn.position, Quaternion.identity);
        soul.name = spawn.playerName;

        GameObject allSouls = GameObject.Find("Souls");
        if (allSouls == null) {
            Debug.LogError("unable to find souls container!");
        } else {
            soul.transform.SetParent(allSouls.transform);

            SoulController sc = soul.GetComponent<SoulController>();
            sc.playerName = spawn.playerName;
        }
    }

    private void onSoulCollected(CollectSoul collected) {
        Debug.LogFormat("a soul was collected: {0}", collected);
        GameObject soul = GameObject.Find("Souls/"+collected.playerName);
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
    }

    private void onLoginResult(LoginResult result) {
        Debug.LogFormat("received login result: {0}", result);
        loginInfo.sentLogin = false;
        if (result.passed) {
            loginInfo.isLoggedIn = true;
        } else {
            loginInfo.loginError = result.error;
            Debug.LogErrorFormat("failed login: {0}", result.error);
        }
    }

    private void autoDisconnect() {
        if (isConnected()) {
            Debug.Log("disconnecting websocket via autoDisconnect");
            Task task = sock.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            task.Wait();
        }
    }

    public bool isConnected() {
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

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

    private ClientWebSocket sock;
    private Task writeTask;
    private Task<WebSocketReceiveResult> readTask;
    private ArraySegment<byte> readBuffer;
    private int seq;

    async public void Connect() {
        if (sock != null) {
            if (sock.State == WebSocketState.Open) {
                return;
            }
            sock = null;
        }

        readBuffer = new ArraySegment<byte>(new byte[32000]);
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

    public void SendLogin(string username, string password) {
        seq++;
        Login login;
        login.seq = seq;
        login.cmd = "login";
        login.username = username;
        login.password = password;
        string msg = JsonUtility.ToJson(login);
        ArraySegment<byte> buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg));
        sock.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async public void CheckForMessages() {
        if (readTask != null) {
            return;
        }
        Debug.Log("checking for messages");
        readTask = sock.ReceiveAsync(readBuffer, CancellationToken.None);
        WebSocketReceiveResult result = await readTask;
        readTask = null;
        Debug.LogFormat("received message: {0}", result);
        Debug.LogFormat("Readbuffer count: {0} offset: {1}", readBuffer.Count, readBuffer.Offset);
        string msg = Encoding.UTF8.GetString(readBuffer.Array, 0, result.Count);
        string[] parts = msg.Split(new char[]{' '}, 2);
        if (parts.Length != 2) {
            Debug.LogFormat("dunno how to handle this msg: {0}", msg);
            return;
        }
        Debug.LogFormat("first: {0} second: {1}", parts[0], parts[1]);
        switch (parts[0]) {

        default:
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

    private struct Login {
        public int seq;
        public string cmd;
        public string username;
        public string password;
    }
}

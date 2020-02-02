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
    private Task connectTask;
    private Task writeTask;
    private bool isConnected;
    private int seq;

    public void Connect() {
        sock = new ClientWebSocket();
        UriBuilder b = new UriBuilder();
        b.Scheme = "ws";
        b.Host = host;
        b.Port = port;
        b.Path = path;
        Debug.LogFormat("Connecting to: {0}", b.Uri);
        connectTask = sock.ConnectAsync(b.Uri, CancellationToken.None);
        connectTask.ContinueWith((x) => {
            Debug.LogFormat("Finished connection task with status: {0}", sock.State);
            isConnected = sock.State == WebSocketState.Open;
        });
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
}

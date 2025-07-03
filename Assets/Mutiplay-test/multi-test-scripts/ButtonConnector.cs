using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class ButtonConnector : MonoBehaviour
{
    [Header("接続情報")]
    [Tooltip("接続先のホストPCのIPアドレスを、インスペクターで直接指定します。")]
    public string hostIpAddress = "192.168.0.1"; // あなたのPCのIPアドレスに書き換えてください

    void Update()
    {
        // まだ接続しておらず、右手コントローラーのAボタンが押された瞬間を検出
        if (!NetworkManager.Singleton.IsClient && OVRInput.GetDown(OVRInput.Button.One))
        {
            Debug.Log("A button pressed. Attempting to join as client...");
            StartClient();
        }
    }

    private void StartClient()
    {
        var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utpTransport != null)
        {
            utpTransport.SetConnectionData(hostIpAddress, 7777); // ポートは通常7777
        }

        NetworkManager.Singleton.StartClient();
        
        // 接続処理を一度実行したら、このスクリプトは不要なので無効化する
        this.enabled = false;
    }
}
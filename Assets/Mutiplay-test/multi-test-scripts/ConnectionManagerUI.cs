using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro; // TextMeshProを使うために必要

public class ConnectionManagerUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TMP_Text statusText;

    void Start()
    {
        // デフォルトでローカルホストIPを設定しておくと便利
        ipAddressInput.text = "127.0.0.1";
        if (statusText != null) statusText.text = "Offline";
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        if (statusText != null) statusText.text = "Hosting";
        gameObject.SetActive(false); // 接続後はUIを非表示にする
    }

    public void StartClient()
    {
        string ipAddress = ipAddressInput.text;

        // UnityTransportコンポーネントを取得して接続情報を設定
        var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utpTransport != null)
        {
            utpTransport.SetConnectionData(ipAddress, 7777); // IPアドレスとポート番号を設定
        }

        NetworkManager.Singleton.StartClient();
        if (statusText != null) statusText.text = "Connecting...";
        gameObject.SetActive(false); // 接続後はUIを非表示にする
    }
}
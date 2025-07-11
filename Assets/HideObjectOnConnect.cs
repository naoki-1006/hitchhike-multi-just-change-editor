using UnityEngine;
using Unity.Netcode;

public class HideObjectOnConnect : MonoBehaviour
{
    // Inspectorから非表示にしたいオブジェクトをここにドラッグ＆ドロップ
    [Tooltip("ネットワーク接続時に非表示にしたいオブジェクト")]
    public GameObject objectToHide;

    void Start()
    {
        // オブジェクトが設定されているか確認
        if (objectToHide == null)
        {
            Debug.LogError("非表示にするオブジェクトが設定されていません！", this);
            return;
        }

        // NetworkManagerのOnClientConnectedCallbackイベントに、自作のメソッドを登録
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDestroy()
    {
        // このオブジェクトが破棄される際に、登録したイベントを解除（メモリリーク防止）
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    /// <summary>
    /// クライアントがサーバーに接続したときに呼び出されるメソッド
    /// </summary>
    /// <param name="clientId">接続してきたクライアントのID</param>
    private void HandleClientConnected(ulong clientId)
    {
        // 接続してきたのが自分自身（ローカルクライアント）であるかを確認
        // これを確認しないと、他のプレイヤーが接続したときにも処理が走ってしまう
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 自分自身の接続が確認されたら、オブジェクトを非表示にする
            objectToHide.SetActive(false);
            
            Debug.Log($"ネットワーク接続を確立しました。オブジェクト「{objectToHide.name}」を非表示にします。");
        }
    }
}
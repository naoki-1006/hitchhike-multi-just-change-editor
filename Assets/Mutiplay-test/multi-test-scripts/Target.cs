using UnityEngine;
using Unity.Netcode;

public class Target : NetworkBehaviour
{
    // 自身の位置を同期するためのネットワーク変数。必ずnewで初期化する。
    // サーバーのみ書き込み可能、全員が読み取り可能に設定。
    public NetworkVariable<Vector3> NetworkPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ulong PlayerId;

    public override void OnNetworkSpawn()
    {
        // NetworkVariableの値が変更されたら、OnPositionChangedメソッドを呼び出すように登録
        NetworkPosition.OnValueChanged += OnPositionChanged;

        // スポーンされた瞬間の位置を一度反映させる（特に後から参加したクライアント用）
        transform.position = NetworkPosition.Value;
    }

    public override void OnNetworkDespawn()
    {
        // オブジェクトが破棄される際に、登録したコールバックを解除
        NetworkPosition.OnValueChanged -= OnPositionChanged;
    }

    /// <summary>
    /// NetworkPositionの値が変更されたときに、全クライアントで自動的に呼ばれる
    /// </summary>
    /// <param name="previousValue">変更前の値</param>
    /// <param name="newValue">新しい値</param>
    private void OnPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        // 自身のTransformの位置を、同期された新しい位置に更新する
        transform.position = newValue;
    }
}
using UnityEngine;
using Unity.Netcode;

public class OwnershipHandler : NetworkBehaviour
{
    // プレイヤーがこのメソッドを呼び出すことで、所有権を要求する
    public void RequestOwnership()
    {
        // 既に自分がオーナーである場合は何もしない
        if (IsOwner) return;

        // サーバーに対して、所有権を自分に移すように要求する
        RequestOwnershipServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        // ### 修正点 ### ChangeOwner -> ChangeOwnership に変更
        GetComponent<NetworkObject>().ChangeOwnership(rpcParams.Receive.SenderClientId);
        
        Debug.Log($"Hand ownership transferred to Client ID: {rpcParams.Receive.SenderClientId}");
    }
}
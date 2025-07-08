using UnityEngine;
using Unity.Netcode;

public class OwnershipHandler : NetworkBehaviour
{
    public int OwnerId => _owner.Value;
    [SerializeField]
    NetworkVariable<int> _owner = new(-1);
    //[SerializeField]
    //NetworkVariable<int> _ownerRight = new(-1);

    /// <summary>
    /// 外部から呼び出して、このオブジェクトの所有権を要求する
    /// </summary>
    public void RequestOwnership()
    {
        //if (IsOwner) return;
        RequestOwnershipServerRpc();
    }

    /// <summary>
    /// 外部から呼び出して、このオブジェクトの所有権を放棄する
    /// </summary>
    public void ReleaseOwnership()
    {
        if (!IsOwner) return;
        ReleaseOwnershipServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ServerRpcParams rpcParams = default)
    {
        NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
        _owner.Value = (int)rpcParams.Receive.SenderClientId;
        Debug.Log($"Hand ownership transferred to Client ID: {rpcParams.Receive.SenderClientId}");
    }

    [ServerRpc] // オーナーだけが呼べる
    private void ReleaseOwnershipServerRpc()
    {
        // 所有権をサーバー(ClientId 0)に戻す
        GetComponent<NetworkObject>().ChangeOwnership(0);
        _owner.Value = -1;
        Debug.Log($"Hand ownership released by Client ID: {OwnerClientId}");
    }
}
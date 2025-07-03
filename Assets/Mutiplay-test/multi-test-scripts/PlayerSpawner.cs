using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject leftHandPrefab;
    [SerializeField] private GameObject rightHandPrefab;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            // 自分がサーバーに接続したら、サーバーに対して「自分の手を生成してください」とお願いする
            SpawnHandsServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc]
    private void SpawnHandsServerRpc(ulong ownerClientId)
    {
        // サーバーが左右の手を生成し、所有権をクライアントに与える
        GameObject leftHand = Instantiate(leftHandPrefab);
        leftHand.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);

        GameObject rightHand = Instantiate(rightHandPrefab);
        rightHand.GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientId);
    }
}
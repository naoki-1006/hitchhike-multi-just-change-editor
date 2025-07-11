using UnityEngine;
using Unity.Netcode;

namespace HitchHikeMultiplayer
{
    public class PlayerHandInteractor : NetworkBehaviour
    {
        [SerializeField]
        private TargetController targetController;

        public override void OnNetworkSpawn()
    {
        // 自分のプレイヤーオブジェクトでなければ、このスクリプトは不要
        // if (!IsOwner)
        // {
        //     enabled = false;
        // }
    }

        private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // 触れたオブジェクトからTargetコンポーネントとNetworkObjectを取得
            if (other.TryGetComponent<Target>(out Target target))
            {
                NetworkObject targetNetworkObject = target.NetworkObject;

                // ★重要：ターゲットの所有者IDが自分自身のIDと一致するか確認
                if (targetNetworkObject.OwnerClientId == OwnerClientId)
                {
                    // 自分自身のターゲットに触れた場合のみ、サーバーに位置変更をリクエスト
                    RequestNewTargetServerRpc();
                    targetController.Reachingcount[(int)OwnerClientId] += 1;
                }
            }
    }

        // クライアントからサーバーへ送るRPC
        [ServerRpc]
    private void RequestNewTargetServerRpc(ServerRpcParams rpcParams = default)
    {
        // RPCを呼び出したクライアントのIDを取得
        ulong clientId = rpcParams.Receive.SenderClientId;

        // そのクライアントのターゲットを新しい位置に移動させるようコントローラーに命令
        TargetController.Instance.SpawnNewTargetPosition(clientId);
    }
    }
}
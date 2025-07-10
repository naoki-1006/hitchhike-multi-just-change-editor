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

        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("tacchi");

            // 参照があり、かつ触れたものがTargetスクリプトを持っていたら
            if (targetController != null && other.GetComponent<Target>() != null)
            {
                // サーバーに「新しいターゲットの位置を生成して」とお願いする
                RequestNewTargetServerRpc();
                targetController.Reachingcount += 1;
            }
        }

        // クライアントからサーバーへ送るRPC
        [ServerRpc]
        private void RequestNewTargetServerRpc()
        {
            // このコードブロックはサーバー上でのみ実行される
            targetController.SpawnNewTargetPosition();
            Debug.Log("Send");
        }
    }
}
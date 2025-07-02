using UnityEngine;
using Unity.Netcode;

// このスクリプトはプレイヤープレハブのルート(NetworkPlayer)にアタッチします
public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private OVRCameraRig cameraRig;
    [SerializeField] private AudioListener audioListener;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // このオブジェクトが自分のものでなければ、
        // カメラとオーディオリスナーを無効化する
        if (!IsOwner)
        {
            if (cameraRig != null) cameraRig.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}
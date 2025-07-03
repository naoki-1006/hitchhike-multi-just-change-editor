using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NetworkHandController : NetworkBehaviour
{
    [SerializeField]
    private Handedness handType;

    private OVRSkeleton localHandDataSource;
    private OVRSkeleton mySkeleton;

    private NetworkVariable<Vector3> rootPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> rootRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<NetworkHandData> boneData = new NetworkVariable<NetworkHandData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public enum Handedness { Left, Right }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        mySkeleton = GetComponent<OVRSkeleton>();

        if (IsOwner)
        {
            var localRig = FindObjectOfType<OVRCameraRig>();
            if (localRig != null)
            {
                var handAnchor = (handType == Handedness.Left) ? localRig.leftHandAnchor : localRig.rightHandAnchor;
                localHandDataSource = handAnchor.GetComponentInChildren<OVRSkeleton>();
            }

            var meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
        else
        {
            GetComponent<OVRHand>().enabled = false;
        }
    }

    void Update()
    {
        if (mySkeleton != null && mySkeleton.Bones.Count == 0)
        {
            return;
        }
        
        if (IsOwner)
        {
            if (localHandDataSource != null)
            {
                // 1. データソースから最新の位置・回転を取得
                Vector3 currentPosition = localHandDataSource.transform.position;
                Quaternion currentRotation = localHandDataSource.transform.rotation;

                // 2. ★★★★★ 自分自身のNetworkHandオブジェクトの位置と回転も更新する（必須の処理）★★★★★
                transform.position = currentPosition;
                transform.rotation = currentRotation;

                // 3. ネットワーク経由で他のプレイヤーに送信
                rootPosition.Value = currentPosition;
                rootRotation.Value = currentRotation;
                
                var poses = new Pose[localHandDataSource.Bones.Count];
                for (int i = 0; i < localHandDataSource.Bones.Count; i++)
                {
                    poses[i] = new Pose(Vector3.zero, localHandDataSource.Bones[i].Transform.localRotation);
                }
                boneData.Value = new NetworkHandData() { bonePoses = poses };
            }
        }
        else
        {
            // --- 他人の手なら、受信したデータで自分の見た目を更新 ---
            transform.position = rootPosition.Value;
            transform.rotation = rootRotation.Value;

            if (boneData.Value.bonePoses != null && mySkeleton.Bones.Count == boneData.Value.bonePoses.Length)
            {
                for (int i = 0; i < mySkeleton.Bones.Count; i++)
                {
                    mySkeleton.Bones[i].Transform.localRotation = boneData.Value.bonePoses[i].rotation;
                }
            }
        }
    }
}
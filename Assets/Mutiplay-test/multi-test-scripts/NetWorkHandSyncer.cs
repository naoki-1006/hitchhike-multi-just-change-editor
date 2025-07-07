using UnityEngine;
using Unity.Netcode;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(HandVisual))]
    public class NetworkHandSyncer : NetworkBehaviour
    {
        private HandVisual _handVisual;
        private Transform _localTrackingSpace;
        private OVRSkeleton _localSkeleton;

        private readonly Quaternion _rotationOffset = Quaternion.Euler(0, 0f, 0);

        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void Awake()
        {
            _handVisual = GetComponent<HandVisual>();
        }

        public override void OnNetworkSpawn()
        {
            // HandVisualのデフォルトの更新機能を無効化する
            if (_handVisual != null)
            {
                _handVisual.enabled = false;
            }

            if (IsOwner)
            {
                FindLocalSkeleton();
            }
            else
            {
                _networkHandPose.OnValueChanged += OnPoseChanged;
            }
        }

        // サーバーからデータが送られてきたときに呼ばれる
        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            // ### 追加 ###
            // このオブジェクトの現在のオーナーIDと、送られてきたデータのIDが一致しない場合は無視する
            if (OwnerClientId != newValue.ClientId) return;
            // 受信したデータでポーズを適用
            ApplyPose(newValue);
        }

        // 自分（オーナー）のトラッキングデータを更新する
        void Update()
        {
            if (!IsOwner) return;

            if (_localSkeleton == null || !_localSkeleton.IsInitialized || !_localSkeleton.IsDataValid || _localTrackingSpace == null)
            {
                return;
            }

            var boneTransforms = _localSkeleton.BoneTransforms;
            if (boneTransforms == null || boneTransforms.Count == 0) return;

            Transform localWristBone = boneTransforms[0];
            
            // 手首の相対座標・回転を取得
            Vector3 relativePos = _localTrackingSpace.InverseTransformPoint(localWristBone.position);
            Quaternion relativeRot = Quaternion.Inverse(_localTrackingSpace.rotation) * localWristBone.rotation;

            // 送信するポーズデータを作成
            HandPoseData currentPose = new HandPoseData
            {
                // ### 追加 ### 自分のクライアントIDをデータに含める
                ClientId = OwnerClientId,

                RootPosition = relativePos,
                RootRotation = relativeRot, // 向きの補正は受信側で行うため、生のデータを送信
                JointRotations = new Quaternion[boneTransforms.Count]
            };

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                currentPose.JointRotations[i] = boneTransforms[i].localRotation;
            }

            // ローカルハンドの表示を更新
            ApplyPose(currentPose);
            
            // ネットワークにポーズデータを送信
            _networkHandPose.Value = currentPose;
        }

        /// <summary>
        /// 受け取ったポーズデータをHandVisualに適用する共通メソッド
        /// </summary>
        private void ApplyPose(HandPoseData pose)
        {
            var joints = _handVisual.Joints;
            if (joints == null || joints.Count == 0) return;

            Transform wristBone = joints[0];

            // 手首の位置・回転を適用
            if (wristBone != null)
            {
                wristBone.localPosition = pose.RootPosition;
                // ここで向きを180度補正する
                wristBone.localRotation = pose.RootRotation * _rotationOffset;
            }
            
            // 指の関節の向きを適用
            if (pose.JointRotations == null || joints.Count != pose.JointRotations.Length) return;
            for (int i = 1; i < joints.Count; i++)
            {
                if (joints[i] != null)
                {
                    joints[i].localRotation = pose.JointRotations[i];
                }
            }
        }

        // ローカルのOVRSkeletonを探す処理（変更なし）
        private void FindLocalSkeleton()
        {
            var rig = GameObject.Find("OVRCameraRig");
            if (rig == null) { Debug.LogError("OVRCameraRigが見つかりませんでした。"); return; }
            
            _localTrackingSpace = rig.transform.Find("TrackingSpace");
            if (_localTrackingSpace == null) { Debug.LogError("TrackingSpaceが見つかりませんでした。"); return; }

            string anchorPath = gameObject.name.Contains("Left") ? "LeftHandAnchor" : "RightHandAnchor";
            Transform anchor = _localTrackingSpace.Find(anchorPath);
            if (anchor == null) { Debug.LogError($"アンカーが見つかりませんでした。パス: {anchorPath}"); return; }

            _localSkeleton = anchor.GetComponentInChildren<OVRSkeleton>();
            if (_localSkeleton == null) { Debug.LogError($"'{anchor.name}' 内で同期元のOVRSkeletonが見つかりませんでした。"); }
        }
    }
}
using UnityEngine;
using Unity.Netcode;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(HandVisual))]
    public class NetworkHandSyncer : NetworkBehaviour
    {
        // ### 削除 ### 手動で設定していたWristTransformフィールドを削除
        // public Transform WristTransform;

        private HandVisual _handVisual;
        private OVRSkeleton _localSkeleton;

        private readonly Quaternion _rotationOffset = Quaternion.Euler(0, 180f, 0);

        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void Awake()
        {
            _handVisual = GetComponent<HandVisual>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                _handVisual.EnableAutomaticSkeletonSearch = false;
                _networkHandPose.OnValueChanged += OnPoseChanged;
                return;
            }

            var rig = GameObject.Find("OVRCameraRig");
            if (rig == null)
            {
                Debug.LogError("OVRCameraRigが見つかりませんでした。シーンに存在するか確認してください。", this);
                return;
            }

            string anchorPath = gameObject.name.Contains("Left") ? "TrackingSpace/LeftHandAnchor" : "TrackingSpace/RightHandAnchor";
            Transform anchor = rig.transform.Find(anchorPath);
            if (anchor == null)
            {
                Debug.LogError($"アンカーが見つかりませんでした。パス: '{anchorPath}' が正しいか確認してください。", this);
                return;
            }

            _localSkeleton = anchor.GetComponentInChildren<OVRSkeleton>();
            if (_localSkeleton == null)
            {
                Debug.LogError($"'{anchor.name}' 内で同期元のOVRSkeletonが見つかりませんでした。", this);
            }
        }

        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            var joints = _handVisual.Joints;
            var rotations = newValue.JointRotations;

            if (joints == null || joints.Count == 0 || rotations == null || joints.Count != rotations.Length) return;

            // ### ここからロジックを修正 ###
            // Step 1: HandVisualの関節リストから手首ボーン(0番目)を取得
            Transform wristBone = joints[0];

            // Step 2: 受信したデータで「手首」の位置・回転を更新
            if (wristBone != null)
            {
                wristBone.position = newValue.RootPosition;
                wristBone.rotation = newValue.RootRotation;
            }
            
            // Step 3: 「指の関節」の向きを更新 (手首は除くため、ループは1から開始)
            for (int i = 1; i < joints.Count; i++)
            {
                if (joints[i] != null && i < rotations.Length)
                {
                    joints[i].localRotation = rotations[i];
                }
            }
        }

        void Update()
        {
            if (!IsOwner) return;

            if (_localSkeleton == null || !_localSkeleton.IsInitialized || !_localSkeleton.IsDataValid)
            {
                return;
            }

            var boneTransforms = _localSkeleton.BoneTransforms;
            if (boneTransforms == null || boneTransforms.Count == 0) return;
            
            Transform localWristBone = boneTransforms[0];

            HandPoseData currentPose = new HandPoseData
            {
                RootPosition = localWristBone.position,
                RootRotation = localWristBone.rotation * _rotationOffset,
                JointRotations = new Quaternion[boneTransforms.Count]
            };

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                currentPose.JointRotations[i] = boneTransforms[i].localRotation;
            }

            _networkHandPose.Value = currentPose;
        }
    }
}
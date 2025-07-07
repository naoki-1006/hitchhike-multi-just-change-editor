using UnityEngine;
using Unity.Netcode;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(HandVisual))]
    public class NetworkHandSyncer : NetworkBehaviour
    {
        [Header("ターゲット")]
        [Tooltip("ポーズを適用するアバターの「手首」のTransform")]
        public Transform WristTransform;

        private HandVisual _handVisual;
        private OVRSkeleton _localSkeleton;

        // 手の向きを補正するための回転オフセット
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
            if (WristTransform != null)
            {
                WristTransform.position = newValue.RootPosition;
                WristTransform.rotation = newValue.RootRotation;
            }

            var joints = _handVisual.Joints;
            var rotations = newValue.JointRotations;

            if (joints == null || rotations == null || joints.Count != rotations.Length) return;

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

            // ### ここからロジックを全面的に修正 ###

            // Step 1: ローカルの「手首ボーン」を取得
            Transform localWristBone = boneTransforms[0];

            HandPoseData currentPose = new HandPoseData
            {
                // Step 2: 「手首ボーン」のワールド位置をセット
                RootPosition = localWristBone.position,
                
                // Step 3: 「手首ボーン」のワールド回転に「180度の補正」を加えてからセット
                RootRotation = localWristBone.rotation * _rotationOffset,
                
                // Step 4: 全関節のlocalRotationを配列にキャプチャ
                JointRotations = new Quaternion[boneTransforms.Count]
            };

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                currentPose.JointRotations[i] = boneTransforms[i].localRotation;
            }

            // Step 5: 補正済みのデータを送信
            _networkHandPose.Value = currentPose;
        }
    }
}
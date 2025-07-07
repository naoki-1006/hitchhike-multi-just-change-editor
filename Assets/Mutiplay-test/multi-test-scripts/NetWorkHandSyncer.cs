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
        private Transform _localAnchor;
        private OVRSkeleton _localSkeleton;

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

            _localAnchor = anchor;
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

            // ### 修正点 ### _localAnchorのチェックも追加
            if (_localAnchor == null || _localSkeleton == null || !_localSkeleton.IsInitialized || !_localSkeleton.IsDataValid)
            {
                return;
            }

            var boneTransforms = _localSkeleton.BoneTransforms;
            if (boneTransforms == null || boneTransforms.Count == 0) return;
            
            HandPoseData currentPose = new HandPoseData
            {
                // ### 修正点 ### 手首ボーンではなく、保存しておいたアンカーの位置・回転を取得
                RootPosition = _localAnchor.position,
                RootRotation = _localAnchor.rotation,
                
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
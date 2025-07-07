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

        private readonly Quaternion _rotationOffset = Quaternion.Euler(0, 180f, 0);

        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void Awake()
        {
            _handVisual = GetComponent<HandVisual>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                GainedOwnership();
            }
            else
            {
                LostOwnership();
            }
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            GainedOwnership();
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
            LostOwnership();
        }

        private void GainedOwnership()
        {
            if (!IsOwner) return; // 念のため
            _handVisual.EnableAutomaticSkeletonSearch = true; // ローカルでの自動検索をON
            _networkHandPose.OnValueChanged -= OnPoseChanged;
            FindLocalSkeleton();
        }

        private void LostOwnership()
        {
            _handVisual.EnableAutomaticSkeletonSearch = false; // 他人の手は自動検索をOFF
            _localSkeleton = null;
            _localTrackingSpace = null;
            _networkHandPose.OnValueChanged += OnPoseChanged;
        }

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

        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            var joints = _handVisual.Joints;
            var rotations = newValue.JointRotations;

            if (joints == null || joints.Count == 0 || rotations == null || joints.Count != rotations.Length) return;

            Transform wristBone = joints[0];
            if (wristBone != null)
            {
                wristBone.localPosition = newValue.RootPosition;
                wristBone.localRotation = newValue.RootRotation * _rotationOffset;
            }
            
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

            if (_localSkeleton == null || !_localSkeleton.IsInitialized || !_localSkeleton.IsDataValid || _localTrackingSpace == null)
            {
                return;
            }

            var boneTransforms = _localSkeleton.BoneTransforms;
            if (boneTransforms == null || boneTransforms.Count == 0) return;

            Transform localWristBone = boneTransforms[0];
            
            Vector3 relativePos = _localTrackingSpace.InverseTransformPoint(localWristBone.position);
            Quaternion relativeRot = Quaternion.Inverse(_localTrackingSpace.rotation) * localWristBone.rotation;

            HandPoseData currentPose = new HandPoseData
            {
                RootPosition = relativePos,
                RootRotation = relativeRot,
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
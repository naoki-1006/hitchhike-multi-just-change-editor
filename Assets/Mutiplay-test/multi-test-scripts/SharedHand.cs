using UnityEngine;
using Unity.Netcode;

namespace Oculus.Interaction
{
    // ### 修正 ### NetworkObjectは親が持つため、この行を削除
    // [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(HandVisual))]
    public class SharedHand : NetworkBehaviour
    {
        private HandVisual _handVisual;
        private OVRSkeleton _localSkeleton;
        private Transform _localTrackingSpace;

        private readonly Quaternion _rotationOffset = Quaternion.Euler(0, 180f, 0);

        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        void Awake()
        {
            _handVisual = GetComponent<HandVisual>();
        }

        public override void OnNetworkSpawn()
        {
            _handVisual.enabled = false;
            _networkHandPose.OnValueChanged += OnPoseChanged;
            
            if (IsOwner)
            {
                GainedOwnership();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            _networkHandPose.OnValueChanged -= OnPoseChanged;
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
            if (!IsOwner) return;
            FindLocalSkeleton();
        }

        private void LostOwnership()
        {
            _localSkeleton = null;
            _localTrackingSpace = null;
        }

        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            if (IsOwner) return;
            ApplyPose(newValue);
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
            
            Vector3 relativePos = _localTrackingSpace.InverseTransformPoint(localWristBone.position);
            Quaternion relativeRot = Quaternion.Inverse(_localTrackingSpace.rotation) * localWristBone.rotation;

            HandPoseData currentPose = new HandPoseData
            {
                ClientId = OwnerClientId,
                RootPosition = relativePos,
                RootRotation = relativeRot,
                JointRotations = new Quaternion[boneTransforms.Count]
            };

            for (int i = 0; i < boneTransforms.Count; i++)
            {
                currentPose.JointRotations[i] = boneTransforms[i].localRotation;
            }

            ApplyPose(currentPose);
            
            _networkHandPose.Value = currentPose;
        }
        
        private void ApplyPose(HandPoseData pose)
        {
            var joints = _handVisual.Joints;
            if (joints == null || joints.Count == 0) return;

            Transform handRoot = joints[0].parent;
            if (handRoot != null)
            {
                handRoot.localPosition = pose.RootPosition;
                handRoot.localRotation = pose.RootRotation * _rotationOffset;
            }

            if (pose.JointRotations == null || joints.Count != pose.JointRotations.Length) return;
            
            for (int i = 0; i < joints.Count; i++)
            {
                if (joints[i] != null)
                {
                    joints[i].localRotation = pose.JointRotations[i];
                }
            }
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
    }
}
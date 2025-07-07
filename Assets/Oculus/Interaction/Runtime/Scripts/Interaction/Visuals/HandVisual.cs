using Oculus.Interaction.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class HandVisual : MonoBehaviour, IHandVisual
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [Header("外部OVRSkeletonによるオーバーライド")]
        [SerializeField, Tooltip("ここにOVRSkeletonを設定すると、通常のIHandの代わりにこのスケルトンをポーズのソースとして使用します。空の場合は自動で検索します。")]
        private OVRSkeleton _overrideSkeleton;

        private Quaternion[] _jointRotationsArray;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        [SerializeField]
        private bool _updateRootPose = true;
        [SerializeField]
        private bool _updateRootScale = true;
        [SerializeField, Optional]
        private Transform _root = null;
        public Transform Root => _root;

        [SerializeField, Optional]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [HideInInspector]
        [SerializeField]
        private List<Transform> _jointTransforms = new List<Transform>();
        public event Action WhenHandVisualUpdated = delegate { };

        public bool IsVisible => _skinnedMeshRenderer != null && _skinnedMeshRenderer.enabled;
        private int _wristScalePropertyId;
        public IList<Transform> Joints => _jointTransforms;
        public bool ForceOffVisibility { get; set; }
        public bool EnableAutomaticSkeletonSearch { get; set; } = true;
        
        private Transform _sourceTrackingSpace;
        private bool _started = false;
        private readonly Quaternion _rotationOffset = Quaternion.Euler(0, 180f, 0);

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            if (_root == null && _jointTransforms.Count > 0 && _jointTransforms[0] != null)
            {
                _root = _jointTransforms[0].parent;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_skinnedMeshRenderer, nameof(_skinnedMeshRenderer));
            if (_handMaterialPropertyBlockEditor != null)
            {
                _wristScalePropertyId = Shader.PropertyToID("_WristScale");
            }

            if (_hand != null || _overrideSkeleton != null)
            {
                StartCoroutine(InitializeOverride());
            }

            this.EndStart(ref _started);
        }

        private System.Collections.IEnumerator InitializeOverride()
        {
            if (_overrideSkeleton == null && EnableAutomaticSkeletonSearch)
            {
                var rig = GameObject.Find("OVRCameraRig");
                if (rig == null)
                {
                    Debug.LogWarning("OVRCameraRigが見つかりませんでした。通常のIHandで動作します。");
                    yield break;
                }

                _sourceTrackingSpace = rig.transform.Find("TrackingSpace");
                if (_sourceTrackingSpace == null)
                {
                    Debug.LogWarning("TrackingSpaceが見つかりませんでした。");
                    yield break;
                }

                string anchorPath = gameObject.name.Contains("Left") ? "LeftHandAnchor" : "RightHandAnchor";
                Transform anchor = _sourceTrackingSpace.Find(anchorPath);

                if (anchor != null)
                {
                    _overrideSkeleton = anchor.GetComponentInChildren<OVRSkeleton>();
                }
                
                if (_overrideSkeleton == null)
                {
                    Debug.LogWarning($"パス '{anchorPath}' でOVRSkeletonが見つかりませんでした。通常のIHandで動作します。");
                    yield break;
                }
            }

            if (_overrideSkeleton == null) yield break;

            while (!_overrideSkeleton.IsInitialized)
            {
                yield return null;
            }

            if (_overrideSkeleton.BoneTransforms.Count != _jointTransforms.Count)
            {
                Debug.LogError($"Override Skeletonのボーン数 ({_overrideSkeleton.BoneTransforms.Count}) と HandVisualの関節数 ({_jointTransforms.Count}) が一致しません。オーバーライドを無効にします。", this);
                _overrideSkeleton = null;
                yield break;
            }

            _jointRotationsArray = new Quaternion[_overrideSkeleton.BoneTransforms.Count];
            Debug.Log($"{this.name}: Override Skeleton '{_overrideSkeleton.name}' の準備が完了しました。");
        }

        protected virtual void OnEnable()
        {
            if (_started && _overrideSkeleton == null) { Hand.WhenHandUpdated += UpdateSkeleton; }
        }

        protected virtual void OnDisable()
        {
            if (_started && _hand != null && _overrideSkeleton == null) { Hand.WhenHandUpdated -= UpdateSkeleton; }
        }

        protected virtual void LateUpdate()
        {
            if (_overrideSkeleton != null && _overrideSkeleton.IsInitialized) { UpdateSkeleton(); }
        }

        public void UpdateSkeleton()
        {
            bool isDataValid = _overrideSkeleton != null ? _overrideSkeleton.IsDataValid : Hand.IsTrackedDataValid;
            if (!isDataValid)
            {
                if (IsVisible || ForceOffVisibility) { _skinnedMeshRenderer.enabled = false; }
                WhenHandVisualUpdated.Invoke();
                return;
            }

            if (!IsVisible && !ForceOffVisibility)
            {
                _skinnedMeshRenderer.enabled = true;
            }
            else if (IsVisible && ForceOffVisibility)
            {
                _skinnedMeshRenderer.enabled = false;
            }

            if (_overrideSkeleton != null)
            {
                var sourceBones = _overrideSkeleton.BoneTransforms;
                if (sourceBones == null || sourceBones.Count == 0 || _sourceTrackingSpace == null) return;

                Transform sourceWrist = sourceBones[0];
                if (_root != null && sourceWrist != null)
                {
                    Vector3 relativePos = _sourceTrackingSpace.InverseTransformPoint(sourceWrist.position);
                    Quaternion relativeRot = Quaternion.Inverse(_sourceTrackingSpace.rotation) * sourceWrist.rotation;

                    _root.localPosition = relativePos;
                    _root.localRotation = relativeRot * _rotationOffset;
                }

                for (int i = 0; i < sourceBones.Count; i++)
                {
                    _jointRotationsArray[i] = sourceBones[i].localRotation;
                }

                for (int i = 0; i < _jointTransforms.Count; i++)
                {
                    if (_jointTransforms[i] != null)
                    {
                        _jointTransforms[i].localRotation = _jointRotationsArray[i];
                    }
                }
            }
            else
            {
                if (_updateRootPose)
                {
                    if (_root != null && Hand.GetRootPose(out Pose handRootPose))
                    {
                        _root.position = handRootPose.position;
                        _root.rotation = handRootPose.rotation;
                    }
                }
                if (_updateRootScale)
                {
                    if (_root != null)
                    {
                        float parentScale = _root.parent != null ? _root.parent.lossyScale.x : 1f;
                        _root.localScale = Hand.Scale / parentScale * Vector3.one;
                    }
                }
                if (!Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
                {
                    return;
                }
                for (var i = 0; i < Constants.NUM_HAND_JOINTS; ++i)
                {
                    if (_jointTransforms[i] == null) { continue; }
                    _jointTransforms[i].SetPose(localJoints[i], Space.Self);
                }
            }

            if (_handMaterialPropertyBlockEditor != null)
            {
                _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloat(_wristScalePropertyId, Hand.Scale);
                _handMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
            }
            WhenHandVisualUpdated.Invoke();
        }

        public Transform GetTransformByHandJointId(HandJointId handJointId)
        {
            return _jointTransforms[(int)handJointId];
        }

        public Pose GetJointPose(HandJointId jointId, Space space)
        {
            return GetTransformByHandJointId(jointId).GetPose(space);
        }

        #region Inject
        public void InjectAllHandSkeletonVisual(IHand hand, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            InjectHand(hand);
            InjectSkinnedMeshRenderer(skinnedMeshRenderer);
        }
        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }
        public void InjectSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
        }
        public void InjectOptionalUpdateRootPose(bool updateRootPose)
        {
            _updateRootPose = updateRootPose;
        }
        public void InjectOptionalUpdateRootScale(bool updateRootScale)
        {
            _updateRootScale = updateRootScale;
        }
        public void InjectOptionalRoot(Transform root)
        {
            _root = root;
        }
        public void InjectOptionalMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            _handMaterialPropertyBlockEditor = editor;
        }
        #endregion
    }
}
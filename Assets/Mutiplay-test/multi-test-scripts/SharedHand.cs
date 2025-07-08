using UnityEngine;
using Unity.Netcode;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(HandVisual))]
    public class SharedHand : NetworkBehaviour
    {
        private HandVisual _handVisual;

        // ### 修正 ### ローカルの参照先を、OVRSkeletonからHandVisualに変更
        private HandVisual _localHandVisualSource;

        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField]
        private OwnershipHandler _ownershipHandler;

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
            // 参照先をクリアし、新しいソースを探す
            _localHandVisualSource = null;
            FindLocalHandSource();
        }

        private void LostOwnership()
        {
            // オーナーでなくなったので、参照をなくす
            _localHandVisualSource = null;
        }

        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            if (IsOwner) return;
            ApplyPose(newValue);
        }

        void Update()
        {
            //if (!IsOwner) return;
            // ここで、ownershipHandlerを参照して、ownerであるならば通過、そうでないならearly returnしておわる
            var ownerId = _ownershipHandler.OwnerClientId;
            if (ownerId != NetworkObject.OwnerClientId) return;

            // ### 修正 ### ローカルのHandVisualからポーズを取得する
            if (_localHandVisualSource == null || _localHandVisualSource.Root == null || _localHandVisualSource.Joints == null)
            {
                // まだソースが見つかっていない場合は探し続ける
                FindLocalHandSource();
                return;
            }

            var sourceJoints = _localHandVisualSource.Joints;
            var sourceRoot = _localHandVisualSource.Root;

            HandPoseData currentPose = new HandPoseData
            {
                ClientId = OwnerClientId,
                // ソースとなるHandVisualの相対的な位置・回転を取得
                RootPosition = sourceRoot.localPosition,
                RootRotation = sourceRoot.localRotation,
                JointRotations = new Quaternion[sourceJoints.Count]
            };

            for (int i = 0; i < sourceJoints.Count; i++)
            {
                currentPose.JointRotations[i] = sourceJoints[i].localRotation;
            }

            // 自分の見た目も更新
            ApplyPose(currentPose);
            
            // ネットワークに送信
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
                // ### 修正 ### 送信されるデータは既に補正済みなので、オフセットは不要
                handRoot.localRotation = pose.RootRotation;
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

        /// <summary>
        /// 現在のオーナーのプレイヤーオブジェクトを探し、そこから対応するHandVisualを見つける
        /// </summary>
        private void FindLocalHandSource()
        {
            if (!IsOwner) return;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

            // 自分のプレイヤーオブジェクトを取得
            NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObject == null)
            {
                Debug.LogWarning($"Owner (ID: {OwnerClientId}) のPlayerObjectが見つかりません。");
                return;
            }

            // 探すべき手が左手か右手かを、このオブジェクトの名前から判断
            bool amILeftHand = gameObject.name.Contains("Left");

            // プレイヤーオブジェクトの子から、全てのHandVisualを探す
            HandVisual[] allHandVisuals = playerObject.GetComponentsInChildren<HandVisual>();
            foreach (var visual in allHandVisuals)
            {
                // この共有ハンド自身は除外する
                if (visual == _handVisual) continue;

                bool isTargetLeftHand = visual.gameObject.name.Contains("Left");
                if (amILeftHand == isTargetLeftHand)
                {
                    _localHandVisualSource = visual;
                    Debug.Log($"同期元のHandVisualとして、プレイヤー'{playerObject.name}'の'{visual.gameObject.name}'を発見しました。");
                    return;
                }
            }
        }
    }
}
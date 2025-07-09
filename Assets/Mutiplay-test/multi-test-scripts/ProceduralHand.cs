using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(HandVisual))]
    public class ProceduralHand : NetworkBehaviour
    {
        [Header("アニメーション設定")]
        [Tooltip("人差し指が動く速さ")]
        public float AnimationSpeed = 2f;
        [Tooltip("人差し指が曲がる最大の角度")]
        public float MaxAngle = 45f;
        [Tooltip("手のひらの向き（オイラー角）")]
        public Vector3 BaseHandRotation = new Vector3(-90, 0, 180);

        private HandVisual _handVisual;

        // NetCodeのバージョンによっては、structの初期化が必要な場合があります
        private readonly NetworkVariable<HandPoseData> _networkHandPose = new NetworkVariable<HandPoseData>(
            new HandPoseData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // 標準的なOVRSkeletonのボーンインデックス
        private const int INDEX_FINGER_1 = 7;
        private const int INDEX_FINGER_2 = 8;
        private const int INDEX_FINGER_3 = 6;

        void Awake()
        {
            _handVisual = GetComponent<HandVisual>();
        }

        public override void OnNetworkSpawn()
        {
            // このスクリプトが全てを制御するため、HandVisualのデフォルト更新は無効化
            _handVisual.enabled = false;

            // 他人のオブジェクトの場合、データ受信時にポーズを適用する
            if (!IsOwner)
            {
                _networkHandPose.OnValueChanged += OnPoseChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkHandPose.OnValueChanged -= OnPoseChanged;
        }

        private void OnPoseChanged(HandPoseData previousValue, HandPoseData newValue)
        {
            ApplyPose(newValue);
        }

        void Update()
        {
            // オーナー（通常はサーバーか、所有権を持つクライアント）でなければ何もしない
            if (!IsOwner) return;

            // --- ここから動きを生成するロジック ---

            // 1. 時間の経過でサイン波を生成し、指の角度を計算
            float angle = Mathf.Sin(Time.time * AnimationSpeed) * MaxAngle;
            Quaternion fingerRotation = Quaternion.Euler(0, 0, angle);

            // 2. 送信するポーズデータを作成
            var joints = _handVisual.Joints;
            if (joints == null || joints.Count == 0) return;

            HandPoseData currentPose = new HandPoseData
            {
                // ClientIdは、複数の共有ハンドがある場合に備えて設定
                ClientId = OwnerClientId,
                
                // 手の根本は原点に固定し、指定された向きにする
                RootPosition = new Vector3(0f, 1f, 0.2f),
                RootRotation = Quaternion.Euler(BaseHandRotation),

                // 関節の回転データを格納する配列を準備
                JointRotations = new Quaternion[joints.Count]
            };

            // 3. 全ての関節を一度リセット（パーの状態）
            for (int i = 0; i < currentPose.JointRotations.Length; i++)
            {
                currentPose.JointRotations[i] = Quaternion.identity;
            }

            // 4. 人差し指の関節にだけ、計算した角度を設定
            if (joints.Count > INDEX_FINGER_3)
            {
                //currentPose.JointRotations[INDEX_FINGER_1] = fingerRotation;
                //currentPose.JointRotations[INDEX_FINGER_2] = fingerRotation;
                currentPose.JointRotations[INDEX_FINGER_3] = fingerRotation;
            }

            // --- 生成ここまで ---

            // 5. ローカルの見た目を更新
            ApplyPose(currentPose);
            
            // 6. ネットワークにポーズデータを送信
            _networkHandPose.Value = currentPose;
        }
        
        /// <summary>
        /// 受け取ったポーズデータをHandVisualに適用する共通メソッド
        /// </summary>
        private void ApplyPose(HandPoseData pose)
        {
            var joints = _handVisual.Joints;
            if (joints == null || joints.Count == 0) return;

            // 手の親（ルート）を取得し、そこに相対的な位置・回転を適用
            Transform handRoot = joints[0];
            if (handRoot != null)
            {
                handRoot.localPosition = pose.RootPosition;
                handRoot.localRotation = pose.RootRotation; // 向きの補正はBaseHandRotationで行うため不要
            }

            if (pose.JointRotations == null || joints.Count != pose.JointRotations.Length) return;
            
            // 全ての関節のlocalRotationを適用
            for (int i = 0; i < joints.Count; i++)
            {
                if (joints[i] != null)
                {
                    joints[i].localRotation = pose.JointRotations[i];
                }
            }
        }
    }
}
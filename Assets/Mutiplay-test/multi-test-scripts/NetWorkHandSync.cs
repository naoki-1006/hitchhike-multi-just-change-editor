using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Oculus.Interaction; // HandVisualを使うために必要

// このスクリプトはプレハブのルート(player-prehab)にアタッチしてください
public class NetworkHandSync : NetworkBehaviour
{
    [Header("オブジェクト参照")]
    [Tooltip("ローカルプレイヤー用の手の親オブジェクト")]
    [SerializeField] private GameObject localHandParent;

    [Tooltip("リモートプレイヤー用の手の親オブジェクト")]
    [SerializeField] private GameObject remoteHandParent;

    [Header("データソース（ローカル）")]
    [Tooltip("ローカルの手からボーン情報を取得するためのHandVisual")]
    [SerializeField] private HandVisual localHandVisual;

    [Header("データ適用先（リモート）")]
    [Tooltip("リモートの手のボーンのTransformリスト")]
    [SerializeField] private List<Transform> remoteBones;

    [Header("同期設定")]
    [Tooltip("リモートの手の動きを滑らかにするための補間速度")]
    [SerializeField] private float interpolationSpeed = 15f;

    // --- 内部変数 ---
    private NetworkVariable<NetworkHandData> networkHandData = new NetworkVariable<NetworkHandData>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Pose[] targetPoses;

    // ネットワークオブジェクトが生成された時に呼ばれる
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // --- 自分用の設定 ---
            localHandParent.SetActive(true);
            remoteHandParent.SetActive(false);
        }
        else
        {
            // --- 他人用の設定 ---
            localHandParent.SetActive(false);
            remoteHandParent.SetActive(true);
            
            // データ受信時のコールバックを登録
            networkHandData.OnValueChanged += OnHandDataChanged;
        }
    }

    // ネットワークオブジェクトが破棄される時に呼ばれる
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsOwner)
        {
            networkHandData.OnValueChanged -= OnHandDataChanged;
        }
    }

    void Update()
    {
        if (IsOwner)
        {
            // 自分なら、手のデータをネットワークに送信する
            UpdateHandData();
        }
        else
        {
            // 他人なら、受信したデータに向かって滑らかに手を動かす
            ApplyInterpolation();
        }
    }

    // --- データ送信 & 受信処理 ---

    private void UpdateHandData()
    {
        if (localHandVisual == null || localHandVisual.Joints == null || localHandVisual.Joints.Count == 0) return;

        var joints = localHandVisual.Joints;
        Pose[] poses = new Pose[joints.Count];
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] == null) continue;
            poses[i] = new Pose(joints[i].localPosition, joints[i].localRotation);
        }
        networkHandData.Value = new NetworkHandData(poses);
    }

    private void OnHandDataChanged(NetworkHandData previousValue, NetworkHandData newValue)
    {
        targetPoses = newValue.BonePoses;
    }

    private void ApplyInterpolation()
    {
        if (targetPoses == null || remoteBones == null || remoteBones.Count == 0) return;

        for (int i = 0; i < remoteBones.Count && i < targetPoses.Length; i++)
        {
            if (remoteBones[i] != null)
            {
                remoteBones[i].localPosition = Vector3.Lerp(remoteBones[i].localPosition, targetPoses[i].position, Time.deltaTime * interpolationSpeed);
                remoteBones[i].localRotation = Quaternion.Slerp(remoteBones[i].localRotation, targetPoses[i].rotation, Time.deltaTime * interpolationSpeed);
            }
        }
    }
}
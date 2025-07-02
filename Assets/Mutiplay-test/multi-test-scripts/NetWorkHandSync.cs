using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections; // コルーチンを使うために追加

// このスクリプトはOVRHandPrefabにアタッチします
public class NetworkHandSync : NetworkBehaviour
{
    // インスペクターでの設定は不要。実行時に自動で見つけます。
    private OVRSkeleton skeleton;
    private List<Transform> bones;

    private NetworkVariable<NetworkHandData> networkHandData = new NetworkVariable<NetworkHandData>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Pose[] targetPoses;
    [SerializeField] private float interpolationSpeed = 15f;

    void Awake()
    {
        // 必要なコンポーネントを自動で取得
        skeleton = GetComponent<OVRSkeleton>();
        bones = new List<Transform>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            // 他人の手の場合、OVRHand(入力)のみを無効化する。
            // OVRSkeleton(骨格)はボーン構造を提供してもらうために残す。
            GetComponent<OVRHand>().enabled = false;
            networkHandData.OnValueChanged += OnHandDataChanged;
        }

        // ボーンが生成されるのを待ってから処理を開始するコルーチンを起動
        StartCoroutine(WaitForBones());
    }

    private IEnumerator WaitForBones()
    {
        // skeleton.Bonesにボーン情報が設定されるまで待機
        while (skeleton.Bones.Count == 0)
        {
            yield return null;
        }

        // ボーンリストが生成されたので、自分のリストにコピーする
        foreach (var bone in skeleton.Bones)
        {
            bones.Add(bone.Transform);
        }
    }

    void Update()
    {
        // ボーンの準備ができるまで何もしない
        if (bones.Count == 0) return;

        if (IsOwner)
        {
            UpdateHandData();
        }
        else
        {
            ApplyInterpolation();
        }
    }
    
    // (以下、UpdateHandData, OnHandDataChanged, ApplyInterpolationはほぼ同じ)

    private void UpdateHandData()
    {
        // bonesリストは実行時に設定済み
        Pose[] poses = new Pose[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            poses[i] = new Pose(bones[i].localPosition, bones[i].localRotation);
        }
        networkHandData.Value = new NetworkHandData() { bonePoses = poses };
    }

    private void OnHandDataChanged(NetworkHandData prev, NetworkHandData next)
    {
        targetPoses = next.bonePoses;
    }

    private void ApplyInterpolation()
    {
        if (targetPoses == null || bones.Count == 0) return;
        
        for (int i = 0; i < bones.Count && i < targetPoses.Length; i++)
        {
            if (bones[i] != null)
            {
                bones[i].localPosition = Vector3.Lerp(bones[i].localPosition, targetPoses[i].position, Time.deltaTime * interpolationSpeed);
                bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, targetPoses[i].rotation, Time.deltaTime * interpolationSpeed);
            }
        }
    }
}
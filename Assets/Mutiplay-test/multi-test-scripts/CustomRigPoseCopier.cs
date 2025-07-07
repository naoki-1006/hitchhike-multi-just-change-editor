using UnityEngine;
using System.Collections.Generic;

public class CustomRigPoseCopier : MonoBehaviour
{
    [Header("コピー元")]
    [Tooltip("ポーズの取得元となるOVRSkeleton")]
    public OVRSkeleton sourceSkeleton;

    [Header("コピー先")]
    [Tooltip("ポーズを適用する関節（Transform）のリスト。\nSource Skeletonのボーンと同じ順序で設定してください。")]
    public List<Transform> destinationJoints = new List<Transform>();

    // 関節の回転情報を格納するための配列
    private Quaternion[] jointRotationsArray;

    void Start()
    {
        // --- 初期化と検証 ---
        if (sourceSkeleton == null)
        {
            Debug.LogError("Source Skeletonが設定されていません。", this);
            this.enabled = false;
            return;
        }
        if (destinationJoints.Count == 0)
        {
            Debug.LogError("Destination Jointsが1つも設定されていません。", this);
            this.enabled = false;
            return;
        }

        // ボーンの数と関節リストの数が一致しない場合は、致命的なエラーとして処理を停止
        if (sourceSkeleton.Bones.Count != destinationJoints.Count)
        {
            Debug.LogError($"Sourceのボーン数 ({sourceSkeleton.Bones.Count}) と Destinationの関節数 ({destinationJoints.Count}) が一致しません！\n" +
                           "リストの順序と数を確認してください。", this);
            this.enabled = false;
            return;
        }

        // 配列を初期化
        jointRotationsArray = new Quaternion[sourceSkeleton.Bones.Count];
    }

    void LateUpdate()
    {
        if (!this.enabled) return;

        // --- ステップ1: Sourceから関節角度を配列に取得 ---
        // ### 修正点 ###
        // OVRSkeleton.BonesはOVRBoneのリストを返すため、ループ内で.Transformにアクセスします。
        for (int i = 0; i < sourceSkeleton.Bones.Count; i++)
        {
            // OVRBone構造体からTransformを取得します
            Transform boneTransform = sourceSkeleton.Bones[i].Transform;
            if (boneTransform != null)
            {
                jointRotationsArray[i] = boneTransform.localRotation;
            }
        }

        // --- ステップ2: 配列のデータをDestinationの各関節に適用 ---
        for (int i = 0; i < destinationJoints.Count; i++)
        {
            if (destinationJoints[i] != null)
            {
                destinationJoints[i].localRotation = jointRotationsArray[i];
            }
        }
    }
}
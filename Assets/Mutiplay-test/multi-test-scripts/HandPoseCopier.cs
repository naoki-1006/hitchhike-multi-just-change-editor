using UnityEngine;

public class HandPoseCopier : MonoBehaviour
{
    [Header("参照元（コピー元）の手")]
    [Tooltip("動きのソースとなるOVRHandを指定します。")]
    public OVRHand sourceHand;

    private OVRSkeleton sourceSkeleton;
    private OVRSkeleton targetSkeleton;

    void Start()
    {
        // このスクリプトがアタッチされているオブジェクトからターゲットのOVRSkeletonを取得//
        targetSkeleton = GetComponent<OVRSkeleton>();

        if (sourceHand != null)
        {
            // 参照元のOVRHandからソースのOVRSkeletonを取得
            sourceSkeleton = sourceHand.GetComponent<OVRSkeleton>();
        }

        // 必要なコンポーネントが見つからない場合はエラーログを出力
        if (sourceSkeleton == null || targetSkeleton == null)
        {
            Debug.LogError("必要なOVRSkeletonコンポーネントが見つかりません。Source HandとTarget Handが正しく設定されているか確認してください。");
            this.enabled = false; // スクリプトを無効化
        }
    }

    void LateUpdate()
{
    // 参照元とターゲットのスケルトンが利用可能かチェック
    if (sourceSkeleton == null || targetSkeleton == null) return;
    
    // 念のため、ボーンの数が一致しているか確認
    if (sourceSkeleton.Bones.Count != targetSkeleton.Bones.Count) return;

    // 全てのボーンに対して処理を実行
    for (int i = 0; i < sourceSkeleton.Bones.Count; i++)
    {
        // ソースのボーンからターゲットのボーンへローカル回転をコピー
        // .Transform を介してlocalRotationにアクセスする
        targetSkeleton.Bones[i].Transform.localRotation = sourceSkeleton.Bones[i].Transform.localRotation;
    }
}
}
using UnityEngine;
using Unity.Netcode;

// Netcode for GameObjectsで送受信するためのデータ構造
// INetworkSerializableを実装する必要があります
public struct NetworkHandData : INetworkSerializable
{
    // 手の各ボーン（関節）の位置と回転を格納する配列
    private Pose[] bonePoses;

    // プロパティとして外部からアクセスできるようにする
    public Pose[] BonePoses => bonePoses;

    // コンストラクタ
    public NetworkHandData(Pose[] poses)
    {
        bonePoses = poses;
    }

    // ネットワーク経由でデータを書き込み/読み込みするためのメソッド
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // --- 書き込み処理 (IsWriter) ---
        if (serializer.IsWriter)
        {
            // 配列がnullでないことを確認
            // nullの場合は長さ0として書き込む
            int length = bonePoses?.Length ?? 0;
            serializer.SerializeValue(ref length);

            // 各ボーンの情報を書き込む
            if (length > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    serializer.SerializeValue(ref bonePoses[i].position);
                    serializer.SerializeValue(ref bonePoses[i].rotation);
                }
            }
        }
        // --- 読み込み処理 (IsReader) ---
        else
        {
            // 配列の長さを読み込む
            int length = 0;
            serializer.SerializeValue(ref length);

            // 読み込んだ長さで配列を初期化
            bonePoses = new Pose[length];

            // 各ボーンの情報を読み込んで配列に格納
            if (length > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    Vector3 pos = Vector3.zero;
                    Quaternion rot = Quaternion.identity;
                    serializer.SerializeValue(ref pos);
                    serializer.SerializeValue(ref rot);
                    bonePoses[i] = new Pose(pos, rot);
                }
            }
        }
    }
}


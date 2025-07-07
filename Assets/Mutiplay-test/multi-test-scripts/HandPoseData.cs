using UnityEngine;
using Unity.Netcode;

public struct HandPoseData : INetworkSerializable
{
    // ### 追加 ### 送信者のクライアントID
    public ulong ClientId;

    public Vector3 RootPosition;
    public Quaternion RootRotation;
    public Quaternion[] JointRotations;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // ### 追加 ### ClientIdの送受信処理
        serializer.SerializeValue(ref ClientId);

        serializer.SerializeValue(ref RootPosition);
        serializer.SerializeValue(ref RootRotation);

        int length = 0;
        if (!serializer.IsReader)
        {
            if (JointRotations != null) { length = JointRotations.Length; }
        }
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            JointRotations = new Quaternion[length];
        }

        for (int i = 0; i < length; i++)
        {
            serializer.SerializeValue(ref JointRotations[i]);
        }
    }
}
using UnityEngine;
using Unity.Netcode;

public struct HandPoseData : INetworkSerializable
{
    // ### ここから追加 ###
    public Vector3 RootPosition;
    public Quaternion RootRotation;
    // ### ここまで追加 ###

    public Quaternion[] JointRotations;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // ### ここから追加 ###
        // Rootの情報を書き込み/読み込み
        serializer.SerializeValue(ref RootPosition);
        serializer.SerializeValue(ref RootRotation);
        // ### ここまで追加 ###

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
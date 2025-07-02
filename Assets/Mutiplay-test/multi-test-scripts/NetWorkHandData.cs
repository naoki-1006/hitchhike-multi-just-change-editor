using UnityEngine;
using Unity.Netcode;

public struct NetworkHandData : INetworkSerializable
{
    public Pose[] bonePoses;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int length = 0;
        if (serializer.IsWriter)
        {
            length = bonePoses?.Length ?? 0;
        }
        
        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            bonePoses = new Pose[length];
        }

        for (int i = 0; i < length; i++)
        {
            Vector3 pos = bonePoses[i].position;
            Quaternion rot = bonePoses[i].rotation;
            serializer.SerializeValue(ref pos);
            serializer.SerializeValue(ref rot);
            if (serializer.IsReader)
            {
                bonePoses[i] = new Pose(pos, rot);
            }
        }
    }
}
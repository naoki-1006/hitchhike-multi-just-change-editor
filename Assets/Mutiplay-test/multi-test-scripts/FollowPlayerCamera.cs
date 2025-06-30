using UnityEngine;

public class FollowPlayerCamera : MonoBehaviour
{
    public Transform player; // プレイヤーのTransformをInspectorから設定
    public Vector3 offset;   // プレイヤーからの相対的な位置オフセット

    void LateUpdate()
    {
        // プレイヤーの位置にオフセットを加えた場所にカメラを移動
        transform.position = player.position + offset;
    }
}

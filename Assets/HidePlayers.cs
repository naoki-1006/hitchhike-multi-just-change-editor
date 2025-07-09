using UnityEngine;
using Unity.Netcode;

/// <summary>
/// NetworkObjectがスポーンされた際、それが自身のオブジェクト（オーナー）であれば、
/// 指定されたRendererを非表示にするスクリプト。
/// 主にローカルプレイヤーの体を非表示にし、一人称視点でのカメラクリッピングを防ぐために使用します。
/// </summary>
public class HideLocalPlayer : NetworkBehaviour
{
    [Header("非表示にしたいRenderer")]
    [Tooltip("このリストに含まれるRendererは、ローカルプレイヤーの画面でのみ非表示になります。")]
    [SerializeField]
    private Renderer[] renderersToHide;

    /// <summary>
    /// このオブジェクトがネットワーク上でスポーンされた時に呼び出されます。
    /// Netcode for GameObjectsのライフサイクルイベントです。
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // このオブジェクトが自分自身（ローカルプレイヤー）のものかを確認します。
        // IsOwnerがtrueの場合、このコードはクライアント側で、
        // かつそのクライアントがこのオブジェクトの所有者であることを意味します。
        if (true)
        {
            // 指定されたすべてのRendererを無効化（非表示に）します。
            foreach (var renderer in renderersToHide)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
        // IsOwnerがfalseの場合（つまり、他のプレイヤーのキャラクターの場合）、
        // 何もしないため、Rendererは有効なままであり、その姿は表示され続けます。
    }
}
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TargetController : NetworkBehaviour
{
    // シーン外からでもアクセスできるSingletonインスタンス
    public static TargetController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private List<Target> targetPrefabs = new(); // ターゲットのプレハブ
    [SerializeField] private float spawnRadius = 0.7f;  // 出現半径
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    public List<int> Reachingcount = new List<int>();
    private Dictionary<ulong, Target> clientTargets = new Dictionary<ulong, Target>();


    // サーバーが管理するターゲットのインスタンス
    private Target currentTargetInstance;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Instance = this;

            // クライアントが接続した時と切断した時のイベントにメソッドを登録
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            // サーバー自身（ホスト）のぶんのターゲットを最初に生成する
            SpawnTargetForClient(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // サーバー（ホスト）は既に生成済みなので、それ以外のクライアントの場合のみ処理
        //if (clientId != NetworkManager.Singleton.LocalClientId)
        if (IsServer)
        {
            SpawnTargetForClient(clientId);
        }
    }

    /// <summary>
    /// クライアントが切断したときにサーバーで呼ばれる
    /// </summary>
    private void HandleClientDisconnected(ulong clientId)
    {
        if (IsServer && clientTargets.TryGetValue(clientId, out Target targetToDestroy))
        {
            // ターゲットをネットワークから破棄し、管理リストから削除
            targetToDestroy.GetComponent<NetworkObject>().Despawn();
            clientTargets.Remove(clientId);
        }
    }

    /// <summary>
    /// ネットワーク上にターゲットオブジェクトを1つ生成する
    /// </summary>
    private void SpawnTargetForClient(ulong clientId)
    {
        if (targetPrefabs.Count == 0) return;

        // クライアントIDに対応するプレハブを選択 (IDがプレハブ数を超える場合は巡回させる)
        int prefabIndex = (int)clientId % targetPrefabs.Count;
        var targetPrefab = targetPrefabs[prefabIndex];

        // プレハブを生成し、所有権(Ownership)をクライアントに与えてスポーンさせる
        var targetGO = Instantiate(targetPrefab) as Target;
        //NetworkObject netObj = targetGO.GetComponent<NetworkObject>();
        //netObj.SpawnWithOwnership(clientId, true); // ★重要：所有権を与える
        targetGO.PlayerId = clientId;
        targetGO.NetworkObject.Spawn();
        Debug.Log(clientId);

        // 管理リストに追加
        Target targetComponent = targetGO.GetComponent<Target>();
        clientTargets[clientId] = targetComponent;

        Reachingcount[(int)clientId] = 0;

        // 最初の位置を決定
        SpawnNewTargetPosition(clientId);
    }


    /// <summary>
    /// 新しいターゲットの位置を計算し、ターゲットに通知する (サーバーでのみ実行)
    /// </summary>
    public void SpawnNewTargetPosition(ulong clientId)
    {
        // 管理リストにそのクライアントのターゲットが存在するか確認
        if (!clientTargets.TryGetValue(clientId, out Target target))
        {
            return;
        }
        if (spawnPoints.Count == 0) return;

        // ランダムな基準点を選択
        Transform selectedPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Vector3 randomPos = selectedPoint.position + Random.onUnitSphere * spawnRadius + new Vector3(0f, 1f, 0.2f);

        // ターゲットの位置情報を更新
        target.NetworkPosition.Value = randomPos;
        Debug.Log(randomPos);
    }
}
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TargetController : NetworkBehaviour
{
    // シーン外からでもアクセスできるSingletonインスタンス
    public static TargetController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject targetPrefab; // ターゲットのプレハブ
    [SerializeField] private float spawnRadius = 0.7f;  // 出現半径
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    public int Reachingcount;


    // サーバーが管理するターゲットのインスタンス
    private Target currentTargetInstance;

    public override void OnNetworkSpawn()
    {
        // サーバー側で自身のインスタンスをセット
        if (IsServer)
        {
            Instance = this;
        }

        // サーバーでなければ、このコンポーネントは不要
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // サーバーとして起動したら、ターゲットを1つ生成する
        SpawnTargetObject();
        // 最初の位置を決定する
        SpawnNewTargetPosition();
        Reachingcount = 0;
    }

    /// <summary>
    /// ネットワーク上にターゲットオブジェクトを1つ生成する
    /// </summary>
    private void SpawnTargetObject()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("Target Prefab is not assigned in TargetController.");
            return;
        }

        GameObject targetGO = Instantiate(targetPrefab);
        targetGO.GetComponent<NetworkObject>().Spawn(true);
        currentTargetInstance = targetGO.GetComponent<Target>();
    }

    /// <summary>
    /// 新しいターゲットの位置を計算し、ターゲットに通知する (サーバーでのみ実行)
    /// </summary>
    public void SpawnNewTargetPosition()
    {
        if (currentTargetInstance == null) return;

        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedPoint = spawnPoints[randomIndex];

        Vector3 randomPos = selectedPoint.position + Random.onUnitSphere * spawnRadius + new Vector3(0f, 1f, 0.2f);
        
        // ターゲットが持つNetworkVariableの値を直接更新する
        currentTargetInstance.NetworkPosition.Value = randomPos;
    }
}
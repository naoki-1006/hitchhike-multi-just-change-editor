using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HitchHikeMultiplayer{
public class OwnershipRequester : MonoBehaviour
{
    // シーン内の全ての共有ハンドのOwnershipHandlerを保持するリスト
    private List<OwnershipHandler> _allHandlers = new List<OwnershipHandler>();

    void Start()
    {
        // シーンが読み込まれた際に、一度だけ全てのOwnershipHandlerを探してリストに格納する
        _allHandlers = FindObjectsOfType<OwnershipHandler>().ToList();

        if (_allHandlers.Count == 0)
        {
            Debug.LogWarning("シーンにOwnershipHandlerを持つオブジェクトが見つかりませんでした。");
        }
    }

    void Update()
    {
        // 数字キーの入力をチェック
        if (Input.GetKeyDown(KeyCode.Alpha1)) { SetExclusiveOwnership("1"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SetExclusiveOwnership("2"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { SetExclusiveOwnership("3"); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { SetExclusiveOwnership("4"); }
    }

    /// <summary>
    /// 指定された識別子を持つハンドの所有権を要求し、それ以外の所有権を放棄する
    /// </summary>
    /// <param name="targetIdentifier">オブジェクト名に含まれる数字（"1", "2" など）</param>
    private void SetExclusiveOwnership(string targetIdentifier)
    {
        Debug.Log($"'{targetIdentifier}' を含むハンドの所有権を要求します。");

        foreach (var handler in _allHandlers)
        {
            // オブジェクト名にターゲットの数字が含まれているかチェック
            if (handler.gameObject.name.Contains(targetIdentifier))
            {
                // これがターゲットなので、所有権を要求
                handler.RequestOwnership();
            }
            else
            {
                // これ以外のものなので、所有権を放棄（もし自分がオーナーなら）
                handler.ReleaseOwnership();
            }
        }
    }
    public void SetExclusiveOwnership(){
        SetExclusiveOwnership(name);
    }
}
}
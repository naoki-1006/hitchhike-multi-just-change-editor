using UnityEngine;

public class OwnershipRequester : MonoBehaviour
{
    // シーンに一つしかない想定のため、FindObjectOfTypeで探す
    private OwnershipHandler _targetHandler;

    void Start()
    {
        // 씬 내의 공유 핸드를 찾습니다.
        _targetHandler = FindObjectOfType<OwnershipHandler>();

        if (_targetHandler == null)
        {
            Debug.LogWarning("シーンにOwnershipHandlerを持つオブジェクトが見つかりませんでした。");
        }
    }

    void Update()
    {
        // 'O'キーが押されたら所有権を要求する
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (_targetHandler != null)
            {
                Debug.Log("キーボード入力で所有権を要求します...");
                _targetHandler.RequestOwnership();
            }
            else
            {
                Debug.LogError("所有権を要求するターゲットが見つかりません。");
            }
        }
    }
}
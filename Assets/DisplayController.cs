using UnityEngine;
using TMPro; // UIを操作するために必要

public class DisplayController : MonoBehaviour
{
    // Inspectorから設定するTextコンポーネント
    public TMP_Text reachingText;

    // TargetControllerスクリプトへの参照を保持する変数
    [SerializeField]
    private TargetController targetController;

    void Start()
    {
        // シーン内に存在するTargetControllerのインスタンスを検索して取得
        //targetController = FindObjectOfType<TargetController>();

        // TargetControllerが見つからなかった場合のエラーログ
        if (targetController == null)
        {
            Debug.LogError("シーン内に TargetController スクリプトが見つかりません。");
        }
    }

    void Update()
    {
        // targetControllerが正常に取得できている場合のみ実行
        if (targetController != null)
        {
            // targetControllerの変数nの値を取得し、テキストを更新
            reachingText.text = "Reaching:" + targetController.Reachingcount;
        }
    }
}
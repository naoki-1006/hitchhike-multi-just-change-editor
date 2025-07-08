using UnityEngine;

namespace HitchHikeMultiplayer
{
    public class LocalSkeleton : MonoBehaviour
    {
        // Singleton
        // シーン中に1つしか存在しない
        // 2つ目のオブジェクトは自動で破棄される
        // 1つしかないのでstatic変数として取り出せる
        
        public static LocalSkeleton Instance { private set; get; }

        public Transform LocalTrackingSpace => _localTrackingSpace;
        [SerializeField]
        Transform _localTrackingSpace;

        public OVRSkeleton LeftSkeleton => _left;
        [SerializeField]
        OVRSkeleton _left;

        public OVRSkeleton RightSkeleton => _right;
        [SerializeField]
        OVRSkeleton _right;

        void Awake(){
            if (Instance != null){
                // すでに1つ目のオブジェクトが存在するので、
                // 自身を破棄する
                Destroy(this);
                return;
            }
            Instance = this;
        }
    }
}
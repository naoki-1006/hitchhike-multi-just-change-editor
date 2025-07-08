using UnityEngine;

namespace HitchHikeMultiplayer{
    public class Gaze : MonoBehaviour
    {
        [SerializeField] // デバッグ用にinspectorに出してるだけ
        OwnershipRequester _currentRequester;

        [SerializeField]
        float _maxGazeTime = 1f;
        float _currentGazeTime = 0f;

        void FixedUpdate()
        {
            RaycastHit hit;
            // Casts a ray from the current position in the forward direction
            if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
            {
                Collider col = hit.collider;
                Transform parent = col.transform.parent; // てきとう
                if (parent.TryGetComponent<OwnershipRequester>(out var requester)){
                    if (_currentRequester != requester){
                        _currentRequester = requester;
                        _currentGazeTime = 0f;
                    }
                    else{
                        _currentGazeTime += Time.fixedDeltaTime;
                    }
                }
            }

            if (_maxGazeTime <= _currentGazeTime){
                _currentGazeTime = 0f;
                // 遷移処理
                _currentRequester.SetExclusiveOwnership();
            }
        }
    }
}
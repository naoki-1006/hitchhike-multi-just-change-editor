using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem.UI;
#endif

namespace Unity.Multiplayer.Center.NetcodeForGameObjectsExample
{
    /// <summary>
    /// A basic example of a UI to start a host or client.
    /// If you want to modify this Script please copy it into your own project and add it to your copied UI Prefab.
    /// </summary>
    public class StartUIForVR : MonoBehaviour
    {
        [SerializeField]
        Button m_StartHostButton;
        [SerializeField]
        Button m_StartClientButton;

        void Awake()
        {
            if (!FindAnyObjectByType<EventSystem>())
            {
                Debug.Log("No");
                var inputType = typeof(OVRInputModule);
#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
                inputType = typeof(OVRInputModule);                
#endif
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), inputType);
                eventSystem.transform.SetParent(transform);
            }


        }

        // Start is called before the first frame update
        void Start()
        {
            m_StartHostButton.onClick.AddListener(StartHost);
            m_StartClientButton.onClick.AddListener(StartClient);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("'C' キーが押された！");
                // ここに 'A' キーが押された時に実行したい処理を書く
                // 例: Jump();
                // 例: PlaySound();
                StartClient();
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                Debug.Log("'H' キーが押された！");
                // ここに 'A' キーが押された時に実行したい処理を書く
                // 例: Jump();
                // 例: PlaySound();
                StartHost();
            }
        }

        void StartClient()
        {
            NetworkManager.Singleton.StartClient();
            DeactivateButtons();
        }

        void StartHost()
        {
            NetworkManager.Singleton.StartHost();
            DeactivateButtons();
        }

        void DeactivateButtons()
        {
            m_StartHostButton.interactable = false;
            m_StartClientButton.interactable = false;
        }
    }
}
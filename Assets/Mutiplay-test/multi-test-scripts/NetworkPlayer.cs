using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
public class NetworkPlayer : NetworkBehaviour
{
    public Transform root;
    public Transform rightHand;
    public Transform leftHand;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner)
        {
            root.position = VRRigReferences.Singleton.root.position;
            root.rotation = VRRigReferences.Singleton.root.rotation;

            leftHand.position = VRRigReferences.Singleton.leftHand.position;
            leftHand.rotation = VRRigReferences.Singleton.leftHand.rotation;

            rightHand.position = VRRigReferences.Singleton.rightHand.position;
            rightHand.rotation = VRRigReferences.Singleton.rightHand.rotation;
    }
        }
        
}

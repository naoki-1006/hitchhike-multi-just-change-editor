using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VRRigReferences : MonoBehaviour
{
    public static VRRigReferences Singleton;

    public Transform root;
    public Transform rightHand;
    public Transform leftHand;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake() {
        Singleton = this;    
    }
    
}

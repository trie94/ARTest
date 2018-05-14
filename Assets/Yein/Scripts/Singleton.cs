using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using GoogleARCore.CrossPlatform;

public class Singleton : MonoBehaviour {

    public static Singleton instance;
    public Transform anchor;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else if (instance != null)
        {
            Debug.Log("already exist");
        }
    }
}

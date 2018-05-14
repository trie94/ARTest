using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    [SerializeField]
    GameObject piecePrefab;

    GameObject piece;
    bool hasSpawned;

	void Start ()
    {
		
	}
	
	void Update ()
    {
        if (Singleton.instance.anchor != null && !hasSpawned)
        {
            piece = Instantiate(piecePrefab, Singleton.instance.anchor);
            Debug.Log("spawn");
            hasSpawned = true;
        }
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using UnityEngine.Networking;

public class Piece : NetworkBehaviour {

	void Start ()
    {
        transform.position = Singleton.instance.anchor.position;
        Debug.Log("piece has been spawned");
        StartCoroutine(MoveAround());
	}
	
	void Update ()
    {
        //transform.RotateAround(Singleton.instance.anchor.transform.position, Vector3.up * 0.1f, Time.deltaTime * 20f);
	}

    IEnumerator MoveAround()
    {
        float lerpSpeed = 0.5f;
        float lerpTime = 0f;
        Vector3 curPos = transform.position;
        Vector3 targetPos = transform.position + new Vector3(0.5f, 0f, 0.5f);

        while (true)
        {
            lerpTime += Time.deltaTime * lerpSpeed;
            transform.position = Vector3.Lerp(curPos, targetPos, lerpTime);

            if (lerpTime >= 1f)
            {
                lerpTime = 0f;
                Vector3 temp = curPos;
                curPos = targetPos;
                targetPos = temp;
            }
            yield return null;
        }
    }
}

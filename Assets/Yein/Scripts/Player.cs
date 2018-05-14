using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    public void AssignHost()
    {
        if (this.gameObject.tag != "player1")
        {
            this.gameObject.tag = "player1";
            Debug.Log("this is host: " + this.gameObject.tag);
        }
    }

    public void AssignClient()
    {
        if (this.gameObject.tag != "player2")
        {
            this.gameObject.tag = "player2";
            Debug.Log("this is client: " + this.gameObject.tag);
        }
    }
}

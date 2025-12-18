using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerBar : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, -1, 0);

    void LateUpdate()
    {
        transform.position = player.position + offset;
        transform.rotation = Camera.main.transform.rotation;
    }   
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Image[] keyHole;
    public int keyCount;
    public Sprite noKey;
    public Sprite withKey;

    void Update()
    {
        keyCount = PlayerMovement.keyCount;
        for (int i=0; i < keyCount; i++)
        {
            keyHole[i].sprite = withKey;
        }
    }
}

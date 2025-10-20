using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerManager : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] public static float remainingTime;

    void Update()
    {
        if (SpawnManager.checkPoint != 24)
        {
            remainingTime -= Time.deltaTime;
        }
            int seconds = Mathf.FloorToInt(remainingTime);
            timerText.text = string.Format("{00}", seconds);

        if (remainingTime <= 0)
        {
            if (SpawnManager.checkPoint != 24)
                FindObjectOfType<SpawnManager>().SwitchToTetris();
        }
    }
}
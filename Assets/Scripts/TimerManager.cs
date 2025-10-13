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
        remainingTime -= Time.deltaTime;
        int seconds = Mathf.FloorToInt(remainingTime);
        timerText.text = string.Format("{00}", seconds);

        if (remainingTime <= 0)
        {
            FindObjectOfType<SpawnManager>().SwitchToTetris();
            FindObjectOfType<SpawnManager>().Spawn();
        }
    }
}
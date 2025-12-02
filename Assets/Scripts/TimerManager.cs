using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerManager : MonoBehaviour
{
    [SerializeField] Text timerText;
    public static float remainingTime;

    void Update()
    {
        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();
        
        if (spawnManager != null)
        {
            remainingTime -= Time.deltaTime;
            
            int seconds = Mathf.FloorToInt(remainingTime);
            timerText.text = string.Format("{0:00}", seconds);

            if (remainingTime <= 0)
            {
                spawnManager.SwitchToTetris();
            }
        }
    }
}
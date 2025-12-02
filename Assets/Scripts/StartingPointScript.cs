using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartingPointScript : MonoBehaviour
{
    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == "Level1")
        {
            int gridY = 0 + TetrisMovement.yOffset;
            TetrisMovement.grid[9, gridY] = this.transform;
        }
        else if (currentScene == "Level2")
        {
            int gridY = 0 + TetrisMovement.yOffset;
            TetrisMovement.grid[19, gridY] = this.transform;
        }
        else if (currentScene == "Level3")
        {
            int gridY = 0 + TetrisMovement.yOffset;
            TetrisMovement.grid[19, gridY] = this.transform;
        }
    }
}
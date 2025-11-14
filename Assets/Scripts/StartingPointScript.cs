using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartingPointScript : MonoBehaviour
{
    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Level1")
        {
            TetrisMovement.grid[9, 0] = this.transform;
        }
        else if (currentScene == "Level2")
        {
            TetrisMovement.grid[19, 0] = this.transform;
        }
        else if (currentScene == "Level3")
        {
            TetrisMovement.grid[19, 0] = this.transform;
        }
    }
}

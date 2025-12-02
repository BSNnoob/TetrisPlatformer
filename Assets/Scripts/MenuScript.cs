using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public void StartGame()
    {
        Time.timeScale = 1f;
        SpawnManager.gameOver = false;
        SpawnManager.spawnedTetrominoCount = 0;
        SceneManager.LoadScene("Level1");
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        SpawnManager.gameOver = false;
        SpawnManager.spawnedTetrominoCount = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    
    public void ExitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}
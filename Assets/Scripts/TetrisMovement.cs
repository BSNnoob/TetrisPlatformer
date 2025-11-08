using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TetrisMovement : MonoBehaviour
{
    public Vector3 rotationPoint;
    public float fallTime = 1f;
    public float fallTimer;
    public float changeTimer;
    public static int height = 20;
    public static int width = 20;
    public static Transform[,] grid;
    [SerializeField] SpawnManager spawnManager;
    [SerializeField] BlockSpriteManager blockSpriteManager;
    
    private static int currentLevel = 1;
    private static string lastSceneName = "";

    void Awake()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        blockSpriteManager = FindObjectOfType<BlockSpriteManager>();
        
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName != lastSceneName)
        {
            grid = new Transform[width, height];
            lastSceneName = sceneName;
            Debug.Log($"Grid reinitialized for scene: {sceneName}");
        }
        
        if (sceneName == "Level1") currentLevel = 1;
        else if (sceneName == "Level2") currentLevel = 2;
        else if (sceneName == "Level3") currentLevel = 3;
        else currentLevel = 1;
        
        if (grid == null)
        {
            grid = new Transform[width, height];
        }
    }

    void Update()
    {
        TetrominoData data = GetComponent<TetrominoData>();
        if (blockSpriteManager != null && data != null)
        {
            blockSpriteManager.UpdateFallingTetromino(gameObject, data.pieceData.blockType);
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            transform.RotateAround(transform.TransformPoint(rotationPoint), new Vector3(0, 0, 1), 90);
            if (!ValidGrid())
            {
                transform.RotateAround(transform.TransformPoint(rotationPoint), new Vector3(0, 0, 1), -90);
            }
            else
            {
                foreach (Transform child in transform)
                {
                    Vector3 pos = child.position;
                    child.position = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
                    
                    child.rotation = Quaternion.identity;
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            transform.position += new Vector3(-1, 0, 0);
            if (!ValidGrid()) transform.position -= new Vector3(-1, 0, 0);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            transform.position += new Vector3(1, 0, 0);
            if (!ValidGrid()) transform.position -= new Vector3(1, 0, 0);
        }

        if (ValidFall())
        {
            fallTime = Input.GetKey(KeyCode.S) ? 0.1f : 0.5f;
            fallTimer -= Time.deltaTime;
            if (fallTimer <= 0)
            {
                transform.position += new Vector3(0, -1, 0);
                if (!ValidGrid())
                {
                    transform.position -= new Vector3(0, -1, 0);
                    AddToGrid();
                    this.enabled = false;
                    return;
                }
                fallTimer = fallTime;
            }
        }
        else
        {
            AddToGrid();
            this.enabled = false;
            return;
        }
    }

    void AddToGrid()
    {
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);
            
            if (roundX >= 0 && roundX < width && roundY >= 0 && roundY < height)
            {
                grid[roundX, roundY] = children;
                spawnManager.blocks++;
            }
            else
            {
                Debug.LogWarning($"Block at ({roundX}, {roundY}) is out of bounds!");
            }
        }
        
        if (blockSpriteManager != null)
        {
            foreach (Transform children in transform)
            {
                int roundX = Mathf.RoundToInt(children.transform.position.x);
                int roundY = Mathf.RoundToInt(children.transform.position.y);
                
                if (roundX >= 0 && roundX < width && roundY >= 0 && roundY < height)
                {
                    blockSpriteManager.UpdateBlockSprite(roundX, roundY, grid, width, height);
                    
                    if (roundY + 1 < height) 
                        blockSpriteManager.UpdateBlockSprite(roundX, roundY + 1, grid, width, height);
                    if (roundY - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(roundX, roundY - 1, grid, width, height);
                    if (roundX - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(roundX - 1, roundY, grid, width, height);
                    if (roundX + 1 < width) 
                        blockSpriteManager.UpdateBlockSprite(roundX + 1, roundY, grid, width, height);
                }
            }
        }
        
        if (!CheckGameOver())
        {
            SpawnManager sm = FindObjectOfType<SpawnManager>();
            if (sm != null)
            {
                sm.Spawn();
            }
        }
    }

    bool CheckGameOver()
    {
        int checkpointY = SpawnManager.checkPoint;
        
        if (currentLevel == 1)
        {
            // Level 1: Standard 10-wide grid (x: 0-9)
            for (int i = 0; i < 10; i++)
            {
                if (checkpointY < height && grid[i, checkpointY] != null)
                {
                    Debug.Log($"Checkpoint reached at Y={checkpointY}, switching to platformer");
                    this.enabled = false;
                    FindObjectOfType<SpawnManager>().Switch();
                    return true;
                }
            }
        }
        else if (currentLevel == 2)
        {
            // Level 2: L-shape
            // Check based on checkpoint Y position
            if (checkpointY <= 10)
            {
                // Bottom section: 20 blocks wide (x: 0-19)
                for (int i = 0; i < 20; i++)
                {
                    if (checkpointY < height && grid[i, checkpointY] != null)
                    {
                        Debug.Log($"Checkpoint reached at Y={checkpointY}, switching to platformer");
                        this.enabled = false;
                        FindObjectOfType<SpawnManager>().Switch();
                        return true;
                    }
                }
            }
            else
            {
                // Top section: 10 blocks wide (x: 0-9)
                for (int i = 0; i < 10; i++)
                {
                    if (checkpointY < height && grid[i, checkpointY] != null)
                    {
                        Debug.Log($"Checkpoint reached at Y={checkpointY}, switching to platformer");
                        this.enabled = false;
                        FindObjectOfType<SpawnManager>().Switch();
                        return true;
                    }
                }
            }
        }
        else if (currentLevel == 3)
        {
            // Level 3: TODO - define game over condition
            for (int i = 0; i < 10; i++)
            {
                if (checkpointY < height && grid[i, checkpointY] != null)
                {
                    Debug.Log($"Checkpoint reached at Y={checkpointY}, switching to platformer");
                    this.enabled = false;
                    FindObjectOfType<SpawnManager>().Switch();
                    return true;
                }
            }
        }
        
        return false; // Game continues
    }

    bool ValidGrid()
    {
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);

            if (roundY < 0 || roundY >= height) return false;

            // Check bounds based on current level
            if (currentLevel == 1)
            {
                // Level 1: Standard 10-wide grid
                if (roundX < 0 || roundX >= 10) return false;
            }
            else if (currentLevel == 2)
            {
                // Level 2: L-shape
                // Y 0-10: width is 20 (x: 0-19)
                // Y 11-20: width is 10 (x: 0-9)
                if (roundY <= 10)
                {
                    if (roundX < 0 || roundX >= 20) return false;
                }
                else
                {
                    if (roundX < 0 || roundX >= 10) return false;
                }
            }
            else if (currentLevel == 3)
            {
                // Level 3: TODO - define structure
                if (roundX < 0 || roundX >= 10) return false;
            }

            if (grid[roundX, roundY] != null)
            {
                return false;
            }
        }
        return true;
    }

    bool ValidFall()
    {
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);

            if (roundY == 0 || (roundX >= 0 && roundX < width && roundY - 1 >= 0 && grid[roundX, roundY - 1] != null)) 
                return false;
        }
        return true;
    }
}
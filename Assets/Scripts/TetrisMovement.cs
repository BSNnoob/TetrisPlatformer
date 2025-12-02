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
    public static int height = 30;
    public static int width = 20;
    public static int yOffset = 10;
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
            int gridY = roundY + yOffset;
            
            if (roundX >= 0 && roundX < width && gridY >= 0 && gridY < height)
            {
                grid[roundX, gridY] = children;
                spawnManager.blocks++;
            }
            else
            {
                Debug.LogWarning($"Block at world({roundX}, {roundY}) grid({roundX}, {gridY}) is out of bounds!");
            }
        }
        
        if (blockSpriteManager != null)
        {
            foreach (Transform children in transform)
            {
                int roundX = Mathf.RoundToInt(children.transform.position.x);
                int roundY = Mathf.RoundToInt(children.transform.position.y);
                int gridY = roundY + yOffset;
                
                if (roundX >= 0 && roundX < width && gridY >= 0 && gridY < height)
                {
                    blockSpriteManager.UpdateBlockSprite(roundX, gridY, grid, width, height);
                    
                    if (gridY + 1 < height) 
                        blockSpriteManager.UpdateBlockSprite(roundX, gridY + 1, grid, width, height);
                    if (gridY - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(roundX, gridY - 1, grid, width, height);
                    if (roundX - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(roundX - 1, gridY, grid, width, height);
                    if (roundX + 1 < width) 
                        blockSpriteManager.UpdateBlockSprite(roundX + 1, gridY, grid, width, height);
                }
            }
        }
        
        SpawnManager sm = FindObjectOfType<SpawnManager>();
        if (sm != null)
        {
            bool switchedToPlatformer = sm.OnTetrominoLanded();
            
            if (!switchedToPlatformer)
            {
                sm.Spawn();
            }
        }
    }

    bool ValidGrid()
    {
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);
            int gridY = roundY + yOffset;

            if (gridY < 0 || gridY >= height) return false;

            if (currentLevel == 1)
            {
                if (roundY < 0 || roundY >= 20) return false;
                if (roundX < 0 || roundX >= 10) return false;
            }
            else if (currentLevel == 2)
            {
                if (roundY < 0 || roundY >= 20) return false;
                
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
                if (roundY < -10 || roundY >= 20) return false;
                
                if (roundY >= 11)
                {
                    if (roundX < 0 || roundX >= 10) return false;
                }
                else if (roundY >= 0)
                {
                    if (roundX < 0 || roundX >= 20) return false;
                }
                else
                {
                    if (roundX < 11 || roundX >= 20) return false;
                }
            }

            if (grid[roundX, gridY] != null)
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
            int gridY = roundY + yOffset;
            int gridYBelow = (roundY - 1) + yOffset;

            bool atBottom = false;
            if (currentLevel == 1)
            {
                atBottom = (roundY == 0);
            }
            else if (currentLevel == 2)
            {
                atBottom = (roundY == 0);
            }
            else if (currentLevel == 3)
            {
                atBottom = (roundY == -10);
            }

            if (atBottom) return false;
            
            if (roundX >= 0 && roundX < width && gridYBelow >= 0 && gridYBelow < height && grid[roundX, gridYBelow] != null)
            {
                return false;
            }
        }
        return true;
    }
}
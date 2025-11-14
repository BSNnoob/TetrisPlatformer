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
    public static int height = 30; // Increased to support Y from -10 to 20
    public static int width = 20; // Max width across all levels
    public static int yOffset = 10; // Offset to handle negative Y coordinates
    public static Transform[,] grid;
    [SerializeField] SpawnManager spawnManager;
    [SerializeField] BlockSpriteManager blockSpriteManager;
    
    private static int currentLevel = 1;
    private static string lastSceneName = "";

    void Awake()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        blockSpriteManager = FindObjectOfType<BlockSpriteManager>();
        
        // Determine level from scene name
        string sceneName = SceneManager.GetActiveScene().name;
        
        // If scene changed, reinitialize grid
        if (sceneName != lastSceneName)
        {
            grid = new Transform[width, height];
            lastSceneName = sceneName;
            Debug.Log($"Grid reinitialized for scene: {sceneName}");
        }
        
        if (sceneName == "Level1") currentLevel = 1;
        else if (sceneName == "Level2") currentLevel = 2;
        else if (sceneName == "Level3") currentLevel = 3;
        else currentLevel = 1; // Default
        
        // Initialize grid if not done yet
        if (grid == null)
        {
            grid = new Transform[width, height];
        }
    }

    void Update()
    {
        // Continuously update sprites while falling
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
                // Snap blocks to grid positions and reset their rotation
                foreach (Transform child in transform)
                {
                    Vector3 pos = child.position;
                    child.position = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
                    
                    // Reset rotation so sprites stay upright
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
        // First, add all blocks to grid
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);
            int gridY = roundY + yOffset; // Convert world Y to grid index
            
            // Safety check before adding to grid
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
        
        // Then update sprites by checking grid
        if (blockSpriteManager != null)
        {
            foreach (Transform children in transform)
            {
                int roundX = Mathf.RoundToInt(children.transform.position.x);
                int roundY = Mathf.RoundToInt(children.transform.position.y);
                int gridY = roundY + yOffset;
                
                // Only update if within bounds
                if (roundX >= 0 && roundX < width && gridY >= 0 && gridY < height)
                {
                    // Update this block
                    blockSpriteManager.UpdateBlockSprite(roundX, gridY, grid, width, height);
                    
                    // Update neighbors (with bounds checking)
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
        
        // Notify SpawnManager that a tetromino has landed
        // OnTetrominoLanded returns true if it switched to platformer mode
        SpawnManager sm = FindObjectOfType<SpawnManager>();
        if (sm != null)
        {
            bool switchedToPlatformer = sm.OnTetrominoLanded();
            
            // Only spawn next piece if we didn't switch to platformer
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

            // Check bounds based on current level
            if (currentLevel == 1)
            {
                // Level 1: Standard 10-wide grid (Y: 0-19, X: 0-9)
                if (roundY < 0 || roundY >= 20) return false;
                if (roundX < 0 || roundX >= 10) return false;
            }
            else if (currentLevel == 2)
            {
                // Level 2: L-shape
                // Y 0-10: width is 20 (x: 0-19)
                // Y 11-20: width is 10 (x: 0-9)
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
                // Level 3: S-shape
                // Top (Y 11-20): X 0-9
                // Middle (Y 0-10): X 0-19
                // Bottom (Y -10 to -1): X 11-19
                if (roundY < -10 || roundY >= 20) return false;
                
                if (roundY >= 11)
                {
                    // Top section: narrow (x: 0-9)
                    if (roundX < 0 || roundX >= 10) return false;
                }
                else if (roundY >= 0)
                {
                    // Middle section: full width (x: 0-19)
                    if (roundX < 0 || roundX >= 20) return false;
                }
                else
                {
                    // Bottom section: offset narrow (x: 11-19)
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

            // Check bottom boundary based on level
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
            
            // Check if block below exists
            if (roundX >= 0 && roundX < width && gridYBelow >= 0 && gridYBelow < height && grid[roundX, gridYBelow] != null)
            {
                return false;
            }
        }
        return true;
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum BlockType
{
    Normal,
    Sticky,
    Pass,
    HighJump,
    Bouncy
}

[System.Serializable]
public class PieceData
{
    public int pieceIndex;
    public BlockType blockType;
    
    public PieceData(int index, BlockType type)
    {
        pieceIndex = index;
        blockType = type;
    }
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] public Text totalBlock;
    [SerializeField] public Button restartButton;
    [SerializeField] public Transform[] previewPositions;
    [SerializeField] public Transform holdPosition;
    [SerializeField] public BlockSpriteManager blockSpriteManager;
    [SerializeField] public Sprite protectionBlockSprite;

    public float blocks = 0;
    public GameObject[] Tetrominoes;
    public GameObject player;
    public GameObject timer;

    private Queue<PieceData> nextPiecesQueue = new Queue<PieceData>();
    private List<GameObject> previewObjects = new List<GameObject>();
    private GameObject holdPreviewObject;
    private PieceData heldPiece = null;
    private GameObject currentTetromino = null;
    private bool canHold = true;
    
    // NEW SYSTEM: Count spawned tetrominos instead of checkpoint
    public static int spawnedTetrominoCount = 0;
    private const int TETROMINOS_PER_ROUND = 8; // Switch to platformer after 8 tetrominos
    
    // HEIGHT LIMIT SYSTEM
    public static bool gameOver = false; // If true, no more spawning or switching
    private static int currentLevel = 1;

    void Start()
    {
        blocks = 0;
        spawnedTetrominoCount = 0;
        gameOver = false; // Reset game over state
        
        // Determine current level from scene name
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Level1") currentLevel = 1;
        else if (sceneName == "Level2") currentLevel = 2;
        else if (sceneName == "Level3") currentLevel = 3;
        else currentLevel = 1;
        
        player = GameObject.Find("Player");
        player.SetActive(false);
        timer = GameObject.Find("TimerManager");
        timer.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            int randomIndex = Random.Range(0, Tetrominoes.Length);
            BlockType randomType = GetRandomBlockType();
            nextPiecesQueue.Enqueue(new PieceData(randomIndex, randomType));
        }

        UpdatePreview();
        Spawn();
    }

    public void RestartScene()
    {
        gameOver = false; // Reset when restarting
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    void Update()
    {
        totalBlock.text = "Total Blocks: " + blocks.ToString();

        // Only allow hold mechanic when player is not active (i.e., in Tetris mode) and game is not over
        if (player != null && !player.activeInHierarchy && !gameOver)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) && canHold && currentTetromino != null)
            {
                HoldCurrentPiece();
            }
        }
    }

    public void Spawn()
    {
        // Don't spawn if game is over
        if (gameOver)
        {
            Switch();
        }
        
        Debug.Log("SpawnManager.Spawn: called. currentTetromino=" + (currentTetromino != null));
        
        PieceData nextPiece = nextPiecesQueue.Dequeue();

        int randomIndex = Random.Range(0, Tetrominoes.Length);
        BlockType randomType = GetRandomBlockType();
        nextPiecesQueue.Enqueue(new PieceData(randomIndex, randomType));

        currentTetromino = Instantiate(Tetrominoes[nextPiece.pieceIndex], transform.position, Quaternion.identity);
        ApplyBlockType(currentTetromino, nextPiece.blockType);

        TetrominoData tetrominoData = currentTetromino.AddComponent<TetrominoData>();
        tetrominoData.pieceData = nextPiece;

        canHold = true;

        UpdatePreview();
    }
    
    public bool OnTetrominoLanded()
    {
        // Check if any block was placed at height 18 or above
        bool blockAtHeight18OrAbove = CheckForBlocksAtHeight18();
        
        if (blockAtHeight18OrAbove)
        {
            Debug.Log("Block placed at Y=18 or above! Game Over!");
            gameOver = true;
            Switch();
            
            // Disable timer if it's active
            if (timer != null && timer.activeInHierarchy)
            {
                timer.SetActive(false);
            }
            
            return true; // Don't spawn more pieces
        }
        
        // This gets called from TetrisMovement when a piece lands
        spawnedTetrominoCount++;
        Debug.Log($"Tetromino landed! Total count: {spawnedTetrominoCount}");
        
        // CHECK IF WE NEED TO SWITCH TO PLATFORMER MODE (only if not game over)
        if (spawnedTetrominoCount >= TETROMINOS_PER_ROUND && !gameOver)
        {
            Debug.Log($"Reached {TETROMINOS_PER_ROUND} tetrominos! Switching to platformer mode.");
            spawnedTetrominoCount = 0; // Reset count for next round
            Switch();
            return true; // Return true to indicate we switched modes
        }
        
        return false; // Return false to indicate normal gameplay continues
    }
    
    bool CheckForBlocksAtHeight18()
    {
        // Determine height limit based on level (world Y coordinates)
        int worldYLimit;
        if (currentLevel == 3)
        {
            worldYLimit = 18; // Level 3: check from world Y=18
        }
        else
        {
            worldYLimit = 18; // Level 1 & 2: check from world Y=18
        }
        
        // Convert world Y limit to grid index
        int gridYLimit = worldYLimit + TetrisMovement.yOffset;
        
        // Check the entire grid for blocks at the height limit or higher
        for (int x = 0; x < TetrisMovement.width; x++)
        {
            for (int gridY = gridYLimit; gridY < TetrisMovement.height; gridY++)
            {
                if (TetrisMovement.grid[x, gridY] != null)
                {
                    int worldY = gridY - TetrisMovement.yOffset;
                    Debug.Log($"Found block at world Y={worldY}, grid position [{x}, {gridY}] - Level {currentLevel} limit is world Y={worldYLimit}");
                    return true;
                }
            }
        }
        return false;
    }

    void HoldCurrentPiece()
    {
        if (currentTetromino == null || gameOver) return;

        TetrominoData tetrominoData = currentTetromino.GetComponent<TetrominoData>();
        if (tetrominoData == null) return;

        PieceData currentPieceData = tetrominoData.pieceData;

        Destroy(currentTetromino);
        currentTetromino = null;

        canHold = false;

        if (heldPiece == null)
        {
            heldPiece = currentPieceData;
            UpdateHoldPreview();
            Spawn();
        }
        else
        {
            PieceData temp = heldPiece;
            heldPiece = currentPieceData;
            UpdateHoldPreview();

            currentTetromino = Instantiate(Tetrominoes[temp.pieceIndex], transform.position, Quaternion.identity);
            ApplyBlockType(currentTetromino, temp.blockType);

            TetrominoData newTetrominoData = currentTetromino.AddComponent<TetrominoData>();
            newTetrominoData.pieceData = temp;
        }
    }

    void UpdateHoldPreview()
    {
        if (holdPreviewObject != null)
        {
            Destroy(holdPreviewObject);
            holdPreviewObject = null;
        }

        if (heldPiece != null && holdPosition != null)
        {
            holdPreviewObject = Instantiate(Tetrominoes[heldPiece.pieceIndex], holdPosition.position, Quaternion.identity);
            holdPreviewObject.transform.SetParent(holdPosition);
            holdPreviewObject.transform.localScale = Vector3.one * 0.5f;

            Rigidbody2D rb = holdPreviewObject.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            MonoBehaviour[] scripts = holdPreviewObject.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                script.enabled = false;
            }

            foreach (Transform children in holdPreviewObject.transform)
            {
                ApplyColor(children.gameObject, heldPiece.blockType);
            }
            
            if (blockSpriteManager != null)
            {
                blockSpriteManager.UpdateFallingTetromino(holdPreviewObject, heldPiece.blockType);
            }
        }
    }

    void UpdatePreview()
    {
        foreach (GameObject preview in previewObjects)
        {
            if (preview != null)
                Destroy(preview);
        }
        previewObjects.Clear();

        int index = 0;
        foreach (PieceData pieceData in nextPiecesQueue)
        {
            if (index >= 3) break;

            if (previewPositions != null && index < previewPositions.Length && previewPositions[index] != null)
            {
                GameObject preview = Instantiate(Tetrominoes[pieceData.pieceIndex], previewPositions[index].position, Quaternion.identity);
                preview.transform.SetParent(previewPositions[index]);
                preview.transform.localScale = Vector3.one * 0.5f;

                Rigidbody2D rb = preview.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;

                MonoBehaviour[] scripts = preview.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    script.enabled = false;
                }

                TetrominoData previewData = preview.AddComponent<TetrominoData>();
                previewData.pieceData = pieceData;

                foreach (Transform children in preview.transform)
                {
                    switch (pieceData.blockType)
                    {
                        case BlockType.Normal:
                            children.gameObject.layer = 7;
                            break;
                        case BlockType.Sticky:
                            children.gameObject.layer = 8;
                            break;
                        case BlockType.Pass:
                            children.gameObject.layer = 9;
                            break;
                        case BlockType.HighJump:
                            children.gameObject.layer = 10;
                            break;
                        case BlockType.Bouncy:
                            children.gameObject.layer = 11;
                            break;
                    }
                    
                    SpriteRenderer spriteRenderer = children.GetComponent<SpriteRenderer>();
                    if (spriteRenderer == null)
                    {
                        spriteRenderer = children.gameObject.AddComponent<SpriteRenderer>();
                    }
                    
                    spriteRenderer.color = Color.white;
                    
                    Renderer renderer = children.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = Color.white;
                    }
                }

                Debug.Log($"Preview piece: type={pieceData.blockType}, index={pieceData.pieceIndex}");
                
                if (blockSpriteManager != null)
                {
                    blockSpriteManager.UpdatePreviewSprites(preview, pieceData.blockType);
                }
                else
                {
                    Debug.LogError("BlockSpriteManager is null when updating preview!");
                }

                previewObjects.Add(preview);
            }

            index++;
        }
    }

    void ApplyBlockType(GameObject newBlock, BlockType blockType)
    {
        foreach (Transform children in newBlock.transform)
        {
            ApplyColor(children.gameObject, blockType);
        }

        if (blockSpriteManager != null)
        {
            blockSpriteManager.UpdateFallingTetromino(newBlock, blockType);
        }
    }

    BlockType GetRandomBlockType()
    {
        int random = Random.Range(0, 100);

        if (random < 20) return BlockType.Normal;
        else if (random < 40) return BlockType.Sticky;
        else if (random < 60) return BlockType.HighJump;
        else if (random < 80) return BlockType.Pass;
        else return BlockType.Bouncy;
    }

    public void Switch()
    {
        // Don't switch to platformer if game is over

        if (gameOver)
        {
            timer.SetActive(false);
        }        
        
        TimerManager.remainingTime = 15f;
        DisablePlayerProtectionBlocks();

        player.SetActive(true);
        timer.SetActive(true);

        CameraController camCtrlEnable = FindObjectOfType<CameraController>();
        if (camCtrlEnable != null && player != null)
        {
            camCtrlEnable.EnablePlatformerMode(player.transform);
        }
    }

    void DisablePlayerProtectionBlocks()
    {
        int disabledCount = 0;

        PlayerProtectionBlock[] comps = Resources.FindObjectsOfTypeAll<PlayerProtectionBlock>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            GameObject go = comp.gameObject;
            if (go == null) continue;

            Vector3 pos = go.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            if (x >= 0 && x < TetrisMovement.width && y >= 0 && y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[x, y] != null && TetrisMovement.grid[x, y].gameObject == go)
                {
                    TetrisMovement.grid[x, y] = null;
                }
            }

            go.SetActive(false);
            disabledCount++;
        }

        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name != "PlayerProtectionBlock") continue;
            if (go.GetComponent<PlayerProtectionBlock>() != null) continue;

            Vector3 pos = go.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            if (x >= 0 && x < TetrisMovement.width && y >= 0 && y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[x, y] != null && TetrisMovement.grid[x, y].gameObject == go)
                {
                    TetrisMovement.grid[x, y] = null;
                }
            }

            go.SetActive(false);
            disabledCount++;
        }
    }

    public void SwitchToTetris()
    {
        // Don't switch back to Tetris if game is over
        if (gameOver)
        {
            Debug.Log("Game Over! Cannot switch back to Tetris mode.");
            return;
        }
        
        player.SetActive(false);
        timer.SetActive(false);
        DestroyPlayerProtectionBlocks();

        PlaceBlocksAroundPlayer();

        Spawn();

        CameraController camCtrlDisable = FindObjectOfType<CameraController>();
        if (camCtrlDisable != null)
        {
            camCtrlDisable.DisablePlatformerMode();
        }
    }
    
    void PlaceBlocksAroundPlayer()
    {
        Vector3 playerPos = player.transform.position;
        int playerX = Mathf.RoundToInt(playerPos.x);
        int playerY = Mathf.RoundToInt(playerPos.y);
        
        Debug.Log($"Player position: ({playerX}, {playerY})");
        
        List<Vector2Int> blockPositions = new List<Vector2Int>
        {
            new Vector2Int(playerX - 1, playerY + 1),
            new Vector2Int(playerX, playerY + 1),
            new Vector2Int(playerX - 1, playerY),
            new Vector2Int(playerX, playerY)
        };
        
        int blocksPlaced = 0;
        foreach (Vector2Int pos in blockPositions)
        {
            Debug.Log($"Trying to place block at ({pos.x}, {pos.y})");
            
            if (pos.x >= 0 && pos.x < TetrisMovement.width && 
                pos.y >= 0 && pos.y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[pos.x, pos.y] == null)
                {
                    GameObject block = new GameObject("PlayerProtectionBlock");
                    block.transform.position = new Vector3(pos.x, pos.y, 0);

                    block.layer = 7;

                    SpriteRenderer spriteRenderer = block.AddComponent<SpriteRenderer>();
                    spriteRenderer.sortingOrder = 1;
                    if (protectionBlockSprite != null)
                    {
                        spriteRenderer.sprite = protectionBlockSprite;
                    }
                    else
                    {
                        spriteRenderer.color = Color.gray;
                    }
                    
                    BoxCollider2D box = block.AddComponent<BoxCollider2D>();
                    if (box != null)
                    {
                        box.isTrigger = false;
                    }
                    
                    block.AddComponent<PlayerProtectionBlock>();

                    TetrisMovement.grid[pos.x, pos.y] = block.transform;
                    blocksPlaced++;
                    
                    Debug.Log($"Block placed at ({pos.x}, {pos.y})");
                    
                    if (blockSpriteManager != null)
                    {
                        blockSpriteManager.UpdateBlockSprite(pos.x, pos.y, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                    }
                }
                else
                {
                    Debug.Log($"Position ({pos.x}, {pos.y}) already occupied");
                }
            }
            else
            {
                Debug.Log($"Position ({pos.x}, {pos.y}) out of bounds");
            }
        }
        
        Debug.Log($"Total blocks placed: {blocksPlaced}");
        
        if (blockSpriteManager != null)
        {
            foreach (Vector2Int pos in blockPositions)
            {
                if (pos.x >= 0 && pos.x < TetrisMovement.width && pos.y >= 0 && pos.y < TetrisMovement.height)
                {
                    if (pos.y + 1 < TetrisMovement.height) 
                        blockSpriteManager.UpdateBlockSprite(pos.x, pos.y + 1, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                    if (pos.y - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(pos.x, pos.y - 1, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                    if (pos.x - 1 >= 0) 
                        blockSpriteManager.UpdateBlockSprite(pos.x - 1, pos.y, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                    if (pos.x + 1 < TetrisMovement.width) 
                        blockSpriteManager.UpdateBlockSprite(pos.x + 1, pos.y, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                }
            }
        }
    }

    void DestroyPlayerProtectionBlocks()
    {
        int removedCount = 0;

        PlayerProtectionBlock[] protectionComponents = Resources.FindObjectsOfTypeAll<PlayerProtectionBlock>();
        Debug.Log($"DestroyPlayerProtectionBlocks: found {protectionComponents.Length} PlayerProtectionBlock components");

        foreach (PlayerProtectionBlock comp in protectionComponents)
        {
            if (comp == null) continue;
            GameObject block = comp.gameObject;
            if (block == null) continue;

            Debug.Log($"DestroyPlayerProtectionBlocks: removing marked block '{block.name}' active={block.activeInHierarchy} at {block.transform.position}");

            Vector3 pos = block.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            if (x >= 0 && x < TetrisMovement.width && y >= 0 && y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[x, y] != null && TetrisMovement.grid[x, y].gameObject == block)
                {
                    TetrisMovement.grid[x, y] = null;
                    Debug.Log($"DestroyPlayerProtectionBlocks: cleared grid[{x},{y}]");
                }
            }

            DestroyImmediate(block);
            removedCount++;
        }

        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int fallbackRemoved = 0;
        foreach (GameObject go in allGameObjects)
        {
            if (go == null) continue;
            if (go.name != "PlayerProtectionBlock") continue;

            if (go.GetComponent<PlayerProtectionBlock>() != null) continue;

            Vector3 pos = go.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            if (x >= 0 && x < TetrisMovement.width && y >= 0 && y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[x, y] != null && TetrisMovement.grid[x, y].gameObject == go)
                {
                    TetrisMovement.grid[x, y] = null;
                }
            }

            DestroyImmediate(go);
            fallbackRemoved++;
        }

        if (blockSpriteManager != null && player != null)
        {
            Vector3 playerPos = player.transform.position;
            int px = Mathf.RoundToInt(playerPos.x);
            int py = Mathf.RoundToInt(playerPos.y);

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int nx = px + dx;
                    int ny = py + dy;
                    if (nx >= 0 && nx < TetrisMovement.width && ny >= 0 && ny < TetrisMovement.height)
                    {
                        blockSpriteManager.UpdateBlockSprite(nx, ny, TetrisMovement.grid, TetrisMovement.width, TetrisMovement.height);
                    }
                }
            }
        }
    }

    void ApplyColor(GameObject block, BlockType blockType)
    {
        Renderer renderer = block.GetComponent<Renderer>();
        switch (blockType)
        {
            case BlockType.Normal:
                renderer.material.color = Color.gray;
                block.layer = 7;
                break;
            case BlockType.Sticky:
                renderer.material.color = Color.red;
                block.layer = 8;
                break;
            case BlockType.Pass:
                Material transparentMat = new Material(renderer.material);

                transparentMat.color = new Color(0f, 0f, 1f, 0.5f);

                transparentMat.SetFloat("_Mode", 3);
                transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMat.SetInt("_ZWrite", 0);
                transparentMat.DisableKeyword("_ALPHATEST_ON");
                transparentMat.EnableKeyword("_ALPHABLEND_ON");
                transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMat.renderQueue = 3000;
                renderer.material = transparentMat;

                Collider2D collider = block.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }

                block.layer = 9;
                break;
            case BlockType.HighJump:
                renderer.material.color = new Color(0.5f, 0f, 1f);
                block.layer = 10;
                break;
            case BlockType.Bouncy:
                renderer.material.color = Color.green;
                PhysicsMaterial2D bouncyMaterial = new PhysicsMaterial2D("BouncyMaterial");
                bouncyMaterial.bounciness = 0.7f;
                bouncyMaterial.friction = 0.1f;

                Collider2D collider2 = block.GetComponent<Collider2D>();
                if (collider2 != null)
                {
                    collider2.sharedMaterial = bouncyMaterial;
                }
                block.layer = 11;
                break;
        }
    }
}

public class TetrominoData : MonoBehaviour
{
    public PieceData pieceData;
}
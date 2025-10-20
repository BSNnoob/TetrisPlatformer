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
    public static int checkPoint = 6;

    void Start()
    {
        blocks = 0;
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
        SceneManager.LoadScene("Level1");
    }

    void Update()
    {
        totalBlock.text = "Total Blocks: " + blocks.ToString();

        // Only allow hold mechanic when player is not active (i.e., in Tetris mode)
        if (player != null && !player.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) && canHold && currentTetromino != null)
            {
                HoldCurrentPiece();
            }
        }
    }

    public void Spawn()
    {
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

    Debug.Log($"SpawnManager.Spawn: spawned pieceIndex={nextPiece.pieceIndex} blockType={nextPiece.blockType}");

        UpdatePreview();
    }

    void HoldCurrentPiece()
    {
        if (currentTetromino == null) return;

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

                foreach (Transform children in preview.transform)
                {
                    ApplyColor(children.gameObject, pieceData.blockType);
                }
                
                if (blockSpriteManager != null)
                {
                    blockSpriteManager.UpdateFallingTetromino(preview, pieceData.blockType);
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
        if (checkPoint == 6) checkPoint = 12;
        else if (checkPoint == 12) checkPoint = 18;
        else if (checkPoint == 18) checkPoint = 24;

        TimerManager.remainingTime = 15f;
        DisablePlayerProtectionBlocks();

        player.SetActive(true);
        timer.SetActive(true);

        // Enable camera follow/zoom for platformer mode if a CameraController exists
        CameraController camCtrlEnable = FindObjectOfType<CameraController>();
        if (camCtrlEnable != null && player != null)
        {
            camCtrlEnable.EnablePlatformerMode(player.transform);
        }
    }

    void DisablePlayerProtectionBlocks()
    {
        int disabledCount = 0;

        // Find all marker components (includes inactive objects)
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

        // Fallback: disable any GameObjects named "PlayerProtectionBlock"
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name != "PlayerProtectionBlock") continue;
            if (go.GetComponent<PlayerProtectionBlock>() != null) continue; // already handled

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

        Debug.Log($"DisablePlayerProtectionBlocks: disabled {disabledCount} objects");
    }

    public void SwitchToTetris()
    {
        player.SetActive(false);
        timer.SetActive(false);
        DestroyPlayerProtectionBlocks();

        PlaceBlocksAroundPlayer();

        Spawn();

        // Disable camera follow/zoom and return to Tetris view size
        CameraController camCtrlDisable = FindObjectOfType<CameraController>();
        if (camCtrlDisable != null)
        {
            camCtrlDisable.DisablePlatformerMode();
        }
    }
    
    void PlaceBlocksAroundPlayer()
    {
        // Get player position
        Vector3 playerPos = player.transform.position;
        int playerX = Mathf.RoundToInt(playerPos.x);
        int playerY = Mathf.RoundToInt(playerPos.y);
        
        Debug.Log($"Player position: ({playerX}, {playerY})");
        
        // Place 2x2 blocks around the player (forming a protective border)
        // Top-left, top-right, bottom-left, bottom-right
        List<Vector2Int> blockPositions = new List<Vector2Int>
        {
            new Vector2Int(playerX - 1, playerY + 1),  // Top-left
            new Vector2Int(playerX, playerY + 1),      // Top-right
            new Vector2Int(playerX - 1, playerY),      // Bottom-left
            new Vector2Int(playerX, playerY)           // Bottom-right
        };
        
        int blocksPlaced = 0;
        foreach (Vector2Int pos in blockPositions)
        {
            Debug.Log($"Trying to place block at ({pos.x}, {pos.y})");
            
            // Check if position is valid and empty
            if (pos.x >= 0 && pos.x < TetrisMovement.width && 
                pos.y >= 0 && pos.y < TetrisMovement.height)
            {
                if (TetrisMovement.grid[pos.x, pos.y] == null)
                {
                    // Create a block at this position as a simple GameObject with SpriteRenderer
                    GameObject block = new GameObject("PlayerProtectionBlock");
                    block.transform.position = new Vector3(pos.x, pos.y, 0);

                    // Apply normal block properties
                    block.layer = 7; // Normal layer

                    // Use SpriteRenderer directly (BlockSpriteManager will assign the correct sprite)
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
                    
                    // Add a 2D BoxCollider so physics/overlap checks behave like other blocks (optional)
                    BoxCollider2D box = block.AddComponent<BoxCollider2D>();
                    if (box != null)
                    {
                        box.isTrigger = false;
                    }
                    
                    // Add to grid
                    // Add marker component so we can find and remove these blocks reliably
                    block.AddComponent<PlayerProtectionBlock>();

                    // Add to grid
                    TetrisMovement.grid[pos.x, pos.y] = block.transform;
                    blocksPlaced++;
                    
                    Debug.Log($"Block placed at ({pos.x}, {pos.y})");
                    
                    // Update sprite based on neighbors
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpriteManager : MonoBehaviour
{
    [System.Serializable]
    public class BlockSprites
    {
        public Sprite centerSprite;      // No edges exposed
        public Sprite topSprite;         // Top edge exposed
        public Sprite bottomSprite;      // Bottom edge exposed
        public Sprite leftSprite;        // Left edge exposed
        public Sprite rightSprite;       // Right edge exposed
        public Sprite topLeftSprite;     // Top and left edges exposed
        public Sprite topRightSprite;    // Top and right edges exposed
        public Sprite bottomLeftSprite;  // Bottom and left edges exposed
        public Sprite bottomRightSprite; // Bottom and right edges exposed
        public Sprite topBottomSprite;   // Top and bottom edges exposed
        public Sprite leftRightSprite;   // Left and right edges exposed
        public Sprite topLeftRightSprite;    // Top, left, right exposed
        public Sprite bottomLeftRightSprite; // Bottom, left, right exposed
        public Sprite topBottomLeftSprite;   // Top, bottom, left exposed
        public Sprite topBottomRightSprite;  // Top, bottom, right exposed
        public Sprite singleBlockSprite;     // All edges exposed (single block)
    }
    
    [Header("Normal Block Sprites (Gray)")]
    [SerializeField] public BlockSprites normalSprites;
    
    [Header("Sticky Block Sprites (Red)")]
    [SerializeField] public BlockSprites stickySprites;
    
    [Header("Pass Block Sprites (Blue/Transparent)")]
    [SerializeField] public BlockSprites passSprites;
    
    [Header("HighJump Block Sprites (Purple)")]
    [SerializeField] public BlockSprites highJumpSprites;
    
    [Header("Bouncy Block Sprites (Green)")]
    [SerializeField] public BlockSprites bouncySprites;
    
    // Update a single block's sprite by checking the grid around it
    public void UpdateBlockSprite(int x, int y, Transform[,] grid, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (grid[x, y] == null) return;
        
        Transform block = grid[x, y];
        if (block == null || block.gameObject == null) return;

        // If this is a player protection block, don't override its sprite
        if (block.gameObject.GetComponent<PlayerProtectionBlock>() != null)
        {
            return;
        }
        
        // Get the block type from the layer
        BlockType blockType = GetBlockTypeFromLayer(block.gameObject.layer);
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        
        if (spriteSet == null) 
        {
            Debug.LogWarning($"No sprite set found for block type {blockType}");
            return;
        }
        
        // Check neighbors in the grid - simple array checks
        bool hasTop = (y + 1 < height) && grid[x, y + 1] != null;
        bool hasBottom = (y - 1 >= 0) && grid[x, y - 1] != null;
        bool hasLeft = (x - 1 >= 0) && grid[x - 1, y] != null;
        bool hasRight = (x + 1 < width) && grid[x + 1, y] != null;
        
        // Get or add sprite renderer
        SpriteRenderer spriteRenderer = block.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            try
            {
                spriteRenderer = block.gameObject.AddComponent<SpriteRenderer>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to add SpriteRenderer to block at ({x},{y}): {e.Message}");
                return;
            }
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer is still null after AddComponent at ({x},{y})");
            return;
        }
        
        // Assign sprite
        Sprite selectedSprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
        if (selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
            spriteRenderer.sortingOrder = 1;
        }
        else
        {
            Debug.LogWarning($"GetSpriteForEdges returned null for block at ({x},{y})");
        }
    }
    
    // Update sprites for a falling tetromino (before it's in grid)
    public void UpdateFallingTetromino(GameObject tetromino, BlockType blockType)
    {
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        Transform[] allChildren = tetromino.GetComponentsInChildren<Transform>();
        
        // Build dictionary of LOCAL positions relative to each other
        Dictionary<Vector2Int, Transform> positionMap = new Dictionary<Vector2Int, Transform>();
        
        // First pass: collect all block positions
        List<Transform> blockList = new List<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child != tetromino.transform)
            {
                blockList.Add(child);
            }
        }
        
        // Second pass: calculate relative positions
        foreach (Transform child in blockList)
        {
            // Use localPosition so preview/hold pieces (which may be parented/scaled) map correctly
            Vector3 localPos = child.localPosition;
            int x = Mathf.RoundToInt(localPos.x);
            int y = Mathf.RoundToInt(localPos.y);
            Vector2Int pos = new Vector2Int(x, y);
            
            if (!positionMap.ContainsKey(pos))
            {
                positionMap.Add(pos, child);
            }
        }
        
        // Update each block based on neighbors within the tetromino
        foreach (var kvp in positionMap)
        {
            Vector2Int pos = kvp.Key;
            Transform child = kvp.Value;
            
            // Check if neighbors exist within THIS tetromino piece
            bool hasTop = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y + 1));
            bool hasBottom = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y - 1));
            bool hasLeft = positionMap.ContainsKey(new Vector2Int(pos.x - 1, pos.y));
            bool hasRight = positionMap.ContainsKey(new Vector2Int(pos.x + 1, pos.y));
            
            // Get or add sprite renderer
            SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            
            spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
            spriteRenderer.sortingOrder = 1;
        }
    }
    
    // Update sprites for preview pieces (all edges exposed)
    public void UpdatePreviewSprites(GameObject tetromino, BlockType blockType)
    {
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        if (spriteSet == null) return;
        
        Transform[] allChildren = tetromino.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in allChildren)
        {
            if (child != tetromino.transform)
            {
                // Get or add sprite renderer
                SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
                }
                
                // All edges exposed for preview - use single block sprite or fallback
                Sprite previewSprite = spriteSet.singleBlockSprite != null ? 
                                      spriteSet.singleBlockSprite : 
                                      spriteSet.centerSprite;
                
                spriteRenderer.sprite = previewSprite;
                spriteRenderer.sortingOrder = 1;
            }
        }
    }
    
    BlockSprites GetSpriteSetForBlockType(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Normal:
                return normalSprites;
            case BlockType.Sticky:
                return stickySprites;
            case BlockType.Pass:
                return passSprites;
            case BlockType.HighJump:
                return highJumpSprites;
            case BlockType.Bouncy:
                return bouncySprites;
            default:
                return normalSprites;
        }
    }
    
    BlockType GetBlockTypeFromLayer(int layer)
    {
        switch (layer)
        {
            case 7: return BlockType.Normal;
            case 8: return BlockType.Sticky;
            case 9: return BlockType.Pass;
            case 10: return BlockType.HighJump;
            case 11: return BlockType.Bouncy;
            default: return BlockType.Normal;
        }
    }
    
    Sprite GetSpriteForEdges(BlockSprites spriteSet, bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        // Count exposed edges (edges without neighbors)
        bool topExposed = !hasTop;
        bool bottomExposed = !hasBottom;
        bool leftExposed = !hasLeft;
        bool rightExposed = !hasRight;
        
        int exposedCount = (topExposed ? 1 : 0) + (bottomExposed ? 1 : 0) + 
                          (leftExposed ? 1 : 0) + (rightExposed ? 1 : 0);
        
        // All edges exposed (single block) - fallback to center if missing
        if (exposedCount == 4)
            return spriteSet.singleBlockSprite != null ? spriteSet.singleBlockSprite : spriteSet.centerSprite;
        
        // Three edges exposed
        if (exposedCount == 3)
        {
            if (!topExposed && spriteSet.bottomLeftRightSprite != null) return spriteSet.bottomLeftRightSprite;
            if (!bottomExposed && spriteSet.topLeftRightSprite != null) return spriteSet.topLeftRightSprite;
            if (!leftExposed && spriteSet.topBottomRightSprite != null) return spriteSet.topBottomRightSprite;
            if (!rightExposed && spriteSet.topBottomLeftSprite != null) return spriteSet.topBottomLeftSprite;
            return spriteSet.centerSprite;
        }
        
        // Two edges exposed
        if (exposedCount == 2)
        {
            // Opposite edges
            if (topExposed && bottomExposed && spriteSet.topBottomSprite != null) return spriteSet.topBottomSprite;
            if (leftExposed && rightExposed && spriteSet.leftRightSprite != null) return spriteSet.leftRightSprite;
            if (leftExposed && rightExposed) return spriteSet.centerSprite;
            
            // Adjacent edges (corners)
            if (topExposed && leftExposed && spriteSet.topLeftSprite != null) return spriteSet.topLeftSprite;
            if (topExposed && rightExposed && spriteSet.topRightSprite != null) return spriteSet.topRightSprite;
            if (bottomExposed && leftExposed && spriteSet.bottomLeftSprite != null) return spriteSet.bottomLeftSprite;
            if (bottomExposed && rightExposed && spriteSet.bottomRightSprite != null) return spriteSet.bottomRightSprite;
        }
        
        // One edge exposed
        if (exposedCount == 1)
        {
            if (topExposed && spriteSet.topSprite != null) return spriteSet.topSprite;
            if (bottomExposed && spriteSet.bottomSprite != null) return spriteSet.bottomSprite;
            if (leftExposed && spriteSet.leftSprite != null) return spriteSet.leftSprite;
            if (rightExposed && spriteSet.rightSprite != null) return spriteSet.rightSprite;
        }
        
        // No edges exposed (center block)
        return spriteSet.centerSprite;
    }
}
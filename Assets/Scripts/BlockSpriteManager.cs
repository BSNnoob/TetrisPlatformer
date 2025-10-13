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
    
    public void UpdateBlockSprite(int x, int y, Transform[,] grid, int width, int height)
    {
        if (grid[x, y] == null) return;
        
        Transform block = grid[x, y];
        
        BlockType blockType = GetBlockTypeFromLayer(block.gameObject.layer);
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        
        bool hasTop = (y + 1 < height) && grid[x, y + 1] != null;
        bool hasBottom = (y - 1 >= 0) && grid[x, y - 1] != null;
        bool hasLeft = (x - 1 >= 0) && grid[x - 1, y] != null;
        bool hasRight = (x + 1 < width) && grid[x + 1, y] != null;
        
        Debug.Log($"Block at ({x},{y}): Top={hasTop}, Bottom={hasBottom}, Left={hasLeft}, Right={hasRight}");
        
        SpriteRenderer spriteRenderer = block.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = block.gameObject.AddComponent<SpriteRenderer>();
        }
        
        spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
        spriteRenderer.sortingOrder = 1;
    }
    
    public void UpdateFallingTetromino(GameObject tetromino, BlockType blockType)
    {
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        Transform[] allChildren = tetromino.GetComponentsInChildren<Transform>();
        
        Debug.Log($"=== Updating Tetromino with {allChildren.Length - 1} blocks ===");
        
        Dictionary<Vector2Int, Transform> positionMap = new Dictionary<Vector2Int, Transform>();
        foreach (Transform child in allChildren)
        {
            if (child != tetromino.transform)
            {
                int x = Mathf.RoundToInt(child.position.x);
                int y = Mathf.RoundToInt(child.position.y);
                Vector2Int pos = new Vector2Int(x, y);
                
                Debug.Log($"Child '{child.name}' at actual pos ({child.position.x}, {child.position.y}) rounded to ({x}, {y})");
                
                if (positionMap.ContainsKey(pos))
                {
                    Debug.LogError($"DUPLICATE POSITION! {pos} already has block {positionMap[pos].name}, trying to add {child.name}");
                }
                else
                {
                    positionMap.Add(pos, child);
                }
            }
        }
        
        Debug.Log($"Position map has {positionMap.Count} unique positions");
        
        foreach (var kvp in positionMap)
        {
            Vector2Int pos = kvp.Key;
            Transform child = kvp.Value;
            
            bool hasTop = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y + 1));
            bool hasBottom = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y - 1));
            bool hasLeft = positionMap.ContainsKey(new Vector2Int(pos.x - 1, pos.y));
            bool hasRight = positionMap.ContainsKey(new Vector2Int(pos.x + 1, pos.y));
            
            Debug.Log($"Block '{child.name}' at ({pos.x},{pos.y}): Neighbors - Top={hasTop}, Bottom={hasBottom}, Left={hasLeft}, Right={hasRight}");
            
            SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            
            spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
            spriteRenderer.sortingOrder = 1;
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
        bool topExposed = !hasTop;
        bool bottomExposed = !hasBottom;
        bool leftExposed = !hasLeft;
        bool rightExposed = !hasRight;
        
        int exposedCount = (topExposed ? 1 : 0) + (bottomExposed ? 1 : 0) + 
                          (leftExposed ? 1 : 0) + (rightExposed ? 1 : 0);
        
        if (exposedCount == 4)
            return spriteSet.singleBlockSprite != null ? spriteSet.singleBlockSprite : spriteSet.centerSprite;
        
        if (exposedCount == 3)
        {
            if (!topExposed && spriteSet.bottomLeftRightSprite != null) return spriteSet.bottomLeftRightSprite;
            if (!bottomExposed && spriteSet.topLeftRightSprite != null) return spriteSet.topLeftRightSprite;
            if (!leftExposed && spriteSet.topBottomRightSprite != null) return spriteSet.topBottomRightSprite;
            if (!rightExposed && spriteSet.topBottomLeftSprite != null) return spriteSet.topBottomLeftSprite;
            return spriteSet.centerSprite;
        }
        
        if (exposedCount == 2)
        {
            if (topExposed && bottomExposed && spriteSet.topBottomSprite != null) return spriteSet.topBottomSprite;
            if (leftExposed && rightExposed && spriteSet.leftRightSprite != null) return spriteSet.leftRightSprite;
            if (leftExposed && rightExposed) return spriteSet.centerSprite;
            
            if (topExposed && leftExposed && spriteSet.topLeftSprite != null) return spriteSet.topLeftSprite;
            if (topExposed && rightExposed && spriteSet.topRightSprite != null) return spriteSet.topRightSprite;
            if (bottomExposed && leftExposed && spriteSet.bottomLeftSprite != null) return spriteSet.bottomLeftSprite;
            if (bottomExposed && rightExposed && spriteSet.bottomRightSprite != null) return spriteSet.bottomRightSprite;
        }
        
        if (exposedCount == 1)
        {
            if (topExposed && spriteSet.topSprite != null) return spriteSet.topSprite;
            if (bottomExposed && spriteSet.bottomSprite != null) return spriteSet.bottomSprite;
            if (leftExposed && spriteSet.leftSprite != null) return spriteSet.leftSprite;
            if (rightExposed && spriteSet.rightSprite != null) return spriteSet.rightSprite;
        }
        
        return spriteSet.centerSprite;
    }
}
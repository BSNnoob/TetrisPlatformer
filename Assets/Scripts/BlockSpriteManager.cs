using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpriteManager : MonoBehaviour
{
    [System.Serializable]
    public class BlockSprites
    {
        public Sprite centerSprite;      
        public Sprite topSprite;         
        public Sprite bottomSprite;      
        public Sprite leftSprite;        
        public Sprite rightSprite;       
        public Sprite topLeftSprite;
        public Sprite topRightSprite;
        public Sprite bottomLeftSprite;
        public Sprite bottomRightSprite;
        public Sprite topBottomSprite;
        public Sprite leftRightSprite;
        public Sprite topLeftRightSprite;
        public Sprite bottomLeftRightSprite;
        public Sprite topBottomLeftSprite;
        public Sprite topBottomRightSprite;
        public Sprite singleBlockSprite;
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

    private BlockSprites GetSpriteSetForBlockType(BlockType blockType)
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

    private BlockType GetBlockTypeFromLayer(int layer)
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

    private Sprite GetSpriteForEdges(BlockSprites spriteSet, bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        bool topExposed = !hasTop;
        bool bottomExposed = !hasBottom;
        bool leftExposed = !hasLeft;
        bool rightExposed = !hasRight;
        
        int exposedCount = (topExposed ? 1 : 0) + (bottomExposed ? 1 : 0) + 
                          (leftExposed ? 1 : 0) + (rightExposed ? 1 : 0);
        
        // All edges exposed
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
        
        return spriteSet.centerSprite;
    }
    
    public void UpdateBlockSprite(int x, int y, Transform[,] grid, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (grid[x, y] == null) return;
        
        Transform block = grid[x, y];
        if (block == null || block.gameObject == null) return;
        
        BlockType blockType = GetBlockTypeFromLayer(block.gameObject.layer);
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        
        if (spriteSet == null) 
        {
            return;
        }
        
        bool hasTop = (y + 1 < height) && grid[x, y + 1] != null;
        bool hasBottom = (y - 1 >= 0) && grid[x, y - 1] != null;
        bool hasLeft = (x - 1 >= 0) && grid[x - 1, y] != null;
        bool hasRight = (x + 1 < width) && grid[x + 1, y] != null;
        
        SpriteRenderer spriteRenderer = block.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            try
            {
                spriteRenderer = block.gameObject.AddComponent<SpriteRenderer>();
            }
            catch (System.Exception e)
            {
                return;
            }
        }
        
        if (spriteRenderer == null)
        {
            return;
        }
        
        Sprite selectedSprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
        if (selectedSprite != null)
        {
            spriteRenderer.sprite = selectedSprite;
            spriteRenderer.sortingOrder = 1;
        }
        else
        {
        }
    }
    
    public void UpdateFallingTetromino(GameObject tetromino, BlockType blockType)
    {
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        Transform[] allChildren = tetromino.GetComponentsInChildren<Transform>();
        
        Dictionary<Vector2Int, Transform> positionMap = new Dictionary<Vector2Int, Transform>();
        
        List<Transform> blockList = new List<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child != tetromino.transform)
            {
                blockList.Add(child);
            }
        }
        
        foreach (Transform child in blockList)
        {
            int x = Mathf.RoundToInt(child.position.x);
            int y = Mathf.RoundToInt(child.position.y);
            Vector2Int pos = new Vector2Int(x, y);
            
            if (!positionMap.ContainsKey(pos))
            {
                positionMap.Add(pos, child);
            }
        }
        
        foreach (var kvp in positionMap)
        {
            Vector2Int pos = kvp.Key;
            Transform child = kvp.Value;
            
            bool hasTop = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y + 1));
            bool hasBottom = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y - 1));
            bool hasLeft = positionMap.ContainsKey(new Vector2Int(pos.x - 1, pos.y));
            bool hasRight = positionMap.ContainsKey(new Vector2Int(pos.x + 1, pos.y));
            
            SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            }
            
            spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
            spriteRenderer.sortingOrder = 1;
        }
    }

    public void UpdatePreviewSprites(GameObject tetromino, BlockType blockType)
    {
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        if (spriteSet == null)
        {
            return;
        }

        Dictionary<Vector2Int, Transform> positionMap = new Dictionary<Vector2Int, Transform>();
        List<Transform> blocks = new List<Transform>();

        foreach (Transform child in tetromino.transform)
        {
            if (child != tetromino.transform)
            {
                blocks.Add(child);
                int x = Mathf.RoundToInt(child.localPosition.x);
                int y = Mathf.RoundToInt(child.localPosition.y);
                Vector2Int pos = new Vector2Int(x, y);

                if (!positionMap.ContainsKey(pos))
                {
                    positionMap.Add(pos, child);
                }
            }
        }

        foreach (var kvp in positionMap)
        {
            Vector2Int pos = kvp.Key;
            Transform block = kvp.Value;

            bool hasTop = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y + 1));
            bool hasBottom = positionMap.ContainsKey(new Vector2Int(pos.x, pos.y - 1));
            bool hasLeft = positionMap.ContainsKey(new Vector2Int(pos.x - 1, pos.y));
            bool hasRight = positionMap.ContainsKey(new Vector2Int(pos.x + 1, pos.y));

            SpriteRenderer spriteRenderer = block.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = block.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
            spriteRenderer.sortingOrder = 1;
        }
    }
}
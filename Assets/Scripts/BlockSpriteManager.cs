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
    
    public void UpdateBlockSprites(GameObject tetromino, BlockType blockType)
    {
        // Get all blocks in the tetromino
        Transform[] blocks = tetromino.GetComponentsInChildren<Transform>();
        List<Vector3> blockPositions = new List<Vector3>();
        List<Transform> blockTransforms = new List<Transform>();
        
        foreach (Transform block in blocks)
        {
            if (block != tetromino.transform) // Skip parent
            {
                blockPositions.Add(block.position);
                blockTransforms.Add(block);
            }
        }
        
        // Get the appropriate sprite set for this block type
        BlockSprites spriteSet = GetSpriteSetForBlockType(blockType);
        
        // Update sprite for each block based on neighbors
        for (int i = 0; i < blockTransforms.Count; i++)
        {
            Vector3 pos = blockPositions[i];
            
            // Check for neighbors in 4 directions
            bool hasTop = HasBlockAt(blockPositions, pos + Vector3.up);
            bool hasBottom = HasBlockAt(blockPositions, pos + Vector3.down);
            bool hasLeft = HasBlockAt(blockPositions, pos + Vector3.left);
            bool hasRight = HasBlockAt(blockPositions, pos + Vector3.right);
            
            // Get the sprite renderer
            SpriteRenderer spriteRenderer = blockTransforms[i].GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = blockTransforms[i].gameObject.AddComponent<SpriteRenderer>();
            }
            
            // Assign appropriate sprite based on exposed edges
            spriteRenderer.sprite = GetSpriteForEdges(spriteSet, hasTop, hasBottom, hasLeft, hasRight);
            spriteRenderer.sortingOrder = 1; // Make sure sprites render properly
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
    
    bool HasBlockAt(List<Vector3> positions, Vector3 targetPos)
    {
        foreach (Vector3 pos in positions)
        {
            if (Vector3.Distance(pos, targetPos) < 0.1f) // Small threshold for floating point comparison
            {
                return true;
            }
        }
        return false;
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
        
        // All edges exposed (single block)
        if (exposedCount == 4)
            return spriteSet.singleBlockSprite;
        
        // Three edges exposed
        if (exposedCount == 3)
        {
            if (!topExposed) return spriteSet.bottomLeftRightSprite;
            if (!bottomExposed) return spriteSet.topLeftRightSprite;
            if (!leftExposed) return spriteSet.topBottomRightSprite;
            if (!rightExposed) return spriteSet.topBottomLeftSprite;
        }
        
        // Two edges exposed
        if (exposedCount == 2)
        {
            // Opposite edges
            if (topExposed && bottomExposed) return spriteSet.topBottomSprite;
            if (leftExposed && rightExposed) return spriteSet.leftRightSprite;
            
            // Adjacent edges (corners)
            if (topExposed && leftExposed) return spriteSet.topLeftSprite;
            if (topExposed && rightExposed) return spriteSet.topRightSprite;
            if (bottomExposed && leftExposed) return spriteSet.bottomLeftSprite;
            if (bottomExposed && rightExposed) return spriteSet.bottomRightSprite;
        }
        
        // One edge exposed
        if (exposedCount == 1)
        {
            if (topExposed) return spriteSet.topSprite;
            if (bottomExposed) return spriteSet.bottomSprite;
            if (leftExposed) return spriteSet.leftSprite;
            if (rightExposed) return spriteSet.rightSprite;
        }
        
        // No edges exposed (center block)
        return spriteSet.centerSprite;
    }
}
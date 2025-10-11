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

// Store both piece index and block type together
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
    
    // Preview system
    [SerializeField] public Transform[] previewPositions; // 3 positions for preview
    
    public float blocks = 0;
    public GameObject[] Tetrominoes;
    public GameObject player;
    
    private Queue<PieceData> nextPiecesQueue = new Queue<PieceData>();
    private List<GameObject> previewObjects = new List<GameObject>();
    
    void Start()
    {
        blocks = 0;
        player = GameObject.Find("Player");
        player.SetActive(false);
        
        // Initialize the queue with 4 pieces (3 for preview + 1 to spawn)
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
    }

    public void Spawn()
    {
        // Get the next piece from queue
        PieceData nextPiece = nextPiecesQueue.Dequeue();
        
        // Add a new piece to the end of the queue
        int randomIndex = Random.Range(0, Tetrominoes.Length);
        BlockType randomType = GetRandomBlockType();
        nextPiecesQueue.Enqueue(new PieceData(randomIndex, randomType));
        
        // Spawn the piece with its predetermined block type
        GameObject newBlock = Instantiate(Tetrominoes[nextPiece.pieceIndex], transform.position, Quaternion.identity);
        ApplyBlockType(newBlock, nextPiece.blockType);
        
        // Update the preview
        UpdatePreview();
    }
    
    void UpdatePreview()
    {
        // Clear existing preview objects
        foreach (GameObject preview in previewObjects)
        {
            if (preview != null)
                Destroy(preview);
        }
        previewObjects.Clear();
        
        // Create new preview objects
        int index = 0;
        foreach (PieceData pieceData in nextPiecesQueue)
        {
            if (index >= 3) break; // Only show 3 pieces
            
            if (previewPositions != null && index < previewPositions.Length && previewPositions[index] != null)
            {
                GameObject preview = Instantiate(Tetrominoes[pieceData.pieceIndex], previewPositions[index].position, Quaternion.identity);
                preview.transform.SetParent(previewPositions[index]);
                preview.transform.localScale = Vector3.one * 0.5f; // Scale down for preview
                
                // Disable physics and scripts on preview
                Rigidbody2D rb = preview.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;
                
                MonoBehaviour[] scripts = preview.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    script.enabled = false;
                }
                
                // Apply the SAME block type that will be used when spawned
                ApplyBlockType(preview, pieceData.blockType);
                
                previewObjects.Add(preview);
            }
            
            index++;
        }
    }

    // Apply a specific block type to all children of a tetromino
    void ApplyBlockType(GameObject newBlock, BlockType blockType)
    {
        foreach (Transform children in newBlock.transform)
        {
            ApplyColor(children.gameObject, blockType);
        }
    }

    BlockType GetRandomBlockType()
    {
        int random = Random.Range(0,100);

        if (random < 20) return BlockType.Normal;
        else if (random < 40) return BlockType.Sticky;
        else if (random < 60) return BlockType.HighJump;
        else if (random < 80) return BlockType.Pass;
        else return BlockType.Bouncy;
    }

    public void Switch()
    {
        player.SetActive(true);
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
                Debug.Log("Assigned Sticky layer: " + block.layer + ", Name: " + LayerMask.LayerToName(block.layer));
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
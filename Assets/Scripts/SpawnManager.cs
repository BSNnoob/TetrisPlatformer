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

        if (Input.GetKeyDown(KeyCode.DownArrow) && canHold && currentTetromino != null)
        {
            HoldCurrentPiece();
        }
    }

    public void Spawn()
    {
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

            ApplyBlockType(holdPreviewObject, heldPiece.blockType);
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

        TimerManager.remainingTime = 5f;
        player.SetActive(true);
        timer.SetActive(true);
    }

    public void SwitchToTetris()
    {
        player.SetActive(false);
        timer.SetActive(false);
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
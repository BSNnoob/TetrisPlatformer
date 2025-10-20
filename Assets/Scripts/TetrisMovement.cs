using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class TetrisMovement : MonoBehaviour
{
    public Vector3 rotationPoint;
    public float fallTime = 1f;
    public float fallTimer;
    public float changeTimer;
    public static int height = 20;
    public static int width = 10;
    public static Transform[,] grid = new Transform[width, height];
    [SerializeField] SpawnManager spawnManager;
    [SerializeField] BlockSpriteManager blockSpriteManager;

    void Awake()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
        blockSpriteManager = FindObjectOfType<BlockSpriteManager>();
    }

    void Update()
    {
        Debug.Log("Cp" + SpawnManager.checkPoint);
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
            grid[roundX, roundY] = children;
            spawnManager.blocks++;
            Debug.Log($"TetrisMovement.AddToGrid: placed block at ({roundX},{roundY})");
        }
        
        if (blockSpriteManager != null)
        {
            foreach (Transform children in transform)
            {
                int roundX = Mathf.RoundToInt(children.transform.position.x);
                int roundY = Mathf.RoundToInt(children.transform.position.y);
                
                blockSpriteManager.UpdateBlockSprite(roundX, roundY, grid, width, height);
                
                if (roundY + 1 < height) blockSpriteManager.UpdateBlockSprite(roundX, roundY + 1, grid, width, height);
                if (roundY - 1 >= 0) blockSpriteManager.UpdateBlockSprite(roundX, roundY - 1, grid, width, height);
                if (roundX - 1 >= 0) blockSpriteManager.UpdateBlockSprite(roundX - 1, roundY, grid, width, height);
                if (roundX + 1 < width) blockSpriteManager.UpdateBlockSprite(roundX + 1, roundY, grid, width, height);
            }
        }
        
        for (int i = 0; i < 9; i++)
        {
            if (grid[i, SpawnManager.checkPoint] != null)
            {
                this.enabled = false;
                Debug.Log("TetrisMovement.AddToGrid: checkpoint reached — switching to platformer mode");
                FindObjectOfType<SpawnManager>().Switch();
                return;
            }
        }

        Debug.Log("TetrisMovement.AddToGrid: spawning next tetromino");
        FindObjectOfType<SpawnManager>().Spawn();
    }

    bool ValidGrid()
    {
        foreach (Transform children in transform)
        {
            int roundX = Mathf.RoundToInt(children.transform.position.x);
            int roundY = Mathf.RoundToInt(children.transform.position.y);

            if (roundX < 0 || roundX >= width || roundY < 0 || roundY >= height)
            {
                return false;
            }

            if (grid[roundX, roundY] != null)
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

            if (roundY == 0 || grid[roundX,roundY-1] != null) return false;
        }
        return true;
    }
}
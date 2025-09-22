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

    void Awake()
    {
        spawnManager = FindObjectOfType<SpawnManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            transform.RotateAround(transform.TransformPoint(rotationPoint), new Vector3(0, 0, 1), 90);
            if (!ValidGrid())
            {
                transform.RotateAround(transform.TransformPoint(rotationPoint), new Vector3(0, 0, 1), -90);
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
            fallTime = Input.GetKey(KeyCode.S) ? 0.1f : 1f;
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
        }
        for (int i = 0; i < 9; i++)
        {
            if (grid[i, 17] != null)
            {
                this.enabled = false;
                FindObjectOfType<SpawnManager>().Switch();
                return;
            }
        }
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

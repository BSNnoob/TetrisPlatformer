using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StartingPointScript : MonoBehaviour
{
    void Start()
    {
        TetrisMovement.grid[9, 0] = this.transform;
    }
}

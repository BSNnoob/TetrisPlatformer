using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public enum BlockType
{
    Normal,
    Sticky,
    Pass,
    HighJump,
    Bouncy
}

public class SpawnManager : MonoBehaviour
{
    public GameObject[] Tetrominoes;
    public GameObject player;
    void Start()
    {
        player = GameObject.Find("Player");
        player.SetActive(false);
        Spawn();
    }

    public void Spawn()
    {
        GameObject newBlock = Instantiate(Tetrominoes[Random.Range(0, Tetrominoes.Length)], transform.position, Quaternion.identity);
        RandomBlock(newBlock);
    }

    void RandomBlock(GameObject newBlock)
    {
        BlockType chosenType = GetRandomBlockType();
        foreach (Transform children in newBlock.transform)
        {
            BlockType blockType = chosenType;
            ApplyColor(children.gameObject, blockType);
        }
    }

    BlockType GetRandomBlockType()
    {
        int random = 99;

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

                // Set blue color with 50% transparency
                transparentMat.color = new Color(0f, 0f, 1f, 0.5f);

                // Configure for transparency (works with Standard shader)
                transparentMat.SetFloat("_Mode", 3); // Transparent mode
                transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMat.SetInt("_ZWrite", 0);
                transparentMat.DisableKeyword("_ALPHATEST_ON");
                transparentMat.EnableKeyword("_ALPHABLEND_ON");
                transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMat.renderQueue = 3000;
                renderer.material = transparentMat;

                // Remove or disable collider to make it passable
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
                bouncyMaterial.friction = 0.1f; // Low friction for better bouncing

                // Apply the physics material to the collider
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

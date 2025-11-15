using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsCheck : MonoBehaviour
{
    private BoxCollider2D coll;
    private PlayerController player;
    public bool touchWall;
    public LayerMask Wall;
    public float checkRaduis;
    public Vector2 Offset;

    private void Awake()
    {
        
        coll = GetComponent<BoxCollider2D>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (player.input.x > 0)
        {
            Offset.x = 0.2f * transform.localScale.x;
            Offset.y = 0;
        }

        if (player.input.y > 0)
        {
            Offset.y = 0.3f;
            Offset.x = 0;
        }

        if (player.input.x < 0)
        {
            Offset.x = -0.2f * transform.localScale.x;
            Offset.y = 0;
        }

        if (player.input.y < 0)
        {
            Offset.y = -0.3f;
            Offset.x = 0;
        } 
        Check();
    }

    public void Check()
    {
        touchWall = Physics2D.OverlapCircle((Vector2)transform.position + Offset * transform.localScale, checkRaduis, Wall);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere((Vector2)transform.position + Offset *transform.localScale , checkRaduis );
    }
}

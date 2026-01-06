using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    Rigidbody2D rb;
    public int nextMove;
    float moveSpeed = 3f;
    float rayDistance = 10f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    void FixedUpdate()
    {
        // 플레이어 감지 (양쪽 방향으로 레이캐스트)
        Vector2 rightVec = new Vector2(rb.position.x + 0.5f, rb.position.y);
        Vector2 leftVec = new Vector2(rb.position.x - 0.5f, rb.position.y);
        
        Debug.DrawRay(rightVec, Vector3.right * 0.5f, new Color(1, 0, 0));
        Debug.DrawRay(leftVec, Vector3.left * 0.5f, new Color(1, 0, 0));
        
        RaycastHit2D playerHitRight = Physics2D.Raycast(rightVec, Vector3.right, rayDistance, LayerMask.GetMask("Player"));
        RaycastHit2D playerHitLeft = Physics2D.Raycast(leftVec, Vector3.left, rayDistance, LayerMask.GetMask("Player"));
        
        // 플레이어 감지 시 플레이어 쪽으로 이동
        if (playerHitRight.collider != null)
        {
            nextMove = 1; // 오른쪽으로 이동
        }
        else if (playerHitLeft.collider != null)
        {
            nextMove = -1; // 왼쪽으로 이동
        }
        
        // 이동 처리
        if (nextMove != 0)
        {
            rb.velocity = new Vector2(nextMove * moveSpeed, rb.velocity.y);
        }
    }


}

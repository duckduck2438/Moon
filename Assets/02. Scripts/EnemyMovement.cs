using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    Rigidbody2D rb;
    public int nextMove;
    public float moveSpeed = 3f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Invoke("Think", 5);
    }

    void FixedUpdate()
    {
        //움직임
        rb.velocity = new Vector2(nextMove * moveSpeed, rb.velocity.y);

        // 앞쪽 벽 체크만 수행
        if (nextMove != 0)
        {
            Vector2 frontVec = new Vector2(rb.position.x + nextMove * 0.5f, rb.position.y);
            Debug.DrawRay(frontVec, Vector3.right * nextMove * 0.5f, new Color(1, 0, 0));
            
            RaycastHit2D wallHit = Physics2D.Raycast(frontVec, Vector3.right * nextMove, 0.5f, LayerMask.GetMask("Wall"));
            
            if (wallHit.collider != null) {
                // 벽에 부딪히면 방향 전환
                nextMove *= -1;
            }
        }
    }

    void Think()
    {
        nextMove = Random.Range(0, 2) == 0 ? -1 : 1; // 항상 -1 또는 1 (정지 없음)

        Debug.Log("nextMove: " + nextMove);

        Invoke("Think", 5);
    }
}

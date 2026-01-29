using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 임시로 만든 movement
public class EnemyMovement : MonoBehaviour
{

    Rigidbody2D rb;
    public int nextMove;
    float moveSpeed = 3f;
    float rayDistance = 10f;
    bool isHit = false;
    int hitCount = 0;

    public PlayerDataWithDash Data;
    
    private Coroutine hitCoroutine; // 현재 실행 중인 피격 코루틴 추적
    
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    void FixedUpdate()
    {

        if(isHit) {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

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

    // 플레이어 공격에 맞았을 때 호출되는 메서드
    public IEnumerator OnHit()
    {
        // 이미 실행 중인 피격 코루틴이 있으면 중단하고 새로 시작
        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }
        
        isHit = true;

        Debug.Log("Enemy Hit! (코루틴 갱신됨)");

        yield return new WaitForSeconds(Data.attackDuration + 0.2f); // 피격 상태 지속 시간

        isHit = false;
        hitCoroutine = null; // 코루틴 종료
        
        Debug.Log("Enemy recovered from hit!");
        // 여기에 피격 처리 추가:
        // - 체력 감소
        // - 넉백 효과
        // - 피격 애니메이션
        // - 이펙트 생성
        // - 사운드 재생
        // 예시:
        // health -= damage;
        // if (health <= 0) Die();
    }
    
    // 외부에서 호출할 메서드 (코루틴 추적)
    public void TakeDamage()
    {
        hitCount++;
        hitCoroutine = StartCoroutine(OnHit());

        if(hitCount >= 5)
        {
            OnDie();
        }
        // health -= damage;
        // if (health <= 0) Die();
    }

    public void OnDie()
    {
        gameObject.SetActive(false);
        hitCount = 0;
    }

}

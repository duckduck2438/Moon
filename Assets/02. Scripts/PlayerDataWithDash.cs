using UnityEngine;

[CreateAssetMenu(menuName = "Player Data With Dash")] // 프로젝트 메뉴에서 우클릭 후 Create/Player Data With Dash로 새 playerData 객체 생성하고 플레이어에게 드래그하세요
public class PlayerDataWithDash : ScriptableObject
{
	[Header("Gravity")]
	[HideInInspector] public float gravityStrength; // 원하는 jumpHeight와 jumpTimeToApex에 필요한 아래 방향 힘(중력)
	[HideInInspector] public float gravityScale; // 중력의 배수로서 플레이어 중력의 강도 (ProjectSettings/Physics2D에서 설정)
										  // 또한 플레이어의 rigidbody2D.gravityScale에 설정되는 값
	[Space(5)]
	public float fallGravityMult; // 낙하 중일 때 플레이어 gravityScale의 배수
	public float maxFallSpeed; // 낙하 중일 때 최대 낙하 속도(종단 속도)
	[Space(5)]
	public float fastFallGravityMult; // 낙하 중 아래 방향 입력을 눌렀을 때 gravityScale의 더 큰 배수
									  // Celeste 같은 게임에서 볼 수 있으며, 원한다면 플레이어가 더 빠르게 떨어질 수 있게 합니다
	public float maxFastFallSpeed; // 빠른 낙하 수행 시 최대 낙하 속도(종단 속도)
	
	[Space(20)]

	[Header("Run")]
	public float runMaxSpeed; // 플레이어가 도달하기를 원하는 목표 속도
	public float runAcceleration; // 최대 속도로 가속하는 속도. 즉각 가속은 runMaxSpeed로, 가속 없음은 0으로 설정 가능
	[HideInInspector] public float runAccelAmount; // 플레이어에게 적용되는 실제 힘 (speedDiff와 곱해짐)
	public float runDecceleration; // 현재 속도에서 감속하는 속도. 즉각 감속은 runMaxSpeed로, 감속 없음은 0으로 설정 가능
	[HideInInspector] public float runDeccelAmount; // 플레이어에게 적용되는 실제 힘 (speedDiff와 곱해짐)
	[Space(5)]
	[Range(0f, 1)] public float accelInAir; // 공중에 있을 때 가속도에 적용되는 배수
	[Range(0f, 1)] public float deccelInAir;
	[Space(5)]
	public bool doConserveMomentum = true;

	[Space(20)]

	[Header("Jump")]
	public float jumpHeight; // 플레이어 점프의 높이
	public float jumpTimeToApex; // 점프 힘을 적용한 후 원하는 점프 높이에 도달하는 시간. 이 값들은 플레이어의 중력과 점프 힘도 제어합니다
	[HideInInspector] public float jumpForce; // 플레이어가 점프할 때 적용되는 실제 힘(위쪽 방향)

	[Header("Both Jumps")]
	public float jumpCutGravityMult; // 점프 중에 점프 버튼을 놓으면 중력을 증가시키는 배수
	[Range(0f, 1)] public float jumpHangGravityMult; // 점프의 정점(원하는 최대 높이)에 가까울 때 중력을 감소시킵니다
	public float jumpHangTimeThreshold; // 플레이어가 추가 "점프 항" 을 경험하는 속도(0에 가까움). 플레이어의 velocity.y는 점프 정점에서 0에 가장 가깝습니다 (포물선이나 이차 함수의 기울기를 생각하세요)
	[Space(0.5f)]
	public float jumpHangAccelerationMult; 
	public float jumpHangMaxSpeedMult; 				

	[Header("Wall Jump")]
	public Vector2 wallJumpForce; // 벽 점프 시 플레이어에게 적용되는 실제 힘(우리가 설정)
	[Space(5)]
	[Range(0f, 1f)] public float wallJumpRunLerp; // 벽 점프 중 플레이어 이동의 효과를 감소시킵니다
	[Range(0f, 1.5f)] public float wallJumpTime; // 벽 점프 후 플레이어 이동이 느려지는 시간
	public bool doTurnOnWallJump; // 플레이어가 벽 점프 방향을 향하도록 회전합니다

	[Space(20)]

	[Header("Slide")]
	public float slideSpeed;
	public float slideAccel;

    [Header("Assists")]
	[Range(0.01f, 0.5f)] public float coyoteTime; // 플랫폼에서 떨어진 후에도 점프할 수 있는 유예 기간
	[Range(0.01f, 0.5f)] public float jumpInputBufferTime; // 점프를 누른 후 요구사항(예: 땅에 있음)이 충족되면 자동으로 점프가 수행되는 유예 기간

	[Space(20)]

	[Header("Dash")]
	public int dashAmount;
	public float dashSpeed;
	public float dashSleepTime; // 대시를 누른 후부터 방향 입력을 읽고 힘을 적용하기 전까지 게임이 정지되는 시간
	[Space(5)]
	public float dashAttackTime;
	[Space(5)]
	public float dashEndTime; // 초기 드래그 단계를 마친 후의 시간, 아이들(또는 모든 표준 상태)로의 전환을 부드럽게 합니다
	public Vector2 dashEndSpeed; // 플레이어를 느리게 하여 대시가 더 반응적으로 느껴지게 합니다 (Celeste에서 사용)
	[Range(0f, 1f)] public float dashEndRunLerp; // 대시 중 플레이어 이동의 효과를 느리게 합니다
	[Space(5)]
	public float dashRefillTime;
	[Space(5)]
	[Range(0.01f, 0.5f)] public float dashInputBufferTime;

	[Space(20)]

	[Header("Attack")]
	public float attackDuration; // 공격 애니메이션/액션의 총 지속 시간
	public float attackCooldown; // 다시 공격할 수 있기 전의 쿨다운 시간
	[Space(5)]
	[Range(0f, 1f)] public float attackMoveSpeedMult; // 공격 중 이동 속도 배수 (0 = 이동 불가, 1 = 최대 속도)
	[Space(5)]
	public int maxComboCount; // 최대 콤보 공격 횟수 (1 = 단일 공격, 3 = 3히트 콤보 등)
	public float comboWindowTime; // 콤보 계속을 위해 다음 공격을 입력할 수 있는 시간 창
	[Space(5)]
	public float attackRange; // 공격 이펙트가 나타나는 플레이어로부터의 거리
	public Vector2 attackHitboxSize; // 공격 히트박스 크기 (x = 너비, y = 높이)
	[Space(5)]
	public float attackKnockbackForce; // 공격 중 플레이어에게 적용되는 힘 (이동하는 공격용)
	public Vector2 attackKnockbackDir; // 공격 중 넷백/이동의 방향 (정규화됨)
	[Space(5)]
	[Range(0.01f, 0.5f)] public float attackInputBufferTime; // 공격 입력 버퍼링을 위한 유예 기간
	

	// Unity 콜백, 인스펙터가 업데이트될 때 호출됨
    private void OnValidate()
    {
		// 공식 (gravity = 2 * jumpHeight / timeToJumpApex^2)을 사용하여 중력 강도 계산
		gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
		
		// rigidbody의 중력 스케일 계산 (즉, Unity의 중력 값에 대한 상대적인 중력 강도, 프로젝트 설정/Physics2D 참조)
		gravityScale = gravityStrength / Physics2D.gravity.y;

		// 공식: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed 를 사용하여 달리기 가속 및 감속 힘 계산
		runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
		runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

		// 공식 (initialJumpVelocity = gravity * timeToJumpApex)을 사용하여 jumpForce 계산
		jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

		#region Variable Ranges
		runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
		runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
		#endregion
	}
}

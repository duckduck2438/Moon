/*
	제작: @DawnosaurDev youtube.com/c/DawnosaurStudios
	확인해주셔서 감사하며 도움이 되었으면 좋겠습니다!
	추가 질문이나 피드백이 있으시면 트위터나 유튜브 댓글로 연락주세요 :D

	자유롭게 게임에 사용하세요. 만든 작품을 보고 싶습니다!
 */

using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	// 플레이어의 모든 이동 파라미터를 담는 ScriptableObject. 사용하지 않으려면
	// 모든 파라미터를 직접 붙여넣으세요. 다만 스크립트의 모든 참조를 수동으로 변경해야 합니다
	public PlayerDataWithDash Data;

	#region COMPONENTS
	public Rigidbody2D RB { get; private set; }
	public SpriteRenderer spriteRenderer { get; private set; }
	#endregion

	#region ATTACK EFFECT
	[Header("Attack Effect")]
	[SerializeField] private GameObject _attackEffectPrefab; // 공격 이펙트 프리팹 또는 자식 오브젝트
	private GameObject _currentAttackEffect; // 현재 활성화된 공격 이펙트
	#endregion

	#region STATE PARAMETERS
	// 플레이어가 언제든지 수행할 수 있는 다양한 액션을 제어하는 변수들
	// 다른 스크립트에서 읽을 수 있도록 public이지만
	// 쓰기는 private으로만 가능합니다
	public bool IsFacingRight { get; private set; }
	public bool IsJumping { get; private set; }
	public bool IsWallJumping { get; private set; }
	public bool IsDashing { get; private set; }
	public bool IsSliding { get; private set; }
	public bool IsAttacking { get; private set; }

	// 타이머들 (모두 필드이며, private으로 만들고 bool을 반환하는 메서드를 사용할 수도 있습니다)
	public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	// 점프
	private bool _isJumpCut;
	private bool _isJumpFalling;
	private bool _jumpKeyWasPressed; // 이전 프레임에 점프 키를 누르고 있었는지

	// 벽 점프
	private float _wallJumpStartTime;
	private int _lastWallJumpDir;

	// 대시
	private int _dashesLeft;
	private bool _dashRefilling;
	private Vector2 _lastDashDir;
	private bool _isDashAttacking;

	// 공격
	private int _currentComboCount;
	private float _lastAttackTime;
	private float _attackEndTime;

	#endregion

	#region INPUT PARAMETERS
	private Vector2 _moveInput;

	public float LastPressedJumpTime { get; private set; }
	public float LastPressedDashTime { get; private set; }
	public float LastPressedAttackTime { get; private set; }
	#endregion

	#region CHECK PARAMETERS
	// 인스펙터에서 모두 설정하세요
	[Header("Checks")]
	[SerializeField] private Transform _groundCheckPoint;
	// groundCheck의 크기는 캐릭터 크기에 따라 다릅니다. 일반적으로 너비(땅용)와 높이(벽 체크용)보다 약간 작게 설정하세요
	[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
	[Space(5)]
	[SerializeField] private Transform _frontWallCheckPoint;
	[SerializeField] private Transform _backWallCheckPoint;
	[SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
	#endregion

	#region LAYERS & TAGS
	[Header("Layers & Tags")]
	[SerializeField] private LayerMask _groundLayer;
	[SerializeField] private LayerMask _wallLayer;
	#endregion

	private void Awake()
	{
		RB = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		SetGravityScale(Data.gravityScale);
		IsFacingRight = true;
	}

	private void Update()
	{
		#region TIMERS
		LastOnGroundTime -= Time.deltaTime;
		LastOnWallTime -= Time.deltaTime;
		LastOnWallRightTime -= Time.deltaTime;
		LastOnWallLeftTime -= Time.deltaTime;

		LastPressedJumpTime -= Time.deltaTime;
		LastPressedDashTime -= Time.deltaTime;
		LastPressedAttackTime -= Time.deltaTime;
		#endregion

		#region INPUT HANDLER
		_moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0 && !IsAttacking)
			CheckDirectionToFace(_moveInput.x > 0);

		if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
		{
			OnJumpInput();
		}

		// 점프 키 상태 수동 추적 (GetKeyUp 대체)
		bool jumpKeyIsPressed = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.J);

		if (_jumpKeyWasPressed && !jumpKeyIsPressed)
		{
			// 이전 프레임에는 누르고 있었는데 지금은 안 누르고 있음 = 키를 떸
			OnJumpUpInput();
			Debug.Log("Manual Jump Up detected! moving: " + (_moveInput.x != 0));
		}

		_jumpKeyWasPressed = jumpKeyIsPressed;

		if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
		{
			OnDashInput();
		}

		if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.L) || Input.GetMouseButtonDown(0))
		{
			OnAttackInput();
		}
		#endregion

		#region COLLISION CHECKS
		if (!IsDashing)
		{
			// 땅 체크 - 점프 중이 아닐 때만
			if (!IsJumping && Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer)) // 설정된 박스가 땅과 겹치는지 확인
			{
				LastOnGroundTime = Data.coyoteTime; // 겹친다면 lastGrounded를 coyoteTime으로 설정
													// 땅에 닿으면 대시 쿨타임 초기화
				_dashesLeft = Data.dashAmount;
			}

			// 오른쪽 벽 체크 - 점프 중에도 작동
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _wallLayer) && IsFacingRight)
					|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _wallLayer) && !IsFacingRight)) && !IsWallJumping)
			{
				LastOnWallRightTime = Data.coyoteTime;
				// 벽에 닿으면 대시 쿨타임 초기화
				_dashesLeft = Data.dashAmount;
			}

			// 왼쪽 벽 체크 - 점프 중에도 작동
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _wallLayer) && !IsFacingRight)
				|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _wallLayer) && IsFacingRight)) && !IsWallJumping)
			{
				LastOnWallLeftTime = Data.coyoteTime;
				// 벽에 닿으면 대시 쿨타임 초기화
				_dashesLeft = Data.dashAmount;
			}

			// 플레이어가 방향을 바꿀 때마다 벽 체크 포인트가 바뀌므로 왼쪽과 오른쪽 벽 모두 두 번 체크 필요
			LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		}
		#endregion

		#region JUMP CHECKS
		if (IsJumping && RB.velocity.y < 0)
		{
			IsJumping = false;

			if (!IsWallJumping)
				_isJumpFalling = true;
		}

		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
		{
			IsWallJumping = false;
		}

		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
		{
			_isJumpCut = false;

			if (!IsJumping)
				_isJumpFalling = false;
		}

		if (!IsDashing)
		{
			// 점프
			if (CanJump() && LastPressedJumpTime > 0)
			{
				IsJumping = true;
				IsWallJumping = false;
				_isJumpCut = false;
				_isJumpFalling = false;
				Jump();
			}
			// 벽 점프
			else if (CanWallJump() && LastPressedJumpTime > 0)
			{
				IsWallJumping = true;
				IsJumping = false;
				_isJumpCut = false;
				_isJumpFalling = false;

				_wallJumpStartTime = Time.time;
				_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

				WallJump(_lastWallJumpDir);
			}
		}
		#endregion

		#region DASH CHECKS
		if (CanDash() && LastPressedDashTime > 0)
		{
			// 게임을 순간적으로 정지. 타격감을 더하고 방향 입력에 약간의 여유를 줍니다
			Sleep(Data.dashSleepTime);

			// 방향 입력이 없으면 앞으로 대시
			if (_moveInput != Vector2.zero)
				_lastDashDir = _moveInput;
			else
				_lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;



			IsDashing = true;
			IsJumping = false;
			IsWallJumping = false;
			_isJumpCut = false;

			StartCoroutine(nameof(StartDash), _lastDashDir);
		}
		#endregion

		#region SLIDE CHECKS
		if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
		{
			IsSliding = true;
		}
		// 점프 중 벽에 닿으면 점프 상태 종료 -> 슬라이딩 가능하게
		else if (IsJumping && LastOnWallTime > 0 && LastOnGroundTime <= 0 &&
				 ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
		{
			IsJumping = false;
			IsSliding = true;
		}
		else
		{
			IsSliding = false;
		}
		#endregion

		#region ATTACK CHECKS
		// 벽에 붙어있으면 공격 입력 버퍼 초기화
		if (LastOnWallTime > 0 && LastPressedAttackTime > 0)
		{
			LastPressedAttackTime = 0;
		}

		if (IsDashing && LastPressedAttackTime > 0)
		{
			StopCoroutine(nameof(StartDash));
			IsDashing = false;
			_isDashAttacking = false;
			SetGravityScale(Data.gravityScale);
		}

		// 슬라이드 중 공격 입력 시 슬라이드 중단
		if (IsSliding && LastPressedAttackTime > 0)
		{
			IsSliding = false;
		}

		if (CanAttack() && LastPressedAttackTime > 0)
		{
			Attack();
		}

		// 공격 종료 체크
		if (IsAttacking && Time.time >= _attackEndTime)
		{
			IsAttacking = false;
			_lastAttackTime = Time.time;
			LastPressedAttackTime = 0; // 공격 끝날 때 입력 버퍼 초기화
		}

		// 공중이 아닇 때 콤보 카운트 리셋
		if (LastOnGroundTime > 0)
		{
			// 콤보 윈도우 시간 체크
			if (Time.time - _lastAttackTime > Data.comboWindowTime)
			{
				_currentComboCount = 0;
			}
		}
		#endregion

		#region GRAVITY
		if (!_isDashAttacking)
		{
			// 공중 공격 중에는 중력 0
			if (IsAttacking && LastOnGroundTime <= 0)
			{
				SetGravityScale(0);
			}
			// 점프 입력을 놓았거나 낙하 중이면 더 높은 중력
			else if (IsSliding)
			{
				SetGravityScale(0);
			}
			else if (RB.velocity.y < 0 && _moveInput.y < 0)
			{
				// 아래 방향키를 누르고 있으면 훨씬 높은 중력
				SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
				// 최대 낙하 속도 제한, 먼 거리를 떨어질 때 미친듯이 빠른 속도로 가속되지 않도록
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFastFallSpeed));
			}
			else if (_isJumpCut)
			{
				// 점프 버튼을 놓으면 더 높은 중력
				SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
			}
			else if (!IsSliding &&
				(IsJumping || IsWallJumping || _isJumpFalling) &&
				Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
			{
				SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
			}
			else if (RB.velocity.y < 0)
			{
				// 낙하 중이면 더 높은 중력
				SetGravityScale(Data.gravityScale * Data.fallGravityMult);
				// 최대 낙하 속도 제한, 먼 거리를 떨어질 때 미친듯이 빠른 속도로 가속되지 않도록
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
			}
			else
			{
				// 플랫폼 위에 서있거나 위로 이동 중일 때 기본 중력
				SetGravityScale(Data.gravityScale);
			}
		}
		else
		{
			// 대시 중에는 중력 없음 (초기 대시 공격 단계가 끝나면 정상으로 복귀)
			SetGravityScale(0);
		}
		#endregion
	}

	private void FixedUpdate()
	{
		// 공격 중에는 멈춰있기
		if (IsAttacking)
		{
			RB.velocity = new Vector2(0, 0);
		}

		// 달리기 처리
		if (!IsDashing && !IsSliding && !IsAttacking)
		{
			if (IsWallJumping)
				Run(Data.wallJumpRunLerp);
			else
				Run(1);
		}
		else if (_isDashAttacking)
		{
			Run(Data.dashEndRunLerp);
		}

		// 슬라이드 처리
		if (IsSliding)
			Slide();
	}

	#region INPUT CALLBACKS
	// Update()에서 감지된 입력을 처리하는 메서드들
	public void OnJumpInput()
	{
		LastPressedJumpTime = Data.jumpInputBufferTime;
	}

	public void OnJumpUpInput()
	{
		Debug.Log($"OnJumpUpInput called! IsJumping: {IsJumping}, IsWallJumping: {IsWallJumping}, velocity.y: {RB.velocity.y}, CanJumpCut: {CanJumpCut()}");
		if (CanJumpCut() || CanWallJumpCut())
		{
			_isJumpCut = true;
			Debug.Log("Jump Cut SUCCESS! velocity.y: " + RB.velocity.y);
		}
		else
		{
			Debug.Log("Jump Cut FAILED - conditions not met");
		}
	}

	public void OnDashInput()
	{
		LastPressedDashTime = Data.dashInputBufferTime;
	}

	public void OnAttackInput()
	{
		// 공격 중이 아닐 때만 입력 받기
		if (!IsAttacking)
		{
			LastPressedAttackTime = Data.attackInputBufferTime;
		}
	}
	#endregion

	#region GENERAL METHODS
	public void SetGravityScale(float scale)
	{
		RB.gravityScale = scale;
	}

	private void Sleep(float duration)
	{
		// 모든 곳에서 StartCoroutine을 호출하지 않아도 되도록 하는 메서드
		// nameof() 표기법을 사용하면 문자열을 직접 입력하지 않아도 됩니다
		// 철자 실수의 가능성을 제거하고 오류 메시지를 개선합니다
		StartCoroutine(nameof(PerformSleep), duration);
	}

	private IEnumerator PerformSleep(float duration)
	{
		Time.timeScale = 0;
		yield return new WaitForSecondsRealtime(duration); // timeScale이 0이 되므로 반드시 Realtime이어야 합니다
		Time.timeScale = 1;
	}
	#endregion

	// 이동 메서드들
	#region RUN METHODS
	private void Run(float lerpAmount)
	{
		// 이동하려는 방향과 원하는 속도 계산
		float targetSpeed = _moveInput.x * Data.runMaxSpeed;
		// Lerp()를 사용하여 제어를 부드럽게 하고 방향과 속도 변화를 부드럽게 만듭니다
		targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, lerpAmount);

		#region Calculate AccelRate
		float accelRate;

		// 가속 중인지(방향 전환 포함) 감속 중인지(정지)에 따라 가속도 값을 가져옵니다
		// 공중에 있으면 배수를 적용합니다
		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
		#endregion

		#region Add Bonus Jump Apex Acceleration
		// 점프의 정점에서 가속도와 최대 속도를 증가시켜 점프가 더 탄력적이고 반응적이며 자연스럽게 느껴지도록 합니다
		if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
		{
			accelRate *= Data.jumpHangAccelerationMult;
			targetSpeed *= Data.jumpHangMaxSpeedMult;
		}
		#endregion

		#region Conserve Momentum
		// 플레이어가 원하는 방향으로 이동하지만 최대 속도보다 빠른 경우 감속하지 않습니다
		if (Data.doConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
		{
			// 감속이 일어나지 않도록 방지, 즉 현재 모멘텀을 보존합니다
			// 이 "상태"에서 플레이어가 속도를 약간 증가시킬 수 있도록 실험해볼 수 있습니다
			accelRate = 0;
		}
		#endregion

		// 현재 속도와 원하는 속도의 차이 계산
		float speedDif = targetSpeed - RB.velocity.x;
		// 플레이어에게 적용할 x축 방향의 힘 계산

		float movement = speedDif * accelRate;

		// 벡터로 변환하여 rigidbody에 적용
		RB.AddForce(movement * Vector2.right, ForceMode2D.Force);

		/*
		 * 참고로 AddForce()가 하는 일은 다음과 같습니다
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Unity에서 Time.fixedDeltaTime은 기본적으로 0.02초이며 초당 50번의 FixedUpdate() 호출과 같습니다
		*/
	}

	private void Turn()
	{
		// 스케일을 저장하고 플레이어를 x축으로 뒤집습니다
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;

		IsFacingRight = !IsFacingRight;
	}
	#endregion

	#region JUMP METHODS
	private void Jump()
	{
		// 한 번의 입력으로 점프를 여러 번 호출할 수 없도록 합니다
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;

		#region Perform Jump
		// 낙하 중이면 적용되는 힘을 증가시킵니다
		// 이렇게 하면 항상 같은 양만큼 점프하는 것처럼 느껴집니다
		// (미리 플레이어의 Y 속도를 0으로 설정해도 같은 효과지만, 이 방법이 더 우아합니다 :D)
		float force = Data.jumpForce;
		if (RB.velocity.y < 0)
			force -= RB.velocity.y;

		RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
		#endregion
	}

	private void WallJump(int dir)
	{
		// 한 번의 입력으로 벽 점프를 여러 번 호출할 수 없도록 합니다
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;

		#region Perform Wall Jump
		Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
		force.x *= dir; // 벽의 반대 방향으로 힘을 적용

		if (Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x))
			force.x -= RB.velocity.x;

		if (RB.velocity.y < 0) // 플레이어가 떨어지고 있는지 확인, 그렇다면 velocity.y를 빼서 중력의 힘을 상쇄합니다. 이렇게 하면 플레이어가 항상 원하는 점프 힘 이상에 도달하게 됩니다
			force.y -= RB.velocity.y;

		// 달리기와 달리 여기서는 Impulse 모드를 사용합니다
		// 기본 모드는 질량을 무시하고 즉시 힘을 적용합니다
		RB.AddForce(force, ForceMode2D.Impulse);
		#endregion
	}
	#endregion

	#region DASH METHODS
	// 대시 코루틴
	private IEnumerator StartDash(Vector2 dir)
	{
		// 전반적으로 이 대시 방법은 Celeste를 모방합니다. 더 물리 기반 접근을 원한다면
		// 점프에 사용된 것과 유사한 방법을 시도해보세요

		LastOnGroundTime = 0;
		LastPressedDashTime = 0;

		float startTime = Time.time;

		_dashesLeft--;
		_isDashAttacking = true;

		SetGravityScale(0);

		// "공격" 단계 동안 플레이어의 속도를 대시 속도로 유지합니다 (Celeste에서는 처음 0.15초)
		while (Time.time - startTime <= Data.dashAttackTime)
		{
			RB.velocity = dir.normalized * Data.dashSpeed;
			// 다음 프레임까지 루프를 일시 중지하여 Update 루프와 비슷하게 만듭니다
			// 여러 타이머를 사용하는 것보다 깔끔한 구현이며, 이 코루틴 접근 방식은 실제로 Celeste에서 사용되는 방법입니다 :D
			yield return null;
		}

		startTime = Time.time;

		_isDashAttacking = false;

		// 대시의 "종료" 단계 시작, 플레이어에게 일부 제어권을 돌려주지만 여전히 달리기 가속을 제한합니다 (Update()와 Run() 참조)
		SetGravityScale(Data.gravityScale);
		RB.velocity = Data.dashEndSpeed * dir.normalized;

		while (Time.time - startTime <= Data.dashEndTime)
		{
			yield return null;
		}

		// 대시 종료
		IsDashing = false;
	}

	// 플레이어가 다시 대시할 수 있기 전의 짧은 시간
	private IEnumerator RefillDash(int amount)
	{
		// 짧은 쿨다운으로 땅에서 계속 대시할 수 없도록 합니다. 이것도 Celeste의 구현이며 자유롭게 변경하세요
		_dashRefilling = true;
		yield return new WaitForSeconds(Data.dashRefillTime);
		_dashRefilling = false;
		_dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
	}
	#endregion

	#region ATTACK METHODS
	private void Attack()
	{
		IsAttacking = true;
		LastPressedAttackTime = 0;

		// 슬라이드 중이었다면 중단
		if (IsSliding)
		{
			IsSliding = false;
		}

		_attackEndTime = Time.time + Data.attackDuration;

		// 콤보 카운트 증가
		_currentComboCount++;

		// 공격 이펙트 생성/활성화
		if (_attackEffectPrefab != null)
		{
			// 생성 위치 계산 (플레이어 위치 + 방향에 따른 attackRange)
			float offsetX = Data.attackRange * (IsFacingRight ? 1 : -1);
			Vector3 spawnPos = transform.position + new Vector3(offsetX, 0, 0);

			// 공격 이펙트 생성
			_currentAttackEffect = Instantiate(_attackEffectPrefab, spawnPos, Quaternion.identity);

			// 방향에 맞춰 스케일 조정 (왼쪽 보면 뒤집기)
			if (!IsFacingRight)
			{
				Vector3 scale = _currentAttackEffect.transform.localScale;
				scale.x *= -1;
				_currentAttackEffect.transform.localScale = scale;
			}

			// Animator가 있으면 콤보 카운트 전달
			Animator effectAnim = _currentAttackEffect.GetComponent<Animator>();
			if (effectAnim != null)
			{
				effectAnim.SetInteger("ComboCount", _currentComboCount);
			}

			// 공격 지속 시간 후 자동 삭제
			Destroy(_currentAttackEffect, Data.attackDuration);
		}

		// 공격 시 이동 효과 (옵션)
		if (Data.attackKnockbackForce > 0)
		{
			Vector2 knockbackDir = Data.attackKnockbackDir.normalized;
			knockbackDir.x *= IsFacingRight ? 1 : -1;
			RB.AddForce(knockbackDir * Data.attackKnockbackForce, ForceMode2D.Impulse);
		}
	}
	#endregion

	#region OTHER MOVEMENT METHODS
	private void Slide()
	{
		// 즉시 slideSpeed로 설정
		RB.velocity = new Vector2(RB.velocity.x, Data.slideSpeed);
	}
	#endregion


	#region CHECK METHODS
	public void CheckDirectionToFace(bool isMovingRight)
	{
		if (isMovingRight != IsFacingRight)
			Turn();
	}

	private bool CanJump()
	{
		// 현재 땅에 있는지 실시간 체크
		bool isOnGround = Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer);

		// 현재 벽에 붙어있는지 실시간 체크
		bool isTouchingWall = Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _wallLayer) ||
							  Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _wallLayer);

		// 땅과 벽에 동시에 붙어있으면 점프 불가
		if (isOnGround && isTouchingWall)
			return false;

		return LastOnGroundTime > 0 && !IsJumping;
	}

	private bool CanWallJump()
	{
		return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!IsWallJumping ||
			 (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
	}

	private bool CanJumpCut()
	{
		return (IsJumping || _isJumpFalling || LastOnGroundTime <= 0) && RB.velocity.y > 0 && !_isJumpCut;
	}

	private bool CanWallJumpCut()
	{
		return IsWallJumping && RB.velocity.y > 0;
	}

	private bool CanDash()
	{
		return _dashesLeft > 0;
	}

	public bool CanSlide()
	{
		if (LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && !IsAttacking && LastOnGroundTime <= 0)
			return true;
		else
			return false;
	}

	private bool CanAttack()
	{
		// 벽에 붙어있을 때는 공격 불가능
		if (LastOnWallTime > 0)
		{
			return false;
		}

		// 공격 중이 아니고, 쿨다운이 끝났으면 공격 가능 (대시와 슬라이드는 중단 가능)
		if (!IsAttacking)
		{
			// 쿨다운 체크
			if (Time.time - _lastAttackTime >= Data.attackCooldown)
			{
				return true;
			}
			// 콤보 윈도우 내에 있고, 아직 최대 콤보에 도달하지 않았으면 공격 가능
			else if (_currentComboCount > 0 && _currentComboCount < Data.maxComboCount && Time.time - _lastAttackTime <= Data.comboWindowTime)
			{
				return true;
			}
		}
		return false;
	}
	#endregion


	#region EDITOR METHODS
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
		Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
	}
	#endregion

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Enemy"))
		{
			Debug.Log("맞았습니다");
			OnDamaged(collision.transform.position);
		}

	}

	void OnDamaged(Vector3 targetPos)
	{
		//맞았으면 PlayerDamaged 레이어로 바꾸기
		gameObject.layer = 9; // 9번은 PlayerDamaged

		spriteRenderer.color = new Color(1, 1, 1, 0.5f); // 반투명 효과


		//1초 후에 원래 레이어로 복귀
		Invoke("ResetLayer", 1f);
	}

	void ResetLayer()
	{
		gameObject.layer = 3; // 3번은 Player
		spriteRenderer.color = new Color(1, 1, 1, 1f); // 원래 상태 복원
	}

}


// Dawnosaur 제작 :D
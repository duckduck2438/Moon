using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
	//Scriptable object which holds all the player's movement parameters. If you don't want to use it
	//just paste in all the parameters, though you will need to manuly change all references in this script

	//HOW TO: to add the scriptable object, right-click in the project window -> create -> Player Data
	//Next, drag it into the slot in playerMovement on your player

	[SerializeField] private PlayerDataWithDash Data;

	#region Variables
	//Components
	public Rigidbody2D Rigidbody { get; private set; }
	public CapsuleCollider2D Collider { get; private set; }

	//Variables control the various actions the player can perform at any time.
	//These are fields which can are public allowing for other sctipts to read them
	//but can only be privately written to.
	public bool IsFacingRight { get; private set; }
	public bool IsJumping { get; private set; }
	public bool IsWallJumping { get; private set; }
	public bool IsSliding { get; private set; }

	//Timers (also all fields, could be private and a method returning a bool could be used)
	public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	//Jump
	private bool _isJumpCut;
	private bool _isJumpFalling;

	//Wall Jump
	private float _wallJumpStartTime;
	private int _lastWallJumpDir;

	private Vector2 _moveInput;
	public float LastPressedJumpTime { get; private set; }

	// 디버그 타이머
	private float _debugTimer;

    [Header("Layers & Tags")]
	[SerializeField] private LayerMask _groundLayer;
	[SerializeField] private LayerMask _wallLayer;
	[SerializeField] private LayerMask _enemyLayer;
	#endregion

    private void Awake()
	{
		Rigidbody = GetComponent<Rigidbody2D>();
		Collider = GetComponent<CapsuleCollider2D>();
	}

	private void Start()
	{
		SetGravityScale(Data.gravityScale);
		IsFacingRight = true; // 스프라이트 이미지를 초기에 오른쪽으로 설정
	}

  private void Update()
	{
		#region INPUT
		_moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0); //플레이어가 움직이는 방향으로 향하게 함


		// 점프 입력 처리
		if (Input.GetButtonDown("Jump")) // 유니티창에서 Edit -> Project Settings -> Input Manager에서 "Jump" 입력 설정 확인
		{
			OnJumpInput();
		}
		// 점프 버튼이 떼어졌을 때 숏점프 처리
		if (Input.GetButtonUp("Jump"))
		{
			OnJumpUpInput();
		}
		#endregion

    #region TIMERS
	// 코요테 타임
    LastOnGroundTime -= Time.deltaTime; 
		LastOnWallTime -= Time.deltaTime; 
		LastOnWallRightTime -= Time.deltaTime;
		LastOnWallLeftTime -= Time.deltaTime;

		LastPressedJumpTime -= Time.deltaTime; // 점프 입력 버퍼링
		#endregion

		#region COLLISION CHECKS
		// Ground Check (점프 중이 아닐 때만)
		if (!IsJumping)
		{
			//Ground Check
			//  ray로 충돌감지           플레이어 중심에서 시작    플레이어크기 		 캡슐모양    회전각도0도  아래방향    거리                     충돌레이어
			if ( Physics2D.CapsuleCast(Collider.bounds.center,Collider.size, Collider.direction, 0, Vector2.down, Data.GrounderDistance, _groundLayer) && !IsJumping) //checks if set box overlaps with ground
			{
				LastOnGroundTime = Data.coyoteTime; //if so sets the lastGrounded to coyoteTime
            }		
		}

		// Wall Check (항상 실행 - 점프 중에도 벽 감지)
		//Right Wall Check
		// _wallLayer 사용 (벽 전용 레이어)
		RaycastHit2D rightWallHit = Physics2D.CapsuleCast(Collider.bounds.center, Collider.size, Collider.direction, 0, Vector2.right, Data.WallDistance, _wallLayer);
		RaycastHit2D leftWallHit = Physics2D.CapsuleCast(Collider.bounds.center, Collider.size, Collider.direction, 0, Vector2.left, Data.WallDistance, _wallLayer);
		
		// 0.5초마다 한 번씩만 디버그 출력
		if (Time.time - _debugTimer > 0.5f)
		{
			Debug.Log($"=== Wall Detection Debug ===");
			Debug.Log($"WallDistance: {Data.WallDistance}");
			Debug.Log($"Player Position: {Collider.bounds.center}");
			Debug.Log($"Collider Size: {Collider.size}");
			Debug.Log($"Wall Layer Value: {_wallLayer.value}");
			Debug.Log($"Right Wall Hit: {rightWallHit.collider != null}, Distance: {rightWallHit.distance}");
			Debug.Log($"Left Wall Hit: {leftWallHit.collider != null}, Distance: {leftWallHit.distance}");
			if (rightWallHit.collider != null) Debug.Log($"Right Wall Object: {rightWallHit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(rightWallHit.collider.gameObject.layer)}");
			if (leftWallHit.collider != null) Debug.Log($"Left Wall Object: {leftWallHit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(leftWallHit.collider.gameObject.layer)}");
			_debugTimer = Time.time;
		}
		
		if (((rightWallHit && IsFacingRight) || (leftWallHit && !IsFacingRight)) && !IsWallJumping)
			LastOnWallRightTime = Data.coyoteTime;

		//Left Wall Check
		// _wallLayer 사용 (벽 전용 레이어)
		if (((rightWallHit && !IsFacingRight) || (leftWallHit && IsFacingRight)) && !IsWallJumping)
			LastOnWallLeftTime = Data.coyoteTime;

		//Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
		LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		#endregion

		#region JUMP CHECKS
		// 낙하 중
		if (IsJumping && Rigidbody.velocity.y < 0)
		{
			IsJumping = false;

			if(!IsWallJumping)
			// 낙하 중을 표시
				_isJumpFalling = true;
		}
		// 벽 점프 중 시간 경과 확인
		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
		{
			IsWallJumping = false;
		}
		// 지면에 닿으면 점프 상태 초기화
		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
        {
			_isJumpCut = false;

			if(!IsJumping)
				_isJumpFalling = false;
		}

		//Jump
		if (CanJump() && LastPressedJumpTime > 0)
		{
			IsJumping = true;
			IsWallJumping = false;
			_isJumpCut = false;
			_isJumpFalling = false;
			Jump();
		}
		//WALL JUMP
		else if (CanWallJump() && LastPressedJumpTime > 0 && Input.GetButton("Grab"))
		{
			IsWallJumping = true;
			IsJumping = false;
			_isJumpCut = false;
			_isJumpFalling = false;
			_wallJumpStartTime = Time.time;
			_lastWallJumpDir = (LastOnWallRightTime > 0) ? 1 : -1; // 벽과 같은 방향으로 점프
			
			WallJump(_lastWallJumpDir);
		}
		#endregion

		#region SLIDE CHECKS
		// 디버그: 각 조건 확인
		bool canSlide = CanSlide();
		bool leftWallCondition = LastOnWallLeftTime > 0 && _moveInput.x < 0;
		bool rightWallCondition = LastOnWallRightTime > 0 && _moveInput.x > 0;
		
		// 0.5초마다 한 번씩만 디버그 출력
		if (Time.time - _debugTimer > 0.5f)
		{
			Debug.Log($"CanSlide: {canSlide}, LeftWall: {leftWallCondition} (Time:{LastOnWallLeftTime}, Input:{_moveInput.x}), RightWall: {rightWallCondition} (Time:{LastOnWallRightTime})");
		}
		
		if (canSlide && (leftWallCondition || rightWallCondition))
			IsSliding = true;
		else
			IsSliding = false;
		#endregion

		#region GRAVITY
		//Higher gravity if we've released the jump input or are falling
		if (IsSliding)
		{
			SetGravityScale(0);
		}
		else if (Rigidbody.velocity.y < 0 && _moveInput.y < 0)
		{
			//Much higher gravity if holding down
			SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
			//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
			Rigidbody.velocity = new Vector2(Rigidbody.velocity.x, Mathf.Max(Rigidbody.velocity.y, -Data.maxFastFallSpeed));
		}
		else if (_isJumpCut)
		{
			//Higher gravity if jump button released
			SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
			Rigidbody.velocity = new Vector2(Rigidbody.velocity.x, Mathf.Max(Rigidbody.velocity.y, -Data.maxFallSpeed));
		}
		else if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(Rigidbody.velocity.y) < Data.jumpHangTimeThreshold)
		{
			SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
		}
		else if (Rigidbody.velocity.y < 0)
		{
			//Higher gravity if falling
			SetGravityScale(Data.gravityScale * Data.fallGravityMult);
			//Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
			Rigidbody.velocity = new Vector2(Rigidbody.velocity.x, Mathf.Max(Rigidbody.velocity.y, -Data.maxFallSpeed));
		}
		else
		{
			//Default gravity if standing on a platform or moving upwards
			SetGravityScale(Data.gravityScale);
		}
		#endregion
    }

    private void FixedUpdate()
	{
		//Handle Run
		if (IsWallJumping)
			Run(Data.wallJumpRunLerp);
		else
			Run(1);

		//Handle Slide
		if (IsSliding)
			Slide();
    }

    #region INPUT CALLBACKS
	//Methods which whandle input detected in Update()
    public void OnJumpInput()
	{
		LastPressedJumpTime = Data.jumpInputBufferTime;
	}

	public void OnJumpUpInput()
	{
		if (CanJumpCut() || CanWallJumpCut())
			_isJumpCut = true;
	}
    #endregion

    #region GENERAL METHODS
    public void SetGravityScale(float scale)
	{
		Rigidbody.gravityScale = scale;
	}
    #endregion

	//MOVEMENT METHODS
    #region RUN METHODS
    private void Run(float lerpAmount)
	{
		//Calculate the direction we want to move in and our desired velocity
		float targetSpeed = _moveInput.x * Data.runMaxSpeed;
		//We can reduce are control using Lerp() this smooths changes to are direction and speed
		targetSpeed = Mathf.Lerp(Rigidbody.velocity.x, targetSpeed, lerpAmount);

		#region Calculate AccelRate
		float accelRate;

		//Gets an acceleration value based on if we are accelerating (includes turning) 
		//or trying to decelerate (stop). As well as applying a multiplier if we're air borne.
		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
		#endregion

		#region Add Bonus Jump Apex Acceleration
		//Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
		if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(Rigidbody.velocity.y) < Data.jumpHangTimeThreshold)
		{
			accelRate *= Data.jumpHangAccelerationMult;
			targetSpeed *= Data.jumpHangMaxSpeedMult;
		}
		#endregion

		#region Conserve Momentum
		//We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
		if(Data.doConserveMomentum && Mathf.Abs(Rigidbody.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(Rigidbody.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
		{
			//Prevent any deceleration from happening, or in other words conserve are current momentum
			//You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
			accelRate = 0; 
		}
		#endregion

		//Calculate difference between current velocity and desired velocity
		float speedDif = targetSpeed - Rigidbody.velocity.x;
		//Calculate force along x-axis to apply to thr player

		float movement = speedDif * accelRate;

		//Convert this to a vector and apply to rigidbody
		Rigidbody.AddForce(movement * Vector2.right, ForceMode2D.Force);

		/*
		 * For those interested here is what AddForce() will do
		 * RB.velocity = new Vector2(RB.velocity.x + (Time.fixedDeltaTime  * speedDif * accelRate) / RB.mass, RB.velocity.y);
		 * Time.fixedDeltaTime is by default in Unity 0.02 seconds equal to 50 FixedUpdate() calls per second
		*/
	}

	private void Turn()
	{
		//stores scale and flips the player along the x axis, 
		Vector3 scale = transform.localScale; 
		scale.x *= -1;
		transform.localScale = scale;

		IsFacingRight = !IsFacingRight;
	}
    #endregion

    #region JUMP METHODS
    private void Jump()
	{
		//Ensures we can't call Jump multiple times from one press
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;

		#region Perform Jump
		//We increase the force applied if we are falling
		//This means we'll always feel like we jump the same amount 
		//(setting the player's Y velocity to 0 beforehand will likely work the same, but I find this more elegant :D)
		float force = Data.jumpForce;
		if (Rigidbody.velocity.y < 0)
			force -= Rigidbody.velocity.y;

		Rigidbody.AddForce(Vector2.up * force, ForceMode2D.Impulse);
		#endregion
	}

	private void WallJump(int dir)
	{
		//Ensures we can't call Wall Jump multiple times from one press
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;

		#region Perform Wall Jump
		Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
		force.x *= dir; //apply force in opposite direction of wall

		if (Mathf.Sign(Rigidbody.velocity.x) != Mathf.Sign(force.x))
			force.x -= Rigidbody.velocity.x;

		if (Rigidbody.velocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
			force.y -= Rigidbody.velocity.y;

		//Unlike in the run we want to use the Impulse mode.
		//The default mode will apply are force instantly ignoring masss
		Rigidbody.AddForce(force, ForceMode2D.Impulse);
		#endregion
	}
	#endregion

	#region OTHER MOVEMENT METHODS
	private void Slide()
	{
		//Works the same as the Run but only in the y-axis
		//THis seems to work fine, buit maybe you'll find a better way to implement a slide into this system
		float speedDif = Data.slideSpeed - Rigidbody.velocity.y;	
		float movement = speedDif * Data.slideAccel;
		//So, we clamp the movement here to prevent any over corrections (these aren't noticeable in the Run)
		//The force applied can't be greater than the (negative) speedDifference * by how many times a second FixedUpdate() is called. For more info research how force are applied to rigidbodies.
		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

		Rigidbody.AddForce(movement * Vector2.up);
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
		return LastOnGroundTime > 0 && !IsJumping;
    }

	private bool CanWallJump()
    {
		return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!IsWallJumping ||
			 (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
	}

	private bool CanJumpCut()
    {
		return IsJumping && Rigidbody.velocity.y > 0;
    }

	private bool CanWallJumpCut()
	{
		return IsWallJumping && Rigidbody.velocity.y > 0;
	}

	public bool CanSlide()
    {
		bool grabPressed = Input.GetButton("Grab");
		
		// Grab 버튼을 누르고 있고, 벽에 닿아있고, 벽점프 중이 아니고, 땅에 닿지 않은 상태
		if (grabPressed && LastOnWallTime > 0 && !IsWallJumping && LastOnGroundTime <= 0){
			if (Time.time - _debugTimer > 0.5f)
				Debug.Log("슬라이딩 가능!");
			return true;
		}
		else
			return false;
	}
    #endregion
}
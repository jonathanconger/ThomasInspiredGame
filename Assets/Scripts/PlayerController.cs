using UnityEngine;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
	Rigidbody2D rigidbody;
	Animator animator;
	SpriteRenderer spriteRenderer;
	
	struct Inputs
	{
		public int RawX;
		public int RawY;
		public float X;
		public float Y;
	}

	private Inputs inputs;

	private void Start()
	{
		rigidbody = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		Goal.eventLevelEnd.AddListener(DisableController);
	}

	private void Update()
	{
		CheckInput();
		Grounding();
		Walking();
		Jumping();
		Dashing();
	}

	#region Input
	bool isFacingLeft;

	private void CheckInput()
	{
		inputs.RawX = (int)Input.GetAxisRaw("Horizontal");
		inputs.RawY = (int)Input.GetAxisRaw("Vertical");
		inputs.X = Input.GetAxis("Horizontal");
		inputs.Y = Input.GetAxis("Vertical");

		isFacingLeft = inputs.RawX != 1 && (inputs.RawX == -1 || isFacingLeft);
		
		if (isFacingLeft)
			spriteRenderer.flipX = true;
		else
			spriteRenderer.flipX = false;
	}
	#endregion

	#region Checks
	[Header("Checks")]
	[SerializeField] LayerMask groundMask;
	[SerializeField] float groundOffset = -1, groundRadius = 0.2f;
	public bool isGrounded;

	private readonly Collider2D[] ground = new Collider2D[1];

	private void Grounding()
	{
		bool grounded = Physics2D.OverlapCircleNonAlloc(transform.position + new Vector3(0, groundOffset), groundRadius, ground, groundMask) > 0;

		if (!isGrounded && grounded)
		{
			isGrounded = true;
			hasDashed = false;
			hasJumped = false;
			moveLerpSpeed = 100;
			transform.SetParent(ground[0].transform);

			animator.SetBool("Grounded", true);
			animator.SetBool("Jumping", false);
			PlaySound(groundedSound);
		}
		else if (isGrounded && !grounded)
		{
			isGrounded = false;
			transform.SetParent(null);
			animator.SetBool("Grounded", false);
		}
	}
	#endregion

	#region Movement
	[Header("Movement")]
	[SerializeField] float moveSpeed = 8;
	[SerializeField] float acceleration = 2;
	[SerializeField] float moveLerpSpeed = 100;

	private void Walking()
	{
		if (isDashing)
			return;

		if(Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
		{
			if (rigidbody.velocity.x > 0)
				inputs.X = 0;
			inputs.X = Mathf.MoveTowards(inputs.X, -1, acceleration * Time.deltaTime);
		}
		else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
		{
			if (rigidbody.velocity.x < 0)
				inputs.X = 0;
			inputs.X = Mathf.MoveTowards(inputs.X, 1, acceleration * Time.deltaTime);
		}
		else
		{
			inputs.X = Mathf.MoveTowards(inputs.X, 0, acceleration * 2 * Time.deltaTime);
		}

		Vector3 velocity = new Vector3(inputs.X * moveSpeed, rigidbody.velocity.y);
		rigidbody.velocity = Vector3.MoveTowards(rigidbody.velocity, velocity, moveLerpSpeed * Time.deltaTime);

		animator.SetBool("Walking", inputs.RawX != 0 && isGrounded);
	}
	#endregion

	#region Jump
	[Header("Jump")]
	[SerializeField] float jumpForce = 15;
	[SerializeField] float fallMultiplier = 7;
	[SerializeField] float jumpVelocityFalloff = 8;
	bool hasJumped;

	private void Jumping()
	{
		if (isDashing)
			return;

		if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z))
		{
			if (!hasJumped)
				PerformJump(new Vector2(rigidbody.velocity.x, jumpForce));
		}

		if (rigidbody.velocity.y < jumpVelocityFalloff || rigidbody.velocity.y > 0 && (!Input.GetKey(KeyCode.Space) && !Input.GetKey(KeyCode.Z)))
			rigidbody.velocity += fallMultiplier * Physics.gravity.y * Vector2.up * Time.deltaTime;
	}

	private void PerformJump(Vector3 direction)
	{
		rigidbody.velocity = direction;
		hasJumped = true;

		animator.SetBool("Jumping", true);
		PlaySound(jumpSound);
	}
	#endregion

	#region Dash
	[Header("Dash")]
	[SerializeField] float dashSpeed = 20;
	[SerializeField] float dashLength = 1;

	float timeStartedDash;
	bool hasDashed;
	bool isDashing;
	Vector3 dashDirection;

	private void Dashing()
	{
		if (Input.GetKeyDown(KeyCode.X) && !hasDashed)
		{
			dashDirection = new Vector3(inputs.RawX, inputs.RawY).normalized;
			
			if (dashDirection == Vector3.zero)
				dashDirection = isFacingLeft ? Vector3.left : Vector3.right;

			isDashing = true;
			hasDashed = true;
			timeStartedDash = Time.time;
			rigidbody.gravityScale = 0;

			PlaySound(dashSound);
		}

		if(isDashing)
		{
			rigidbody.velocity = dashDirection * dashSpeed;

			if (Time.time >= timeStartedDash + dashLength)
			{
				isDashing = false;
				rigidbody.velocity = new Vector2(rigidbody.velocity.x, rigidbody.velocity.y > 2 ? 2 : rigidbody.velocity.y);
				rigidbody.gravityScale = 1;

				if (isGrounded)
					hasDashed = false;
			}
		}
	}
	#endregion

	#region Audio
	[Header("Audio")]
	[SerializeField] AudioSource audioSource;
	[SerializeField] AudioClip jumpSound;
	[SerializeField] AudioClip groundedSound;
	[SerializeField] AudioClip dashSound;

	private void PlaySound(AudioClip clip)
	{
		audioSource.clip = clip;
		audioSource.Play();
	}
	#endregion

	public void EnableController()
	{
		this.enabled = true;
	}

	public void DisableController()
	{
		this.enabled = false;
		rigidbody.velocity = Vector2.zero;
	}
}

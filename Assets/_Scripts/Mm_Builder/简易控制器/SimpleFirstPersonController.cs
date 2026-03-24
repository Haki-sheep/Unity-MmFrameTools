using UnityEngine;

/// <summary>
/// 简单第一人称控制器
/// 用于建造系统演示的基础移动和视角控制
/// </summary>
public class SimpleFirstPersonController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("视角设置")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("组件引用")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController characterController;

    // 视角旋转
    private float verticalRotation = 0f;
    
    // 移动向量
    private Vector3 moveDirection;
    private Vector3 velocity;
    
    // 地面检测
    private bool isGrounded;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // 输入状态
    private float currentSpeed;
    private bool isSprinting = false;

    private void Start()
    {
        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 如果没有指定相机，自动获取
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // 如果没有角色控制器，添加一个
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        currentSpeed = moveSpeed;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
        HandleJump();
        ApplyGravity();
    }

    /// <summary>
    /// 处理鼠标视角旋转
    /// </summary>
    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转：旋转整个玩家对象
        transform.Rotate(Vector3.up * mouseX);

        // 垂直旋转：只旋转相机
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    /// <summary>
    /// 处理键盘移动输入
    /// </summary>
    private void HandleMovement()
    {
        // 冲刺检测
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 基于玩家朝向计算移动方向
        moveDirection = transform.right * horizontal + transform.forward * vertical;
        
        // 限制对角线移动速度
        if (moveDirection.magnitude > 1f)
            moveDirection = moveDirection.normalized;

        // 应用移动
        if (characterController != null)
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 处理跳跃
    /// </summary>
    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    /// <summary>
    /// 应用重力
    /// </summary>
    private void ApplyGravity()
    {
        // 地面检测
        isGrounded = Physics.CheckSphere(transform.position - Vector3.up * (characterController.height / 2f), groundCheckDistance, groundLayer);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 保持与地面接触
        }

        // 应用重力
        velocity.y += gravity * Time.deltaTime;
        
        if (characterController != null)
            characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 解锁鼠标（当按下ESC时）
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// 处理ESC键解锁鼠标
    /// </summary>
    private void OnGUI()
    {
        // 按ESC解锁/锁定鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 按下任意鼠标按钮时重新锁定
        if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

}

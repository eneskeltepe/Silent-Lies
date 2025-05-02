using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 5f;
    public float moveSmoothTime = 0.1f;
    public bool useRootMotion = false; // Animasyon kök hareketini kullanıp kullanmama
    
    [Header("Kamera Ayarları")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float lookUpClamp = 80f;
    public KeyCode toggleMouseLockKey = KeyCode.Escape; // Fare kilidini açma/kapama tuşu

    [Header("Yakınlaştırma Ayarları")]
    public bool enableZooming = true;
    public float normalFOV = 60f;
    public float zoomFOV = 30f;
    public float zoomSmoothTime = 0.2f;

    [Header("Animasyon Ayarları")]
    public string walkParameterName = "IsWalking";
    public float animationSmoothTime = 0.1f;

    private CharacterController controller;
    private float verticalRotation = 0f;
    private Vector3 playerVelocity;
    private float gravity = -9.81f;
    private bool isGrounded;
    private Animator animator;
    private float speedSmoothVelocity;
    private float currentAnimationSpeed;
    
    // Hareket yumuşatması için değişkenler
    private Vector2 currentInputVector;
    private Vector2 inputSmoothVelocity;
    
    // Düzeltme için referans yönler
    private Vector3 lastMoveDirection;
    
    // Fare kilidi durumu
    private bool isMouseLocked = true;
    
    // Kamera bileşeni ve yakınlaştırma değişkenleri
    private Camera mainCamera;
    private float currentFOV;
    private float fovSmoothVelocity;

    private void Start()
    {
        // Fare imlecini kilitle
        SetMouseLockState(true);

        // Character Controller bileşenini al
        controller = GetComponent<CharacterController>();

        // Eğer kamera belirtilmediyse ana kamerayı kullan
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
            
        // Kamera bileşenini al
        mainCamera = cameraTransform.GetComponent<Camera>();
        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main;
        }
        
        // Başlangıç FOV değerini ayarla
        if (mainCamera != null)
        {
            normalFOV = mainCamera.fieldOfView;
            currentFOV = normalFOV;
        }

        // Animator bileşenini al
        animator = GetComponent<Animator>();
        
        // Animator bileşeni yoksa konsola uyarı yaz
        if (animator == null)
        {
            Debug.LogWarning("Animator bileşeni bulunamadı! Animasyonlar çalışmayacak.");
        }
        
        // Root motion kullanımını ayarla
        if (animator != null)
        {
            animator.applyRootMotion = useRootMotion;
        }
        
        // Character Controller merkez pozisyonunu doğrula
        if (controller != null)
        {
            // Controller merkezini karakter merkezine hizala
            // Bu değeri karakterinize göre ayarlayabilirsiniz
            controller.center = new Vector3(0, 1f, 0);
        }
        
        lastMoveDirection = transform.forward;
    }

    private void Update()
    {
        // Fare kilidi durumunu kontrol et
        CheckMouseLockToggle();

        // Fare kilitli ise kontrolleri işle
        if (isMouseLocked)
        {
            HandleMouseLook();
            HandleMovement();
            HandleZoom();
        }
    }

    private void CheckMouseLockToggle()
    {
        // Belirtilen tuşa basılırsa fare kilidini aç/kapa
        if (Input.GetKeyDown(toggleMouseLockKey))
        {
            SetMouseLockState(!isMouseLocked);
        }
    }

    private void SetMouseLockState(bool lockMouse)
    {
        isMouseLocked = lockMouse;
        
        // Fare durumunu ayarla
        Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockMouse;
    }

    private void HandleMouseLook()
    {
        // Yatay fare hareketi - karakteri döndür
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up, mouseX);

        // Dikey fare hareketi - kamerayı döndür
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -lookUpClamp, lookUpClamp);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void HandleZoom()
    {
        // Yakınlaştırma etkin değilse işlem yapma
        if (!enableZooming || mainCamera == null) return;
        
        // Sağ tık basılı tutulduğunda yakınlaştır
        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV;
        
        // FOV değerini yumuşak bir şekilde değiştir
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovSmoothVelocity, zoomSmoothTime);
        mainCamera.fieldOfView = currentFOV;
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        
        // Yere değdiysek hızı sıfırla
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        // Hareket yönünü hesapla
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Raw input değerlerini al (anlık değerler)
        Vector2 rawInput = new Vector2(moveX, moveZ);
        
        // Input değerlerini kısıtla - çapraz harekette daha iyi kontrol için
        if (rawInput.magnitude > 1f)
        {
            rawInput.Normalize();
        }
        
        // Yumuşatılmış hareket için SmoothDamp kullan
        currentInputVector = Vector2.SmoothDamp(
            currentInputVector, 
            rawInput, 
            ref inputSmoothVelocity, 
            moveSmoothTime
        );
        
        // Animasyon kontrolü - ham input değeri ile kontrol et
        UpdateAnimation(rawInput.magnitude);

        // Hareket vektörünü oluştur
        Vector3 move;
        
        // Sadece ileri/geri hareketi (W/S tuşları) düzeltme
        if (Mathf.Abs(currentInputVector.y) > 0.1f && Mathf.Abs(currentInputVector.x) < 0.1f)
        {
            // İleri/geri hareketi için sadece kameranın bakış yönünü kullan
            // Ve tam olarak ileri/geri hareketini sağla
            if (currentInputVector.y > 0) // İleri hareket (W tuşu)
            {
                move = transform.forward * currentInputVector.y;
                lastMoveDirection = transform.forward;
            }
            else // Geri hareket (S tuşu)
            {
                move = -transform.forward * -currentInputVector.y;
                lastMoveDirection = -transform.forward;
            }
        }
        else // Yatay hareket veya çapraz hareket
        {
            move = transform.right * currentInputVector.x + transform.forward * currentInputVector.y;
            if (move.magnitude > 0.1f)
            {
                lastMoveDirection = move.normalized;
            }
        }
        
        // Sıfıra yakın değerleri temizle (çok küçük hareketleri önle)
        if (move.magnitude < 0.05f)
        {
            move = Vector3.zero;
        }
        
        // Yakınlaştırma sırasında hareket hızını azalt (isteğe bağlı)
        float speedMultiplier = 1f;
        if (enableZooming && Input.GetMouseButton(1))
        {
            speedMultiplier = 0.6f; // Yakınlaştırma sırasında %60 hızla hareket et
        }
        
        // Root Motion kullanılıyorsa controller hareketi ayarla
        if (useRootMotion && animator != null && animator.applyRootMotion)
        {
            // Controller sadece yerçekimi için kullanılır
            controller.Move(playerVelocity * Time.deltaTime);
        }
        else
        {
            // Normal hareket - karakter controlleri kullan
            controller.Move(move * moveSpeed * speedMultiplier * Time.deltaTime);
            
            // Yerçekimi uygula
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
    }
    
    private void UpdateAnimation(float inputMagnitude)
    {
        if (animator == null) return;
        
        // Hareket edip etmediğini belirle (eşik değerine göre)
        bool isMoving = inputMagnitude > 0.1f;
        
        // Animasyon geçişi için yumuşak değer hesapla
        float targetSpeed = isMoving ? 1f : 0f;
        currentAnimationSpeed = Mathf.SmoothDamp(
            currentAnimationSpeed, 
            targetSpeed, 
            ref speedSmoothVelocity, 
            animationSmoothTime
        );
        
        // Animator parametrelerini güncelle
        animator.SetBool(walkParameterName, isMoving);
        animator.SetFloat("Speed", currentAnimationSpeed);
    }
} 
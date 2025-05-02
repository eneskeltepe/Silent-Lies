using UnityEngine;

public class ToyDragDrop : MonoBehaviour
{
    private Vector3 originalPosition;
    private float originalY;
    private bool isPickedUp = false;
    [SerializeField] float hoverHeight = 0.5f;
    [SerializeField] float followSpeed = 10f;
    
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Rigidbody rb;
    private Collider[] objectColliders; // Nesnenin tüm collider'ları
    
    public Material highlightMaterial;
    public int toyCount = 0;
    
    private Transform playerTransform;
    [SerializeField] private Vector3 holdOffset = new Vector3(0, 1f, 1f);
    
    // Tüm oyuncaklar için ortak tek hedef noktası
    public static GameObject sharedHoldTarget;
    
    // Maksimum bir nesnenin tutulması için statik kontrol
    public static bool isAnyToyHeld = false;
    public static ToyDragDrop currentlyHeldToy = null;
    
    private bool isInRange = false;
    private bool isAtTarget = false;
    private float distanceToTarget = float.MaxValue;
    private bool canBePickedUp = true; // Nesnenin tekrar alınabilir olup olmadığını kontrol etmek için

    void Start()
    {
        originalPosition = transform.position;
        originalY = transform.position.y;
        meshRenderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        
        // Tüm collider'ları kaydet
        objectColliders = GetComponents<Collider>();
        
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
        
        // Tag kontrolü
        if (gameObject.tag != "Toy")
        {
            Debug.LogWarning("Bu nesne 'Toy' tag'ine sahip değil. Kaldırma işlemi çalışmayabilir.");
        }
        
        // Rigidbody kontrolü
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("Rigidbody otomatik olarak eklendi: " + gameObject.name);
        }
        
        // Collider kontrolü
        if (objectColliders.Length == 0)
        {
            // Otomatik collider ekle
            MeshCollider meshCol = gameObject.AddComponent<MeshCollider>();
            meshCol.convex = true; // Rigidbody için convex olmalı
            Debug.Log("MeshCollider otomatik olarak eklendi: " + gameObject.name);
            
            // Collider listesini güncelle
            objectColliders = GetComponents<Collider>();
        }
    }

    void Update()
    {
        // Eğer nesne tutuluyorsa ve E veya G tuşuna basıldıysa
        if (isPickedUp || isAtTarget)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("E veya G tuşuna basıldı, nesne bırakılıyor: " + gameObject.name);
                DropObject();
            }
        }
        // Eğer nesne tutulmuyorsa ve oyuncu menzildeyse ve E tuşuna basıldıysa
        else if (isInRange && Input.GetKeyDown(KeyCode.E) && !isAnyToyHeld && canBePickedUp)
        {
            PickupObject();
            
            // Hedef nokta varsa, doğrudan oraya hareket etmeye başla
            if (sharedHoldTarget != null)
            {
                StartMoveToTarget();
            }
        }
        else if (isInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Zaten başka bir nesne tutuyorsunuz! Önce onu bırakın.");
        }
        
        // Hedef noktaya doğru hareket et
        if (isPickedUp && !isAtTarget && sharedHoldTarget != null)
        {
            MoveTowardsTarget();
        }
        
        // Hedef noktada sabit kal
        if (isPickedUp && isAtTarget && sharedHoldTarget != null)
        {
            // Nesnenin tam olarak hedef noktada kalmasını sağla
            transform.position = sharedHoldTarget.transform.position;
            transform.rotation = sharedHoldTarget.transform.rotation;
        }
    }
    
    private void PickupObject()
    {
        if (rb == null) return;
        
        isPickedUp = true;
        isAtTarget = false;
        
        // Statik kontrolleri güncelle - bu nesne şu an tutuluyor
        isAnyToyHeld = true;
        currentlyHeldToy = this;
        
        // Fiziği devre dışı bırak
        rb.isKinematic = true;
        rb.useGravity = false; // Yerçekimini kapat
        
        // Collider'ları aktif et (eğer devre dışı kalmışsa)
        SetCollidersEnabled(true);
        
        // Malzemeyi güncelle
        if (meshRenderer != null && highlightMaterial != null)
        {
            meshRenderer.material = highlightMaterial;
        }
        
        Debug.Log("NESNE TUTULDU: " + gameObject.name);
    }
    
    private void StartMoveToTarget()
    {
        if (rb == null || sharedHoldTarget == null) return;
        
        // Fiziği devre dışı bırak
        rb.isKinematic = true;
        rb.useGravity = false; // Yerçekimini kapat
        
        Debug.Log("NESNE HEDEFE GÖTÜRÜLÜYOR: " + gameObject.name);
    }
    
    private void MoveTowardsTarget()
    {
        // Hedef noktasına yumuşak bir şekilde hareket ettir
        transform.position = Vector3.Lerp(transform.position, sharedHoldTarget.transform.position, Time.deltaTime * followSpeed * 2);
        
        // Hedef noktasına yaklaştık mı kontrol et
        distanceToTarget = Vector3.Distance(transform.position, sharedHoldTarget.transform.position);
        if (distanceToTarget < 0.05f)
        {
            // Tam olarak hedef noktasına yerleştir
            transform.position = sharedHoldTarget.transform.position;
            transform.rotation = sharedHoldTarget.transform.rotation;
            
            // Artık hedefteyiz
            isAtTarget = true;
            
            // Hedef noktada collider'ları devre dışı bırak
            SetCollidersEnabled(false);
            
            // Nesne halen tutulmuş durumda (isPickedUp = true) ve hedefte (isAtTarget = true)
            // E tuşuna basılana kadar bu konumda kalacak
            
            Debug.Log("NESNE HEDEFE YERLEŞTİRİLDİ VE TUTULMAYA DEVAM EDİYOR: " + gameObject.name);
        }
    }
    
    private void DropObject()
    {
        if (rb == null) return;
        
        Debug.Log("DropObject çağrıldı: " + gameObject.name);
        
        isPickedUp = false;
        isAtTarget = false;
        
        // Statik kontrolleri güncelle - artık hiçbir nesne tutulmuyor
        if (currentlyHeldToy == this)
        {
            isAnyToyHeld = false;
            currentlyHeldToy = null;
        }
        
        // Fiziği tekrar aktif et - yer çekimi etkisiyle düşmesini sağlar
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Hedef noktadan tamamen çıkar
        transform.parent = null;
        
        // Düşerken rastgele bir itme kuvveti uygula (opsiyonel)
        rb.AddForce(new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)), ForceMode.Impulse);
        
        // Collider'ları tekrar aktif et
        SetCollidersEnabled(true);
        
        // Orijinal malzemeye geri dön
        if (meshRenderer != null)
        {
            meshRenderer.material = originalMaterial;
        }
        
        // Yeni pozisyonu kaydet
        originalPosition = new Vector3(transform.position.x, originalY, transform.position.z);
        
        Debug.Log("NESNE BIRAKILDI (YER ÇEKİMİ AKTİF): " + gameObject.name);
    }
    
    // Tüm collider'ları aktif/devre dışı bırakmak için yardımcı metod
    private void SetCollidersEnabled(bool enabled)
    {
        if (objectColliders == null || objectColliders.Length == 0) return;
        
        foreach (Collider col in objectColliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
        
        if (enabled)
        {
            Debug.Log("NESNE COLLİDER'LARI AKTİF: " + gameObject.name);
        }
        else
        {
            Debug.Log("NESNE COLLİDER'LARI DEVRE DIŞI: " + gameObject.name);
        }
    }

    // Çarpışma kontrolü - fiziksel çarpışma için
    private void OnCollisionEnter(Collision collision)
    {
        // Player tag'ine sahip bir nesne ile çarpışma olduğunda
        if (collision.gameObject.CompareTag("Player"))
        {
            isInRange = true;
            playerTransform = collision.transform;
            Debug.Log("OYUNCUYLA Çarpışma tespit edildi: " + collision.gameObject.name);
        }
        
        if (gameObject.tag == "Toy")
        {
            if (collision.gameObject.CompareTag("Toy"))
            {
                Debug.Log("Çarpışma tespit edildi: " + collision.gameObject.name);
                toyCount++;
            }
        }
    }
    
    // Tetikleyici kontrolü - trigger olarak ayarlanmış collider'lar için
    private void OnTriggerEnter(Collider other)
    {
        // Player tag'ine sahip bir nesne ile temas olduğunda
        if (other.CompareTag("Player"))
        {
            isInRange = true;
            playerTransform = other.transform;
            Debug.Log("OYUNCUYLA Tetikleyici tespit edildi: " + other.gameObject.name);
        }
        
        if (gameObject.tag == "Toy")
        {
            if (other.CompareTag("Toy"))
            {
                Debug.Log("Tetikleyici tespit edildi: " + other.gameObject.name);
                toyCount++;
            }
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        // Player tag'ine sahip bir nesne ile çarpışma bittiğinde
        if (collision.gameObject.CompareTag("Player"))
        {
            isInRange = false;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Player tag'ine sahip bir nesne ile temas bittiğinde
        if (other.CompareTag("Player"))
        {
            isInRange = false;
        }
    }
    
    // Uygulama kapandığında veya nesne yok edildiğinde statik değişkenleri temizle
    private void OnDestroy()
    {
        if (currentlyHeldToy == this)
        {
            isAnyToyHeld = false;
            currentlyHeldToy = null;
        }
    }
} 
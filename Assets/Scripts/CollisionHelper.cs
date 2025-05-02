using UnityEngine;

public class CollisionHelper : MonoBehaviour
{
    [Tooltip("Tetik alanının boyutu")]
    [SerializeField] private Vector3 triggerSize = new Vector3(2f, 2f, 2f);
    
    private SphereCollider triggerCollider;
    
    void Start()
    {
        // Eğer bu obje Player tag'ine sahipse trigger oluştur
        if (gameObject.CompareTag("Player"))
        {
            // Eğer zaten bir trigger collider varsa, onu kullan
            triggerCollider = GetComponent<SphereCollider>();
            
            if (triggerCollider == null)
            {
                // Yeni bir trigger collider oluştur
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            
            // Collider'ı trigger olarak ayarla
            triggerCollider.isTrigger = true;
            
            // Boyutu ayarla (varsayılan olarak 2 birim)
            triggerCollider.radius = triggerSize.x / 2f;
            
            Debug.Log("Player için trigger collider oluşturuldu.");
        }
        else
        {
            Debug.LogWarning("Bu script sadece 'Player' tag'ine sahip nesnelerde çalışır.");
        }
    }
    
    // İstersen yardımcı olmak için gizmo çizebilirsin
    void OnDrawGizmos()
    {
        // Trigger alanını görselleştir
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Yarı saydam yeşil
        Gizmos.DrawSphere(transform.position, triggerSize.x / 2f);
    }
} 
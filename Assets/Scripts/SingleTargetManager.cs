using UnityEngine;

public class SingleTargetManager : MonoBehaviour
{
    [Tooltip("Tüm oyuncaklar için ortak kullanılacak tek hedef noktası")]
    [SerializeField] private GameObject targetObject;
    
    [Tooltip("Hedef noktasını görünür kılmak için Gizmo çiz")]
    [SerializeField] private bool showGizmo = true;
    
    [Tooltip("Gizmo rengi")]
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    [Tooltip("Gizmo boyutu")]
    [SerializeField] private float gizmoSize = 0.5f;
    
    [Header("Hedef Nokta Görünürlüğü")]
    [Tooltip("Hedef noktasını görselle belirtmek için kullanılır")]
    [SerializeField] private bool showTargetVisual = true;
    
    [Tooltip("Hedef noktasını gösterecek 3D model (boş bırakılırsa küp oluşturulur)")]
    [SerializeField] private GameObject targetVisualPrefab;
    
    private GameObject targetVisual;
    
    void Start()
    {
        SetupTargetObject();
        CreateTargetVisual();
    }
    
    void SetupTargetObject()
    {
        // Hedef objesi tanımlanmışsa, tüm ToyDragDrop nesnelerine bu hedefi ata
        if (targetObject != null)
        {
            // Statik değişkene hedefi ata
            ToyDragDrop.sharedHoldTarget = targetObject;
            
            Debug.Log("Tüm oyuncaklar için ortak hedef noktası atandı: " + targetObject.name);
        }
        else
        {
            // Otomatik olarak bir hedef objesi oluştur
            targetObject = new GameObject("DefaultTargetPoint");
            targetObject.transform.position = new Vector3(0, 1, 0); // Varsayılan konum
            targetObject.transform.parent = transform;
            
            // Statik değişkene hedefi ata
            ToyDragDrop.sharedHoldTarget = targetObject;
            
            Debug.Log("Varsayılan hedef noktası oluşturuldu ve atandı.");
        }
    }
    
    void CreateTargetVisual()
    {
        if (showTargetVisual && targetObject != null)
        {
            if (targetVisualPrefab != null)
            {
                // Prefab'ı kullanarak görsel oluştur
                targetVisual = Instantiate(targetVisualPrefab, targetObject.transform.position, targetObject.transform.rotation);
                targetVisual.transform.parent = targetObject.transform;
                
                // Prefab'ın görünürlüğünü kapat
                Renderer rendererPrefab = targetVisual.GetComponent<Renderer>();
                if (rendererPrefab != null)
                {
                    rendererPrefab.enabled = false;
                }
            }
            else
            {
                // Basit bir küp oluştur
                targetVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                targetVisual.transform.position = targetObject.transform.position;
                targetVisual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // Küçük bir küp
                targetVisual.transform.parent = targetObject.transform;
                
                // Renderer bileşenini al ve kapat
                Renderer renderer = targetVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false; // Renderer'ı kapat, böylece küp görünmez olur
                }
                
                // Yerleştirme noktasını göstermeye yarayan bir görsel, fiziksel etkileşime girmemeli
                Collider collider = targetVisual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }
            
            Debug.Log("Hedef noktası için görsel oluşturuldu (görünmez).");
        }
    }
    
    // Editor'da ve çalışma zamanında hedef noktasını görselleştir
    void OnDrawGizmos()
    {
        if (showGizmo && targetObject != null)
        {
            // Mevcut Gizmo rengini kaydet
            Color oldColor = Gizmos.color;
            
            // Hedef noktasında bir küre çiz
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(targetObject.transform.position, gizmoSize);
            
            // Gizmo rengini eski haline getir
            Gizmos.color = oldColor;
        }
    }
} 
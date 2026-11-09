using UnityEngine;

/// <summary>
/// DroppedPackageItem: Objek 3D kardus paket yang terjatuh di area parkir saat Misi 4.
/// Dapat dipungut oleh Rian menggunakan tombol [E] saat berada di dekatnya.
/// </summary>
public class DroppedPackageItem : MonoBehaviour
{
    [Tooltip("Radius jarak interaksi pemungutan paket.")]
    public float interactionRadius = 2.0f;

    private Transform playerTransform;

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Rotasi mengambang / bobbing halus agar mudah dikenali di area parkir
        transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= interactionRadius)
        {
            if (NPCDialogueOverlay.IsDialogueActive) return;

            DeliveryHUD hud = DeliveryHUD.Instance;
            if (hud != null)
            {
                hud.RegisterNearbyInteractable("Ambil Paket yang Terjatuh", () =>
                {
                    PickUp();
                });
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                PickUp();
            }
        }
    }

    public void PickUp()
    {
        if (NPCDialogueOverlay.IsDialogueActive) return;

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.PickUpDroppedPackage();
        }

        gameObject.SetActive(false);
    }
}

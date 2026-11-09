using UnityEngine;

/// <summary>
/// DroppedPackageItem: Objek 3D kardus paket yang terjatuh di area parkir saat Misi 4.
/// Dapat dipungut oleh Rian menggunakan tombol [E] saat berada di dekatnya.
/// </summary>
public class DroppedPackageItem : MonoBehaviour
{
    [Tooltip("Radius jarak interaksi pemungutan paket.")]
    public float interactionRadius = 3.0f;

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
            return;
        }

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerTransform = playerGo.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Rotasi mengambang perlahan agar mudah dikenali di sela mobil area parkir
        transform.Rotate(Vector3.up, 50f * Time.deltaTime, Space.World);

        // Gunakan jarak horizontal 2D (XZ) agar perbedaan elevasi tanah tidak menghalangi
        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
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


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

    private void Awake()
    {
        // Pastikan scale kotak kardus tepat 0.1 di seluruh sumbu (X, Y, Z)
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    private void Start()
    {
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
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

        // Pastikan scale kotak kardus selalu tepat 0.1 di seluruh sumbu sesuai gambar
        if (transform.localScale != new Vector3(0.1f, 0.1f, 0.1f))
        {
            transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }

        // Gunakan jarak horizontal 2D (XZ) agar perbedaan elevasi tanah tidak menghalangi
        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
        if (dist <= interactionRadius)
        {
            if (NPCDialogueOverlay.IsDialogueActive) return;

            DeliveryGameManager gm = DeliveryGameManager.Instance;
            bool canPickup = (gm == null || gm.CurrentState == DeliveryMissionState.Package4_DroppedPackage || gm.CurrentState == DeliveryMissionState.Package3_HardwareStore);

            if (canPickup)
            {
                DeliveryHUD hud = DeliveryHUD.Instance;
                if (hud != null)
                {
                    // Prioritas tinggi (10) agar tidak tertimpa oleh NPC sekitar (misal Dimas)
                    hud.RegisterNearbyInteractable("Ambil Paket yang Terjatuh", () =>
                    {
                        PickUp();
                    }, priority: 10);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUp();
                }
            }
            else if (gm != null && (int)gm.CurrentState < (int)DeliveryMissionState.Package4_DroppedPackage)
            {
                DeliveryHUD hud = DeliveryHUD.Instance;
                if (hud != null)
                {
                    hud.RegisterNearbyInteractable("Periksa Paket Terjatuh", () =>
                    {
                        NPCDialogueOverlay.ShowSequentialDialogue(transform, "Kotak Paket", new string[]
                        {
                            "Ini salah satu kotak paket kiriman yang terjatuh!",
                            "Selesaikan pengiriman paket sebelumnya terlebih dahulu."
                        });
                    });
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    NPCDialogueOverlay.ShowSequentialDialogue(transform, "Kotak Paket", new string[]
                    {
                        "Ini salah satu kotak paket kiriman yang terjatuh!",
                        "Selesaikan pengiriman paket sebelumnya terlebih dahulu."
                    });
                }
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


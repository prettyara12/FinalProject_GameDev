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

    private void Awake()
    {
    }

    private void Start()
    {
        FindPlayer();

        // Paket tersembunyi hingga diungkap oleh Petugas Parkir saat Misi 4,
        // dan harus tetap tersembunyi jika sudah pernah dipungut
        DeliveryGameManager gm = DeliveryGameManager.Instance;
        bool shouldBeVisible = (gm != null && gm.IsDroppedPackageRevealed && !gm.IsDroppedPackagePickedUp);
        SetPackageVisible(shouldBeVisible);
    }

    public void SetPackageVisible(bool visible)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = visible;
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = visible;

        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
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

        DeliveryGameManager gm = DeliveryGameManager.Instance;

        // Jika paket sudah dipungut, pastikan objek benar-benar tidak aktif dan keluar dari Update
        if (gm != null && gm.IsDroppedPackagePickedUp)
        {
            SetPackageVisible(false);
            return;
        }

        // Jika belum diungkap oleh Petugas Parkir, sembunyikan objek dan keluar
        if (gm != null && !gm.IsDroppedPackageRevealed)
        {
            SetPackageVisible(false);
            return;
        }
        else if (gm != null && gm.IsDroppedPackageRevealed && !gm.IsDroppedPackagePickedUp)
        {
            // Pastikan renderer aktif terlihat di aspal
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (!r.enabled) r.enabled = true;
            }
        }

        // Gunakan jarak horizontal 2D (XZ) agar perbedaan elevasi tanah tidak menghalangi
        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
        if (dist <= interactionRadius)
        {
            if (NPCDialogueOverlay.IsDialogueActive) return;

            bool canPickup = (gm == null || (gm.CurrentState == DeliveryMissionState.Package4_DroppedPackage && gm.IsDroppedPackageRevealed && !gm.IsDroppedPackagePickedUp));

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

        // Matikan langsung seluruh visual renderer & collider agar seketika lenyap dari jalan aspal
        SetPackageVisible(false);

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.PickUpDroppedPackage();
        }
    }
}


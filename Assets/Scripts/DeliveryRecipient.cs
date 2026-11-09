using UnityEngine;

/// <summary>
/// DeliveryRecipient: Komponen penanda karakter NPC yang menjadi tujuan pengiriman atau pemberi petunjuk.
/// Mendukung interaksi tombol [E] ketika didekati oleh Rian.
/// </summary>
public class DeliveryRecipient : MonoBehaviour
{
    public enum RecipientType
    {
        PakBeni,
        Clara,
        PakJoko,
        Dimas,
        Naya,
        PetugasParkir,
        AmbientCitizen
    }

    [Header("=== Profil Karakter ===")]
    public string characterName = "Warga Kota";
    public RecipientType recipientType = RecipientType.AmbientCitizen;

    [Header("=== Konfigurasi Misi ===")]
    [Tooltip("Target misi di mana penerima ini menerima paket yang benar.")]
    public DeliveryMissionState targetForMission = DeliveryMissionState.Prolog;

    [Tooltip("Radius jarak agar pemain dapat berinteraksi dengan tombol [E].")]
    public float interactionRadius = 3.5f;

    [Header("=== Dialog Percakapan ===")]
    [TextArea(2, 4)]
    public string[] introDialogueLines;

    [TextArea(2, 4)]
    public string[] successDialogueLines;

    [TextArea(2, 4)]
    public string[] wrongDeliveryLines;

    private Transform playerTransform;
    private Animator anim;

    private void Awake()
    {
        // Hapus komponen NPCWalker agar karakter penerima tidak bergerak/patroli
        NPCWalker walker = GetComponent<NPCWalker>();
        if (walker != null)
        {
            Destroy(walker);
        }

        anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.SetFloat("Speed_f", 0f);
            anim.SetInteger("Animation_int", 0);
            anim.SetBool("Static_b", false);
            anim.SetBool("Grounded", true);
            anim.SetBool("Jump_b", false);
            anim.SetBool("Crouch_b", false);
        }
    }

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
        // Kunci parameter animator agar karakter tetap diam sempurna di tempat tanpa menggerakkan kaki
        if (anim != null)
        {
            if (anim.GetFloat("Speed_f") != 0f)
            {
                anim.SetFloat("Speed_f", 0f);
            }
            if (anim.GetInteger("Animation_int") != 0)
            {
                anim.SetInteger("Animation_int", 0);
            }
        }

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Gunakan jarak horizontal 2D (XZ) agar perbedaan elevasi trotoar tidak menghalangi interaksi
        float dist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
        if (dist <= interactionRadius)
        {
            // Jangan trigger jika dialog berurutan sedang aktif
            if (NPCDialogueOverlay.IsDialogueActive) return;

            // Beri tahu HUD bahwa Rian sedang berada dekat NPC ini
            DeliveryHUD hud = DeliveryHUD.Instance;
            if (hud != null)
            {
                string actionLabel = GetInteractionPromptText();
                hud.RegisterNearbyInteractable(actionLabel, () =>
                {
                    OnInteract();
                });
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                OnInteract();
            }
        }
    }

    private string GetInteractionPromptText()
    {
        if (DeliveryGameManager.Instance == null) return "Bicara";

        var state = DeliveryGameManager.Instance.CurrentState;
        if (state == DeliveryMissionState.Prolog && recipientType == RecipientType.PakBeni)
        {
            return "Bicara dengan Pak Beni";
        }
        if (state == DeliveryMissionState.Final_ReturnToHub && recipientType == RecipientType.PakBeni)
        {
            return "Lapor Tugas Selesai";
        }
        if (targetForMission == state)
        {
            return $"Serahkan Paket ke {characterName}";
        }

        return $"Bicara dengan {characterName}";
    }

    public void OnInteract()
    {
        if (NPCDialogueOverlay.IsDialogueActive) return;

        // Hadapkan NPC ke arah player saat bicara
        if (playerTransform != null)
        {
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.InteractWithRecipient(this);
        }
    }
}


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
    private bool hasParamSpeedF = false;
    private bool hasParamAnimationInt = false;
    private bool hasParamStaticB = false;
    private bool hasParamGrounded = false;
    private bool hasParamGroundedB = false;

    private void Awake()
    {
        // Hapus komponen NPCWalker agar karakter penerima tidak bergerak/patroli
        NPCWalker walker = GetComponent<NPCWalker>();
        if (walker != null)
        {
            Destroy(walker);
        }

        CacheAnimatorParams();
    }

    private void Start()
    {
        FindPlayer();
        CacheAnimatorParams();
    }

    public void CacheAnimatorParams()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (anim == null) return;

        hasParamSpeedF = false;
        hasParamAnimationInt = false;
        hasParamStaticB = false;
        hasParamGrounded = false;
        hasParamGroundedB = false;

        if (anim.runtimeAnimatorController != null)
        {
            foreach (var p in anim.parameters)
            {
                if (p.name == "Speed_f") hasParamSpeedF = true;
                else if (p.name == "Animation_int") hasParamAnimationInt = true;
                else if (p.name == "Static_b") hasParamStaticB = true;
                else if (p.name == "Grounded") hasParamGrounded = true;
                else if (p.name == "Grounded_b") hasParamGroundedB = true;
            }
        }

        anim.applyRootMotion = false;
        if (hasParamSpeedF) anim.SetFloat("Speed_f", 0f);
        if (hasParamAnimationInt) anim.SetInteger("Animation_int", 0);
        if (hasParamStaticB) anim.SetBool("Static_b", false);
        if (hasParamGrounded) anim.SetBool("Grounded", true);
        if (hasParamGroundedB) anim.SetBool("Grounded_b", true);
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
            if (hasParamSpeedF && anim.GetFloat("Speed_f") != 0f)
            {
                anim.SetFloat("Speed_f", 0f);
            }
            if (hasParamAnimationInt && anim.GetInteger("Animation_int") != 0)
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

            // Syarat Alur Misi: Di scene kota, Rian harus pergi ke Dimas terlebih dahulu sebelum bertanya ke Petugas Parkir
            // Namun di scene Level 2 Bandara, Tukang Parkir 2 adalah penerima langsung paket 3
            bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
            if (!isAirportLevel2 && recipientType == RecipientType.PetugasParkir)
            {
                DeliveryGameManager gm = DeliveryGameManager.Instance;
                if (gm == null || gm.CurrentState < DeliveryMissionState.Package4_DroppedPackage)
                {
                    return;
                }
            }

            // Saat Misi 4 kota dan paket kardus sudah terungkap di jalan aspal tapi belum diambil
            if (!isAirportLevel2 && (recipientType == RecipientType.Dimas || recipientType == RecipientType.PetugasParkir) &&
                DeliveryGameManager.Instance != null &&
                DeliveryGameManager.Instance.CurrentState == DeliveryMissionState.Package4_DroppedPackage &&
                DeliveryGameManager.Instance.IsDroppedPackageRevealed &&
                !DeliveryGameManager.Instance.IsDroppedPackagePickedUp)
            {
                return;
            }

            // Beri tahu HUD bahwa Rian sedang berada dekat NPC ini (prioritas standar = 1)
            DeliveryHUD hud = DeliveryHUD.Instance;
            if (hud != null)
            {
                string actionLabel = GetInteractionPromptText();
                int priority = (DeliveryGameManager.Instance != null && DeliveryGameManager.Instance.CurrentState == targetForMission) ? 5 : 1;
                hud.RegisterNearbyInteractable(actionLabel, () =>
                {
                    OnInteract();
                }, priority);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                OnInteract();
            }
        }
    }

    private string GetInteractionPromptText()
    {
        var state = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.CurrentState : DeliveryMissionState.Prolog;

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (!isAirportLevel2 && recipientType == RecipientType.PetugasParkir)
        {
            if (state == DeliveryMissionState.Package4_DroppedPackage)
            {
                return "Bertanya kepada Petugas Parkir";
            }
            return "Bicara dengan Petugas Parkir";
        }

        if (DeliveryGameManager.Instance == null) return "Bicara";
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

using UnityEngine;

/// <summary>
/// Robust Player Controller for exploring the city.
/// Supports W, A, S, D and Arrow keys for movement, Shift to sprint.
/// Works reliably without physics/CharacterController bugs.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== Movement Settings ===")]
    [Tooltip("Walking speed in world units (Skala 0.1).")]
    public float walkSpeed = 0.45f;

    [Tooltip("Sprint speed when holding Left Shift (Skala 0.1).")]
    public float sprintSpeed = 0.95f;

    [Tooltip("How fast the character rotates towards movement direction.")]
    public float rotationSpeed = 12f;

    [Tooltip("Jump height.")]
    public float jumpHeight = 0.25f;

    [Header("=== Haus (Hidrasi) ===")]
    [Tooltip("Hidrasi maksimum. Turun saat Rian bergerak, terutama saat berlari.")]
    public float maxHydration = 100f;

    [Tooltip("Hidrasi saat ini. Naik saat membeli minuman di vending machine.")]
    public float hydration = 100f;

    [Tooltip("Penurunan hidrasi per detik saat berjalan.")]
    public float walkThirstPerSecond = 0.35f;

    [Tooltip("Penurunan hidrasi per detik saat berlari.")]
    public float sprintThirstPerSecond = 1.8f;

    [Tooltip("Penurunan hidrasi per detik saat diam.")]
    public float idleThirstPerSecond = 0.08f;

    [Tooltip("Di bawah nilai ini Rian haus: kecepatan lari menurun bertahap sampai sama dengan berjalan.")]
    public float thirstyThreshold = 40f;

    public float Hydration01 => maxHydration > 0f ? Mathf.Clamp01(hydration / maxHydration) : 0f;
    public bool IsThirsty => hydration <= thirstyThreshold;
    public bool IsExhausted => hydration <= 0.5f;
    private bool thirstWarned;
    private bool exhaustedWarned;

    /// <summary>Menambah hidrasi (mis. setelah membeli minuman).</summary>
    public void Drink(float amount)
    {
        hydration = Mathf.Clamp(hydration + amount, 0f, maxHydration);
        if (hydration > thirstyThreshold) thirstWarned = false;
        if (hydration > 0.5f) exhaustedWarned = false;
    }

    [Header("=== References ===")]
    [Tooltip("Reference to the camera to move relative to view direction.")]
    public Camera playerCamera;

    private Animator animator;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;
    private const float gravity = -9.81f;

    // Animator parameter hashes for SimpleCharacter_5.0 and SimpleCitizens
    private static readonly int ParamSpeedF = Animator.StringToHash("Speed_f");
    private static readonly int ParamGrounded = Animator.StringToHash("Grounded");
    private static readonly int ParamGroundedB = Animator.StringToHash("Grounded_b");
    private static readonly int ParamStaticB = Animator.StringToHash("Static_b");
    private static readonly int ParamJumpB = Animator.StringToHash("Jump_b");
    private static readonly int ParamAnimationInt = Animator.StringToHash("Animation_int");

    private bool hasParamSpeedF = false;
    private bool hasParamGrounded = false;
    private bool hasParamGroundedB = false;
    private bool hasParamStaticB = false;
    private bool hasParamJumpB = false;
    private bool hasParamAnimationInt = false;

    [Header("=== Collision & Obstacle Settings ===")]
    [Tooltip("Radius kapsul tabrakan badan karakter.")]
    public float bodyRadius = 0.035f;

    [Tooltip("Tinggi kapsul tabrakan badan karakter.")]
    public float bodyHeight = 0.18f;

    [Tooltip("Tinggi maksimal undakan/trotoar yang bisa dinaiki otomatis.")]
    public float maxStepHeight = 0.02f;

    [Tooltip("Jarak aman permukaan untuk mencegah rounding error.")]
    public float skinWidth = 0.003f;

    [Tooltip("Layer rintangan yang tidak bisa ditembus.")]
    public LayerMask obstacleMask = ~0;

    [Header("=== State ===")]
    [Tooltip("Is the character currently sitting (Toggle with C or X key).")]
    public bool isSitting = false;

    [Header("=== Freeze & Hazard State ===")]
    [Tooltip("Is movement frozen (e.g. during sequential dialogues).")]
    public bool isMovementFrozen = false;
    private float hazardStumbleTimer = 0f;

    public void SetMovementFrozen(bool frozen)
    {
        isMovementFrozen = frozen;
        if (frozen && animator != null && hasParamSpeedF)
        {
            animator.SetFloat(ParamSpeedF, 0f);
        }
    }

    public void ApplyKnockback(Vector3 force, float stumbleDuration = 0.7f)
    {
        hazardStumbleTimer = stumbleDuration;
        transform.position += force * Time.deltaTime;
        ResolveDepenetration();
    }

    private CapsuleCollider ownCapsule;
    private Collider[] ownColliders;
    private string lastBlockedObstacle = null;
    private float lastBlockedTime = -999f;
    private bool isTryingToMove = false;
    private float airTime = 0f;
    private string lastBumpLabel = null;
    private float lastBumpNoticeTime = -999f;

    /// <summary>
    /// Mencatat benturan NYATA (kapsul Rian benar-benar menyentuh collider) dan menampilkan
    /// pemberitahuan "Anda menabrak ..." sesuai benda yang tersentuh. Hanya saat pemain sedang berjalan.
    /// </summary>
    private void RegisterBump(Collider hitCollider)
    {
        if (hitCollider == null || !isTryingToMove) return;

        lastBlockedObstacle = hitCollider.gameObject.name;
        lastBlockedTime = Time.time;

        string label = ObstacleNaming.GetFriendlyName(hitCollider);
        if (label == lastBumpLabel && Time.time - lastBumpNoticeTime < 1.6f) return;

        lastBumpLabel = label;
        lastBumpNoticeTime = Time.time;
        GameAudioManager.Instance?.PlayBump();
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.ShowBumpNotice("Anda menabrak " + label);
        }
    }
    private float footstepTimer = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ownCapsule = GetComponent<CapsuleCollider>();
        ownColliders = GetComponentsInChildren<Collider>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Matikan CharacterController bawaan jika ada agar tidak bentrok
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // Matikan NavMeshAgent bawaan agar tidak mengunci koordinat player saat WASD
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // Matikan motor dan controller mobile bawaan agar tidak bentrok dengan WASD
        MovableObjectMotor motor = GetComponent<MovableObjectMotor>();
        if (motor != null)
        {
            motor.enabled = false;
        }

        PlayerAvatarController pac = GetComponent<PlayerAvatarController>();
        if (pac != null)
        {
            pac.enabled = false;
        }

        // Pastikan Rigidbody isKinematic agar kalkulasi manual tidak di-override fisika
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Matikan applyRootMotion agar pergerakan input presisi
        if (animator != null)
        {
            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.nameHash == ParamSpeedF) hasParamSpeedF = true;
                    else if (p.nameHash == ParamGrounded) hasParamGrounded = true;
                    else if (p.nameHash == ParamGroundedB) hasParamGroundedB = true;
                    else if (p.nameHash == ParamStaticB) hasParamStaticB = true;
                    else if (p.nameHash == ParamJumpB) hasParamJumpB = true;
                    else if (p.nameHash == ParamAnimationInt) hasParamAnimationInt = true;
                }
            }
        }

        // Sesuaikan parameter kecepatan dan ukuran jika di skala normal (lossyScale ~ 1.0)
        float currentScaleY = Mathf.Abs(transform.lossyScale.y);
        if (currentScaleY > 0.5f && walkSpeed < 1.0f)
        {
            walkSpeed = 4.5f;
            sprintSpeed = 8.0f;
            jumpHeight = 1.2f;
            maxStepHeight = 0.45f;
            bodyRadius = 0.35f;
            bodyHeight = 1.8f;
            skinWidth = 0.02f;
        }
        else if (ownCapsule != null)
        {
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            bodyRadius = Mathf.Max(0.035f, ownCapsule.radius * scaleX);
            bodyHeight = Mathf.Max(0.16f, ownCapsule.height * currentScaleY);
            maxStepHeight = 0.02f;
        }

        if (animator != null)
        {
            if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
            if (hasParamGroundedB) animator.SetBool(ParamGroundedB, true);
            if (hasParamStaticB) animator.SetBool(ParamStaticB, true);
        }
    }

    private void Start()
    {
        if (transform.lossyScale.y > 0.5f && walkSpeed < 1.0f)
        {
            walkSpeed = 4.5f;
            sprintSpeed = 8.5f;
            jumpHeight = 1.2f;
        }
        SnapToGround();
        ResolveDepenetration();
    }

    private void Update()
    {
        // Safety Unfreeze: Jika game sedang berjalan aktif tanpa dialog atau menu, pastikan pemain tidak terkunci
        if (isMovementFrozen)
        {
            bool hasBlockingUI = false;
            if (DeliveryHUD.Instance != null && DeliveryHUD.Instance.IsMainMenuOpen) hasBlockingUI = true;
            if (NPCDialogueOverlay.IsDialogueActive) hasBlockingUI = true;
            if (VendingMenu.IsOpen) hasBlockingUI = true;
            if (RianDeliveryCutscene.Instance != null && RianDeliveryCutscene.Instance.IsPlaying) hasBlockingUI = true;
            if (KurirIntroCutscene.IsPlaying) hasBlockingUI = true;

            if (!hasBlockingUI && DeliveryGameManager.Instance != null && DeliveryGameManager.Instance.IsGameStarted)
            {
                isMovementFrozen = false;
            }
        }

        HandleMovement();
    }

    /// <summary>Menurunkan hidrasi selama permainan berjalan dan memberi peringatan saat mulai haus.</summary>
    private void UpdateHydration(bool moving, bool sprinting)
    {
        DeliveryGameManager gm = DeliveryGameManager.Instance;
        if (gm == null || !gm.IsGameStarted) return;

        float rate = sprinting ? sprintThirstPerSecond : (moving ? walkThirstPerSecond : idleThirstPerSecond);
        hydration = Mathf.Max(0f, hydration - rate * Time.deltaTime);

        if (!thirstWarned && hydration <= thirstyThreshold)
        {
            thirstWarned = true;
            DeliveryHUD.Instance?.ShowBumpNotice("Rian mulai haus. Lari melambat, cari vending machine!");
        }
        if (!exhaustedWarned && IsExhausted)
        {
            exhaustedWarned = true;
            DeliveryHUD.Instance?.ShowBumpNotice("Rian kehausan! Tidak bisa berlari. Beli minuman di vending machine.");
        }
    }

    private void OnDisable()
    {
        if (animator != null && animator.runtimeAnimatorController != null && hasParamSpeedF)
        {
            animator.SetFloat(ParamSpeedF, 0f);
        }
    }

    private void HandleMovement()
    {
        // Pastikan input selalu aktif
        InputManager.bCanPlayerMove = true;
        isTryingToMove = false;

        // Kunci input hanya jika sedang mode jeda / dialog aktif
        if (isMovementFrozen)
        {
            if (animator != null && hasParamSpeedF)
            {
                animator.SetFloat(ParamSpeedF, 0f);
            }
            return;
        }

        if (hazardStumbleTimer > 0f)
        {
            hazardStumbleTimer -= Time.deltaTime;
        }

        // 1. Read input axes (with direct KeyCode fallback for 100% responsiveness)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical = -1f;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Haus: tanpa hidrasi Rian tidak bisa berlari, hanya berjalan
        if (IsExhausted) isSprinting = false;

        // 2. Calculate movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camRight = playerCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * vertical + camRight * horizontal).normalized;
        }
        else
        {
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }

        // 0. Sitting toggle (Key C or Key X)
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.X))
        {
            isSitting = !isSitting;
            if (isSitting)
            {
                verticalVelocity = 0f;
            }
            if (animator != null && hasParamAnimationInt)
            {
                animator.SetInteger(ParamAnimationInt, isSitting ? 9 : 0);
            }
        }

        float inputMagnitude = new Vector2(horizontal, vertical).magnitude;
        inputMagnitude = Mathf.Clamp01(inputMagnitude);
        isTryingToMove = inputMagnitude > 0.05f;

        // If sitting and player presses movement or jump, automatically stand up
        if (isSitting && (inputMagnitude > 0.05f || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            isSitting = false;
            if (animator != null && hasParamAnimationInt)
            {
                animator.SetInteger(ParamAnimationInt, 0);
            }
        }

        // If currently sitting, stay in place and keep sitting animation
        if (isSitting)
        {
            if (animator != null)
            {
                if (hasParamSpeedF) animator.SetFloat(ParamSpeedF, 0f);
                if (hasParamAnimationInt) animator.SetInteger(ParamAnimationInt, 9);
                if (hasParamGrounded) animator.SetBool(ParamGrounded, true);
                if (hasParamGroundedB) animator.SetBool(ParamGroundedB, true);
                if (hasParamStaticB) animator.SetBool(ParamStaticB, true);
            }
            return;
        }

        // 3. Determine current speed
        // Saat haus, kecepatan lari turun bertahap mendekati kecepatan jalan
        float sprintFactor = thirstyThreshold > 0f ? Mathf.Clamp01(hydration / thirstyThreshold) : 1f;
        float effectiveSprint = Mathf.Lerp(walkSpeed * 1.05f, sprintSpeed, sprintFactor);
        UpdateHydration(inputMagnitude > 0.05f, isSprinting);

        float targetSpeed = (isSprinting ? effectiveSprint : walkSpeed) * (hazardStumbleTimer > 0f ? 0.45f : 1.0f);
        float currentMoveSpeed = targetSpeed * inputMagnitude;

        // 4. Smooth rotation towards movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 facingDirection = moveDirection;

            // Jika berjalan mundur (tombol S), tetap menghadap ke depan (searah pandang kamera)
            // agar kamera di belakang tetap stabil dan tidak berputar bolak-balik 180 derajat
            if (vertical < -0.1f && playerCamera != null)
            {
                Vector3 forwardRef = playerCamera.transform.forward;
                forwardRef.y = 0f;
                if (forwardRef.sqrMagnitude > 0.001f)
                {
                    Vector3 sideOffset = playerCamera.transform.right * horizontal * 0.5f;
                    facingDirection = (forwardRef + sideOffset).normalized;
                }
            }

            Quaternion targetRot = Quaternion.LookRotation(facingDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 5. Jump & Gravity
        if (isGrounded)
        {
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isGrounded = false;
                GameAudioManager.Instance?.PlayJump();
                if (animator != null && hasParamJumpB) animator.SetBool(ParamJumpB, true);
            }
            else
            {
                if (animator != null && hasParamJumpB) animator.SetBool(ParamJumpB, false);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 6. Anti-Tembus Dinding: Selesaikan penetrasi sebelum bergerak
        ResolveDepenetration();

        // 7. Pergerakan Horizontal dengan Swept Capsule & Wall Sliding
        Vector3 horizontalStep = moveDirection * currentMoveSpeed * Time.deltaTime;
        if (horizontalStep.sqrMagnitude > 0.0000001f)
        {
            MoveWithCollision(horizontalStep);
        }

        // 8. Pergerakan Vertikal (Lompat / Gravitasi) dengan cek benturan kepala
        if (Mathf.Abs(verticalVelocity) > 0.0001f)
        {
            if (verticalVelocity > 0f)
            {
                // Cek jika kepala membentur atap
                if (Physics.SphereCast(transform.position + Vector3.up * (bodyHeight * 0.5f), bodyRadius, Vector3.up, out RaycastHit ceilHit, (bodyHeight * 0.5f) + 0.01f, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (!ceilHit.collider.isTrigger && !ceilHit.collider.transform.IsChildOf(transform))
                    {
                        verticalVelocity = 0f;
                    }
                }
            }
            transform.position += Vector3.up * (verticalVelocity * Time.deltaTime);
        }

        // 9. Ground Snap (Menempel di tanah/trotoar secara akurat)
        SnapToGround();

        // Suara mendarat setelah benar-benar melayang di udara
        if (!isGrounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            if (airTime > 0.18f) GameAudioManager.Instance?.PlayLand();
            airTime = 0f;
        }

        // 10. Anti-Tembus Dinding: Selesaikan penetrasi setelah bergerak (100% aman)
        ResolveDepenetration();

        // 11. Update Animator parameters (Speed_f controls Walk & Run)
        if (animator != null)
        {
            if (hasParamGrounded) animator.SetBool(ParamGrounded, isGrounded);
            if (hasParamGroundedB) animator.SetBool(ParamGroundedB, isGrounded);
            if (hasParamStaticB) animator.SetBool(ParamStaticB, true);

            float animSpeed = 0f;
            if (inputMagnitude > 0.05f)
            {
                animSpeed = isSprinting ? Mathf.Lerp(0.55f, 1.0f, sprintFactor) : 0.45f;
            }

            if (hasParamSpeedF) animator.SetFloat(ParamSpeedF, animSpeed);
        }

        // 12. Efek Suara Langkah Kaki (Footstep SFX)
        if (isGrounded && inputMagnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            float stepInterval = isSprinting ? 0.28f : 0.42f;
            if (footstepTimer <= 0f)
            {
                footstepTimer = stepInterval;
                GameAudioManager.Instance?.PlayFootstep(isSprinting ? 0.6f : 0.42f, isSprinting);
            }
        }
        else
        {
            footstepTimer = 0.1f;
        }
    }

    /// <summary>
    /// Menggerakkan karakter dengan swept-capsule cast dan pantulan geser (wall slide).
    /// Menjamin 100% karakter tidak bisa menembus dinding, planter, pagar, pohon, kendaraan, atau karakter NPC apapun.
    /// </summary>
    private void MoveWithCollision(Vector3 horizontalStep)
    {
        DepenetrateObstaclesOnly();

        Vector3 moveRemaining = horizontalStep;
        int maxBounces = 3;

        for (int b = 0; b < maxBounces; b++)
        {
            if (moveRemaining.sqrMagnitude < 0.0000001f) break;

            float moveDist = moveRemaining.magnitude;
            Vector3 moveDir = moveRemaining / moveDist;

            // Titik kapsul bawah & atas
            float r = bodyRadius;
            float bottomY = maxStepHeight + r;
            float topY = Mathf.Max(bottomY + 0.02f, bodyHeight - r);

            Vector3 pt1 = transform.position + Vector3.up * bottomY;
            Vector3 pt2 = transform.position + Vector3.up * topY;

            // Gunakan castRadius yang sedikit lebih kecil dari bodyRadius (r - 0.02f)
            // agar jika kapsul sudah bersentuhan dengan dinding/NPC, cast TIDAK gagal (false) akibat penetrasi awal
            float castRadius = Mathf.Max(0.08f, r - 0.02f);

            RaycastHit hit;
            bool hasHit = Physics.CapsuleCast(
                pt1, pt2, castRadius, moveDir, out hit,
                moveDist + skinWidth + 0.02f, obstacleMask, QueryTriggerInteraction.Ignore
            );

            // Abaikan collider milik diri sendiri, trigger, atau permukaan jalan/tanah datar
            if (hasHit)
            {
                if (hit.collider.isTrigger || hit.collider == ownCapsule || hit.collider.transform.IsChildOf(transform) || !IsObstacle(hit.collider))
                {
                    hasHit = false;
                }
            }

            // Pengecekan tambahan di level kaki (feet level) agar objek rendah/beton/divider tidak luput
            if (!hasHit)
            {
                Vector3 feetOrigin = transform.position + Vector3.up * (maxStepHeight + 0.01f);
                if (Physics.SphereCast(feetOrigin, castRadius * 0.8f, moveDir, out RaycastHit lowHit, moveDist + skinWidth + 0.02f, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (!lowHit.collider.isTrigger && lowHit.collider != ownCapsule && !lowHit.collider.transform.IsChildOf(transform) && IsObstacle(lowHit.collider))
                    {
                        hit = lowHit;
                        hasHit = true;
                    }
                }
            }

            if (hasHit && hit.collider != null)
            {
                RegisterBump(hit.collider);

                // Maju sampai batas aman tepat sebelum rintangan
                float safeDist = Mathf.Max(0f, hit.distance - skinWidth);
                transform.position += moveDir * safeDist;

                float distLeft = moveDist - safeDist;

                // Normal dinding di bidang horizontal
                Vector3 wallNormal = hit.normal;
                wallNormal.y = 0f;
                if (wallNormal.sqrMagnitude > 0.001f)
                {
                    wallNormal.Normalize();
                }
                else
                {
                    wallNormal = -moveDir;
                }

                // Geser ke samping permukaan dinding (sliding)
                Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, wallNormal);
                slideDir.y = 0f;

                if (slideDir.sqrMagnitude > 0.001f)
                {
                    slideDir.Normalize();
                    float forwardProjection = Vector3.Dot(moveDir, slideDir);
                    if (forwardProjection > 0.1f)
                    {
                        moveRemaining = slideDir * (distLeft * forwardProjection);
                    }
                    else
                    {
                        moveRemaining = Vector3.zero;
                    }
                }
                else
                {
                    moveRemaining = Vector3.zero;
                }
            }
            else
            {
                // Verifikasi proactive: pastikan posisi target bebas dari benturan rintangan padat apapun
                Vector3 targetPos = transform.position + moveRemaining;
                Vector3 targetPt1 = targetPos + Vector3.up * bottomY;
                Vector3 targetPt2 = targetPos + Vector3.up * topY;

                Collider[] targetOverlaps = Physics.OverlapCapsule(targetPt1, targetPt2, r * 0.88f, obstacleMask, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < targetOverlaps.Length; i++)
                {
                    Collider col = targetOverlaps[i];
                    if (col != null && !col.isTrigger && col != ownCapsule && !col.transform.IsChildOf(transform) && IsObstacle(col))
                    {
                        blocked = true;
                        RegisterBump(col);
                        break;
                    }
                }

                if (!blocked)
                {
                    transform.position = targetPos;
                }
                break;
            }
        }

        DepenetrateObstaclesOnly();
    }

    private void DepenetrateObstaclesOnly()
    {
        if (ownCapsule == null) ownCapsule = GetComponent<CapsuleCollider>();
        if (ownCapsule == null) return;

        for (int pass = 0; pass < 2; pass++)
        {
            Vector3 worldCenter = transform.TransformPoint(ownCapsule.center);
            float worldHeight = ownCapsule.height * Mathf.Abs(transform.lossyScale.y);

            Collider[] overlaps = Physics.OverlapSphere(worldCenter, worldHeight * 0.85f, obstacleMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider other = overlaps[i];
                if (other == null || other.isTrigger || other == ownCapsule || other.transform.IsChildOf(transform))
                    continue;

                if (IsObstacle(other))
                {
                    if (Physics.ComputePenetration(
                        ownCapsule, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        if (distance > 0.0001f)
                        {
                            Vector3 push = new Vector3(direction.x, 0f, direction.z);
                            if (push.sqrMagnitude > 0.00001f)
                            {
                                push.Normalize();
                                transform.position += push * (distance + 0.003f);
                                RegisterBump(other);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Mendeteksi jika karakter sedikit menembus/bersentuhan dengan objek lain
    /// dan secara matematis mendorong keluar ke batas luar (0% tembus dinding/planter/gedung).
    /// </summary>
    private void ResolveDepenetration()
    {
        if (ownCapsule == null) ownCapsule = GetComponent<CapsuleCollider>();
        if (ownCapsule == null) return;

        for (int pass = 0; pass < 2; pass++)
        {
            Vector3 worldCenter = transform.TransformPoint(ownCapsule.center);
            float worldHeight = ownCapsule.height * Mathf.Abs(transform.lossyScale.y);

            Collider[] overlaps = Physics.OverlapSphere(worldCenter, worldHeight * 0.85f, obstacleMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider other = overlaps[i];
                if (other == null || other.isTrigger || other == ownCapsule || other.transform.IsChildOf(transform))
                    continue;

                bool isObstacle = IsObstacle(other);

                if (Physics.ComputePenetration(
                    ownCapsule, transform.position, transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float distance))
                {
                    if (distance > 0.0001f)
                    {
                        // Penetrasi murni ke lantai / tanah datar -> dorong ke atas
                        if (!isObstacle && direction.y > 0.75f)
                        {
                            transform.position += Vector3.up * distance;
                            if (verticalVelocity < 0f) verticalVelocity = 0f;
                            isGrounded = true;
                        }
                        else if (isObstacle)
                        {
                            // SELURUH RINTANGAN (beton, rumah, orang, mobil, pagar, dsb) SELALU DIDORONG KELUAR SECARA HORIZONTAL!
                            Vector3 push = new Vector3(direction.x, 0f, direction.z);
                            if (push.sqrMagnitude > 0.00001f)
                            {
                                push.Normalize();
                                transform.position += push * (distance + 0.003f);
                                RegisterBump(other);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Menentukan apakah suatu collider merupakan rintangan fisik padat yang tidak boleh ditembus sama sekali
    /// (beton, rumah, orang, mobil, pagar, pohon, dsb).
    /// </summary>
    public static bool IsObstacle(Collider col)
    {
        if (col == null || col.isTrigger) return false;

        string n = col.gameObject.name.ToLower();
        string p = col.transform.parent != null ? col.transform.parent.name.ToLower() : "";

        // 1. Permukaan Tanah, Jalan, Trotoar, Lantai, Rumput, Tempat Parkir, dan Objek Geometry Plane (BUKAN RINTANGAN)
        bool isWalkableSurface = 
            n == "plane" || n.StartsWith("plane_") || n.EndsWith("_plane") || n.Contains("dirt") ||
            p == "plane" || p.StartsWith("plane_") || p.Contains("dirt") ||
            n.Contains("road") || n.Contains("jalan") || n.Contains("asphalt") || n.Contains("aspal") || 
            n.Contains("ground") || n.Contains("terrain") || n.Contains("sidewalk") || n.Contains("trotoar") || 
            n.Contains("grass") || n.Contains("tile") || n.Contains("floor") || n.Contains("runway") ||
            n.Contains("apron") || n.Contains("taxiway") || p.Contains("ground") || p.Contains("terrain") ||
            p.Contains("runway") || p.Contains("sidewalk") ||
            n.Contains("carpark") || n.Contains("parking") || n.Contains("parkir") ||
            p.Contains("carpark") || p.Contains("parking") || p.Contains("parkir") ||
            n.Contains("plaza") || n.Contains("walkway") || n.Contains("pavement");

        if (isWalkableSurface)
        {
            return false;
        }

        // 2. Orang / Karakter / NPC (termasuk seluruh penerima paket Bandara & Kota)
        if (col.GetComponentInParent<Animator>() != null || 
            col.CompareTag("NPC") || 
            col.CompareTag("Player") ||
            col.GetComponentInParent<DeliveryRecipient>() != null ||
            col.GetComponent<DeliveryRecipient>() != null)
        {
            return true;
        }

        // 3. Bangunan, gedung, toko, pos, terminal, hangar bandara
        if (n.Contains("building") || n.Contains("bangunan") || n.Contains("house") || 
            n.Contains("rumah") || n.Contains("shop") || n.Contains("store") || 
            n.Contains("office") || n.Contains("station") || n.Contains("fire") || 
            n.Contains("police") || n.Contains("petrol") || n.Contains("hospital") || 
            n.Contains("church") || n.Contains("factory") || n.Contains("town_hall") || 
            n.Contains("hall") || n.Contains("mall") || n.Contains("garage") || 
            n.Contains("hotel") || n.Contains("motel") || n.Contains("diner") || 
            n.Contains("warehouse") || n.Contains("hangar") || n.Contains("tower") || 
            n.Contains("apartment") || n.Contains("terminal") || n.Contains("baber") || 
            n.Contains("barber") || n.Contains("donut") || n.Contains("burger") ||
            n.Contains("concourse") || n.Contains("gate") || n.Contains("gangway") ||
            p.Contains("building") || p.Contains("bangunan") || p.Contains("house") ||
            p.Contains("terminal") || p.Contains("hangar"))
        {
            return true;
        }

        // 4. Kendaraan & Pesawat darat (mobil, truk, bus, pesawat darat bandara, truk bahan bakar, tangga)
        if (n.Contains("veh_") || n.Contains("car") || n.Contains("truck") || 
            n.Contains("bus") || n.Contains("van") || n.Contains("cop") || 
            n.Contains("ambo") || n.Contains("ute") || n.Contains("taxi") || 
            n.Contains("aircraft") || n.Contains("airplane") || n.Contains("plane0") || n.Contains("plane_0") ||
            n.Contains("jet") || n.Contains("fighter") || n.Contains("fuel") ||
            n.Contains("cart") || n.Contains("trailer") || n.Contains("stair") ||
            n.Contains("tug") || n.Contains("tractor") || n.Contains("baggage") ||
            p.Contains("cars") || p.Contains("veh") || p.Contains("vehicles") || p.Contains("aircraft"))
        {
            return true;
        }

        // 5. Pagar, tembok, pohon, batu, tiang, dsb.
        if (n.Contains("wall") || n.Contains("dinding") || n.Contains("fence") || 
            n.Contains("pagar") || n.Contains("tree") || n.Contains("pohon") || 
            n.Contains("palm") || n.Contains("rock") || n.Contains("batu") || 
            n.Contains("bench") || n.Contains("bin") || n.Contains("trash") || 
            n.Contains("dumpster") || n.Contains("post") || n.Contains("pole") || 
            n.Contains("hydrant") || n.Contains("memorial") || n.Contains("statue") ||
            n.Contains("prop_") || n.Contains("cone") || n.Contains("chock") ||
            p.Contains("wall") || p.Contains("pagar") || p.Contains("pohon") ||
            p.Contains("fence") || p.Contains("props"))
        {
            return true;
        }

        // 6. Beton, pembatas, planter, barrier
        if (n.Contains("planter") || n.Contains("divider") || n.Contains("barrier") || 
            n.Contains("curb") || n.Contains("beton") || n.Contains("seawall") || 
            p.Contains("planter") || p.Contains("divider") || p.Contains("barrier"))
        {
            return true;
        }

        // 7. Objek fisik yang berdiri di atas tanah dengan ketebalan vertikal nyata > 8 cm
        if (col.bounds.size.y > 0.08f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Memastikan kaki karakter selalu menempel sempurna pada permukaan tanah / trotoar (tidak melayang).
    /// Tidak menempel pada dinding vertikal atau objek tinggi seperti planter / pagar.
    /// </summary>
    private void SnapToGround()
    {
        if (verticalVelocity > 0.01f)
        {
            isGrounded = false;
            return;
        }

        if (ownColliders == null || ownColliders.Length == 0)
        {
            ownColliders = GetComponentsInChildren<Collider>();
        }

        float rayStartOffset = Mathf.Max(0.08f, maxStepHeight + 0.04f);
        float rayDistance = rayStartOffset + 0.15f;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position + Vector3.up * rayStartOffset,
            Vector3.down, rayDistance, obstacleMask, QueryTriggerInteraction.Ignore
        );

        bool foundGround = false;
        float highestGroundY = -9999f;

        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;

            // Jangan pernah menempel di atas rintangan fisik (beton, rumah, orang, mobil, pagar, dsb.)
            if (IsObstacle(h.collider)) continue;

            // Abaikan collider tubuh sendiri
            bool isSelf = false;
            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (h.collider == ownColliders[i]) { isSelf = true; break; }
            }
            if (isSelf) continue;

            // Hanya anggap permukaan tanah jika cukup datar (normal.y > 0.55f)
            // Ini mencegah karakter 'menempel' di dinding vertikal planter atau tembok
            if (h.normal.y < 0.55f) continue;

            if (h.point.y > highestGroundY)
            {
                highestGroundY = h.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            float currentY = transform.position.y;
            float diff = highestGroundY - currentY;

            // Naik trotoar atau turun undakan
            if (diff > -0.1f && diff <= maxStepHeight + 0.01f)
            {
                transform.position = new Vector3(transform.position.x, highestGroundY, transform.position.z);
                verticalVelocity = 0f;
                isGrounded = true;
            }
            else if (diff <= -0.1f)
            {
                isGrounded = false;
            }
        }
        else
        {
            // Fallback permukaan aspal jalan raya kota (Y = 0.0f)
            if (transform.position.y <= 0.08f && verticalVelocity <= 0f)
            {
                transform.position = new Vector3(transform.position.x, 0.0f, transform.position.z);
                verticalVelocity = 0f;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }
    }
}

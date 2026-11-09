using UnityEngine;

/// <summary>
/// CarHazardTrigger: Komponen pada kendaraan bergerak untuk mendeteksi senggolan dengan Rian.
/// Memberikan efek dorongan mundur (knockback) dan penalti waktu 5 detik sesuai dokumen cerita.
/// Dilengkapi masa kebal (cooldown) 2 detik untuk mencegah spam pengurangan waktu.
/// </summary>
public class CarHazardTrigger : MonoBehaviour
{
    [Tooltip("Besar kekuatan dorongan knockback saat menabrak pemain.")]
    public float knockbackStrength = 3.5f;

    [Tooltip("Jeda waktu kebal (cooldown) antar tabrakan.")]
    public float hitCooldown = 2.0f;

    private static float lastGlobalHitTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void HandlePlayerCollision(GameObject hitObj)
    {
        if (hitObj == null) return;

        PlayerController player = hitObj.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (Time.time - lastGlobalHitTime < hitCooldown) return;
        lastGlobalHitTime = Time.time;

        // Hitung arah dorongan menjauh dari mobil
        Vector3 pushDir = (player.transform.position - transform.position);
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.01f)
        {
            pushDir = transform.right; // fallback ke samping mobil
        }
        pushDir.Normalize();

        Vector3 force = pushDir * knockbackStrength;
        player.ApplyKnockback(force, 0.75f);

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.ApplyCarHitPenalty();
        }
    }
}


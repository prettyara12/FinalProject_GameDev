using UnityEngine;

/// <summary>
/// AirportFlyingAirplane: Mengontrol pergerakan animasi 1 pesawat komersial (Plane01)
/// yang terbang melintas di langit bandara Level 2 secara looping searah jarum jam (clockwise).
/// Dilengkapi efek banking aerodinamis (sayap miring alami saat belok), undulasi ketinggian,
/// serta penyembunyian roda pendaratan (retracted landing gear) saat di udara.
/// Mematuhi standar antislop dan ponytail: bersih, ringan, mandiri tanpa dependensi rumit.
/// </summary>
public class AirportFlyingAirplane : MonoBehaviour
{
    private static AirportFlyingAirplane instance;
    public static AirportFlyingAirplane Instance => instance;

    [Header("=== Flight Path Parameters ===")]
    [Tooltip("Titik pusat lintasan sirkuit penerbangan di atas bandara.")]
    public Vector3 flightCenter = new Vector3(35f, 13f, 130f);

    [Tooltip("Jari-jari lintasan horizontal X (lebar sirkuit).")]
    public float radiusX = 95f;

    [Tooltip("Jari-jari lintasan horizontal Z (panjang sirkuit sejajar runway).")]
    public float radiusZ = 120f;

    [Tooltip("Ketinggian jelajah terbang di atas bandara (meter).")]
    public float cruiseAltitude = 13f;

    [Tooltip("Kecepatan terbang pesawat (meter per detik).")]
    public float flightSpeed = 26f;

    [Tooltip("Maksimum kemiringan sayap (bank angle) saat pesawat berbelok (derajat).")]
    public float maxBankAngle = 18f;

    [Tooltip("Variasi gelombang ketinggian jelajah (meter).")]
    public float altitudeWobble = 1.0f;

    [Tooltip("Apakah roda pendaratan (landing gear) disembunyikan saat terbang.")]
    public bool retractLandingGear = false;

    [Tooltip("Terbang berlawanan arah jarum jam (counter-clockwise).")]
    public bool counterClockwise = false;

    [Tooltip("Posisi awal pada lintasan (derajat, 0-360) agar beberapa pesawat tidak berimpit.")]
    public float startAngleDegrees = 0f;

    private float currentAngle = 0f;
    private float bankVelocity = 0f;
    private float currentBank = 0f;

    private void Awake()
    {
        if (instance == null) instance = this;
        currentAngle = startAngleDegrees * Mathf.Deg2Rad;
        enabled = true;
        CleanFlightColliders();
        if (retractLandingGear) RetractLandingGear();
    }

    private void Start()
    {
        enabled = true;
        CleanFlightColliders();
        if (retractLandingGear) RetractLandingGear();
        // Atur posisi awal di sirkuit sejak detik pertama
        UpdateFlightTransform(0f);
    }

    private void Update()
    {
        // Hitung pertambahan sudut berdasarkan kecepatan terbang dan keliling rata-rata
        float approxRadius = Mathf.Max(10f, (radiusX + radiusZ) * 0.5f);
        float angularSpeed = flightSpeed / approxRadius;

        float dir = counterClockwise ? -1f : 1f;
        currentAngle += dir * angularSpeed * Time.deltaTime;
        if (currentAngle > Mathf.PI * 2f)
        {
            currentAngle -= Mathf.PI * 2f;
        }
        else if (currentAngle < 0f)
        {
            currentAngle += Mathf.PI * 2f;
        }

        UpdateFlightTransform(Time.deltaTime);
    }

    /// <summary>Mengembalikan pesawat ke sudut awal lintasannya (dipakai saat setup di editor).</summary>
    public void ResetToStartAngle()
    {
        currentAngle = Mathf.Repeat(startAngleDegrees, 360f) * Mathf.Deg2Rad;
    }

    public void UpdateFlightTransform(float deltaTime)
    {
        float approxRadius = Mathf.Max(10f, (radiusX + radiusZ) * 0.5f);

        // 1. Lintasan Searah Jarum Jam (Clockwise) di Bidang Horizontal:
        // x = center.x + sin(angle) * rx
        // z = center.z + cos(angle) * rz
        // Saat angle bertambah: bergerak dari Utara -> Timur -> Selatan -> Barat (Clockwise)
        float posX = flightCenter.x + Mathf.Sin(currentAngle) * radiusX;
        float posZ = flightCenter.z + Mathf.Cos(currentAngle) * radiusZ;
        float posY = cruiseAltitude + Mathf.Sin(currentAngle * 2f) * altitudeWobble;
        Vector3 targetPos = new Vector3(posX, posY, posZ);

        // 2. Vektor Tangen / Arah Hadap Maju (Turunan Pertama terhadap angle):
        // dx/dt = cos(angle) * rx
        // dz/dt = -sin(angle) * rz
        float tangentX = Mathf.Cos(currentAngle) * radiusX;
        float tangentZ = -Mathf.Sin(currentAngle) * radiusZ;
        float tangentY = Mathf.Cos(currentAngle * 2f) * (altitudeWobble * 2f) * (flightSpeed / approxRadius);
        Vector3 tangentDir = new Vector3(tangentX, tangentY, tangentZ) * (counterClockwise ? -1f : 1f);
        tangentDir = tangentDir.normalized;

        if (tangentDir.sqrMagnitude < 0.0001f)
        {
            tangentDir = Vector3.forward;
        }

        // 3. Sudut Yaw (Arah Hadap Pesawat Maju Sejajar Tangen) & Pitch (Undulasi Halus)
        float targetYaw = Mathf.Atan2(tangentDir.x, tangentDir.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Clamp(tangentDir.y * 3.5f, -8f, 8f);

        // 4. Sudut Bank (Roll Miring ke Kanan Saat Berbelok Searah Jarum Jam):
        // Pada lintasan oval clockwise, kemiringan sayap kanan selalu masuk ke dalam kurva (negatif roll)
        float curveWeight = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(currentAngle * 2f));
        float targetBank = -Mathf.Clamp(maxBankAngle * curveWeight, 8f, maxBankAngle) * (counterClockwise ? -1f : 1f);

        if (deltaTime > 0f)
        {
            currentBank = Mathf.SmoothDamp(currentBank, targetBank, ref bankVelocity, 0.2f, Mathf.Infinity, deltaTime);
        }
        else
        {
            currentBank = targetBank;
        }

        // Terapkan Transformasi World Space
        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(targetPitch, targetYaw, currentBank);
    }

    /// <summary>
    /// Sembunyikan roda pendaratan (Wheel01, Wheel02, Wheel03) agar pesawat tampak terbang jelajah di udara.
    /// </summary>
    public void RetractLandingGear()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            if (t != null && t != transform && t.name.ToLower().Contains("wheel"))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Pastikan pesawat yang terbang tidak memiliki collider padat yang dapat menghalangi kamera pemain.
    /// </summary>
    public void CleanFlightColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        int segments = 48;
        Vector3 prevPt = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            float px = flightCenter.x + Mathf.Sin(a) * radiusX;
            float pz = flightCenter.z + Mathf.Cos(a) * radiusZ;
            float py = cruiseAltitude + Mathf.Sin(a * 2f) * altitudeWobble;
            Vector3 pt = new Vector3(px, py, pz);

            if (i > 0)
            {
                Gizmos.DrawLine(prevPt, pt);
            }
            prevPt = pt;
        }
    }
#endif
}

using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    public enum FireMode
    {
        Semi,       // Un disparo por pulsación del gatillo
        Automatic,  // Disparo continuo mientras se mantiene el gatillo
        Shotgun     // Un disparo de N pellets en cono por pulsación
    }

    public enum WeaponCategory
    {
        Firearm,
        Melee
    }

    /// <summary>
    /// Contiene todas las stats balanceables de un arma. Vive como asset en
    /// Assets/Data/Weapons/. Los prefabs solo guardan referencias a este asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Zombie VR/Weapon Data", order = 0)]
    public class WeaponData : ScriptableObject
    {
        [Header("Identificación")]
        public string displayName = "Weapon";
        public WeaponCategory category = WeaponCategory.Firearm;

        [Header("Daño")]
        public float damage = 10f;
        [Range(0f, 1f)] public float criticalChance = 0.1f;
        public float criticalMultiplier = 1.5f;

        [Header("Disparo (Firearm)")]
        public FireMode fireMode = FireMode.Semi;
        [Tooltip("Tiempo mínimo entre disparos en segundos.")]
        public float fireRate = 0.5f;
        [Tooltip("Distancia máxima del raycast.")]
        public float range = 100f;

        [Header("Escopeta (solo si fireMode = Shotgun)")]
        [Min(1)] public int pelletCount = 8;
        [Tooltip("Ángulo total del cono de dispersión en grados.")]
        public float spreadAngle = 6f;

        [Header("Munición (Firearm)")]
        public int magazineSize = 10;
        public int reserveAmmoMax = 40;
        public float reloadDuration = 1.5f;

        [Header("Melee (solo si category = Melee)")]
        [Tooltip("Cooldown entre golpes en segundos.")]
        public float meleeCooldown = 0.5f;
        [Tooltip("Velocidad mínima de la mano (m/s) para que un swing cuente como golpe.")]
        public float meleeMinSwingVelocity = 1.0f;
    }
}

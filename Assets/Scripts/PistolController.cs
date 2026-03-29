using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PistolController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootMuzzle;
    [SerializeField] private InputActionProperty shootAction;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Stats")]
    [SerializeField] private float weaponDamage = 10f;
    [SerializeField] private float criticalChance = 0.1f;
    [SerializeField] private float criticalMultiplier = 1.5f;
    [SerializeField] private float fireRate = 0.5f;

    private RaycastHit hit;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (shootAction.action.triggered)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        audioSource.PlayOneShot(shootSound);
        if (Physics.Raycast(shootMuzzle.position, shootMuzzle.forward, out hit, float.PositiveInfinity))
        {
            // 1. Obtener el número de la capa (ej: 0, 8, 31)
            int capaNumero = hit.collider.gameObject.layer;

            // 2. Convertir ese número al nombre que pusiste en Unity (ej: "Enemigo")
            string capaNombre = LayerMask.LayerToName(capaNumero);

            Debug.Log("Impacto en: " + hit.collider.name + " | Capa: " + capaNombre + " (ID: " + capaNumero + ")");
        }
    }
}

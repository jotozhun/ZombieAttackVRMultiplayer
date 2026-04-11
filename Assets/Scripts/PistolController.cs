
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PistolController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootMuzzle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private ParticleSystem[] shootParticles;

    [Header("Stats")]
    [SerializeField] private float weaponDamage = 10f;
    [SerializeField] private float criticalChance = 0.1f;
    [SerializeField] private float criticalMultiplier = 1.5f;
    [SerializeField] private float fireRate = 0.5f;

    [Header("Ammo")]
    [SerializeField] private int magazineMaxAmmo = 10;
    [SerializeField] private int magazineCurrentAmmo = 5;
    [SerializeField] private int currentAmmo = 10;
    [SerializeField] private int maxAmmo = 40;

    private bool isTriggerPressed = false;
    private XRBaseInteractable interactable;
    // Guardamos la referencia al interactor (mano) que activó el arma
    private IXRActivateInteractor actualActivator;
    private RaycastHit hit;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        
        interactable.activated.AddListener(args =>
        {
            isTriggerPressed = true;
            actualActivator = args.interactorObject;
        });
        
        interactable.activated.AddListener(args =>
        {
            isTriggerPressed = false;
            actualActivator = null;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (isTriggerPressed)
        {
            Shoot();
        }
    }

    private void Reload()
    {
        if (magazineCurrentAmmo == magazineMaxAmmo || currentAmmo == 0)
        {
            return;
        }

        int bulletsToFullyReload = magazineMaxAmmo - magazineCurrentAmmo;
        if (bulletsToFullyReload <= currentAmmo)
        {
            magazineCurrentAmmo = magazineMaxAmmo;
            currentAmmo -= bulletsToFullyReload;
        }
        else
        {
            magazineCurrentAmmo += currentAmmo;
            currentAmmo = 0;
        }
        audioSource.PlayOneShot(reloadSound);
    }

    private void Shoot()
    {
        if (magazineCurrentAmmo == 0)
        {
            return;
        }
        magazineCurrentAmmo -= 1;
        audioSource.PlayOneShot(shootSound);
        foreach (var ps in shootParticles) {
            ps.Play();
        }
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

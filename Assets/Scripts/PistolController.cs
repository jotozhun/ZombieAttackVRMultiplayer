using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PistolController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootMuzzle;
    [SerializeField] private InputActionProperty shootAction;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Stats")]
    [SerializeField] private float weaponDamage = 10f;
    [SerializeField] private float criticalChance = 0.1f;
    [SerializeField] private float criticalMultiplier = 1.5f;
    [SerializeField] private float fireRate = 0.5f;
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
        muzzleFlash.Play();

    }
}

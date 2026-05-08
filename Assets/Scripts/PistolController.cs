
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
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
    [SerializeField] private float fireRate = 1.2f;

    [Header("Ammo")]
    [SerializeField] private int magazineMaxAmmo = 10;
    [SerializeField] private int magazineCurrentAmmo = 5;
    [SerializeField] private int currentAmmo = 10;
    [SerializeField] private int maxAmmo = 40;

    [Header("Reload Input - Dynamic Discovery")]
    [Tooltip("Nombre de la acción de recarga (debe existir en el InputActionAsset del controlador)")]
    [SerializeField] private string reloadActionName = "Secondary Button";
    [Tooltip("Action map para la mano derecha")]
    [SerializeField] private string rightHandActionMapName = "XRI Right Interaction";
    [Tooltip("Action map para la mano izquierda")]
    [SerializeField] private string leftHandActionMapName = "XRI Left Interaction";

    [Header("Reload Input - Fallback (si la acción no existe en el asset)")]
    [Tooltip("Binding usado como fallback para la mano derecha")]
    [SerializeField] private string rightHandReloadBinding = "<XRController>{RightHand}/secondaryButton";
    [Tooltip("Binding usado como fallback para la mano izquierda")]
    [SerializeField] private string leftHandReloadBinding = "<XRController>{LeftHand}/secondaryButton";

    private bool isTriggerPressed = false;
    private float timeBeforeNextShot;
    private XRBaseInteractable interactable;

    // Acción activa actualmente (puede venir del asset o haber sido creada al vuelo)
    private InputAction activeReloadAction;
    // Si la creamos nosotros, hay que liberarla en OnReleased
    private bool reloadActionIsOwned;
    private RaycastHit hit;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        interactable.activated.AddListener(args => isTriggerPressed = true);
        interactable.deactivated.AddListener(args => isTriggerPressed = false);

        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        var inputInteractor = args.interactorObject as XRBaseInputInteractor;
        if (inputInteractor == null)
        {
            Debug.LogWarning("[PistolController] El interactor no es XRBaseInputInteractor.");
            return;
        }

        bool isRightHand = inputInteractor.handedness == InteractorHandedness.Right;

        // 1) Intentar resolver el InputAction desde el asset del interactor.
        InputActionAsset asset = null;
        if (inputInteractor.selectInput?.inputActionReferencePerformed != null)
            asset = inputInteractor.selectInput.inputActionReferencePerformed.asset;
        else if (inputInteractor.activateInput?.inputActionReferencePerformed != null)
            asset = inputInteractor.activateInput.inputActionReferencePerformed.asset;

        if (asset != null)
        {
            string mapName = isRightHand ? rightHandActionMapName : leftHandActionMapName;
            var actionMap = asset.FindActionMap(mapName, throwIfNotFound: false);
            activeReloadAction = actionMap?.FindAction(reloadActionName, throwIfNotFound: false);
        }

        // 2) Fallback: si no existe en el asset, crear InputAction al vuelo.
        if (activeReloadAction == null)
        {
            string binding = isRightHand ? rightHandReloadBinding : leftHandReloadBinding;
            activeReloadAction = new InputAction(
                name: $"DynamicReload_{(isRightHand ? "Right" : "Left")}",
                type: InputActionType.Button,
                binding: binding);
            activeReloadAction.Enable();
            reloadActionIsOwned = true;
            Debug.LogWarning($"[PistolController] Acción '{reloadActionName}' no encontrada en el asset. Usando fallback: {binding}");
        }
        else
        {
            reloadActionIsOwned = false;
        }

        activeReloadAction.performed += OnReloadInput;
        Debug.Log($"[PistolController] Arma agarrada por mano {(isRightHand ? "DERECHA" : "IZQUIERDA")}. Recarga vinculada a: {activeReloadAction.actionMap?.name ?? "(creada al vuelo)"}/{activeReloadAction.name}");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        ReleaseReloadAction();
        isTriggerPressed = false;
    }

    private void OnDestroy()
    {
        ReleaseReloadAction();
    }

    private void ReleaseReloadAction()
    {
        if (activeReloadAction == null) return;

        activeReloadAction.performed -= OnReloadInput;

        // Solo deshabilitar/desechar si la creamos nosotros.
        // Las acciones del asset las gestiona el InputActionManager.
        if (reloadActionIsOwned)
        {
            activeReloadAction.Disable();
            activeReloadAction.Dispose();
        }

        activeReloadAction = null;
        reloadActionIsOwned = false;
        Debug.Log("[PistolController] Acción de recarga liberada.");
    }

    private void OnReloadInput(InputAction.CallbackContext ctx) => Reload();

    void Update()
    {
        if (isTriggerPressed && Time.time >= timeBeforeNextShot)
        {
            Shoot();
            timeBeforeNextShot = Time.time + fireRate;
        }
    }

    private void Reload()
    {
        if (magazineCurrentAmmo == magazineMaxAmmo || currentAmmo == 0)
            return;

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
        Debug.Log("Starting shooting");
        if (magazineCurrentAmmo == 0)
            return;

        magazineCurrentAmmo -= 1;
        audioSource.PlayOneShot(shootSound);
        foreach (var ps in shootParticles)
            ps.Play();

        if (Physics.Raycast(shootMuzzle.position, shootMuzzle.forward, out hit, float.PositiveInfinity))
        {
            int capaNumero = hit.collider.gameObject.layer;
            string capaNombre = LayerMask.LayerToName(capaNumero);
            Debug.Log("Impacto en: " + hit.collider.name + " | Capa: " + capaNombre + " (ID: " + capaNumero + ")");
        }
    }
}

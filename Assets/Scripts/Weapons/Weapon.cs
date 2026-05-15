using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Base abstracta para todas las armas. Maneja:
    /// - Wiring del XRBaseInteractable (grab/release, activate/deactivate)
    /// - Estado networked del Holder
    /// - Resolución dinámica del InputAction de recarga al agarrar (heredado de PistolController)
    ///
    /// Subclases (Firearm, MeleeWeapon) implementan la lógica específica de disparo/golpe.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public abstract class Weapon : NetworkBehaviour
    {
        [Header("Weapon Data")]
        [Tooltip("ScriptableObject con todas las stats balanceables.")]
        [SerializeField] protected WeaponData data;

        [Header("References (prefab-bound)")]
        [SerializeField] protected AudioSource audioSource;

        [Header("Reload Input - Dynamic Discovery")]
        [SerializeField] private string reloadActionName = "Secondary Button";
        [SerializeField] private string rightHandActionMapName = "XRI Right Interaction";
        [SerializeField] private string leftHandActionMapName = "XRI Left Interaction";
        [SerializeField] private string rightHandReloadBinding = "<XRController>{RightHand}/secondaryButton";
        [SerializeField] private string leftHandReloadBinding = "<XRController>{LeftHand}/secondaryButton";

        // Estado networked.
        [Networked] public PlayerRef Holder { get; set; }
        [Networked] public NetworkBool IsHeldByRightHand { get; set; }

        // Estado local (no networked) — interacción XR es local al jugador que agarra.
        protected XRBaseInteractable interactable;
        protected XRBaseInputInteractor currentInteractor;
        protected bool isTriggerHeld;

        // Acción de recarga resuelta dinámicamente en OnGrabbed.
        private InputAction activeReloadAction;
        private bool reloadActionIsOwned;

        public WeaponData Data => data;
        public bool IsHeld => currentInteractor != null;

        protected virtual void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                Debug.LogError($"[Weapon] {name} requiere un XRBaseInteractable.", this);
                return;
            }

            interactable.activated.AddListener(OnActivated);
            interactable.deactivated.AddListener(OnDeactivated);
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        protected virtual void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.activated.RemoveListener(OnActivated);
                interactable.deactivated.RemoveListener(OnDeactivated);
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.selectExited.RemoveListener(OnSelectExited);
            }
            ReleaseReloadAction();
        }

        // ────────────────────────────────────────────────────────────────────────
        // XR event wiring
        // ────────────────────────────────────────────────────────────────────────

        private void OnActivated(ActivateEventArgs args)
        {
            isTriggerHeld = true;
            OnTriggerPressed();
        }

        private void OnDeactivated(DeactivateEventArgs args)
        {
            isTriggerHeld = false;
            OnTriggerReleased();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            currentInteractor = args.interactorObject as XRBaseInputInteractor;
            if (currentInteractor == null)
            {
                Debug.LogWarning($"[Weapon] {name} agarrada por un interactor no XR.", this);
                return;
            }

            bool isRightHand = currentInteractor.handedness == InteractorHandedness.Right;
            ResolveReloadAction(isRightHand);

            // Si tenemos state authority (host con bot, o cliente en modo Shared), grabamos el Holder.
            // En modo Host-autoritativo, el cliente NO escribe Holder directamente — eso debe llegar
            // vía RPC desde el host, pero por simplicidad inicial dejamos que el cliente lo pida.
            if (HasStateAuthority)
            {
                Holder = Object.InputAuthority;
                IsHeldByRightHand = isRightHand;
            }
            else
            {
                Rpc_RequestSetHolder(Object != null ? Runner.LocalPlayer : PlayerRef.None, isRightHand);
            }

            OnGrabbed(isRightHand);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            OnReleased();
            ReleaseReloadAction();
            isTriggerHeld = false;

            if (HasStateAuthority)
            {
                Holder = PlayerRef.None;
            }
            else
            {
                Rpc_RequestClearHolder();
            }

            currentInteractor = null;
        }

        // ────────────────────────────────────────────────────────────────────────
        // RPCs para sincronización del Holder
        // ────────────────────────────────────────────────────────────────────────

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void Rpc_RequestSetHolder(PlayerRef player, NetworkBool isRightHand)
        {
            Holder = player;
            IsHeldByRightHand = isRightHand;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void Rpc_RequestClearHolder()
        {
            Holder = PlayerRef.None;
        }

        // ────────────────────────────────────────────────────────────────────────
        // Resolución dinámica del input de recarga (preservado de PistolController)
        // ────────────────────────────────────────────────────────────────────────

        private void ResolveReloadAction(bool isRightHand)
        {
            // 1) Intentar resolver desde el InputActionAsset del interactor.
            InputActionAsset asset = null;
            if (currentInteractor.selectInput?.inputActionReferencePerformed != null)
                asset = currentInteractor.selectInput.inputActionReferencePerformed.asset;
            else if (currentInteractor.activateInput?.inputActionReferencePerformed != null)
                asset = currentInteractor.activateInput.inputActionReferencePerformed.asset;

            if (asset != null)
            {
                string mapName = isRightHand ? rightHandActionMapName : leftHandActionMapName;
                var actionMap = asset.FindActionMap(mapName, throwIfNotFound: false);
                activeReloadAction = actionMap?.FindAction(reloadActionName, throwIfNotFound: false)
                                     ?? asset.FindAction(reloadActionName, throwIfNotFound: false);
            }

            // 2) Fallback: crear al vuelo.
            if (activeReloadAction == null)
            {
                string binding = isRightHand ? rightHandReloadBinding : leftHandReloadBinding;
                activeReloadAction = new InputAction(
                    name: $"DynamicReload_{(isRightHand ? "Right" : "Left")}",
                    type: InputActionType.Button,
                    binding: binding);
                activeReloadAction.Enable();
                reloadActionIsOwned = true;
            }
            else
            {
                reloadActionIsOwned = false;
            }

            activeReloadAction.performed += OnReloadInputPerformed;
        }

        private void ReleaseReloadAction()
        {
            if (activeReloadAction == null) return;
            activeReloadAction.performed -= OnReloadInputPerformed;
            if (reloadActionIsOwned)
            {
                activeReloadAction.Disable();
                activeReloadAction.Dispose();
            }
            activeReloadAction = null;
            reloadActionIsOwned = false;
        }

        private void OnReloadInputPerformed(InputAction.CallbackContext ctx) => OnReloadPressed();

        // ────────────────────────────────────────────────────────────────────────
        // Hooks abstractos para subclases
        // ────────────────────────────────────────────────────────────────────────

        protected virtual void OnGrabbed(bool isRightHand) { }
        protected virtual void OnReleased() { }
        protected virtual void OnTriggerPressed() { }
        protected virtual void OnTriggerReleased() { }
        protected virtual void OnReloadPressed() { }
    }
}

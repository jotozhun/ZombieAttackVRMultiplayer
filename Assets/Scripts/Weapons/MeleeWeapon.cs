using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Arma cuerpo a cuerpo. Detección por collider trigger en la zona de daño (hoja).
    /// El daño se aplica cuando:
    /// 1. El collider trigger contacta con un IDamageable
    /// 2. La velocidad de la mano (XR) supera data.meleeMinSwingVelocity
    /// 3. El cooldown (data.meleeCooldown) ha expirado
    ///
    /// La validación de velocidad se hace en el cliente que sostiene (input authority).
    /// El daño efectivo se aplica en el host vía RPC.
    /// </summary>
    public class MeleeWeapon : Weapon
    {
        [Header("Melee References")]
        [Tooltip("Collider tipo trigger ubicado en la zona de daño (filo del cuchillo).")]
        [SerializeField] private Collider damageCollider;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip swingSound;

        [Header("Haptics")]
        [SerializeField] private float hitHapticAmplitude = 0.8f;
        [SerializeField] private float hitHapticDuration = 0.15f;

        [Networked]
        private int LastHitTick { get; set; }

        // Tracking de velocidad local (no networked — solo se evalúa en input authority).
        private Vector3 _previousHandPosition;
        private float _currentHandSpeed;
        private bool _hasPositionSample;

        protected override void Awake()
        {
            base.Awake();
            if (damageCollider == null)
            {
                Debug.LogWarning($"[MeleeWeapon] {name} no tiene damageCollider asignado.", this);
            }
            else if (!damageCollider.isTrigger)
            {
                Debug.LogWarning($"[MeleeWeapon] El damageCollider de {name} debería ser isTrigger=true.", this);
            }
            LastHitTick = -1;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
                LastHitTick = -1;
        }

        // Solo el cliente que sostiene el arma actualiza la velocidad de la mano.
        public override void FixedUpdateNetwork()
        {
            if (!IsHeld || currentInteractor == null) return;
            // El holder local rastrea la velocidad por su cuenta.
            if (Object.InputAuthority != Runner.LocalPlayer) return;

            Vector3 handPos = currentInteractor.transform.position;
            if (_hasPositionSample)
            {
                _currentHandSpeed = (handPos - _previousHandPosition).magnitude / Runner.DeltaTime;
            }
            _previousHandPosition = handPos;
            _hasPositionSample = true;
        }

        // Llamado por Unity al entrar en contacto un collider trigger.
        // Se ejecuta en TODOS los clientes; filtramos para que solo el holder responda.
        private void OnTriggerEnter(Collider other)
        {
            if (Object == null) return;
            if (!IsHeld) return;
            // Solo el cliente que sostiene reporta golpes.
            if (Object.InputAuthority != Runner.LocalPlayer) return;
            if (data == null) return;

            // Validación de velocidad mínima.
            if (_currentHandSpeed < data.meleeMinSwingVelocity) return;

            // Validación de cooldown (basado en tick).
            int tickCooldown = Mathf.CeilToInt(data.meleeCooldown * Runner.TickRate);
            if (Runner.Tick - LastHitTick < tickCooldown) return;

            // Buscar IDamageable en el objetivo.
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;

            // Solicita al host que aplique el daño.
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            NetworkId targetId = default;
            if (other.GetComponentInParent<NetworkObject>() is NetworkObject netObj)
                targetId = netObj.Id;

            if (!targetId.IsValid) return;

            Rpc_RequestApplyHit(targetId, hitPoint, hitNormal, _currentHandSpeed);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void Rpc_RequestApplyHit(NetworkId targetId, Vector3 hitPoint, Vector3 hitNormal, float swingVelocity)
        {
            // Host valida nuevamente la velocidad (anti-cheat ligero) y aplica el daño.
            if (data == null) return;
            if (swingVelocity < data.meleeMinSwingVelocity * 0.5f) return; // tolerancia de red.

            int tickCooldown = Mathf.CeilToInt(data.meleeCooldown * Runner.TickRate);
            if (Runner.Tick - LastHitTick < tickCooldown) return;

            if (!Runner.TryFindObject(targetId, out var target)) return;
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null) damageable = target.GetComponentInChildren<IDamageable>();
            if (damageable == null || !damageable.IsAlive) return;

            LastHitTick = Runner.Tick;

            float damage = data.damage;
            if (data.criticalChance > 0f && UnityEngine.Random.value < data.criticalChance)
                damage *= data.criticalMultiplier;

            damageable.TakeDamage(damage, hitPoint, hitNormal, Object.InputAuthority);
            Rpc_PlayHitFx(hitPoint);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayHitFx(Vector3 hitPoint)
        {
            if (audioSource != null && hitSound != null)
                audioSource.PlayOneShot(hitSound);

            if (Runner.LocalPlayer == Holder && currentInteractor != null)
                currentInteractor.SendHapticImpulse(hitHapticAmplitude, hitHapticDuration);
        }

        protected override void OnTriggerPressed()
        {
            // En melee no se "dispara" con el gatillo. Un swing es un movimiento físico.
            // Opcional: reproducir sonido de swing si el gatillo se mantiene (fantasía vibratoria).
            if (audioSource != null && swingSound != null && !audioSource.isPlaying)
                audioSource.PlayOneShot(swingSound);
        }

        protected override void OnReleased()
        {
            _hasPositionSample = false;
            _currentHandSpeed = 0f;
        }
    }
}

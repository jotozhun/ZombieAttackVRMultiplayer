using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Arma de fuego unificada. Comporta semi/auto/shotgun según data.fireMode.
    /// La selección de proyección (SingleRay vs Spread) se hace según data.fireMode.
    ///
    /// Flujo:
    /// 1. Cliente local agarra el arma → InputAuthority queda en él
    /// 2. Cliente lee gatillo XR localmente, lo empaqueta en WeaponInput (BasicSpawner.OnInput)
    /// 3. Host recibe WeaponInput en FixedUpdateNetwork
    /// 4. Host valida ammo + cooldown, ejecuta DoFire (lag-comp raycast)
    /// 5. Host aplica daño a IDamageable y dispara RPC a todos para FX
    /// </summary>
    public class Firearm : Weapon
    {
        [Header("Firearm References")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptyClickSound;
        [SerializeField] private ParticleSystem[] muzzleParticles;

        [Header("Haptics")]
        [SerializeField] private float hapticAmplitude = 0.5f;
        [SerializeField] private float hapticDuration = 0.1f;

        // Estado networked.
        [Networked, OnChangedRender(nameof(OnAmmoChanged))]
        public int MagazineAmmo { get; private set; }

        [Networked]
        public int ReserveAmmo { get; private set; }

        [Networked]
        public NetworkBool IsReloading { get; private set; }

        // Tick en el que se efectuó el último disparo (host). Se compara con Tick actual para fire rate.
        [Networked]
        private int LastFireTick { get; set; }

        [Networked]
        private int ReloadCompleteTick { get; set; }

        // Estrategia de proyección, resuelta en Spawned a partir de data.fireMode.
        private IProjectionStrategy projection;
        // Buffer reutilizable para evitar allocations por disparo.
        private readonly List<LagCompensatedHit> hitBuffer = new List<LagCompensatedHit>(16);

        // Evento UI (HUD de munición).
        public System.Action<int, int> OnAmmoChangedEvent; // (magazine, reserve)

        public override void Spawned()
        {
            if (data == null)
            {
                Debug.LogError($"[Firearm] {name} no tiene WeaponData asignada.", this);
                return;
            }

            projection = data.fireMode == FireMode.Shotgun
                ? (IProjectionStrategy)new SpreadProjection()
                : new SingleRayProjection();

            if (HasStateAuthority)
            {
                MagazineAmmo = data.magazineSize;
                ReserveAmmo = data.reserveAmmoMax;
                LastFireTick = -1;
                ReloadCompleteTick = -1;
            }

            OnAmmoChangedEvent?.Invoke(MagazineAmmo, ReserveAmmo);
        }

        public override void FixedUpdateNetwork()
        {
            // Solo el host ejecuta lógica de disparo, validada con input del holder.
            if (!HasStateAuthority) return;
            if (data == null) return;

            // El arma debe estar en mano para procesar input.
            if (Holder == PlayerRef.None) return;

            if (!GetInput<WeaponInput>(out var input)) return;

            // Recarga: edge-detect (botón presionado este tick, no antes).
            // NetworkButtons tiene métodos GetPressed para detección de edge.
            bool reloadPressed = input.buttons.IsSet(WeaponButton.Reload);
            if (reloadPressed && !IsReloading)
                TryStartReload();

            // Si terminó la recarga, completarla.
            if (IsReloading && Runner.Tick >= ReloadCompleteTick)
                CompleteReload();

            // Disparo.
            bool triggerHeld = input.buttons.IsSet(WeaponButton.Trigger);
            HandleTriggerInput(triggerHeld, input.weaponPosition, input.weaponForward);
        }

        private void HandleTriggerInput(bool triggerHeld, Vector3 weaponPos, Vector3 weaponForward)
        {
            if (IsReloading) return;

            // Modo Semi/Shotgun: requieren detección de "trigger acabado de presionar".
            // Sin embargo, en Host mode no tenemos un "previous" trivial. Usamos un truco:
            // si el ultimo disparo fue HACE MUCHO (más de fireRate * 2), permitimos disparo.
            // Para gameplay sólido conviene mover esto a un edge detector con estado por tick.
            int currentTick = Runner.Tick;
            float ticksPerSecond = Runner.TickRate;
            int fireRateTicks = Mathf.CeilToInt(data.fireRate * ticksPerSecond);
            bool cooldownReady = (currentTick - LastFireTick) >= fireRateTicks;

            switch (data.fireMode)
            {
                case FireMode.Automatic:
                    if (triggerHeld && cooldownReady)
                        TryFire(weaponPos, weaponForward);
                    break;

                case FireMode.Semi:
                case FireMode.Shotgun:
                    // Por simplicidad inicial trato Semi como auto-con-cooldown.
                    // Una mejora futura: añadir [Networked] bool TriggerWasHeldLastTick para edge real.
                    if (triggerHeld && cooldownReady)
                        TryFire(weaponPos, weaponForward);
                    break;
            }
        }

        private void TryFire(Vector3 weaponPos, Vector3 weaponForward)
        {
            if (MagazineAmmo <= 0)
            {
                Rpc_PlayEmptyClick();
                LastFireTick = Runner.Tick; // Evita spam del click.
                return;
            }

            MagazineAmmo -= 1;
            LastFireTick = Runner.Tick;

            // Usar posición/dirección que vino con el input (lo que el cliente vio).
            // Si están en cero (input ausente), caer al muzzle local del host.
            Vector3 origin = weaponPos != Vector3.zero
                ? weaponPos
                : (muzzle != null ? muzzle.position : transform.position);
            Vector3 forward = weaponForward != Vector3.zero
                ? weaponForward.normalized
                : (muzzle != null ? muzzle.forward : transform.forward);

            DoFire(origin, forward);
            Rpc_PlayFireFx(origin, forward);
        }

        private void DoFire(Vector3 origin, Vector3 forward)
        {
            projection.Project(Runner, origin, forward, data, Object.InputAuthority, hitBuffer);

            for (int i = 0; i < hitBuffer.Count; i++)
            {
                var hit = hitBuffer[i];
                ApplyHit(hit);
            }
        }

        private void ApplyHit(LagCompensatedHit hit)
        {
            // El target puede estar en Hitbox.Root (networked) o ser un collider PhysX normal.
            IDamageable damageable = null;
            Vector3 hitPoint = hit.Point;
            Vector3 hitNormal = hit.Normal;

            if (hit.Hitbox != null && hit.Hitbox.Root != null)
            {
                damageable = hit.Hitbox.Root.GetComponent<IDamageable>();
            }
            else if (hit.Collider != null)
            {
                damageable = hit.Collider.GetComponentInParent<IDamageable>();
            }

            if (damageable == null || !damageable.IsAlive) return;

            // Cálculo de daño con critical.
            float damage = data.damage;
            if (data.criticalChance > 0f && UnityEngine.Random.value < data.criticalChance)
                damage *= data.criticalMultiplier;

            damageable.TakeDamage(damage, hitPoint, hitNormal, Object.InputAuthority);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Recarga
        // ────────────────────────────────────────────────────────────────────────

        private void TryStartReload()
        {
            if (MagazineAmmo >= data.magazineSize) return;
            if (ReserveAmmo <= 0) return;

            IsReloading = true;
            ReloadCompleteTick = Runner.Tick + Mathf.CeilToInt(data.reloadDuration * Runner.TickRate);
            Rpc_PlayReloadFx();
        }

        private void CompleteReload()
        {
            int missing = data.magazineSize - MagazineAmmo;
            int toLoad = Mathf.Min(missing, ReserveAmmo);
            MagazineAmmo += toLoad;
            ReserveAmmo -= toLoad;
            IsReloading = false;
        }

        protected override void OnReloadPressed()
        {
            // Solo enviamos un flag al host vía WeaponInput; pero también permitimos disparo local rápido
            // del flag si el cliente local es el host (single-player en editor).
            // El input se recolecta en BasicSpawner.OnInput, así que aquí no hacemos nada extra.
        }

        // ────────────────────────────────────────────────────────────────────────
        // RPCs cosméticos (todos los clientes ven el efecto del disparo)
        // ────────────────────────────────────────────────────────────────────────

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayFireFx(Vector3 origin, Vector3 forward)
        {
            if (audioSource != null && shootSound != null)
                audioSource.PlayOneShot(shootSound);

            if (muzzleParticles != null)
            {
                for (int i = 0; i < muzzleParticles.Length; i++)
                    if (muzzleParticles[i] != null) muzzleParticles[i].Play();
            }

            // Háptica solo en el cliente que sostiene el arma.
            if (Runner.LocalPlayer == Holder && currentInteractor != null)
            {
                currentInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayReloadFx()
        {
            if (audioSource != null && reloadSound != null)
                audioSource.PlayOneShot(reloadSound);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayEmptyClick()
        {
            if (audioSource != null && emptyClickSound != null)
                audioSource.PlayOneShot(emptyClickSound);
        }

        // ────────────────────────────────────────────────────────────────────────
        // Callback OnChanged
        // ────────────────────────────────────────────────────────────────────────

        private void OnAmmoChanged()
        {
            OnAmmoChangedEvent?.Invoke(MagazineAmmo, ReserveAmmo);
        }
    }
}

using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Componente de salud networked. Implementa IDamageable.
    /// El estado vive en la state authority (host en GameMode.Server).
    /// Todos los clientes ven CurrentHealth replicado vía [Networked].
    /// </summary>
    public class Health : NetworkBehaviour, IDamageable
    {
        [Header("Configuración")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = false;
        [Tooltip("Segundos antes de destruir el objeto tras morir (si destroyOnDeath está activado).")]
        [SerializeField] private float destroyDelay = 3f;

        [Header("Eventos")]
        public UnityEvent<float, float> OnHealthChanged; // (current, max)
        public UnityEvent<PlayerRef> OnDeath;             // attacker que mató
        public UnityEvent<float, Vector3, Vector3, PlayerRef> OnDamaged; // damage, point, normal, attacker

        [Networked, OnChangedRender(nameof(HandleHealthChanged))]
        public float CurrentHealth { get; private set; }

        [Networked]
        public NetworkBool IsDead { get; private set; }

        public float MaxHealth => maxHealth;
        public bool IsAlive => !IsDead && CurrentHealth > 0f;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentHealth = maxHealth;
                IsDead = false;
            }
            // Refresh inicial para que el listener tenga el valor correcto
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, PlayerRef attacker)
        {
            // Solo la state authority muta el estado.
            if (!HasStateAuthority)
            {
                Debug.LogWarning($"[Health] TakeDamage llamado sin state authority en {name}. Ignorado.");
                return;
            }

            if (IsDead || damage <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            Rpc_NotifyDamaged(damage, hitPoint, hitNormal, attacker);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                Rpc_NotifyDeath(attacker);
                if (destroyOnDeath && Object != null)
                {
                    // En Fusion 2, Despawn programa el despawn al final del tick.
                    // Si quieres delay (animación de muerte), usa StartCoroutine + WaitForSeconds(destroyDelay).
                    if (destroyDelay <= 0f)
                        Runner.Despawn(Object);
                    else
                        Invoke(nameof(DelayedDespawn), destroyDelay);
                }
            }
        }

        // Notifica a TODOS los clientes (incluyendo el host) para FX y eventos UnityEvent.
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_NotifyDamaged(float damage, Vector3 hitPoint, Vector3 hitNormal, PlayerRef attacker)
        {
            OnDamaged?.Invoke(damage, hitPoint, hitNormal, attacker);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_NotifyDeath(PlayerRef attacker)
        {
            OnDeath?.Invoke(attacker);
        }

        // Callback OnChanged: dispara el evento de cambio en todos los clientes.
        private void HandleHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        /// <summary>
        /// Útil para iniciar entidades con HP no llenas (ej. zombies débiles).
        /// Llamar solo en host antes/durante Spawned.
        /// </summary>
        public void SetMaxHealth(float newMax, bool refillCurrent = true)
        {
            maxHealth = Mathf.Max(1f, newMax);
            if (HasStateAuthority && refillCurrent)
                CurrentHealth = maxHealth;
        }

        private void DelayedDespawn()
        {
            if (Object != null && Runner != null && HasStateAuthority)
                Runner.Despawn(Object);
        }
    }
}

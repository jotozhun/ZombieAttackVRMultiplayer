using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Implementado por cualquier entidad que pueda recibir daño (zombies, jugadores, props).
    /// Las llamadas a TakeDamage deben hacerse SOLO en el host (state authority del agresor).
    /// La implementación es responsable de validar y replicar el cambio de salud.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Aplica daño a esta entidad.
        /// </summary>
        /// <param name="damage">Cantidad de daño base (antes de críticos, ya calculados por el arma).</param>
        /// <param name="hitPoint">Punto exacto del impacto en world space.</param>
        /// <param name="hitNormal">Normal de la superficie impactada.</param>
        /// <param name="attacker">PlayerRef del jugador que originó el daño. PlayerRef.None para daño ambiental/NPC.</param>
        void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal, PlayerRef attacker);

        /// <summary>
        /// Útil para que las armas eviten apuntar a entidades muertas o impávidas.
        /// </summary>
        bool IsAlive { get; }
    }
}

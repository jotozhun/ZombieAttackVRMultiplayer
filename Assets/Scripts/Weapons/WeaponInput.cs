using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Input recolectado en el cliente y enviado al host vía Fusion para resolución determinista.
    /// Cada jugador empaqueta una instancia por tick. El host la consume en FixedUpdateNetwork.
    /// </summary>
    public struct WeaponInput : INetworkInput
    {
        public NetworkButtons buttons;

        // Posición y rotación del arma en el momento del input (para que el host
        // pueda hacer raycast desde donde el cliente la veía).
        public Vector3 weaponPosition;
        public Vector3 weaponForward;
    }

    /// <summary>
    /// Botones de WeaponInput. Index estable, no reordenar.
    /// </summary>
    public enum WeaponButton
    {
        Trigger = 0,    // Gatillo (disparo)
        Reload = 1,     // Botón secundario (Y/B)
        Grip = 2        // Reservado para futuro (agarre alternativo, etc.)
    }
}

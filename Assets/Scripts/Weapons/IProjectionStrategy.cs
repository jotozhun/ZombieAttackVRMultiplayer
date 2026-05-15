using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Estrategia que abstrae CÓMO un arma proyecta sus disparos.
    /// - SingleRayProjection: un raycast (pistola, rifle).
    /// - SpreadProjection: N raycasts en cono (escopeta).
    ///
    /// Todas las implementaciones DEBEN usar Runner.LagCompensation.Raycast en host
    /// para que los hits sean correctos con clientes de alta latencia.
    /// </summary>
    public interface IProjectionStrategy
    {
        /// <summary>
        /// Proyecta los disparos y devuelve los hits con lag compensation aplicada.
        /// </summary>
        /// <param name="runner">NetworkRunner para acceder a LagCompensation.</param>
        /// <param name="origin">Punto de origen (muzzle).</param>
        /// <param name="forward">Dirección base del disparo.</param>
        /// <param name="data">WeaponData del arma (range, pelletCount, spreadAngle).</param>
        /// <param name="attacker">PlayerRef del jugador que disparó. Usado por LagCompensation para rebobinar a su tick.</param>
        /// <param name="results">Lista donde se acumulan los hits. Se limpia al inicio.</param>
        void Project(NetworkRunner runner, Vector3 origin, Vector3 forward,
                     WeaponData data, PlayerRef attacker, List<LagCompensatedHit> results);
    }
}

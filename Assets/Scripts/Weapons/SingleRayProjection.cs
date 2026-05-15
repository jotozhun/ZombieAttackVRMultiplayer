using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// Un único raycast con lag compensation. Usada por pistolas, rifles, etc.
    /// </summary>
    public class SingleRayProjection : IProjectionStrategy
    {
        public void Project(NetworkRunner runner, Vector3 origin, Vector3 forward,
                            WeaponData data, PlayerRef attacker, List<LagCompensatedHit> results)
        {
            results.Clear();
            if (runner == null) return;

            const HitOptions options = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

            if (runner.LagCompensation.Raycast(
                    origin, forward, data.range,
                    player: attacker,
                    out LagCompensatedHit hit,
                    layerMask: -1,
                    options: options))
            {
                results.Add(hit);
            }
        }
    }
}

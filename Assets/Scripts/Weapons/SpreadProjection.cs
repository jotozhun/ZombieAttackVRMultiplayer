using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ZombieAttackVR.Weapons
{
    /// <summary>
    /// N raycasts en cono (escopeta). Cada pellet con dirección aleatoria dentro del spread.
    /// La aleatoriedad usa Runner.SimulationTime como seed para mantener determinismo en re-simulaciones.
    /// </summary>
    public class SpreadProjection : IProjectionStrategy
    {
        public void Project(NetworkRunner runner, Vector3 origin, Vector3 forward,
                            WeaponData data, PlayerRef attacker, List<LagCompensatedHit> results)
        {
            results.Clear();
            if (runner == null) return;

            const HitOptions options = HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority;

            int pellets = Mathf.Max(1, data.pelletCount);
            float halfAngleRad = Mathf.Deg2Rad * (data.spreadAngle * 0.5f);

            // Seed determinista basado en tick para que el cono sea reproducible.
            int seed = unchecked(runner.Tick * 397 ^ attacker.RawEncoded);
            var rng = new System.Random(seed);

            // Calcular ejes laterales una sola vez.
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.right;
            Vector3 up = Vector3.Cross(right, forward).normalized;

            for (int i = 0; i < pellets; i++)
            {
                // Dirección aleatoria dentro del cono.
                float u = (float)rng.NextDouble();
                float v = (float)rng.NextDouble();
                float angle = halfAngleRad * Mathf.Sqrt(u);
                float azimuth = 2f * Mathf.PI * v;

                Vector3 pelletDir =
                    forward * Mathf.Cos(angle) +
                    (right * Mathf.Cos(azimuth) + up * Mathf.Sin(azimuth)) * Mathf.Sin(angle);

                if (runner.LagCompensation.Raycast(
                        origin, pelletDir.normalized, data.range,
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
}

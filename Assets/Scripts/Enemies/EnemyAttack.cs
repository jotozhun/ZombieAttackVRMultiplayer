using Fusion;
using UnityEngine;
using ZombieAttackVR.Weapons;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack stats")]
    [SerializeField] public float _attackRange { get; set; } = 1.2f;
    [SerializeField] public float _attackDamage { get; set; } = 10f;
    [SerializeField] private float _gapBetweenAttacks = 1.5f;

    private float _lastAttackTime;
    private IDamageable _cachedTarget;
    private Transform _cachedTargetTransform;

    void Start()
    {
        _lastAttackTime = -_gapBetweenAttacks; // Permite ataque inmediato al inicio
    }

    /// <summary>
    /// Ejecuta un golpe contra el objetivo. La animación se reproduce siempre;
    /// el daño solo se aplica si ya pasó el cooldown.
    /// El daño se delega al IDamageable del target — solo el host (state authority)
    /// efectivamente mutará la salud (ver Health.TakeDamage).
    /// </summary>
    public void Attack(Animator animator, Transform target)
    {
        if (Time.time - _lastAttackTime < _gapBetweenAttacks)
            return;

        animator.SetTrigger("attack");
        _lastAttackTime = Time.time;

        if (target == null)
        {
            Debug.LogWarning("[EnemyAttack] Target nulo, no se aplica daño.");
            return;
        }

        // Cachea el IDamageable mientras sea el mismo target.
        if (_cachedTargetTransform != target)
        {
            _cachedTarget = target.GetComponentInParent<IDamageable>();
            _cachedTargetTransform = target;
        }

        if (_cachedTarget == null || !_cachedTarget.IsAlive)
            return;

        Vector3 hitPoint = target.position;
        Vector3 hitNormal = (transform.position - target.position).normalized;
        _cachedTarget.TakeDamage(_attackDamage, hitPoint, hitNormal, PlayerRef.None);
    }
}

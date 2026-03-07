using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack stats")]
    [SerializeField] public float _attackRange { get; set; } = 1.2f; 
    [SerializeField] public float _attackDamage { get; set; } = 10f;
    [SerializeField] private float _gapBetweenAttacks = 1.5f;

    private float _lastAttackTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _lastAttackTime = -_gapBetweenAttacks; // Permite ataque inmediato al inicio
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Attack(Animator animator){
        // Solo hace daño si ha pasado el tiempo de cooldown
        if (Time.time - _lastAttackTime >= _gapBetweenAttacks)
        {
            // Siempre reproduce la animación de ataque
            animator.SetTrigger("attack");
            Debug.Log("Attacking player with damage!");
            // Aquí iría la lógica de daño al player
            // Por ejemplo: playerHealth.TakeDamage(_attackDamage);
            
            _lastAttackTime = Time.time; // Resetea el timer
        }
        // else
        // {
        //     Debug.Log("Attack on cooldown, only animation played.");
        // }
    }

}

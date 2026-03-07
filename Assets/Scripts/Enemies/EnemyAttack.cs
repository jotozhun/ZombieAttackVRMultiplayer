using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack stats")]
    [SerializeField] public float _attackRange { get; set; } = 1.2f; 
    [SerializeField] public float _attackDamage { get; set; } = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Attack(){
        Debug.Log("Attacking player!");
    }

}

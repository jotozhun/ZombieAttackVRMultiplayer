using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BasicZombie : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    private NavMeshAgent _agent;
    private Animator zombieAnimator;
    private int _currentPatrolIndex;
    private bool _isWaiting;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        zombieAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }

    void Update()
    {
        Patrol();
        UpdateAnimations();
    }

    private void Patrol()
    {
        if (_isWaiting) return;

        if (!_agent.pathPending && _agent.remainingDistance < stopAtDistance)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }        
        
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        _agent.isStopped = false;
    
        GoToNextPatrolPoint();
        _isWaiting = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        // If no more points, change to first point
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateAnimations()
    {
        var isMoving = _agent.velocity.magnitude > 0.1f;
        zombieAnimator.SetBool("isWalking", isMoving);
    }

    // protected void OnTriggerEnter(Collider other)
    // {  
    //     if(other.gameObject.CompareTag("Player"))
    //     {
    //         zombieAnimator.SetBool("isPlayerClose", true);
    //     }
    // }

    // protected void OnTriggerExit(Collider other)
    // {
    //     if(other.gameObject.CompareTag("Player"))
    //     {
    //         zombieAnimator.SetBool("isPlayerClose", false);
    //     }
    // }
}

using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrolling,
    Following,
    Attacking
}

public class EnemyPatrol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private EnemyAttack _enemyAttack;

    [Header("Patrol stats")]
    [SerializeField] private float _patrolWaitTime = 2f;
    [SerializeField] private float _stopAtDistance = 0.5f;
    [SerializeField] private float _detectionRange = 5f;
    [SerializeField] private float _viewAngle = 120f;
    [SerializeField] private float _losePlayerTime = 3f;

    private NavMeshAgent _navAgent;
    private Animator _zombieAnimator;
    [SerializeField] private EnemyState _state = EnemyState.Patrolling;
    private int _currentPatrolIndex;
    private float _timeSinceLostPlayer;

    private bool _isWaiting;
    private float _startWaitingPatrolTime;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _zombieAnimator = GetComponent<Animator>();
        _enemyAttack = GetComponent<EnemyAttack>();
    }

    void Start()
    {
        _startWaitingPatrolTime = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        var distanceToPlayer = Vector3.Distance(_player.position, transform.position);
        switch (_state)
        {
            case EnemyState.Patrolling:
                Patrol();
        //         if (distanceToPlayer <= _detectionRange && CanSeePlayer())
        //         {
        //             _state = EnemyState.Following;
        //         }
                break;
        //     case EnemyState.Following:
        //         FollowPlayer();
        //         if (distanceToPlayer <= _attackRange)
        //         {
        //             _state = EnemyState.Attacking;
        //             StartAttack();
        //         }
        //         if (!CanSeePlayer())
        //         {
        //             _timeSinceLostPlayer += Time.deltaTime;
        //             if (_timeSinceLostPlayer >= _losePlayerTime)
        //             {
        //                 _state = EnemyState.Patrolling;
        //                 GoToClosesPatrolPoint();
        //             }
        //         }
        //         else
        //         {
        //             _timeSinceLostPlayer = 0f;
        //         }
        //         break;
        //     case EnemyState.Attacking:
        //         _enemyAttack.Attack();
        //         if (distanceToPlayer > _enemyAttack._attackRange)
        //         {
        //             _state = EnemyState.Following;
        //             _navAgent.isStopped = false;
        //         }
        //         break;
        }
        // UpdateAnimations();
    }

    /// <summary>
    /// This method handles patrolling behavior only when Patrol state is active.
    /// </summary>
    private void Patrol()
    {
        if (!_navAgent.pathPending && _navAgent.remainingDistance < _stopAtDistance)
        {
            WaitAtPatrolPoint();
        }
    }

    /// <summary>
    /// This method handles the waiting behavior when reaching a patrol point. 
    /// </summary>
    /// <remarks>
    /// The enemy will wait for a specified time in a patrol point, then continue to the next 
    /// patrol point. Also, it stays alert to the player to start following or attacking.
    /// </remarks>
    private void WaitAtPatrolPoint()
    {
        if (_isWaiting)
        {
            _startWaitingPatrolTime += Time.deltaTime;
            if (_startWaitingPatrolTime >= _patrolWaitTime)
            {
                _isWaiting = false;
                GoToNextPatrolPoint();
                _navAgent.isStopped = false;
            }
        } else
        {
            _startWaitingPatrolTime = 0.0f;
            _navAgent.isStopped = true;
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (_patrolPoints.Length == 0) return;

        _navAgent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
        // If no more points, change to first point
        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
    }
}

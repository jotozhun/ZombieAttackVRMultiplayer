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
    [SerializeField] private Transform _targetActual;
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private EnemyAttack _enemyAttack;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private LayerMask _obstacleMask;

    private float _nextCheckTimeForPlayer;
    private float _cosHalfViewAngle;
    [SerializeField] private Collider[] _playersInRange = new Collider[10];

    [Header("Patrol stats")]
    [SerializeField] private float _checkFrequency = 0.2f;
    [SerializeField] private float _patrolWaitTime = 2f;
    [SerializeField] private float _stopAtDistance = 0.5f;
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private float _losePlayerTime = 3f;

    private NavMeshAgent _navAgent;
    private Animator _zombieAnimator;
    [SerializeField] private EnemyState _state = EnemyState.Patrolling;
    private int _currentPatrolIndex;
    private float _timeSinceLostPlayer;

    private bool _isWaiting = false;
    private float _startWaitingPatrolTime;
    private float _distanceToPlayer;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _zombieAnimator = GetComponent<Animator>();
        _enemyAttack = GetComponent<EnemyAttack>();
    }

    void Start()
    {
        _startWaitingPatrolTime = 0.0f;
        _cosHalfViewAngle = Mathf.Cos(_viewAngle * 0.5f * Mathf.Deg2Rad);
        GoToNextPatrolPoint();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= _nextCheckTimeForPlayer)
        {
            Debug.Log("Checking for player...");
            _nextCheckTimeForPlayer = Time.time + _checkFrequency;
            if (ExecuteVisionCheck())
            {
                _state = EnemyState.Following;
            }
            else
            {
                if (_state == EnemyState.Following)
                {
                    _timeSinceLostPlayer += Time.deltaTime;
                    if (_timeSinceLostPlayer >= _losePlayerTime)
                    {
                        Debug.Log("Lost sight of player, returning to patrol.");
                        _state = EnemyState.Patrolling;
                        _targetActual = null;
                        GoToClosesPatrolPoint();
                    }
                }
                else
                {
                    _timeSinceLostPlayer = 0f;
                }
            }
        }
        switch (_state)
        {
            case EnemyState.Patrolling:
                Patrol();
                break;
            case EnemyState.Following:
                FollowPlayer();
                // if (distanceToPlayer <= _attackRange)
                // {
                //     _state = EnemyState.Attacking;
                //     StartAttack();
                // }
                break;
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
    /// This method finds the closest patrol point to the enemy after losing sight of the player 
    /// and sets it as the next destination for the NavMeshAgent.
    /// </summary>
    private void GoToClosesPatrolPoint()
    {
        if (_patrolPoints.Length == 0) return;
        var closestIndex = 0;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < _patrolPoints.Length; i++)
        {
            var distance = Vector3.Distance(transform.position, _patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        _currentPatrolIndex = closestIndex;
        _navAgent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
    }

    /// <summary>
    /// This method sets the player's position as the destination for the NavMeshAgent, allowing the enemy to follow the player.
    /// </summary>
    public void FollowPlayer()
    {
        _navAgent.SetDestination(_targetActual.position); // Offset to prevent getting too close
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
            _isWaiting = true;
        }
    }

    /// <summary>
    /// This method sets the next patrol point to the NavMeshAgent attached to the enemy.
    /// It also updates the current patrol index to loop through the patrol points.
    /// </summary>
    private void GoToNextPatrolPoint()
    {
        if (_patrolPoints.Length == 0) return;

        _navAgent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
        // If no more points, change to first point
        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
    }

    private bool ExecuteVisionCheck() {
        // 1. Proximidad (Filtro ultra rápido)
        int count = Physics.OverlapSphereNonAlloc(transform.position, _detectionRange, _playersInRange, _playerLayer);
        
        for (int i = 0; i < count; i++) {
            Transform t = _playersInRange[i].transform;
            Vector3 diff = t.position - transform.position;
            Vector3 dir = diff.normalized;

            // 2. Ángulo (Dot Product)
            if (Vector3.Dot(transform.forward, dir) >= _cosHalfViewAngle) {
                // 3. Obstáculos (Raycast - Solo si pasó lo anterior)
                float dist = diff.magnitude;
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
                Vector3 rayDest = t.position + Vector3.up * 1.5f;

                if (!Physics.Raycast(rayOrigin, (rayDest - rayOrigin).normalized, dist, _obstacleMask)) {
                    _targetActual = t; // Guardamos a quién estamos viendo
                    return true; 
                }
            }
        }
        // _targetActual = null;
        return false;
    }

}

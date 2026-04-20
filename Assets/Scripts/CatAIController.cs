using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CreatureMover))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CatMood))]
[RequireComponent(typeof(CatNeeds))]
[RequireComponent(typeof(CatSenses))]
public class CatAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private Vector2 idlePauseRange = new(1f, 3f);

    [Header("Follow Player")]
    [SerializeField] private float followDistance = 8f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float runDistance = 4f;
    [SerializeField, Range(0f, 1f)] private float farFollowChance = 0.20f;

    [Header("Flee")]
    [SerializeField] private float fleeActivationDistance = 3f;
    [SerializeField] private float fleeRadius = 8f;
    [SerializeField] private float fleeCooldown = 6f;

    [Header("Play")]
    [SerializeField] private float toyStopDistance = 0.8f;
    [SerializeField] private float playDuration = 8f;

    [Header("Needs")]
    [SerializeField] private float bowlStopDistance = 0.7f;
    [SerializeField] private float litterStopDistance = 0.7f;
    [SerializeField] private float eatDuration = 4f;
    [SerializeField] private float drinkDuration = 3f;
    [SerializeField] private float litterDuration = 5f;

    [Header("Look-at")]
    [SerializeField] private float lookAtMaxDistance = 10f;

    [Header("Decision Timing")]
    [SerializeField] private float decisionInterval = 1.0f;

    [Header("NavMesh Throttle")]
    [SerializeField] private float destinationUpdateThreshold = 0.25f;

    [Header("Arrival Behaviour")]
    [SerializeField] private float arrivalDistance = 0.5f;
    [SerializeField] private Vector2 rubDurationRange = new(1f, 2.5f);
    [SerializeField] private float nudgeDistance = 1.2f;

    private enum CatState
    {
        Wander, FollowPlayer, PlayWithToy,
        EatFood, DrinkWater, UseLitterBox,
        Flee, Busy, CommandedInteract
    }

    private CatState _state = CatState.Wander;
    private CatState _busyReturnTo = CatState.Wander;

    private float _nextDecisionTime;
    private float _waitUntilTime;
    private float _busyUntilTime;
    private float _fleeAvailableTime;
    private Vector3 _homePosition;
    private Vector3 _lastSetDestination;

    private System.Action _pendingBusyCallback;

    private bool _arrivedAtDestination;
    private float _rubUntilTime;

    private Transform _commandTarget;
    private PetInteractable _commandInteractable;
    private PetStats _commandPetStats;
    private float _commandStopDist;
    private float _commandBusyDuration;

    private CreatureMover _mover;
    private NavMeshAgent _agent;
    private CatMood _mood;
    private CatNeeds _needs;
    private CatSenses _senses;

    public Transform Player => player;

    private void Awake()
    {
        _mover = GetComponent<CreatureMover>();
        _agent = GetComponent<NavMeshAgent>();
        _mood = GetComponent<CatMood>();
        _needs = GetComponent<CatNeeds>();
        _senses = GetComponent<CatSenses>();

        _homePosition = transform.position;

        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.stoppingDistance = stopDistance;
        _agent.autoBraking = true;
        _agent.autoRepath = true;
    }

    private void Start()
    {
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError(name + " is not on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        _agent.Warp(transform.position);
        PickNewWanderPoint();
    }

    private void Update()
    {
        if (Time.time >= _nextDecisionTime)
        {
            _nextDecisionTime = Time.time + decisionInterval;
            DecideState();
        }

        DriveMovement();
        _agent.nextPosition = transform.position;
    }

    public void GoToAndInteract(
        Transform target,
        PetInteractable interactable,
        PetStats pet,
        float stopDist = 1f,
        float busyDuration = 3f)
    {
        if (target == null || interactable == null || pet == null) return;

        _commandTarget = target;
        _commandInteractable = interactable;
        _commandPetStats = pet;
        _commandStopDist = stopDist;
        _commandBusyDuration = busyDuration;

        TransitionTo(CatState.CommandedInteract);
    }

    public void OnPatted() => _mood.Reward(4f);
    public void OnTreatGiven() => _mood.Reward(10f);
    public void OnStartled() => _mood.OnStartled();

    private void DecideState()
    {
        if (_state == CatState.Busy || _state == CatState.CommandedInteract)
        {
            if (_state == CatState.Busy && Time.time >= _busyUntilTime)
                TransitionTo(_busyReturnTo);
            return;
        }

        if (_mood.IsWary && player != null &&
            DistanceTo(player) < fleeActivationDistance &&
            Time.time >= _fleeAvailableTime)
        {
            TransitionTo(CatState.Flee);
            return;
        }

        switch (_needs.MostUrgentNeed)
        {
            case NeedType.Litter when _senses.NearestLitter != null:
                TransitionTo(CatState.UseLitterBox); return;
            case NeedType.Food when _senses.NearestFood != null:
                TransitionTo(CatState.EatFood); return;
            case NeedType.Water when _senses.NearestWater != null:
                TransitionTo(CatState.DrinkWater); return;
        }

        if (_mood.Affinity >= _mood.playThreshold && _senses.NearestToy != null)
        {
            TransitionTo(CatState.PlayWithToy); return;
        }

        if (player != null && _mood.Affinity >= _mood.followThreshold)
        {
            float dist = DistanceTo(player);
            if (dist <= followDistance || Random.value < farFollowChance)
            {
                TransitionTo(CatState.FollowPlayer); return;
            }
        }

        if (_state != CatState.Wander)
            TransitionTo(CatState.Wander);
        else if (!_agent.hasPath || _agent.remainingDistance <= stopDistance + 0.2f)
        {
            _waitUntilTime = Time.time + Random.Range(idlePauseRange.x, idlePauseRange.y);
            PickNewWanderPoint();
        }
    }

    private void TransitionTo(CatState next)
    {
        if (_state == next) return;

        _state = next;
        _arrivedAtDestination = false;
        _rubUntilTime = 0f;

        switch (next)
        {
            case CatState.Flee:
                SetFleeDestination();
                break;
            case CatState.EatFood:
                if (_senses.NearestFood != null)
                    SetDestinationNear(_senses.NearestFood.position, bowlStopDistance, force: true);
                break;
            case CatState.DrinkWater:
                if (_senses.NearestWater != null)
                    SetDestinationNear(_senses.NearestWater.position, bowlStopDistance, force: true);
                break;
            case CatState.UseLitterBox:
                if (_senses.NearestLitter != null)
                    SetDestinationNear(_senses.NearestLitter.position, litterStopDistance, force: true);
                break;
            case CatState.PlayWithToy:
                if (_senses.NearestToy != null)
                    SetDestinationNear(_senses.NearestToy.position, toyStopDistance, force: true);
                _waitUntilTime = Time.time + playDuration;
                break;
            case CatState.CommandedInteract:
                if (_commandTarget != null)
                    SetDestinationNear(_commandTarget.position, _commandStopDist, force: true);
                break;
            case CatState.Wander:
                PickNewWanderPoint();
                break;
        }
    }

    private void DriveMovement()
    {
        switch (_state)
        {
            case CatState.FollowPlayer when player != null:
                SetDestinationNear(player.position, stopDistance);
                if (DistanceTo(player) <= stopDistance + 0.3f)
                    _waitUntilTime = Time.time + 0.5f;
                break;

            case CatState.Flee:
                if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
                {
                    _fleeAvailableTime = Time.time + fleeCooldown;
                    TransitionTo(CatState.Wander);
                }
                break;

            case CatState.EatFood:
                if (_senses.IsNearTarget(_senses.NearestFood, bowlStopDistance + 0.1f))
                    StartBusy(eatDuration, CatState.Wander, () =>
                    {
                        _needs.Eat();
                        _mood.OnFed();
                        PetStats.Instance?.ModifyStat("hunger", 30f);
                    });
                break;

            case CatState.DrinkWater:
                if (_senses.IsNearTarget(_senses.NearestWater, bowlStopDistance + 0.1f))
                    StartBusy(drinkDuration, CatState.Wander, () =>
                    {
                        _needs.Drink();
                        _mood.OnWatered();
                        PetStats.Instance?.ModifyStat("thirst", 40f);
                    });
                break;

            case CatState.UseLitterBox:
                if (_senses.IsNearTarget(_senses.NearestLitter, litterStopDistance + 0.1f))
                    StartBusy(litterDuration, CatState.Wander, () => _needs.RelieveBladder());
                break;

            case CatState.PlayWithToy:
                if (_senses.NearestToy != null)
                    SetDestinationNear(_senses.NearestToy.position, toyStopDistance);
                if (Time.time > _waitUntilTime)
                    TransitionTo(CatState.Wander);
                break;

            case CatState.CommandedInteract when _commandTarget != null:
                SetDestinationNear(_commandTarget.position, _commandStopDist);
                if (_senses.IsNearTarget(_commandTarget, _commandStopDist + 0.2f))
                {
                    var interactable = _commandInteractable;
                    var pet = _commandPetStats;
                    float busy = _commandBusyDuration;
                    ClearCommandedState();
                    StartBusy(busy, CatState.Wander, () => interactable.Interact(pet));
                }
                break;

            case CatState.Wander:
                CheckWanderArrival();
                break;
        }

        Vector3 lookTarget = ComputeLookTarget();

        Vector3 moveDir = _agent.desiredVelocity;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude < 0.001f)
        {
            Vector3 steer = _agent.hasPath ? _agent.steeringTarget : transform.position;
            moveDir = steer - transform.position;
            moveDir.y = 0f;
        }

        // Rubbing: cat is stopped, facing slightly away from the object it nudged
        bool isRubbing = Time.time < _rubUntilTime;
        bool isWaiting = Time.time < _waitUntilTime || _state == CatState.Busy || isRubbing;
        bool hasVelocity = _agent.velocity.sqrMagnitude > 0.04f;
        bool shouldMove = !isWaiting && moveDir.magnitude > 0.1f && hasVelocity;

        if (!shouldMove && player != null && _mood.IsFriendly)
            lookTarget = player.position;

        bool shouldRun =
            (_state == CatState.FollowPlayer && player != null && DistanceTo(player) > runDistance) ||
            _state == CatState.Flee;

        _mover.SetInput(shouldMove ? new Vector2(0f, 1f) : Vector2.zero, lookTarget, shouldRun, false);
    }

    // Called every frame while wandering. Detects when the cat has reached its
    // destination, stops it, runs a short rub pause, then nudges it away before
    // picking a new wander point. This prevents the cat from walking into objects.
    private void CheckWanderArrival()
    {
        // Already rubbing — wait it out then pick a new point
        if (Time.time < _rubUntilTime)
            return;

        if (_arrivedAtDestination)
        {
            // Rub finished — nudge away from the object and pick a new wander point
            _arrivedAtDestination = false;
            NudgeAwayFromObstacle();
            _waitUntilTime = Time.time + Random.Range(idlePauseRange.x, idlePauseRange.y);
            PickNewWanderPoint();
            return;
        }

        if (!_agent.pathPending && _agent.hasPath &&
            _agent.remainingDistance <= arrivalDistance)
        {
            // Arrived — stop and start the rub pause
            _agent.ResetPath();
            _arrivedAtDestination = true;
            _rubUntilTime = Time.time + Random.Range(rubDurationRange.x, rubDurationRange.y);
        }
    }

    // Picks a point slightly behind and to the side of the cat's current facing
    // so it turns and walks away from whatever it just reached, rather than
    // immediately charging back into it.
    private void NudgeAwayFromObstacle()
    {
        Vector3 back = -transform.forward;
        float angle = Random.Range(-60f, 60f);
        Vector3 nudge = Quaternion.AngleAxis(angle, Vector3.up) * back * nudgeDistance;
        Vector3 target = transform.position + nudge;

        if (TrySampleNavMeshPoint(target, 2f, out Vector3 pt))
        {
            _agent.SetDestination(pt);
            _lastSetDestination = pt;
        }
    }

    private void ClearCommandedState()
    {
        _commandTarget = null;
        _commandInteractable = null;
        _commandPetStats = null;
    }

    private Vector3 ComputeLookTarget()
    {
        Vector3 defaultLook = transform.position + transform.forward * 5f;
        if (player == null) return defaultLook;

        float dist = DistanceTo(player);
        if (dist > lookAtMaxDistance || _state == CatState.Flee) return defaultLook;

        float affinityWeight = Mathf.InverseLerp(0f, 100f, _mood.Affinity);
        float proximityWeight = 1f - Mathf.Clamp01(dist / lookAtMaxDistance);
        float lookWeight = Mathf.Clamp01(affinityWeight * 0.6f + proximityWeight * 0.4f);

        if (_mood.IsWary) lookWeight *= 0.3f;

        Vector3 playerEyeLevel = player.position + Vector3.up * 1.2f;
        return Vector3.Lerp(defaultLook, playerEyeLevel, lookWeight);
    }

    private void StartBusy(float duration, CatState returnState, System.Action onComplete = null)
    {
        if (_state == CatState.Busy) return;

        _busyUntilTime = Time.time + duration;
        _busyReturnTo = returnState;
        _pendingBusyCallback = onComplete;
        _state = CatState.Busy;

        _agent.ResetPath();
        _waitUntilTime = _busyUntilTime;

        StartCoroutine(BusyCallback(duration));
    }

    private IEnumerator BusyCallback(float delay)
    {
        yield return new WaitForSeconds(delay);
        _pendingBusyCallback?.Invoke();
        _pendingBusyCallback = null;
    }

    private void PickNewWanderPoint()
    {
        if (TryGetRandomNavMeshPoint(_homePosition, wanderRadius, out Vector3 pt))
        {
            _agent.SetDestination(pt);
            _lastSetDestination = pt;
        }
    }

    private void SetDestinationNear(Vector3 worldPos, float nearRadius = 1.5f, bool force = false)
    {
        if (!force && Vector3.SqrMagnitude(worldPos - _lastSetDestination) <
                      destinationUpdateThreshold * destinationUpdateThreshold)
            return;

        if (TrySampleNavMeshPoint(worldPos, Mathf.Max(nearRadius, 1.5f), out Vector3 pt))
        {
            _agent.SetDestination(pt);
            _lastSetDestination = worldPos;
        }
    }

    private void SetFleeDestination()
    {
        if (player == null) { PickNewWanderPoint(); return; }

        Vector3 awayDir = (transform.position - player.position).normalized;
        Vector3 candidate = transform.position + awayDir * fleeRadius;

        if (TrySampleNavMeshPoint(candidate, 3f, out Vector3 pt))
        {
            _agent.SetDestination(pt);
            _lastSetDestination = pt;
        }
        else
        {
            PickNewWanderPoint();
        }
    }

    private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 point)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);
            if (TrySampleNavMeshPoint(candidate, 2f, out point)) return true;
        }
        point = transform.position;
        return false;
    }

    private bool TrySampleNavMeshPoint(Vector3 target, float maxDist, out Vector3 point)
    {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        point = transform.position;
        return false;
    }

    private float DistanceTo(Transform target) =>
        Vector3.Distance(transform.position, target.position);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(0.4f, 0.7f, 1f, 0.12f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, followDistance);

        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.2f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, fleeActivationDistance);

        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"{_state} | Affinity: {(_mood != null ? _mood.Affinity : 0f):F0}");
    }
#endif
}
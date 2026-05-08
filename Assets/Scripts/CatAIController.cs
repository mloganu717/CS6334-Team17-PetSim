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
    [SerializeField, Min(0.15f)] private float toyStopDistance = 0.8f;
    [SerializeField] private float playDuration = 8f;
    [SerializeField] private float lowHappinessPlayThreshold = 45f;

    [Header("PetStats feedback (matches stats card / UI)")]
    [SerializeField] private float lowPetHungerThreshold = 40f;
    [SerializeField] private float lowPetThirstThreshold = 40f;
    [SerializeField] private float lowPetEnergyThreshold = 35f;

    [Header("Needs")]
    [SerializeField, Min(0.15f)] private float bowlStopDistance = 0.7f;
    [SerializeField, Min(0.15f)] private float litterStopDistance = 0.7f;
    [SerializeField] private float eatDuration = 4f;
    [SerializeField] private float drinkDuration = 3f;
    [SerializeField] private float litterDuration = 5f;
    [SerializeField, Min(0.05f)] private float interactArrivalPadding = 0.22f;
    [SerializeField, Min(0.1f)] private float navSamplePadding = 0.32f;

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

    private enum DesireType
    {
        None,
        Food,
        Water,
        Litter,
        Play
    }

    private CatState _state = CatState.Wander;
    private CatState _busyReturnTo = CatState.Wander;

    private bool _wasHungry;
    private bool _wasThirsty;
    private bool _wasNeedingLitter;
    private bool _wasUnhappy;
    private bool _wasSleepy;

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

    private Transform _lockedFood;
    private Transform _lockedWater;
    private Transform _lockedLitter;
    private Transform _lockedToy;

    private float _navDefaultStoppingDistance;

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
        _navDefaultStoppingDistance = stopDistance;
        _agent.stoppingDistance = stopDistance;
        _agent.autoBraking = true;
        _agent.autoRepath = true;
    }

    private void Start()
    {
        if (!_agent.isOnNavMesh)
        {
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
        UpdateDesireTimestamps();

        if (_state == CatState.Busy || _state == CatState.CommandedInteract)
        {
            if (_state == CatState.Busy && Time.time >= _busyUntilTime)
            {
                TransitionTo(_busyReturnTo);
            }

            return;
        }

        if (_mood.IsWary && player != null &&
            DistanceTo(player) < fleeActivationDistance &&
            Time.time >= _fleeAvailableTime)
        {
            TransitionTo(CatState.Flee);
            return;
        }

        if (IsActiveNeedChaseState(_state))
        {
            if (StillHasTargetForNeedState(_state) &&
                !ShouldInterruptChaseForStrongerNeed(_state))
                return;
        }

        DesireType desire = GetStrongestActiveDesire();

        switch (desire)
        {
            case DesireType.Food:
                if (_senses.NearestFood != null)
                {
                    TransitionTo(CatState.EatFood);
                    return;
                }
                break;

            case DesireType.Water:
                if (_senses.NearestWater != null)
                {
                    TransitionTo(CatState.DrinkWater);
                    return;
                }
                break;

            case DesireType.Litter:
                if (_senses.NearestLitter != null)
                {
                    TransitionTo(CatState.UseLitterBox);
                    return;
                }
                break;

            case DesireType.Play:
                if (_senses.NearestToy != null)
                {
                    TransitionTo(CatState.PlayWithToy);
                    return;
                }
                break;
        }

        if (player != null && _mood.Affinity >= _mood.followThreshold)
        {
            float dist = DistanceTo(player);

            if (dist <= followDistance || Random.value < farFollowChance)
            {
                TransitionTo(CatState.FollowPlayer);
                return;
            }
        }

        if (_state != CatState.Wander)
        {
            TransitionTo(CatState.Wander);
        }
        else if (!_agent.hasPath || _agent.remainingDistance <= stopDistance + 0.2f)
        {
            _waitUntilTime = Time.time + Random.Range(idlePauseRange.x, idlePauseRange.y);
            PickNewWanderPoint();
        }
    }

    private void UpdateDesireTimestamps()
    {
        PetStats petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        bool wantsFood = _needs.IsHungry ||
                         (petStats != null && petStats.Hunger < lowPetHungerThreshold);
        bool wantsWater = _needs.IsThirsty ||
                          (petStats != null && petStats.Thirst < lowPetThirstThreshold);
        bool needsLitter = _needs.NeedsLitter;
        bool isUnhappy = petStats != null && petStats.Happiness <= lowHappinessPlayThreshold;
        bool isSleepy = petStats != null && petStats.Energy < lowPetEnergyThreshold;

        if (wantsFood && !_wasHungry)
            petStats?.RaiseFeedback(
                "The cat is hungry.");

        if (wantsWater && !_wasThirsty)
            petStats?.RaiseFeedback(
                "The cat is thirsty.");

        if (needsLitter && !_wasNeedingLitter)
            petStats?.RaiseFeedback(
                "Needs the litter box.");

        if (isUnhappy && !_wasUnhappy)
            petStats?.RaiseFeedback(
                "The cat is bored and wants to play.");

        if (isSleepy && !_wasSleepy)
            petStats?.RaiseFeedback(
                "The cat is sleepy and wants to rest.");

        _wasHungry = wantsFood;
        _wasThirsty = wantsWater;
        _wasNeedingLitter = needsLitter;
        _wasUnhappy = isUnhappy;
        _wasSleepy = isSleepy;
    }

    private static bool IsActiveNeedChaseState(CatState s) =>
        s == CatState.EatFood || s == CatState.DrinkWater ||
        s == CatState.UseLitterBox || s == CatState.PlayWithToy;

    private bool StillHasTargetForNeedState(CatState s)
    {
        return s switch
        {
            CatState.EatFood => ResolveFoodTarget() != null,
            CatState.DrinkWater => ResolveWaterTarget() != null,
            CatState.UseLitterBox => ResolveLitterTarget() != null,
            CatState.PlayWithToy => ResolveToyTarget() != null,
            _ => false
        };
    }

    private DesireType GetStrongestActiveDesire()
    {
        PetStats petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        bool wantsFood = _needs.IsHungry ||
                         (petStats != null && petStats.Hunger < lowPetHungerThreshold);
        bool wantsWater = _needs.IsThirsty ||
                          (petStats != null && petStats.Thirst < lowPetThirstThreshold);
        bool needsLitter = _needs.NeedsLitter;
        bool isUnhappy = petStats != null && petStats.Happiness <= lowHappinessPlayThreshold;

        if (needsLitter) return DesireType.Litter;
        if (wantsFood) return DesireType.Food;
        if (wantsWater) return DesireType.Water;
        if (isUnhappy) return DesireType.Play;
        return DesireType.None;
    }

    private static DesireType DesireForNeedState(CatState s) =>
        s switch
        {
            CatState.EatFood => DesireType.Food,
            CatState.DrinkWater => DesireType.Water,
            CatState.UseLitterBox => DesireType.Litter,
            CatState.PlayWithToy => DesireType.Play,
            _ => DesireType.None
        };

    private static int DesireRank(DesireType d) =>
        d switch
        {
            DesireType.Litter => 4,
            DesireType.Food => 3,
            DesireType.Water => 2,
            DesireType.Play => 1,
            _ => 0
        };

    private bool ShouldInterruptChaseForStrongerNeed(CatState current)
    {
        DesireType strongest = GetStrongestActiveDesire();
        DesireType chasing = DesireForNeedState(current);
        return DesireRank(strongest) > DesireRank(chasing);
    }

    private static bool TargetAlive(Transform t) =>
        t != null && t.gameObject.activeInHierarchy;

    private void ClearInteractLocks()
    {
        _lockedFood = _lockedWater = _lockedLitter = _lockedToy = null;
    }

    private Transform ResolveFoodTarget() =>
        TargetAlive(_lockedFood) ? _lockedFood : _senses.NearestFood;

    private Transform ResolveWaterTarget() =>
        TargetAlive(_lockedWater) ? _lockedWater : _senses.NearestWater;

    private Transform ResolveLitterTarget() =>
        TargetAlive(_lockedLitter) ? _lockedLitter : _senses.NearestLitter;

    private Transform ResolveToyTarget() =>
        TargetAlive(_lockedToy) ? _lockedToy : _senses.NearestToy;

    private void TransitionTo(CatState next)
    {
        if (_state == next)
            return;

        _state = next;
        _arrivedAtDestination = false;
        _rubUntilTime = 0f;

        switch (next)
        {
            case CatState.Flee:
                ClearInteractLocks();
                PetStats.Instance?.RaiseFeedback("The cat got startled and ran away.");
                SetFleeDestination();
                break;

            case CatState.FollowPlayer:
                ClearInteractLocks();
                break;

            case CatState.EatFood:
                ClearInteractLocks();
                _lockedFood = _senses.NearestFood;
                PetStats.Instance?.RaiseFeedback("The cat is going to the food bowl.");
                if (_lockedFood != null)
                    SetDestinationNear(_lockedFood.position, EffectiveBowlStandoff(), force: true);
                break;

            case CatState.DrinkWater:
                ClearInteractLocks();
                _lockedWater = _senses.NearestWater;
                PetStats.Instance?.RaiseFeedback("The cat is going to the water bowl.");
                if (_lockedWater != null)
                    SetDestinationNear(_lockedWater.position, EffectiveBowlStandoff(), force: true);
                break;

            case CatState.UseLitterBox:
                ClearInteractLocks();
                _lockedLitter = _senses.NearestLitter;
                PetStats.Instance?.RaiseFeedback("The cat is going to the litter box.");
                if (_lockedLitter != null)
                    SetDestinationNear(_lockedLitter.position, EffectiveLitterStandoff(), force: true);
                break;

            case CatState.PlayWithToy:
                ClearInteractLocks();
                _lockedToy = _senses.NearestToy;
                PetStats.Instance?.RaiseFeedback("The cat is going to play with the ball.");
                if (_lockedToy != null)
                    SetDestinationNear(_lockedToy.position, EffectiveToyStandoff(), force: true);
                _waitUntilTime = 0f;
                break;

            case CatState.CommandedInteract:
                ClearInteractLocks();
                if (_commandTarget != null)
                    SetDestinationNear(_commandTarget.position, _commandStopDist, force: true);
                break;

            case CatState.Wander:
                ClearInteractLocks();
                PickNewWanderPoint();
                break;
        }

        SyncAgentStoppingDistance(next);
    }

    private void DriveMovement()
    {
        switch (_state)
        {
            case CatState.FollowPlayer when player != null:
                SetDestinationNear(player.position, stopDistance);

                if (HorizontalDistanceTo(player) <= stopDistance + 0.3f)
                {
                    _agent.ResetPath();
                    _waitUntilTime = Time.time + 0.5f;
                }
                break;

            case CatState.Flee:
                if (!_agent.pathPending && (!_agent.hasPath || _agent.remainingDistance < 0.5f))
                {
                    _fleeAvailableTime = Time.time + fleeCooldown;
                    TransitionTo(CatState.Wander);
                }
                break;

            case CatState.EatFood:
                {
                    Transform food = ResolveFoodTarget();
                    if (food != null)
                    {
                        float stand = EffectiveBowlStandoff();
                        SetDestinationNear(food.position, stand);

                        if (ReachedInteractTarget(food, stand))
                        {
                            StartBusy(eatDuration, CatState.Wander, () =>
                            {
                                _needs.Eat();
                                _mood.OnFed();
                                PetStats.Instance?.ModifyStat("hunger", 100f);
                                PetStats.Instance?.RaiseFeedback("The cat ate from the food bowl.");
                                ResolveFoodBowlFromLockedOrSense()?.PlayEatAudio();
                            });
                        }
                    }
                }
                break;

            case CatState.DrinkWater:
                {
                    Transform water = ResolveWaterTarget();
                    if (water != null)
                    {
                        float stand = EffectiveBowlStandoff();
                        SetDestinationNear(water.position, stand);

                        if (ReachedInteractTarget(water, stand))
                        {
                            StartBusy(drinkDuration, CatState.Wander, () =>
                            {
                                _needs.Drink();
                                _mood.OnWatered();
                                PetStats.Instance?.ModifyStat("thirst", 100f);
                                PetStats.Instance?.RaiseFeedback("The cat drank from the water bowl.");
                                ResolveWaterBowlFromLockedOrSense()?.PlayDrinkAudio();
                            });
                        }
                    }
                }
                break;

            case CatState.UseLitterBox:
                {
                    Transform litter = ResolveLitterTarget();
                    if (litter != null)
                    {
                        float stand = EffectiveLitterStandoff();
                        SetDestinationNear(litter.position, stand);

                        if (ReachedInteractTarget(litter, stand))
                        {
                            StartBusy(litterDuration, CatState.Wander, () =>
                            {
                                _needs.RelieveBladder();
                                PetStats.Instance?.ModifyStat("hygiene", 100f);
                                PetStats.Instance?.RaiseFeedback("The cat used the litter box.");
                            });
                        }
                    }
                }
                break;

            case CatState.PlayWithToy:
                {
                    Transform toyTr = ResolveToyTarget();
                    if (toyTr != null)
                    {
                        float stand = EffectiveToyStandoff();
                        SetDestinationNear(toyTr.position, stand);

                        if (ReachedInteractTarget(toyTr, stand))
                        {
                            PetInteractable toy = toyTr.GetComponentInParent<PetInteractable>();
                            PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

                            StartBusy(playDuration, CatState.Wander, () =>
                            {
                                if (toy != null && pet != null)
                                {
                                    toy.Interact(pet);
                                }
                                else if (pet != null)
                                {
                                    pet.ModifyStat("happiness", 100f);
                                    pet.RaiseFeedback("The cat played and feels happier.");
                                }
                            });
                        }
                    }
                }
                break;

            case CatState.CommandedInteract when _commandTarget != null:
                SetDestinationNear(_commandTarget.position, _commandStopDist);

                if (ReachedInteractTarget(_commandTarget, Mathf.Max(_commandStopDist, 0.25f)))
                {
                    var interactable = _commandInteractable;
                    var pet = _commandPetStats;
                    float busy = _commandBusyDuration;

                    ClearCommandedState();

                    StartBusy(busy, CatState.Wander, () =>
                    {
                        if (interactable != null && pet != null)
                        {
                            interactable.Interact(pet);
                        }
                    });
                }
                break;

            case CatState.Wander:
                CheckWanderArrival();
                break;
        }

        Vector3 moveDirection = GetNavMoveDirection();

        bool isRubbing = Time.time < _rubUntilTime;
        bool isWaiting = Time.time < _waitUntilTime || _state == CatState.Busy || isRubbing;

        bool shouldMove =
            !isWaiting &&
            !_agent.pathPending &&
            _agent.hasPath &&
            moveDirection.sqrMagnitude > 0.01f;

        Vector3 lookTarget;

        if (shouldMove)
        {
            lookTarget = transform.position + moveDirection.normalized * 5f;
        }
        else
        {
            lookTarget = ComputeLookTarget();
        }

        bool shouldRun =
            (_state == CatState.FollowPlayer && player != null && DistanceTo(player) > runDistance) ||
            _state == CatState.Flee;

        _mover.SetInput(
            shouldMove ? new Vector2(0f, 1f) : Vector2.zero,
            lookTarget,
            shouldRun,
            false
        );
    }

    private Vector3 GetNavMoveDirection()
    {
        Vector3 targetPoint;

        if (_agent.hasPath)
        {
            targetPoint = _agent.steeringTarget;
        }
        else
        {
            targetPoint = transform.position;
        }

        Vector3 direction = targetPoint - transform.position;
        direction.y = 0f;

        return direction;
    }

    private bool ReachedCurrentDestination(float allowedDistance)
    {
        if (_agent.pathPending)
            return false;

        if (!_agent.hasPath)
            return false;

        if (_agent.remainingDistance <= allowedDistance)
            return true;

        Vector3 destination = _agent.destination;
        destination.y = transform.position.y;

        return Vector3.Distance(transform.position, destination) <= allowedDistance;
    }

    private float EffectiveBowlStandoff() => Mathf.Max(bowlStopDistance, 0.15f);

    private float EffectiveLitterStandoff() => Mathf.Max(litterStopDistance, 0.15f);

    private float EffectiveToyStandoff() => Mathf.Max(toyStopDistance, 0.15f);

    private void SyncAgentStoppingDistance(CatState state)
    {
        switch (state)
        {
            case CatState.EatFood:
            case CatState.DrinkWater:
                _agent.stoppingDistance = Mathf.Clamp(EffectiveBowlStandoff(), 0.12f, 0.85f);
                break;
            case CatState.UseLitterBox:
                _agent.stoppingDistance = Mathf.Clamp(EffectiveLitterStandoff(), 0.12f, 0.85f);
                break;
            case CatState.PlayWithToy:
                _agent.stoppingDistance = Mathf.Clamp(EffectiveToyStandoff(), 0.12f, 0.85f);
                break;
            case CatState.CommandedInteract:
                _agent.stoppingDistance = Mathf.Clamp(Mathf.Max(_commandStopDist, 0.25f), 0.12f, 1.25f);
                break;
            case CatState.Flee:
                _agent.stoppingDistance = Mathf.Min(_navDefaultStoppingDistance, 0.65f);
                break;
            case CatState.Busy:
                break;
            default:
                _agent.stoppingDistance = _navDefaultStoppingDistance;
                break;
        }
    }

    private bool ReachedInteractTarget(Transform target, float standoffRadius)
    {
        if (target == null) return false;

        float pad = interactArrivalPadding;
        float navTol = Mathf.Max(standoffRadius + pad, _agent.stoppingDistance + pad * 0.85f);

        if (ReachedCurrentDestination(navTol))
            return true;

        return HorizontalDistanceTo(target) <= standoffRadius + pad;
    }

    private float HorizontalDistanceTo(Transform target)
    {
        if (target == null)
            return Mathf.Infinity;

        Vector3 a = transform.position;
        Vector3 b = target.position;

        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    // Called every frame while wandering. Detects when the cat has reached its
    // destination, stops it, runs a short rub pause, then nudges it away before
    // picking a new wander point. This prevents the cat from walking into objects.
    private void CheckWanderArrival()
    {
        // Already rubbing  wait it out then pick a new point
        if (Time.time < _rubUntilTime)
            return;

        if (_arrivedAtDestination)
        {
            // Rub finished  nudge away from the object and pick a new wander point
            _arrivedAtDestination = false;
            NudgeAwayFromObstacle();
            _waitUntilTime = Time.time + Random.Range(idlePauseRange.x, idlePauseRange.y);
            PickNewWanderPoint();
            return;
        }

        if (!_agent.pathPending && _agent.hasPath &&
            _agent.remainingDistance <= arrivalDistance)
        {
            // Arrived  stop and start the rub pause
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

        float primary = Mathf.Clamp(nearRadius + navSamplePadding, 0.35f, 4f);
        if (!TrySampleNavMeshPoint(worldPos, primary, out Vector3 pt))
        {
            float fallback = Mathf.Max(primary, 2f);
            if (!TrySampleNavMeshPoint(worldPos, fallback, out pt))
                return;
        }

        _agent.SetDestination(pt);
        _lastSetDestination = worldPos;
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

    private FoodBowl ResolveFoodBowlFromLockedOrSense()
    {
        Transform t = ResolveFoodTarget();
        if (t == null) return null;
        return t.GetComponentInParent<FoodBowl>();
    }

    private WaterBowl ResolveWaterBowlFromLockedOrSense()
    {
        Transform t = ResolveWaterTarget();
        if (t == null) return null;
        return t.GetComponentInParent<WaterBowl>();
    }
}
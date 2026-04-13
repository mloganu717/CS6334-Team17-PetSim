using UnityEngine;
using UnityEngine.AI;

    // ai controller for cat - computes path
    [RequireComponent(typeof(CreatureMover))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CatAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;

        [Header("Behavior")]
        [SerializeField] private float wanderRadius = 6f;
        [SerializeField] private float decisionInterval = 1.25f;
        [SerializeField] private float followDistance = 8f;
        [SerializeField, Range(0f, 1f)] private float farFollowChance = 0.25f;
        [SerializeField] private float stopDistance = 1.2f;
        [SerializeField] private float runDistance = 4f;
        [SerializeField] private Vector2 idlePauseRange = new Vector2(1f, 3f);

        private CreatureMover mover;
        private NavMeshAgent agent;
        private Vector3 homePosition;

        private float nextDecisionTime;
        private float waitUntilTime;

        private enum Mode
        {
            Wander,
            FollowPlayer,
            GoToTarget
        }

        private Mode mode = Mode.Wander;

        //interactionstate
        private bool isInteracting = false;
        private PetInteractable pendingInteractable = null;
        private PetStats pendingPetStats = null;

        private void Awake()
        {
            mover = GetComponent<CreatureMover>();
            agent = GetComponent<NavMeshAgent>();
            homePosition = transform.position;


            // navMeshAgent compute the path
            // but creaturemover does visible movement/rotation
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.stoppingDistance = stopDistance;
            agent.autoBraking = true;
            agent.autoRepath = true;
        }

        private void Start()
        {
            if (!agent.isOnNavMesh)
            {
                Debug.LogError(name + " is not on a baked NavMesh. Bake the NavMesh and place the cat on it.", this);
                enabled = false;
                return;
            }

            agent.Warp(transform.position);
            PickNewWanderPoint();
        }

        private void Update()
        {
            if (Time.time >= nextDecisionTime)
            {
                nextDecisionTime = Time.time + decisionInterval;
                DecideWhatToDo();
            }

            DriveCreatureMover();

            // keep navmeshagent's position in sync with the real object
            agent.nextPosition = transform.position;
        }

        private void DecideWhatToDo()
        {
            // don't interrupt interaction walk
            if (isInteracting) return;

            if (Time.time < waitUntilTime)
            {
                return;
            }

            float distanceToPlayer = player == null
                ? Mathf.Infinity
                : Vector3.Distance(transform.position, player.position);

            bool shouldFollowPlayer =
                distanceToPlayer <= followDistance || Random.value < farFollowChance;

            if (player != null && shouldFollowPlayer)
            {
                mode = Mode.FollowPlayer;
                SetDestinationNear(player.position);
                return;
            }

            if (!agent.hasPath || agent.remainingDistance <= stopDistance + 0.2f)
            {
                mode = Mode.Wander;
                waitUntilTime = Time.time + Random.Range(idlePauseRange.x, idlePauseRange.y);
                PickNewWanderPoint();
            }
        }

        private void DriveCreatureMover()
        {
            if (mode == Mode.FollowPlayer && player != null)
            {
                SetDestinationNear(player.position);
            }
            // keeps its destination set by the coroutine

            Vector3 moveDirection = agent.desiredVelocity;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude < 0.001f)
            {
                Vector3 steeringPoint = agent.hasPath ? agent.steeringTarget : transform.position;
                moveDirection = steeringPoint - transform.position;
                moveDirection.y = 0f;
            }

            bool shouldMove = Time.time >= waitUntilTime && moveDirection.magnitude > 0.1f;

            Vector3 lookTarget = shouldMove
                ? transform.position + moveDirection.normalized * 5f
                : transform.position + transform.forward;

            Vector2 axis = shouldMove ? new Vector2(0f, 1f) : Vector2.zero;

            // In GoToTarget mode always move, and run if far away
            bool shouldRun =
                (mode == Mode.FollowPlayer && player != null &&
                 Vector3.Distance(transform.position, player.position) > runDistance) ||
                (mode == Mode.GoToTarget &&
                 agent.hasPath && agent.remainingDistance > runDistance);

            mover.SetInput(axis, lookTarget, shouldRun, false);
        }

        
        public void GoToAndInteract(Transform target, PetInteractable interactable, PetStats pet, float overrideStopDistance = -1f, float stayDuration = 3f)
        {
            Debug.Log("GoToAndInteract called with stayDuration: " + stayDuration);
            if (isInteracting) return;
            pendingInteractable = interactable;
            pendingPetStats = pet;
            isInteracting = true;
            mode = Mode.GoToTarget;
            agent.stoppingDistance = overrideStopDistance > 0f ? overrideStopDistance : 0f;
            SetDestinationNear(target.position);
            StartCoroutine(WaitForArrivalAndInteract(target, overrideStopDistance > 0f ? overrideStopDistance : 0.3f, stayDuration));
        }

        private System.Collections.IEnumerator WaitForArrivalAndInteract(Transform target, float arrivalDistance = 0.3f, float stayDuration = 3f)
        {
            while (Vector3.Distance(transform.position, target.position) > arrivalDistance)
            {
                SetDestinationNear(target.position);
                yield return null;
            }

            agent.ResetPath();
            mover.SetInput(Vector2.zero, transform.position + transform.forward, false, false);

            yield return new WaitForSeconds(0.5f);

            if (pendingInteractable != null && pendingPetStats != null)
                pendingInteractable.Interact(pendingPetStats);

            Debug.Log("Cat arrived, staying for " + stayDuration + " seconds");
            yield return new WaitForSeconds(stayDuration);
            Debug.Log("Cat leaving bowl");

            agent.stoppingDistance = stopDistance;
            isInteracting = false;
            pendingInteractable = null;
            pendingPetStats = null;
            mode = Mode.Wander;
            PickNewWanderPoint();
        }

        private void PickNewWanderPoint()
        {
            if (TryGetRandomNavMeshPoint(homePosition, wanderRadius, out Vector3 point))
            {
                agent.SetDestination(point);
            }
        }

        private void SetDestinationNear(Vector3 targetWorldPosition)
        {
        
            if (TrySampleNavMeshPoint(targetWorldPosition, 5f, out Vector3 point))
            {
                agent.SetDestination(point);
            }
        }

        private bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 point)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 circle = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

                if (TrySampleNavMeshPoint(candidate, 2f, out point))
                {
                    return true;
                }
            }

            point = transform.position;
            return false;
        }

        private bool TrySampleNavMeshPoint(Vector3 target, float maxDistance, out Vector3 point)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }

            point = transform.position;
            return false;
        }
    }

using System.Collections.Generic;
using UnityEngine;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Rendering.Character
{
    [DisallowMultipleComponent]
    public sealed class Zombie : Entity
    {
        private enum ZombieState
        {
            Idle,
            Wander,
            Chase
        }

        private static readonly List<Zombie> ActiveZombies = new();

        [Header("Zombie AI")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float loseInterestRange = 15f;
        [SerializeField] private float chaseSpeed = 0.08f;
        [SerializeField] private float wanderSpeed = 0.02f;
        [SerializeField] private float idleDuration = 3f;
        [SerializeField] private float wanderDuration = 3f;

        [Header("Zombie Presentation")]
        [SerializeField] private ZombieModelView modelView;
        [SerializeField] private float walkCycleFrequency = 5f;
        [SerializeField] private float pushApartStrength = 0.05f;
        [SerializeField] private bool logPlayerCollision = true;

        private ZombieState currentState = ZombieState.Idle;
        private Transform playerTransform;
        private ZombieContactDebug playerContactDebug;
        private Vector3 wanderDirection = Vector3.forward;
        private float stateTimer;
        private float walkPhase;
        private float currentYaw;

        protected override Vector3 EntityColliderSize => new(0.6f, 1.8f, 0.6f);
        protected override float EntityEyeHeight => 1.62f;

        protected override void Awake()
        {
            base.Awake();
            ResolveZombieReferences();
            ResetEntityKinematics();
            stateTimer = idleDuration;
            ChooseRandomWanderDirection();
        }

        private void OnEnable()
        {
            if (!ActiveZombies.Contains(this))
            {
                ActiveZombies.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveZombies.Remove(this);
        }

        private void FixedUpdate()
        {
            if (gameManager == null || !gameManager.HasGeneratedWorld)
            {
                return;
            }

            ResolveZombieReferences();

            previousPosition = transform.position;
            TickAi();

            velocity.y -= gameManager.PhysicsConfig.GravityPerTick;
            MoveWithWorldCollision(velocity);

            velocity.x *= gameManager.PhysicsConfig.HorizontalDrag;
            velocity.y *= gameManager.PhysicsConfig.VerticalDrag;
            velocity.z *= gameManager.PhysicsConfig.HorizontalDrag;

            if (onGround)
            {
                velocity.x *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
                velocity.z *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
            }

            ResolveZombieCollisions();
            ResolvePlayerCollision();
        }

        private void Update()
        {
            if (modelView == null)
            {
                ResolveZombieReferences();
            }

            var planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            if (planarSpeed > 0.0001f)
            {
                walkPhase += Time.deltaTime * walkCycleFrequency * Mathf.Clamp(planarSpeed * 8f, 0.35f, 1.35f);
            }

            modelView?.SetAnimationPose(
                walkPhase,
                Mathf.Clamp01(planarSpeed * 12f),
                currentYaw,
                0f,
                Time.time);
        }

        private void ResolveZombieReferences()
        {
            if (modelView == null)
            {
                modelView = GetComponent<ZombieModelView>();
            }

            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<Player.PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerContactDebug = player.GetComponent<ZombieContactDebug>();
                    if (playerContactDebug == null)
                    {
                        playerContactDebug = player.gameObject.AddComponent<ZombieContactDebug>();
                    }
                }
            }
        }

        private void TickAi()
        {
            if (playerTransform == null)
            {
                if (currentState == ZombieState.Chase)
                {
                    currentState = ZombieState.Wander;
                    stateTimer = wanderDuration;
                    ChooseRandomWanderDirection();
                }

                if (currentState == ZombieState.Wander)
                {
                    Wander();
                }
                else
                {
                    Idle();
                }

                return;
            }

            var toPlayer = playerTransform.position - transform.position;
            var planarToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
            var distanceToPlayer = planarToPlayer.magnitude;

            if (distanceToPlayer <= detectionRange)
            {
                currentState = ZombieState.Chase;
            }
            else if (currentState == ZombieState.Chase && distanceToPlayer > loseInterestRange)
            {
                currentState = ZombieState.Wander;
                stateTimer = wanderDuration;
                ChooseRandomWanderDirection();
            }

            switch (currentState)
            {
                case ZombieState.Chase:
                    ChasePlayer(planarToPlayer);
                    break;
                case ZombieState.Wander:
                    Wander();
                    break;
                default:
                    Idle();
                    break;
            }
        }

        private void Idle()
        {
            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
            {
                currentState = ZombieState.Wander;
                stateTimer = wanderDuration;
                ChooseRandomWanderDirection();
            }
        }

        private void Wander()
        {
            stateTimer -= Time.fixedDeltaTime;
            MoveRelative(wanderDirection.x, wanderDirection.z, wanderSpeed);
            UpdateYawFromDirection(wanderDirection);

            if (stateTimer <= 0f)
            {
                currentState = ZombieState.Idle;
                stateTimer = idleDuration;
            }
        }

        private void ChasePlayer(Vector3 planarToPlayer)
        {
            if (planarToPlayer.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var direction = planarToPlayer.normalized;
            MoveRelative(direction.x, direction.z, chaseSpeed);
            UpdateYawFromDirection(direction);
        }

        private void MoveRelative(float x, float z, float speed)
        {
            var direction = new Vector2(x, z);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            direction = direction.normalized * speed;
            velocity.x += direction.x;
            velocity.z += direction.y;
        }

        private void ChooseRandomWanderDirection()
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            wanderDirection = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        private void UpdateYawFromDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            currentYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void ResolveZombieCollisions()
        {
            for (var i = 0; i < ActiveZombies.Count; i++)
            {
                var other = ActiveZombies[i];
                if (other == null || other == this)
                {
                    continue;
                }

                if (!BoundingBox.Intersects(other.BoundingBox))
                {
                    continue;
                }

                var delta = BoundingBox.Center - other.BoundingBox.Center;
                delta.y = 0f;

                if (delta.sqrMagnitude < 0.0001f)
                {
                    delta = Random.insideUnitSphere;
                    delta.y = 0f;
                }

                delta.Normalize();
                var push = delta * (pushApartStrength * 0.5f);
                ApplyExternalDisplacement(push);
                other.ApplyExternalDisplacement(-push);
            }
        }

        private void ResolvePlayerCollision()
        {
            if (playerTransform == null)
            {
                return;
            }

            var player = playerTransform.GetComponent<Player.PlayerController>();
            if (player == null)
            {
                return;
            }

            if (!BoundingBox.Intersects(player.BoundingBox))
            {
                return;
            }

            if (logPlayerCollision)
            {
                playerContactDebug?.NotifyPlayerHit();
            }
        }
    }
}

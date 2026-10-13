using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class ZombieBehavior : MonoBehaviour
{
    private Transform player;

    [Header("Settings")]
    public float detectionRadius = 10f;
    public float attackRadius = 2f;
    public float maxSampleDistance = 2f;

    private NavMeshAgent agent;
    private Animator animator;

    private float searchCooldown = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        searchCooldown = Random.value;
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
    }

    void HandleFindPlayer()
    {
        if (searchCooldown > 0f) {
            searchCooldown -= Time.deltaTime;
            ThirdPersonController controller = FindAnyObjectByType<ThirdPersonController>();
            if (controller) {
                player = controller.transform;
            }
        } else
        {
            searchCooldown = 2f;
        }
    }

    void Update()
    {
        if (player == null) {
            HandleFindPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRadius)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Attack");
        }
        else if (distance <= detectionRadius)
        {
            agent.isStopped = false;

            if (NavMesh.SamplePosition(player.position, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            animator.SetFloat("Speed", agent.velocity.magnitude);
            animator.ResetTrigger("Attack");
        }
        else
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("Attack");
        }
    }
}
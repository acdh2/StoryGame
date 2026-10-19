using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieBehaviour : MonoBehaviour
{
    private Transform player;

    [Header("Settings")]
    public float detectionRadius = 10f;
    public float attackRadius = 2f;
    public float maxSampleDistance = 2f;

    private NavMeshAgent agent;
    private Animator animator;

    private float searchCooldown = 0f;
    private int hits = 0;

    private Vector3 velocity = Vector3.zero;
    private bool shouldMove = false;

    private float cooldown = 0f;
    // private float playerDiedCooldown = 0f;

    private Vector3 spawn = Vector3.zero;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        searchCooldown = Random.value;

        spawn = transform.position;
    }

    void FixedUpdate()
    {
        if (shouldMove) { //pushback
            transform.position += velocity;
            velocity *= 0.9f;
            if (velocity.magnitude < 0.01f) shouldMove = false;
        }
    }

    public void GetDamage()
    {
        //if (!hasWeapon && Random.value < 0.8f) return;

        velocity = -transform.forward * 0.1f;
        shouldMove = true;

        hits ++;
        if (hits < 3) {
        } else {
            Collider collider = GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            if (animator != null) animator.SetTrigger("Die");
            agent.isStopped = true;
            Destroy(this);
        }
    }

    void HandleFindPlayer()
    {
        if (searchCooldown > 0f) {
            searchCooldown -= Time.deltaTime;
        } else {
            ThirdPersonController controller = FindAnyObjectByType<ThirdPersonController>();
            if (controller) {
                player = controller.transform;
            }
            searchCooldown = 2f;
        }
    }

    void DoPlayerDamage()
    {
        if (cooldown > 0f) return;
        cooldown = 1f;

        if (player != null)
        {
            RespawnBehaviour respawnBehaviour = player.GetComponent<RespawnBehaviour>();
            if (respawnBehaviour != null) 
                if (respawnBehaviour.isAlive)
                    respawnBehaviour.DoDamage(40);
                // else
                //     playerDiedCooldown = 10f;
        }
    }

    void NavigateTo(Vector3 target)
    {
        agent.isStopped = false;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
        animator.ResetTrigger("Attack");        
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (cooldown > 0f) cooldown -= Time.deltaTime; //attack cooldown

        // if (playerDiedCooldown > 0f) {
        //     playerDiedCooldown -= Time.deltaTime;
        //     NavigateTo(spawn);
        //     return;
        // }
        
        if (player == null) {
            NavigateTo(spawn);
            HandleFindPlayer();
            return;
        }

        if (player != null)
        {
            RespawnBehaviour respawnBehaviour = player.GetComponent<RespawnBehaviour>();
            if (respawnBehaviour != null) {
                if (!respawnBehaviour.isAlive) {
                    agent.isStopped = true;
                    animator.SetFloat("Speed", 0f);
                    return;
                }
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRadius)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Attack");   
            DoPlayerDamage();
            if (distance > 0.01f)
                transform.forward = new Vector3(player.position.x - transform.position.x, 0f, player.position.z - transform.position.z).normalized;
        }
        else if (distance <= detectionRadius)
        {
            NavigateTo(player.position);
        }
        else
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("Attack");
        }
    }
}
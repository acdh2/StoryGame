using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    private InputSystem_Actions inputActions;

    private float attackRadius = 2.0f;    

    private Animator animator;
    //private bool playerDied = false;

    void Awake()
    {
        animator = GetComponent<Animator>();        
    }

    private void OnEnable()
    {        
        inputActions = InputManager.Instance.Actions;
        inputActions.Player.Attack.performed += OnAttackPerformed;
        InputManager.Instance?.EnableMap("Player");
    }

    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        InputManager.Instance?.DisableMap("Player");
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (animator) animator.SetTrigger("Attack");

        bool hasWeapon = false;
        PickupItemBehaviour pickupItemBehaviour = GetComponent<PickupItemBehaviour>();
        if (pickupItemBehaviour != null)
        {
            hasWeapon = pickupItemBehaviour.HasWeapon();
        }
        if (!hasWeapon) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius);
        System.Collections.Generic.List<ZombieBehaviour> zombies = new System.Collections.Generic.List<ZombieBehaviour>();

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<ZombieBehaviour>(out var zombie))
            {
                Vector3 directionToZombie = (zombie.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, directionToZombie);

                if (dot > 0.5f)
                {
                    zombies.Add(zombie);
                }
            }
        }

        if (zombies.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, zombies.Count);
            zombies[randomIndex].ApplyDamage();
        }
    }

}
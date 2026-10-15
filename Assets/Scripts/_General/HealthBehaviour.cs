using UnityEngine;
using System.Collections;
using NUnit.Framework.Constraints;
using StarterAssets;

public partial class RespawnBehaviour : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;

    private RespawnBehaviour respawnBehaviour;
    // private GameObject forceField = null;

    private float cooldown = 0f;

    private bool isActive = true;

    private float flashTimeout = 0f;

    // void EnableForceField()
    // {
    //     return;
    //     if (forceField == null) {
    //         forceField = Instantiate(Resources.Load<GameObject>("ForceField"));
    //         Transform newParent = transform.Find("Hips");
    //         newParent = newParent?newParent:transform;
    //         forceField.transform.SetParent(newParent, false);
    //         forceField.transform.localPosition = GetComponent<CharacterController>().center; 
    //     }
    // }

    private void FixedUpdate()
    {
        if (flashTimeout > 0f) flashTimeout -= Time.deltaTime;
        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (isActive) {
            if (health <= 0f)
            {
                StartCoroutine(Die());
            } else {
                if (health < 100) health += 0.05f;
                if (health > 100) health = 100;
            }
        }
    }

    public void NotifyPlayerDied()
    {
        ZombieBehaviour[] zombies = Object.FindObjectsByType<ZombieBehaviour>(FindObjectsSortMode.None);
        foreach (var zombie in zombies)
        {
            zombie.OnPlayerDied();
        }
    }

    private IEnumerator Die() {
        SetScriptActive(false);
        AudioClip clip = Resources.Load<AudioClip>("resetsound");

        if (clip != null)
        {
            audioSource.pitch = Random.Range(1.3f, 1.55f);
            audioSource.PlayOneShot(clip);
        }
        Animator animator = GetComponent<Animator>();
        animator.SetBool("Dead", true);
        NotifyPlayerDied();
        yield return new WaitForSeconds(3);
        health = 100f;
        Respawn();

        animator.SetBool("Dead", false);
        animator.SetTrigger("Reset");
        SetScriptActive(true);
    }

    public void DoDamage(float damage)
    {
        flashTimeout = 0.05f;
        health = health - damage;
    }

    private void HandleHit(ControllerColliderHit hit)
    {
        if (cooldown > 0f) return;
        cooldown = 1f;

        Renderer rend = hit.collider.GetComponent<Renderer>();
        if (rend != null && rend.sharedMaterial != null)
        {
            if (rend.sharedMaterial.name.Contains("lava"))
            {
                DoDamage(120);
            }
            if (rend.sharedMaterial.name.Contains("water"))
            {
                DoDamage(52);
            }
        }
        
    }

    private void SetScriptActive(bool enabled)
    {
        isActive = enabled;
        
        PlayerControlBlocker playerControlBlocker = GetComponent<PlayerControlBlocker>();
        if (playerControlBlocker != null) 
            if (enabled) playerControlBlocker.EnableControls();
            else playerControlBlocker.DisableControls();
    }

    void OnGUI()
    {
        if (flashTimeout > 0f)
        {
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            return;
        }

        float scale = Screen.width / 1280.0f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float barWidth = 200f;
        float barHeight = 20f;
        float padding = 10f;

        // Positie rechtsboven berekenen
        float startX = padding;
        float startY = padding;

        // Teken de healthbar naast het icoon
        float barX = startX;
        float barY = startY;

        // Achtergrond van de bar (rood/grijs)
        GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");

        // Huidige health berekenen en tekenen (groen)
        float healthPercent = Mathf.Clamp01(health / maxHealth);
        GUI.BeginGroup(new Rect(barX, barY, barWidth * healthPercent, barHeight));
        GUI.Box(new Rect(0, 0, barWidth, barHeight), ""); 
        GUI.EndGroup();
    }
}

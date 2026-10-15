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

    private bool isActive = true;

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

    float cooldown = 0f;

    private void FixedUpdate()
    {
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
        yield return new WaitForSeconds(1);
        health = 100f;
        Respawn();

        animator.SetBool("Dead", false);
        animator.SetTrigger("Reset");
        SetScriptActive(true);
    }

    public void DoDamage(float damage)
    {
        if (cooldown <= 0f) {
            cooldown = 1.5f;
            StartCoroutine(FlashRoutine(Camera.main));
            health = health - damage;
        }
    }

    private IEnumerator FlashRoutine(Camera targetCamera, float duration = 0.05f)
    {
        if (targetCamera != null) {

            // Bewaar oude instellingen
            int originalMask = targetCamera.cullingMask;
            CameraClearFlags originalFlags = targetCamera.clearFlags;
            Color originalColor = targetCamera.backgroundColor;

            // Zet scherm op rood
            targetCamera.cullingMask = 0;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = Color.red;

            yield return new WaitForSeconds(duration);

            // Herstel instellingen
            targetCamera.cullingMask = originalMask;
            targetCamera.clearFlags = originalFlags;
            targetCamera.backgroundColor = originalColor;
        }
    }

    private void HandleHit(ControllerColliderHit hit)
    {
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

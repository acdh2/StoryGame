using UnityEngine;
using System.Collections;
using StarterAssets;

public partial class RespawnBehaviour : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;

    private RespawnBehaviour respawnBehaviour;
    // private GameObject forceField = null;

    public event System.Action OnPlayerDied = null;

    private float cooldown = 0f;

    private bool isActive = true;

    private float flashTimeout = 0f;

    public bool isAlive = true;

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
        OnPlayerDied?.Invoke();
    }

    private IEnumerator Die() {
        isAlive = false;
        SetScriptActive(false);
        AudioClip clip = Resources.Load<AudioClip>("resetsound");

        if (clip != null)
        {
            audioSource.pitch = Random.Range(1.3f, 1.55f);
            audioSource.PlayOneShot(clip);
        }
        Animator animator = GetComponent<Animator>();
        animator.SetBool("Dead", true);
        yield return new WaitForSeconds(3);
        NotifyPlayerDied();
        health = 100f;
        Respawn();

        animator.SetBool("Dead", false);
        animator.SetTrigger("Reset");
        SetScriptActive(true);
        isAlive = true;
    }

    public void DoDamage(float damage)
    {
        flashTimeout = 0.05f;
        health = health - damage;
    }

    private void HandleHit(ControllerColliderHit hit)
    {
        if (health <= 0f) return;

        if (cooldown > 0f) return;

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
                cooldown = 1f;
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
        GUI.color = new Color32(224, 90, 71, 255);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        return;
    }

    float virtualWidth = 1280.0f;
    float scale = Screen.width / virtualWidth;
    GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

    float barWidth = 200f;
    float barHeight = 20f;
    float padding = 10f;

    float barX = virtualWidth - barWidth - padding;
    float barY = padding;

    // Achtergrond
    GUI.color = new Color32(90, 98, 107, 255);
    GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

    float healthPercent = Mathf.Clamp01(health / maxHealth);

    Color healthColor;
    if (healthPercent > 0.5f)
        healthColor = new Color32(75, 175, 110, 255);
    else if (healthPercent > 0.25f)
        healthColor = new Color32(245, 195, 65, 255);
    else
        healthColor = new Color32(224, 90, 71, 255);

    // Health-balk
    GUI.color = healthColor;
    GUI.DrawTexture(new Rect(barX, barY, barWidth * healthPercent, barHeight), Texture2D.whiteTexture);

    GUI.color = Color.white;
}  

    void OnGUIOld()
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

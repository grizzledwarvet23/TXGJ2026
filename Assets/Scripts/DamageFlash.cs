using UnityEngine;
using System.Collections;

//briefly flashes a SpriteRenderer to a solid color (white by default) when TriggerFlash() is called.
//SpriteRenderer.color can only multiply/darken the existing texture, never brighten it to pure
//white, so this swaps to a small custom shader (Custom/SpriteFlash) that lerps toward the flash
//color instead - only for the flash's brief duration, normal rendering is untouched otherwise.
public class DamageFlash : MonoBehaviour
{
    public float flashDuration = 0.1f;
    public Color flashColor = Color.white;

    SpriteRenderer spriteRenderer;
    Material originalMaterial;
    Material flashMaterial;
    Coroutine flashRoutine;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        originalMaterial = spriteRenderer.material;

        Shader flashShader = Shader.Find("Custom/SpriteFlash");
        if (flashShader != null)
        {
            flashMaterial = new Material(flashShader);
        }
    }

    public void TriggerFlash()
    {
        if (spriteRenderer == null || flashMaterial == null) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        flashMaterial.SetColor("_FlashColor", flashColor);
        flashMaterial.SetFloat("_FlashAmount", 1f);
        spriteRenderer.material = flashMaterial;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.material = originalMaterial;
        flashRoutine = null;
    }
}

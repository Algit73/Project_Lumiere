using UnityEngine;

public class OutlineController : MonoBehaviour
{
    public Color outlineColor = Color.yellow;
    public float outlineWidth = 0.005f;
    
    private Material outlineMaterial;
    private Renderer[] objectRenderers;
    private Material[][] originalMaterials;

    private void Start()
    {
        objectRenderers = GetComponentsInChildren<Renderer>();
        
        if (objectRenderers.Length == 0)
        {
            Debug.LogWarning("No Renderers found in this object or its children.");
            return;
        }

        outlineMaterial = new Material(Shader.Find("Unlit/Outline_Shader"));
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);

        // Store original materials
        originalMaterials = new Material[objectRenderers.Length][];
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            originalMaterials[i] = objectRenderers[i].materials;
        }
    }

    public void ToggleOutline(bool enable)
    {
        
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            Material[] newMaterials = new Material[originalMaterials[i].Length + 1];

            if (enable)
            {
                for (int j = 0; j < originalMaterials[i].Length; j++)
                {
                    newMaterials[j] = originalMaterials[i][j];
                }
                newMaterials[newMaterials.Length - 1] = outlineMaterial;
                objectRenderers[i].materials = newMaterials;
            }
            else
            {
                objectRenderers[i].materials = originalMaterials[i];
            }
        }
    }

    private void OnDisable()
    {
        // Ensure original materials are restored when the script is disabled
        if (objectRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < objectRenderers.Length; i++)
            {
                if (objectRenderers[i] != null)
                {
                    objectRenderers[i].materials = originalMaterials[i];
                }
            }
        }
    }
}

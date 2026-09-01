using UnityEngine;
using UnityEditor;

public static class MeshRender
{
    [MenuItem("Tools/Enable Mesh Renderers")]
    private static void EnableRenderers()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning("Select a GameObject first!");
            return;
        }

        MeshRenderer[] renderers = selected.GetComponentsInChildren<MeshRenderer>(true);

        int count = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            if (!renderer.enabled)
            {
                Undo.RecordObject(renderer, "Enable Mesh Renderer");
                renderer.enabled = true;
                count++;
            }
        }

        Debug.Log($"Enabled {count} Mesh Renderers in '{selected.name}'.");
    }

    [MenuItem("Tools/Enable Mesh Renderers", true)]
    private static bool ValidateEnableRenderers()
    {
        return Selection.activeGameObject != null;
    }
}
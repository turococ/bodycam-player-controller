/*
 * Copyright (c) 2026 turococ
 * Licensed under the MIT License. See LICENSE file in the project root for full license information.
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Volume))]
public class BodycamDepthOfField : MonoBehaviour
{
    [Header("Lens Settings")]
    [SerializeField] float baseFocusDistance = 1.8f;
    [SerializeField] float minFocusDistance = 0.15f;
    [SerializeField] float maxFocusDistance = 50f;
    [SerializeField] float focusSpeed = 10f;
    [SerializeField] float focusDeadZone = 0.04f;

    [Header("Raycast")]
    [SerializeField] LayerMask focusLayers = ~0;
    [SerializeField] float raycastOffset = 0.15f;
    [SerializeField] float raycastRadius = 0.02f;

    [Header("Debug")]
    [SerializeField] bool drawDebugRay = false;

    Volume volume;
    DepthOfField dof;
    float currentFocus;
    float targetFocus;
    bool hasTarget;

    void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume.profile.TryGet(out dof))
        {
            currentFocus = baseFocusDistance;
            dof.focusDistance.overrideState = true;
            dof.focusDistance.value = currentFocus;
        }
    }

    void Update()
    {
        CalculateTargetFocus();
        SmoothFocus();
    }

    void CalculateTargetFocus()
    {
        targetFocus = baseFocusDistance;
        hasTarget = false;

        Vector3 origin = transform.position + transform.forward * raycastOffset;

        if (Physics.SphereCast(origin, raycastRadius, transform.forward, out RaycastHit hit, maxFocusDistance, focusLayers))
        {
            if (hit.distance >= minFocusDistance)
            {
                targetFocus = hit.distance;
                hasTarget = true;
            }
        }

        if (drawDebugRay)
        {
            Color col = hasTarget ? Color.green : Color.red;
            Debug.DrawRay(origin, transform.forward * (hasTarget ? targetFocus : maxFocusDistance), col);
        }
    }

    void SmoothFocus()
    {
        float diff = Mathf.Abs(currentFocus - targetFocus);

        if (diff > focusDeadZone)
        {
            float t = focusSpeed * Time.deltaTime;
            currentFocus = Mathf.Lerp(currentFocus, targetFocus, t);
        }
        else
        {
            currentFocus = targetFocus;
        }

        dof.focusDistance.value = currentFocus;
    }

    public void SetFocusOverride(float distance)
    {
        targetFocus = Mathf.Clamp(distance, minFocusDistance, maxFocusDistance);
        hasTarget = true;
    }

    public void ResetFocus()
    {
        targetFocus = baseFocusDistance;
        hasTarget = false;
    }
}
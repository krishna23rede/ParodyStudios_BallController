using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    #region Inspector Variables

    [Header("References")]
    public Transform bounceMarker;
 
    public Transform bowlerHandPosition;
 
    [Header("Swing Delivery Settings")]
    public float deliveryDuration = 0.6f;
 
    public float arcHeight = 1.8f;

    [Range(-100f, 100f)]
    public float swingAmount = 0f;
 
    [Header("Post-Bounce (Rigidbody) Settings")]
    public float postBounceSpeed = 14f;
    #endregion
    public bool IsDelivering { get; private set; } // Flags

    #region Runtime State private variables
 
    [Header("Runtime State — Read Only")]
    private bool hasBounced   = false;
    private float currentT    = 0f;
 
    private Vector3 p0;   // Start   : bowler hand
    private Vector3 p1;   // Control : arc apex + lateral swing offset
    private Vector3 p2;   // End     : bounce marker (always the exact landing spot)
 
    private float deliveryTimer = 0f;
    private static readonly float bounceUpwardSpeed = 10f;
    private static readonly float maxSwingDeviation = 10f;
 
    private Rigidbody rb;

    #endregion
 
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }
 
    private void FixedUpdate()
    {
        if (!IsDelivering || hasBounced) return;
        UpdateAirPhase();
    }

    public void BowlBall()
    {
        if (IsDelivering) return;   // Prevent double-trigger
 
        hasBounced    = false;
        IsDelivering  = true;
        deliveryTimer = 0f;
        currentT      = 0f;
 
        rb.isKinematic = true;
        rb.velocity       = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
 
        p0 = (bowlerHandPosition != null) ? bowlerHandPosition.position : transform.position;
 
        p2 = bounceMarker.position;
 
        Vector3 midPoint = Vector3.Lerp(p0, p2, 0.5f);
        Vector3 apex     = midPoint + Vector3.up * arcHeight;
 
        Vector3 forwardFlat = p2 - p0;
        forwardFlat.y = 0f;
        forwardFlat.Normalize();
        Vector3 swingAxis = Vector3.Cross(Vector3.up, forwardFlat).normalized;
 
        float normalizedSwing  = swingAmount / 100f;
        float lateralOffset    = normalizedSwing * maxSwingDeviation;
 
        p1 = apex + swingAxis * lateralOffset;
 
        // Snap ball to start position
        transform.position = p0;
    }
    private void UpdateAirPhase()
    {
        deliveryTimer += Time.fixedDeltaTime;
        currentT       = Mathf.Clamp01(deliveryTimer / deliveryDuration);
 
        transform.position = EvaluateBezier(p0, p1, p2, currentT);
 
        Vector3 tangent = EvaluateBezierTangent(p0, p1, p2, currentT);
        if (tangent.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(tangent);
 
        if (currentT >= 1f)
        {
            OnBallReachedMarker();
        }
    }
    private void OnBallReachedMarker()
    {
        hasBounced   = false;   // Will be true once we hand off
        IsDelivering = false;
 
        transform.position = p2;
        Vector3 tangentAtImpact = EvaluateBezierTangent(p0, p1, p2, 1f);
 
        Vector3 horizontalDir  = tangentAtImpact;
        horizontalDir.y        = 0f;
        horizontalDir.Normalize();
 
        Vector3 bounceVelocity = horizontalDir * postBounceSpeed
                               + Vector3.up    * bounceUpwardSpeed;
 
        rb.isKinematic = false;
        rb.velocity    = bounceVelocity;
 
        hasBounced = true;
    }
 
    private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u       * p0)
             + (2f * u * t  * p1)
             + (      t * t * p2);
    }

    private static Vector3 EvaluateBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return 2f * u * (p1 - p0)
             + 2f * t * (p2 - p1);
    }
 
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (bounceMarker == null || bowlerHandPosition == null) return;
 
        // Recompute control points using current inspector values
        Vector3 gP0 = bowlerHandPosition.position;
        Vector3 gP2 = bounceMarker.position;
 
        Vector3 midPoint  = Vector3.Lerp(gP0, gP2, 0.5f);
        Vector3 apex      = midPoint + Vector3.up * arcHeight;
 
        Vector3 fwd = gP2 - gP0; fwd.y = 0f; fwd.Normalize();
        Vector3 axis = Vector3.Cross(Vector3.up, fwd).normalized;
 
        float lateralOffset = (swingAmount / 100f) * maxSwingDeviation;
        Vector3 gP1 = apex + axis * lateralOffset;
 
        // --- Draw the Bézier path ---
        Gizmos.color = Color.red;
        Vector3 prev = gP0;
        const int steps = 60;
        for (int i = 1; i <= steps; i++)
        {
            float   t   = i / (float)steps;
            Vector3 pos = EvaluateBezier(gP0, gP1, gP2, t);
            Gizmos.DrawLine(prev, pos);
            prev = pos;
        }
 
        // --- Draw control point P1 and legs ---
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gP1, 0.12f);
        Gizmos.DrawLine(gP0, gP1);
        Gizmos.DrawLine(gP1, gP2);
 
        // --- Draw the landing marker ---
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gP2, 0.18f);
 
        // --- Draw the post-bounce direction ---
        Vector3 tangentAtEnd = EvaluateBezierTangent(gP0, gP1, gP2, 1f);
        Vector3 horizDir     = tangentAtEnd; horizDir.y = 0f; horizDir.Normalize();
        Gizmos.color = Color.green;
        Gizmos.DrawRay(gP2, horizDir * 2f);
    }
#endif
}
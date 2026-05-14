using System;
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

    [Header("Spin Delivery Settings")]
    [Range(-100f, 100f)]
    public float spinAmount = 0f;

    public float maxSpinAngle = 25f;

    [Header("Post-Bounce (Rigidbody) Settings")]
    public float postBounceSpeed = 14f;

    [Header("Reset")]
    public float resetDelay = 3f;

    #endregion


    #region Runtime State

    public enum BallState { Idle, Airborne, PostBounce }
    public BallState currentState { get; private set; } = BallState.Idle;

    private float currentT      = 0f;
    private float deliveryTimer = 0f;
    private float postBounceTimer = 0f;

    // Bézier control points
    private Vector3 p0;   // Start   : bowler hand
    private Vector3 p1;   // Control : arc apex  (+lateral swing offset when Swing)
    private Vector3 p2;   // End     : bounce marker

    private static readonly float bounceUpwardSpeed  = 10f;
    private static readonly float maxSwingDeviation  = 10f;

    private Rigidbody rb;

    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case BallState.Airborne:
                UpdateAirPhase();
                break;

            case BallState.PostBounce:
                UpdatePostBounce();
                break;
        }
    }

    public void BowlBall()
    {
        if (currentState != BallState.Idle) return;   // Prevent double-trigger

        currentState  = BallState.Airborne;
        deliveryTimer = 0f;
        currentT      = 0f;

        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        p0 = (bowlerHandPosition != null) ? bowlerHandPosition.position : transform.position;
        p2 = bounceMarker.position;

        Vector3 midPoint = Vector3.Lerp(p0, p2, 0.5f);
        Vector3 apex     = midPoint + Vector3.up * arcHeight;

        Vector3 forwardFlat = p2 - p0;
        forwardFlat.y = 0f;
        if (forwardFlat.sqrMagnitude > 0.0001f) forwardFlat.Normalize();

        Vector3 swingAxis      = Vector3.Cross(Vector3.up, forwardFlat).normalized;
        float   lateralOffset  = (swingAmount / 100f) * maxSwingDeviation;

        p1 = apex + swingAxis * lateralOffset;

        // Snap ball to start position
        rb.position = p0;
    }

    private void UpdateAirPhase()
    {
        deliveryTimer += Time.fixedDeltaTime;
        currentT       = Mathf.Clamp01(deliveryTimer / deliveryDuration);

        rb.MovePosition(EvaluateBezier(p0, p1, p2, currentT));

        Vector3 tangent = EvaluateBezierTangent(p0, p1, p2, currentT);
        if (tangent.sqrMagnitude > 0.001f) rb.MoveRotation(Quaternion.LookRotation(tangent));

        if (currentT >= 1f)
        {
            OnBallReachedMarker();
        }
    }

    private void OnBallReachedMarker()
    {
        rb.position = p2;

        Vector3 tangentAtImpact = EvaluateBezierTangent(p0, p1, p2, 1f);
        Vector3 horizontalDir   = tangentAtImpact;
        horizontalDir.y = 0f;
        if (horizontalDir.sqrMagnitude > 0.0001f) horizontalDir.Normalize();

        float spinAngle   = (spinAmount / 100f) * maxSpinAngle;
        Quaternion spinRot = Quaternion.AngleAxis(spinAngle, Vector3.up);
        horizontalDir      = spinRot * horizontalDir;

        Vector3 bounceVelocity = horizontalDir * postBounceSpeed
                               + Vector3.up    * bounceUpwardSpeed;

        rb.velocity       = bounceVelocity;
        postBounceTimer   = 0f;
        currentState      = BallState.PostBounce;
    }

    private void UpdatePostBounce()
    {
        postBounceTimer += Time.fixedDeltaTime;
        if (postBounceTimer >= resetDelay)
        {
            ResetBall();
        }
    }

    private void ResetBall() => currentState = BallState.Idle;

    private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return (u * u      * p0)
             + (2f * u * t * p1)
             + (     t * t * p2);
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

        Vector3 gP0 = bowlerHandPosition.position;
        Vector3 gP2 = bounceMarker.position;

        // Rebuild arc apex
        Vector3 midPoint = Vector3.Lerp(gP0, gP2, 0.5f);
        Vector3 apex     = midPoint + Vector3.up * arcHeight;

        // ── Compute P1 based on delivery type ─────────────────────────────────
        Vector3 gP1;

        Vector3 fwd = gP2 - gP0; fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

        Vector3 axis          = Vector3.Cross(Vector3.up, fwd).normalized;
        float   lateralOffset = (swingAmount / 100f) * maxSwingDeviation;

        gP1 = apex + axis * lateralOffset;

        // ── Draw the Bézier path ───────────────────────────────────────────────
        Gizmos.color = Color.red;
        Vector3 prev  = gP0;
        const int steps = 60;
        for (int i = 1; i <= steps; i++)
        {
            float   t   = i / (float)steps;
            Vector3 pos = EvaluateBezier(gP0, gP1, gP2, t);
            Gizmos.DrawLine(prev, pos);
            prev = pos;
        }

        // ── Draw control point P1 and its hull lines ───────────────────────────
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(gP1, 0.12f);
        Gizmos.DrawLine(gP0, gP1);
        Gizmos.DrawLine(gP1, gP2);

        // ── Draw the landing marker ────────────────────────────────────────────
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gP2, 0.18f);

        // ── Draw the post-bounce direction ─────────────────────────────────────
        Vector3 tangentAtEnd = EvaluateBezierTangent(gP0, gP1, gP2, 1f);
        Vector3 horizDir     = tangentAtEnd; horizDir.y = 0f;
        if (horizDir.sqrMagnitude > 0.0001f) horizDir.Normalize();

        // Rotate the gizmo ray to reflect spin deviation
        float     spinAngle = (spinAmount / 100f) * maxSpinAngle;
        Quaternion spinRot  = Quaternion.AngleAxis(spinAngle, Vector3.up);
        horizDir            = spinRot * horizDir;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(gP2, horizDir * 2f);
    }
#endif
}
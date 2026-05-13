using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMarker : MonoBehaviour
{
    [Header("References")]
    public BallController ballLauncher;
 
    [Header("Movement Settings")]
    public float markerSpeed = 5f;
 
    [Range(1f, 20f)]
    public float movementSmoothing = 10f;
 
    [Header("Ground Lock")]
    public float groundY = 0f;
 
    [Header("Pitch Boundary Clamps")]
    public Vector2 pitchBoundsMin = new Vector2(-1.5f, 5f);
    public Vector2 pitchBoundsMax = new Vector2(1.5f, 18f);
 
    private Vector3 targetPosition;

    private void Start()
    {
        targetPosition = transform.position;
        LockYAxis();
    }
 
    private void Update()
    {
        if (ballLauncher != null && ballLauncher.IsDelivering)
        {
            return;
        }
 
        ReadInputAndMove();
    }
    private void ReadInputAndMove()
    {
        float inputForward = Input.GetAxis("Vertical");    // W = +1, S = -1
        float inputRight   = Input.GetAxis("Horizontal");  // D = +1, A = -1
 
        Vector3 moveDelta = new Vector3(inputRight, 0f, inputForward)
                            * markerSpeed
                            * Time.deltaTime;
 
        targetPosition += moveDelta;
 
        targetPosition.x = Mathf.Clamp(targetPosition.x, pitchBoundsMin.x, pitchBoundsMax.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z, pitchBoundsMin.y, pitchBoundsMax.y);
 
        targetPosition.y = groundY;
 
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            movementSmoothing * Time.deltaTime
        );
 
        LockYAxis();
    }
 
    private void LockYAxis()
    {
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
    }
 
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
 
        // Draw the allowed movement rectangle on the pitch surface
        Vector3 bottomLeft  = new Vector3(pitchBoundsMin.x, groundY, pitchBoundsMin.y);
        Vector3 bottomRight = new Vector3(pitchBoundsMax.x, groundY, pitchBoundsMin.y);
        Vector3 topLeft     = new Vector3(pitchBoundsMin.x, groundY, pitchBoundsMax.y);
        Vector3 topRight    = new Vector3(pitchBoundsMax.x, groundY, pitchBoundsMax.y);
 
        Gizmos.DrawLine(bottomLeft,  bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight,    topLeft);
        Gizmos.DrawLine(topLeft,     bottomLeft);
 
        // Current marker position indicator
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
#endif
}
 
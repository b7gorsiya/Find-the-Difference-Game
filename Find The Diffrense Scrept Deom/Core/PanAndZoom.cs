using DG.Tweening;
using System;
using UnityEngine;

public class PanAndZoom : MonoBehaviour
{
    public RectTransform contentRect; // The image or content to zoom and pan
    public RectTransform viewportRect; // The parent (masked) container
    public float minZoom = 1f; // Minimum zoom scale
    public float maxZoom = 3f; // Maximum zoom scale
    public float zoomSpeed = 0.1f; // Speed of zooming
    public float smoothTime = 0.2f; // Smooth transition time for scaling and panning

    private Vector2 lastTouchPosition; // Used for single-finger dragging
    private bool isPanning; // Indicates if panning is active
    private float targetScale; // Target scale for smooth zoom
    private Vector2 targetPosition; // Target position for smooth pan
    private Vector2 scaleVelocity = Vector2.zero; // Velocity for smooth scaling
    private Vector2 positionVelocity = Vector2.zero; // Velocity for smooth panning

    public static Action<float> Zoomin;
    public static Action<float> ZoomOut;

    public static Action ZoomOutCompleted;

    bool isZoomingOut = false;
    void Start()
    {
        targetScale = contentRect.localScale.x;
        targetPosition = contentRect.anchoredPosition;
    }

    void Update()
    {
        if (GameManager.Instance.stopZoomAndPan)
        {
            return;
        }

        if (Application.isMobilePlatform)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }

        SmoothTransition();
    }

    private void HandleMouseInput()
    {
        // Zoom using scroll wheel
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0f)
        {
            AdjustZoom(scrollDelta, Input.mousePosition);
        }

        // Pan using mouse
        if (Input.GetMouseButtonDown(0))
        {
            lastTouchPosition = Input.mousePosition;
            isPanning = true;
        }
        else if (Input.GetMouseButton(0) && isPanning)
        {
            Vector2 panDelta = (Vector2)Input.mousePosition - lastTouchPosition;
            targetPosition += panDelta;
            lastTouchPosition = Input.mousePosition;

            // Continuously constrain the position while panning
            ConstrainToBounds();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isPanning = false;
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 2)
        {
            HandleTwoFingerTouch();
        }
        else if (Input.touchCount == 1)
        {
            HandleSingleFingerTouch();
        }
        else
        {
            isPanning = false;
        }
    }
/*
    private void HandleTwoFingerTouch()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        Vector2 currentPanCenter = (touch1.position + touch2.position) / 2;

        // Detect zoom (pinch gesture)
        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float prevDistance = (touch1.position - touch1.deltaPosition - (touch2.position - touch2.deltaPosition)).magnitude;
            float currentDistance = (touch1.position - touch2.position).magnitude;

            float zoomDelta = (currentDistance - prevDistance) * zoomSpeed * Time.deltaTime;
            AdjustZoom(zoomDelta, currentPanCenter);
        }
    }
*/
    private void HandleTwoFingerTouch()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        Vector2 currentPanCenter = (touch1.position + touch2.position) / 2;

        // Detect zoom (pinch gesture)
        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float prevDistance = (touch1.position - touch1.deltaPosition - (touch2.position - touch2.deltaPosition)).magnitude;
            float currentDistance = (touch1.position - touch2.position).magnitude;

            float zoomDelta = (currentDistance - prevDistance) * zoomSpeed * Time.deltaTime;

            // Ignore very small changes (helps prevent rapid drift)
            if (Mathf.Abs(zoomDelta) > 0.001f)
            {
                AdjustZoom(zoomDelta, currentPanCenter);
            }
        }
    }

    private void HandleSingleFingerTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            lastTouchPosition = touch.position;
        }
        else if (touch.phase == TouchPhase.Moved && targetScale > minZoom)
        {
            Vector2 panDelta = touch.position - lastTouchPosition;
            targetPosition += panDelta;
            lastTouchPosition = touch.position;

            ConstrainToBounds();
        }
    }

    private void AdjustZoom(float delta, Vector2 pivot)
    {
        Vector2 pivotLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, pivot, null, out pivotLocalPos);

        float newScale = Mathf.Clamp(targetScale + delta * zoomSpeed, minZoom, maxZoom);

        //Evenets Call
        if (newScale > targetScale)
        {
            Zoomin?.Invoke(newScale);
        }
        else if (newScale < targetScale)
        {
            ZoomOut?.Invoke(newScale);
        }
        Vector2 scaleChange = Vector2.one * (newScale - targetScale);

        targetScale = newScale;

        targetPosition -= pivotLocalPos * scaleChange;

        ConstrainToBounds();
    }
  
    private void ConstrainToBounds()
    {
        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 contentSize = contentRect.rect.size * targetScale;

        float xdump = contentSize.x - viewportSize.x;
        float ydump = contentSize.y - viewportSize.y; ;

        if (targetPosition.x < 0)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -(xdump / 2), targetPosition.x);
        }
        else
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, targetPosition.x, xdump / 2);
        }

        if (targetPosition.y < 0)
        {
            targetPosition.y = Mathf.Clamp(targetPosition.y, -(ydump / 2), targetPosition.y);
        }
        else
        {
            targetPosition.y = Mathf.Clamp(targetPosition.y, targetPosition.y, ydump / 2);
        }
    }

    private void SmoothTransition()
    {
        if (GameManager.Instance.stopZoomAndPan)
        {
            return;
        }
        if (isZoomingOut) return;

        float smoothScale = Mathf.SmoothDamp(contentRect.localScale.x, targetScale, ref scaleVelocity.x, smoothTime);
        contentRect.localScale = Vector3.one * smoothScale;
        Vector2 smoothedPosition = Vector2.SmoothDamp(contentRect.anchoredPosition, targetPosition, ref positionVelocity, smoothTime);
        contentRect.anchoredPosition = smoothedPosition;
    }

    public void CompleteZoomOut(float speed=0.1f)
    {
        if (isZoomingOut) return; // Prevent multiple calls during animation

        isZoomingOut = true; // Set the flag
        contentRect.DOAnchorPos(Vector2.zero, speed);
        contentRect.DOScale(minZoom, speed).OnComplete(() =>
        {
            targetScale = minZoom; // Sync the target scale to the minimum zoom
            targetPosition = Vector2.zero; // Sync the target position to the center
            contentRect.localScale = Vector3.one * targetScale;
            contentRect.anchoredPosition = targetPosition;
            isZoomingOut = false; // Reset the flag after animation completes
            ZoomOut?.Invoke(contentRect.localScale.x);
            ZoomOutCompleted?.Invoke();
        });
    }
    public void ZoomToPoint(Vector2 normalizedPoint, float targetZoomLevel,float duration)
    {
        Debug.Log($"Point {normalizedPoint}, ZoomLevel {targetZoomLevel}");

        // Clamp the target zoom level
        float newScale = Mathf.Clamp(targetZoomLevel, minZoom, maxZoom);

        // Convert the normalized point to local coordinates relative to the contentRect
        Vector2 contentSize = contentRect.rect.size;
        Vector2 contentLocalPoint = new Vector2(
            (normalizedPoint.x - 0.5f) * contentSize.x,
            (normalizedPoint.y - 0.5f) * contentSize.y
        );

        // Scale delta between the current and target scales
        float scaleDelta = newScale / contentRect.localScale.x;

        // Adjust the position to zoom into the specified point
        targetPosition = contentRect.anchoredPosition - (contentLocalPoint * (scaleDelta - 1));

        // Update the target scale
        targetScale = newScale;

        // Constrain the new position to bounds
        ConstrainToBounds();

        // Smoothly transition to the new scale and position
        contentRect.DOAnchorPos(targetPosition, duration);
        contentRect.DOScale(Vector3.one * targetScale, duration);
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrimsonGames.CBN.InputHandling
{
    public class InputHandler : MonoBehaviour
    {
        private Camera mainCamera;
        public GraphicRaycaster graphicRaycaster;
        public EventSystem eventSystem;
        public float touchRecognitionPixelError = 12f;
        public PinchZoomHandler zoomHandler;

        [Header("Tap Settings")]
        public float doubleTapThreshold = 0.3f; // Time window for a double tap
        public float holdThreshold = 0.5f;      // Time required for a hold
        public float tapMoveThreshold = 1f;   // Maximum movement for a tap

        public static Action<Vector2> OnSingleTap;     // Triggered on single tap
        public static Action<Vector2> OnDoubleTap;     // Triggered on double tap
        public static Action<Vector2> OnHold;
        public static Action<Vector2> OnTouchEnd;          // Triggered on hold end
        // Triggered on hold

        private bool isHolding = false;
        private bool isPotentialTap = false;
        private int tapCount = 0;
        private float tapTime = 0f;
        private float holdStartTime = 0f;
        private Vector2 startTouchPosition;

        public bool simulateZoom = false;
        void Start()
        {
            mainCamera = Camera.main;
            zoomHandler = GetComponent<PinchZoomHandler>();
        }

        void Update()
        {
            DetectInputBasedOnDevice();
#if UNITY_EDITOR || UNITY_STANDALONE

            if (simulateZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll > 0f)
                {
                    zoomHandler.SimZoomIn();
                }
                else if (scroll < 0f)
                {
                    zoomHandler.SimZoomOut();
                }
            }
#endif
        }

        void DetectInputBasedOnDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegin(touch.position);
                        break;

                    case TouchPhase.Moved:
                        HandleTouchMove(touch.position);
                        break;

                    case TouchPhase.Stationary:
                        HandleTouchHold(touch.position);
                        break;

                    case TouchPhase.Ended:
                        HandleTouchEnd(touch.position);
                        break;
                }
            }
            else if (Input.touchCount > 1)
            { 
                ResetTap(); 
            }
#else
            if (Input.GetMouseButtonDown(0)) // For mouse input
            {
                HandleTouchBegin(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                HandleTouchHold(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleTouchEnd(Input.mousePosition);
            }
#endif
        }

        private void HandleTouchBegin(Vector2 position)
        {
            startTouchPosition = position;
            holdStartTime = Time.time;
            isHolding = false;
            isPotentialTap = true;
        }

        private void HandleTouchMove(Vector2 position)
        {
            if (Vector2.Distance(startTouchPosition, position) > tapMoveThreshold)
            {
                if (Time.time - holdStartTime > 0.1f) // Allow a brief movement during fast taps
                {
                    Debug.Log("Touch Moved Beyond Tap Threshold");
                    isPotentialTap = false;
                }
            }
        }

        private void HandleTouchHold(Vector2 position)
        {
            if (!isHolding && Time.time - holdStartTime >= holdThreshold)
            {
                isHolding = true;
                OnHold?.Invoke(position); // Trigger hold event
            }
        }

        private void HandleTouchEnd(Vector2 position)
        {
            OnTouchEnd?.Invoke(position);
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleTouchMove(position);
#endif

            if (isHolding)
            {
                return; // Don't process as a tap if it was a hold
            }
            float currentTime = Time.time;

            if (isPotentialTap)
            {
                tapCount++;
                if (tapCount == 1)
                {
                    // First tap
                    tapTime = currentTime;
                }
                else if (tapCount == 2 && currentTime - tapTime <= doubleTapThreshold)
                {
                    // Double tap detected
                    OnDoubleTap?.Invoke(position);
                    ResetTap();
                }
            }

            if (tapCount == 1/* && currentTime - tapTime > doubleTapThreshold*/)
            {
                // Single tap detected
                OnSingleTap?.Invoke(position);
                ResetTap();
            }
        }

        private void ResetTap()
        {
            tapCount = 0;
            tapTime = 0f;
            isPotentialTap = false;
        }
    }


}
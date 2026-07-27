using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonGames.CBN.InputHandling
{
    public class PinchZoomHandler : MonoBehaviour
    {
        public RectTransform gameplayPanel; // This is the Container object being scaled
        public ScrollRect scrollRect;
        public float minScale = 0.5f;
        public float maxScale = 2.4f;
        public float simZoomStep = 0.1f;
        public bool pinchAndPan = false;

        public static Action<Vector2> OnZoomStarted;
        public static Action<float> onZoomIn;
        public static Action<float> onZoomOut;
        //Invoked with distance delta and center point delta
        public Action<float, Vector2> OnZoom;
        public static Action<float> ZoomOut;

        private bool isPinching = false;

        private Coroutine zoomingRoutine;
        private Vector2 zoomPoint;
        private bool zoomStarted;
        [SerializeField]
        private float zoomSpeed = 2.5f;
        public GameObject blockerPanel;

        bool doPan = false;
        private void OnEnable()
        {
            InputHandler.OnHold += Panning;
            InputHandler.OnTouchEnd += TouchEnd;
        }

        private void OnDestroy()
        {
            InputHandler.OnHold -= Panning;
            InputHandler.OnTouchEnd -= TouchEnd;
        }
        private void Panning(Vector2 vector)
        {
            scrollRect.GetComponent<CanvasGroup>().blocksRaycasts = true;
            doPan = true;
        }
        private void TouchEnd(Vector2 vector)
        {
            doPan = false;
           scrollRect.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
        private void Update()
        {
            HandlePinchZoom();
            HandleScrollRect();
        }

        public void SimZoomSpeedIncrease()
        {
            float newZoomSpeed = zoomSpeed + 0.1f;
            zoomSpeed = Mathf.Clamp(newZoomSpeed, 0.1f, 10f);
            Debug.Log($"zoomSpeed set to {zoomSpeed}");
        }

        public void SimZoomSpeedDecrease()
        {
            float newZoomSpeed = zoomSpeed - 0.1f;
            zoomSpeed = Mathf.Clamp(newZoomSpeed, 0.1f, 10f);
            Debug.Log($"zoomSpeed set to {zoomSpeed}");
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount == 2)
            {
                ManageScroll();

                UnityEngine.Touch touchZero = Input.GetTouch(0);
                UnityEngine.Touch touchOne = Input.GetTouch(1);

                // Handle the start of a pinch zoom gesture
                if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
                {
                    zoomStarted = true;
                    StartZooming(touchZero.position, touchOne.position);
                }

                // Handle the end of a pinch zoom gesture
                if (touchZero.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended)
                {
                    if (!zoomStarted)
                    {
                        return;
                    }
                    zoomStarted = false;

                    if (zoomingRoutine != null)
                    {
                        StopZooming();
                    }
                }
            }
            else
            {
                // If touch count is not 2, ensure zooming is stopped
                if (zoomStarted)
                {
                    zoomStarted = false;

                    if (zoomingRoutine != null)
                    {
                        StopZooming();
                    }
                }
            }
        }

        bool twoFingerTouch = false;
        private void ManageScroll()
        {
            if (Input.touchCount < 2) return;

            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (!twoFingerTouch&& (touch1.phase.Equals(TouchPhase.Began) || touch2.phase.Equals(TouchPhase.Began)))
            {
                print("Touched with two finger");
                scrollRect.GetComponent<CanvasGroup>().blocksRaycasts = false;
                twoFingerTouch = true;
            }

            if(twoFingerTouch && (touch1.phase.Equals(TouchPhase.Ended) ||touch2.phase.Equals(TouchPhase.Ended)))
            {
                print("Touched Ended with two finger");
                scrollRect.GetComponent<CanvasGroup>().blocksRaycasts = true;
                twoFingerTouch = false;
            }
        }
        private void HandleScrollRect()
        {
            if (Input.touchCount == 1)
            {
                if (!pinchAndPan & !doPan)
                { return; }

                if (isPinching)
                {
                    isPinching = false;
                    if (pinchAndPan)
                    {
                        scrollRect.enabled = true;
                    }
                }
            }
            else
            {
                if (!isPinching)
                {
                    isPinching = true;
                    if (pinchAndPan)
                    {
                        scrollRect.enabled = false;
                    }
                }
            }
        }

        private Vector3 ClampScale(Vector3 scale)
        {
            scale.x = Mathf.Clamp(scale.x, minScale, maxScale);
            scale.y = Mathf.Clamp(scale.y, minScale, maxScale);
            scale.z = 1; // Assuming we're working in a 2D context
            return scale;
        }

        private void SmoothClampScale(Vector3 clampedScale)
        {

        }

        public void SimZoomIn()
        {
            var scaleX = gameplayPanel.localScale.x;
            if ((scaleX + simZoomStep) > maxScale)
            {
                return;
            }
            gameplayPanel.DOScale(scaleX + simZoomStep, 0.1f).OnComplete(() =>
            {
                onZoomIn?.Invoke(gameplayPanel.localScale.x);
            });
        }

        public void SimZoomOut()
        {
            var scaleX = gameplayPanel.localScale.x;
            if ((scaleX - simZoomStep) < minScale)
            {
                gameplayPanel.DOScale(minScale, 0.1f).OnComplete(() =>
                {
                    onZoomOut?.Invoke(gameplayPanel.localScale.x);
                });
                return;
            }

            gameplayPanel.DOScale(scaleX - simZoomStep, 0.1f).OnComplete(() =>
            {
                onZoomOut?.Invoke(gameplayPanel.localScale.x);
            });
        }

        public void CompleteZoomOut()
        {
            Debug.Log("Colpete zoom out");
            gameplayPanel.DOAnchorPos(Vector2.zero, 0.1f);
            gameplayPanel.DOScale(minScale, 0.1f).OnComplete(() =>
            {
                onZoomOut?.Invoke(gameplayPanel.localScale.x);
            });
        }

        private void StartZooming(Vector2 touchZeroPos, Vector2 touchOnePos)
        {
            //Debug.Log($"[StartZooming] called touchZeroPos : {touchZeroPos}, touchOnePos : {touchOnePos}");
            Vector2 pos = (touchOnePos - touchZeroPos) * .5f + touchZeroPos;
            var gameplayPanel2 = gameplayPanel;// GameManager.Instance.gameplayPanelRectUpdater.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(gameplayPanel2, pos, Camera.main, out Vector2 onRect))
            {
                // Remember pinch point, used for zooming into specific point in panel.
                zoomPoint = onRect;
                //Debug.Log($"[StartZooming] zoomPoint set to {zoomPoint}");
            }
            //Debug.Log($"[StartZooming] zoomStarted set to {zoomStarted}");
            OnZoomStarted.SafeInvoke(pos);
            zoomingRoutine = StartCoroutine(ZoomingRoutine(touchZeroPos, touchOnePos));
        }

        private void StopZooming()
        {
            //Debug.Log($"[StopZooming] called");
            if (zoomingRoutine != null)
            {
                StopCoroutine(zoomingRoutine);
                zoomingRoutine = null;
                ZoomOut.Invoke(gameplayPanel.localScale.x);
                //Debug.Log($"[StopZooming] zooming coroutine stopped");
            }
        }

        private void ZoomPanel(float zoomDelta, Vector2 posDelta)
        {
            //Debug.Log($"[ZoomPanel] called, zoomStarted value is {zoomStarted}");
            if (!zoomStarted)
            {
                return;
            }

            var rectTransform = gameplayPanel;// GameManager.Instance.gameplayPanelRectUpdater.GetComponent<RectTransform>();
            float screenPercent = (Screen.width + Screen.height) * .5f;
            float zoomDeltaAdjusted = zoomDelta / screenPercent * zoomSpeed;
            float prevZoom = rectTransform.localScale.x;
            float zoomValue = Mathf.Clamp(rectTransform.localScale.x + zoomDeltaAdjusted, minScale, maxScale);
            if (zoomValue < minScale) zoomValue = minScale;
            rectTransform.localScale = new Vector3(zoomValue, zoomValue, 1f);
            //OnPanelScaleChanged.SafeInvoke(rectTransform.localScale.x);
            // Since we're zooming into specific point, move panel towards it.
            if (pinchAndPan)
            {
                rectTransform.anchoredPosition += -zoomPoint * (rectTransform.localScale.x - prevZoom) + posDelta;
            }
            Debug.Log($"Gameplay Panel anchoredPosition : {rectTransform.anchoredPosition}");
        }

        private IEnumerator ZoomingRoutine(Vector2 touchZeroPos, Vector2 touchOnePos)
        {
            float previousDistance = Vector2.Distance(touchZeroPos, touchOnePos);
            Vector2 previousCenter = (touchOnePos - touchZeroPos) * .5f + touchZeroPos;

            while (true)
            {
                yield return null;

                UnityEngine.Touch touchZero = Input.GetTouch(0);
                UnityEngine.Touch touchOne = Input.GetTouch(1);

                Vector2 newTouchZero = touchZero.position;
                Vector2 newTouchOne = touchOne.position;
                float currentDistance = Vector2.Distance(newTouchZero, newTouchOne);
                Vector2 center = (newTouchOne - newTouchZero) * .5f + newTouchZero;

                ZoomPanel(currentDistance - previousDistance, center - previousCenter);
                OnZoom?.Invoke(currentDistance - previousDistance, center - previousCenter);
                previousDistance = currentDistance;
                previousCenter = center;
            }
        }
    }
}

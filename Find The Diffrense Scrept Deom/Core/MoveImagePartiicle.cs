using System;
using System.Collections;
using UnityEngine;

public class MoveImageParticle : MonoBehaviour
{
    public RectTransform targetRectTransform; // Target RectTransform to move towards
    public AnimationCurve movementCurve;      // Curve to control the movement
    public float duration = 2f;               // Duration of the movement
    private RectTransform rectTransform;      // Reference to this object's RectTransform
    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(MoveWithCurve());
    }

    private IEnumerator MoveWithCurve()
    {
        Vector2 startPos = rectTransform.anchoredPosition;

        // Convert target's world position to the local space of the moving object's parent
        Vector2 targetPos = rectTransform.parent.InverseTransformPoint(targetRectTransform.position);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = movementCurve.Evaluate(t);

            // Interpolate X and Y positions with the curve value
            float newX = Mathf.Lerp(startPos.x, targetPos.x, curveValue);
            float newY = Mathf.Lerp(startPos.y, targetPos.y, curveValue);
            rectTransform.anchoredPosition = new Vector2(newX, newY);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is exactly the target position
        rectTransform.anchoredPosition = targetPos;
        Destroy(gameObject, 1f);
    }
}

using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ExpandCard : MonoBehaviour
{
    public RectTransform rectTransform;  // Assign the item's RectTransform
    public RectTransform contentRect;    // Assign the Scroll View's Content RectTransform
    public VerticalLayoutGroup layoutGroup; // Assign the VerticalLayoutGroup (or HorizontalLayoutGroup)
    public ScrollRect scrollRect;        // Assign the Scroll View component

    public Button toggleButton;  // Assign the button in the Inspector
    public Sprite expandSprite;  // Icon for "Expand"
    public Sprite shrinkSprite;  // Icon for "Shrink"

    public float minHeight = 100f;  // Collapsed height
    public float maxHeight = 300f;  // Expanded height
    public float animationTime = 0.5f;

    internal bool isExpanded = false;
    private Image buttonImage;

    public static Action Expand;

    private void Start()
    {
        if (toggleButton != null)
            buttonImage = toggleButton.GetComponent<Image>(); // Get button image
    }

    public void ToggleExpand()
    {
        float targetHeight = isExpanded ? minHeight : maxHeight;

        // Animate the height change
        rectTransform.DOSizeDelta(new Vector2(rectTransform.sizeDelta.x, targetHeight), animationTime)
            .SetEase(Ease.OutQuad)
            .OnUpdate(UpdateLayout)
            .OnComplete(UpdateLayout); // Ensure layout updates after animation

        toggleButton.GetComponent<Image>().sprite = isExpanded ? expandSprite : shrinkSprite;

        isExpanded = !isExpanded;
        Expand?.Invoke();
    }

    private void UpdateLayout()
    {
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;  // Temporarily disable to force update
            layoutGroup.enabled = true;
        }

        // Force UI to refresh
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();

        // Adjust scroll view to ensure the item stays visible
        scrollRect.verticalNormalizedPosition = 1f; // Adjust this based on your needs
    }
}

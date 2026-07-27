using CrimsonGames.CBN.InputHandling;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ImageMarkerWithPanZoom : MonoBehaviour
{
    public RawImage image1; // First image (RawImage component)
    public RawImage image2; // Second image (RawImage component)
    public GameObject markerPrefab; // Prefab for the marker (e.g., a small dot)
    public RectTransform maskContainer1; // Mask container for the first image
    public RectTransform maskContainer2; // Mask container for the second image

    [SerializeField] private List<MarkedDifferenceData> markedPoints = new();

    private float zoomFactor = 1f; // Default zoom factor (1 = no zoom)
    public GameObject zoomInParent;

    GameObject lastMarkerImg1;
    GameObject lastMarkerImg2;

    private bool isAdjustingScale = false; // Whether the marker scale is being adjusted
    private GameObject currentlyAdjustingMarker1; // Currently selected marker on image1
    private GameObject currentlyAdjustingMarker2; // Currently selected marker on image2

    public InputHandler inputHandler;
    void Update()
    {
        SimulateZoomAndPanSibling();
        // Handle Mouse Click for marking points on images
        if (Input.GetMouseButtonDown(1) && !isAdjustingScale) // Left mouse click (place marker)
        {
            Vector2 mousePos = Input.mousePosition;

            if (RectTransformUtility.RectangleContainsScreenPoint(image1.rectTransform, mousePos))
            {
                AddMarkerAtPoint(mousePos, image1.rectTransform);
            }
            else if (RectTransformUtility.RectangleContainsScreenPoint(image2.rectTransform, mousePos))
            {
                AddMarkerAtPoint(mousePos, image2.rectTransform);
            }
        }
        // Adjust scale of the last marker using mouse scroll
        if (isAdjustingScale && (currentlyAdjustingMarker1 != null || currentlyAdjustingMarker2 != null))
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0)
            {
                AdjustMarkerScale(currentlyAdjustingMarker1, scrollDelta);
                AdjustMarkerScale(currentlyAdjustingMarker2, scrollDelta);
            }

            // End scaling adjustment on left mouse click
            if (Input.GetMouseButtonDown(0)) // Left mouse click
            {
                markedPoints[markedPoints.Count - 1].scale = currentlyAdjustingMarker1.transform.localScale.x;
                isAdjustingScale = false;
                inputHandler.simulateZoom = true;
                currentlyAdjustingMarker1 = null;
                currentlyAdjustingMarker2 = null;
            }
        }
    }
    internal void ClearUI()
    {
        //clean up 
        foreach (var item in image1.transform.GetComponentsInChildren<Image>())
        {
            Destroy(item);
        }
        foreach (var item in image2.transform.GetComponentsInChildren<Image>())
        {
            Destroy(item);
        }
        zoomInParent.GetComponent<ScrollRect>().enabled = false;
        zoomInParent.GetComponent<PinchZoomHandler>().enabled = false;
        zoomInParent.GetComponent<TouchSimulation>().enabled = false;
        zoomInParent.GetComponent<InputHandler>().enabled = false;

        image1.texture = null;
        image2.texture = null;

        image1.GetComponent<RectTransform>().localScale = Vector2.one;
        image2.GetComponent<RectTransform>().localScale = Vector2.one;

        image1.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        image2.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

        // Recreate markers
        markedPoints.Clear();

        zoomInParent.GetComponent<ScrollRect>().enabled = true;
        zoomInParent.GetComponent<PinchZoomHandler>().enabled = true;
        zoomInParent.GetComponent<TouchSimulation>().enabled = true;
        zoomInParent.GetComponent<InputHandler>().enabled = true;

    }
    void AddMarkerAtPoint(Vector2 mousePos, RectTransform _rectTransform)
    {
        if (markedPoints.Count >= 20)
        {
            Debug.LogError("Have marked max diffrence 20");
            return;
        }
        // Get the local point of the mouse click within the image
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePos, null, out localPoint);

        // Original rect dimensions (without scale adjustments)
        float originalWidth = _rectTransform.rect.width;
        float originalHeight = _rectTransform.rect.height;

        // Adjusted dimensions (with scale adjustments)
        float adjustedWidth = originalWidth * _rectTransform.localScale.x;
        float adjustedHeight = originalHeight * _rectTransform.localScale.y;

        // Calculate the normalized point without considering scale
        Vector2 normalizedPointWithoutScale = new Vector2(
            (localPoint.x / originalWidth) + 0.5f,
            (localPoint.y / originalHeight) + 0.5f
        );

        MarkedDifferenceData _point = new();
        _point.point = normalizedPointWithoutScale;
        markedPoints.Add(_point);

        // Calculate the normalized point with scale
        Vector2 normalizedPointWithScale = new Vector2(
            (localPoint.x / adjustedWidth) + 0.5f,
            (localPoint.y / adjustedHeight) + 0.5f
        );

        //// Adjust marker position for zoom and pan
        AddMarker(normalizedPointWithScale);

        // Begin scaling adjustment
        isAdjustingScale = true;
        inputHandler.simulateZoom = false;
        currentlyAdjustingMarker1 = lastMarkerImg1;
        currentlyAdjustingMarker2 = lastMarkerImg2;
    }
    void AdjustMarkerScale(GameObject marker, float scrollDelta)
    {
        if (marker != null)
        {
            RectTransform rectTransform = marker.GetComponent<RectTransform>();
            float newScale = Mathf.Clamp(rectTransform.localScale.x + scrollDelta * 0.1f, 0.5f, 3f);
            rectTransform.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
    void SimulateZoomAndPanSibling()
    {
        zoomFactor = 1 + (image1.transform.localScale.x - 1);
        // Scale the images based on the zoom factor
        image2.rectTransform.localScale = image1.rectTransform.localScale;
        image2.rectTransform.anchoredPosition = image1.rectTransform.anchoredPosition;
    }
    void AddMarker(Vector2 normalizedPoint, float scale = 1)
    {
        // Instantiate marker on both images, adjusted for zoom and pan
        lastMarkerImg1 = InstantiateMarker(normalizedPoint, image1.rectTransform, scale);
        lastMarkerImg2 = InstantiateMarker(normalizedPoint, image2.rectTransform, scale);
    }
    GameObject InstantiateMarker(Vector2 normalizedPoint, RectTransform parentImage, float scale = 1)
    {
        // Get the adjusted size of the image based on zoom factor
        float adjustedWidth = parentImage.rect.width * zoomFactor;
        float adjustedHeight = parentImage.rect.height * zoomFactor;

        // Convert normalized point to local position within the image
        Vector2 localPoint = new Vector2(
            (normalizedPoint.x - 0.5f) * adjustedWidth,
            (normalizedPoint.y - 0.5f) * adjustedHeight
        );

        // Instantiate the marker at the correct position
        GameObject marker = Instantiate(markerPrefab, parentImage);
        marker.transform.localScale = Vector3.one * scale;
        marker.GetComponent<RectTransform>().anchoredPosition = localPoint;

        return marker;
    }

    internal void SaveLevel(bool _forceOverwrite)
    {
        LevelCreation.instance.SaveMarkersToJSON(markedPoints,_forceOverwrite);
    }
    internal void LoadLevel(LevelData markerData)
    {
        if (markerData == null)
        {
            Debug.LogError("Failed to parse JSON data.");
            return;
        }
        //clean up 
        foreach (var item in image1.transform.GetComponentsInChildren<Image>())
        {
            Destroy(item);
        }
        foreach (var item in image2.transform.GetComponentsInChildren<Image>())
        {
            Destroy(item);
        }
        zoomInParent.GetComponent<ScrollRect>().enabled = false;
        zoomInParent.GetComponent<PinchZoomHandler>().enabled = false;
        zoomInParent.GetComponent<TouchSimulation>().enabled = false;
        zoomInParent.GetComponent<InputHandler>().enabled = false;

        image1.GetComponent<RectTransform>().localScale = Vector2.one;
        image2.GetComponent<RectTransform>().localScale = Vector2.one;

        image1.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        image2.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

        // Load images from base64 strings
        if (!string.IsNullOrEmpty(markerData.image1_Base64))
        {
            Texture2D texture1 = LoadTextureFromBase64(markerData.image1_Base64);
            if (texture1 != null)
            {
                image1.texture = texture1;
                // image1.SetNativeSize();
            }
        }

        if (!string.IsNullOrEmpty(markerData.image2_Base64))
        {
            Texture2D texture2 = LoadTextureFromBase64(markerData.image2_Base64);
            if (texture2 != null)
            {
                image2.texture = texture2;
                // image2.SetNativeSize();
            }
        }

        // Recreate markers
        markedPoints.Clear();
        foreach (var normalizedPoint in markerData.points)
        {
            AddMarker(normalizedPoint.point, normalizedPoint.scale);
        }

        Debug.Log("Level data loaded successfully.");
        zoomInParent.GetComponent<ScrollRect>().enabled = true;
        zoomInParent.GetComponent<PinchZoomHandler>().enabled = true;
        zoomInParent.GetComponent<TouchSimulation>().enabled = true;
        zoomInParent.GetComponent<InputHandler>().enabled = true;
    }
    private Texture2D LoadTextureFromBase64(string base64)
    {
        byte[] imageData = System.Convert.FromBase64String(base64);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        if (texture.LoadImage(imageData))
        {
            return texture;
        }
        Debug.LogError("Failed to load texture from base64 string.");
        return null;
    }



    public void UnDoLast()
    {
        if (lastMarkerImg1 == null || lastMarkerImg2 == null)
            return;

        if (lastMarkerImg1 != null) Destroy(lastMarkerImg1);
        if (lastMarkerImg2 != null) Destroy(lastMarkerImg2);
        markedPoints.RemoveAt(markedPoints.Count - 1);
    }
    public void ClearAll()
    {
        UIImageUploader.instance.ClearUI();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

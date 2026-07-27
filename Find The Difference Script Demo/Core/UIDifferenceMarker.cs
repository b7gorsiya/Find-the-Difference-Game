using CrimsonGames.Analytics;
using CrimsonGames.CBN.InputHandling;
using DG.Tweening;
using MobileHapticsProFreeEdition;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDifferenceMarker : MonoBehaviour
{
    public static UIDifferenceMarker Instance;

    public static Action mark;
    public static Action wrongMark;

    public RawImage image1; // First image (RawImage component)
    public RawImage image2; // Second image (RawImage component)

    public GameObject correctMarkerPrefab; // Prefab for correct marker
    public GameObject errorMarkerPrefab;   // Prefab for error feedback
    public GameObject markUIContainer;
    public GameObject markedParticle;

    [Header("Hint Properties")]
    public GameObject hintHiglightPrefab;
    public GameObject hintBackgroud; // Prefab for error feedback
    public float hintTransitionTime = 0.5f;

    public PanAndZoom pinchZoomHandler;

    private int maxLives = 3;               // Total lives

    private int currentLives;
    private float zoomFactor = 1f; // Default zoom factor (1 = no zoom)

    [SerializeField] private float clickTolerance = 0.05f; // Tolerance for matching clicks (in normalized units)
    private LevelData markerData;
    private int totalDifference = 0;
    private int markedDifference = 0;
    private bool haveLoadedPuzzle = false;

    private Dictionary<Vector2, GameObject> markerList = new();
    private List<GameObject> markerListDUMP = new();
    public List<MarkedDifferenceData> unmarkedHintPoints = new();

    private GameObject hint1, hint2;
    private Vector2 lastTouchPosition;
    private bool isPotentialTap;
    private float tapThreshold = 0.1f;
    private List<MarkUI> markUIList;

    private bool enableVibration = true;// local settings for now BETA

    public int CurrentLives { get => currentLives; }
    public int RemainingDifferencePoints { get => unmarkedHintPoints.Count; }

    private void Awake()
    {
        Instance = this;
        markUIList = markUIContainer.GetComponentsInChildren<MarkUI>(true).ToList();
    }
    private void OnEnable()
    {
        enableVibration = (PlayerPrefs.GetInt("Vibration", 1) == 1);
        LoadingJsonData.dataDownloaded += LoadPuzzle;
        InputHandler.OnSingleTap += DetectTap;
    }
    void Start()
    {
        currentLives = maxLives; // Initialize lives
    }
    void Update()
    {
        // if (!haveLoadedPuzzle || GameManager.Instance.state != GameManager.GameState.GamePlay) return;

        SimulateZoomAndPanSibling();
    }
    private void OnDestroy()
    {
        LoadingJsonData.dataDownloaded -= LoadPuzzle;
        InputHandler.OnSingleTap -= DetectTap;
    }
    private void LoadPuzzle(LevelData data, Texture texture1, Texture texture2)
    {
        enableVibration = (PlayerPrefs.GetInt("Vibration", 1) == 1);

        if (!CheckDataValidation(data, texture1, texture2))
        {
            return;
        }
        // Clean already placed marker if any
        foreach (var item in markerList.Values)
        {
            Destroy(item);
        }
        foreach (var item in markerListDUMP)
        {
            Destroy(item);
        }
        markerList.Clear();
        markerListDUMP.Clear();

        Destroy(image1.texture);
        Destroy(image2.texture);

        image1.texture = texture1;
        image2.texture = texture2;
        markerData = data;
        totalDifference = markerData.points.Count;
        markedDifference = 0;
        maxLives = currentLives = GameManager.Instance.maxLives;// custom for now 

        // UIManager.Instance.UpdateMarkersCount(totalDiffrnece, markedDiffrence);
        UIManager.Instance.UpdateLive(maxLives);
        haveLoadedPuzzle = true;

        unmarkedHintPoints.Clear();
        unmarkedHintPoints.AddRange(markerData.points);
        pinchZoomHandler.CompleteZoomOut();

        MarkUIBehaviour();
    }
    private void MarkUIBehaviour()
    {
        foreach (var mark in markUIList)
        {
            mark.gameObject.SetActive(false);
        }
        Debug.Log("check 1 :" + totalDifference + "  mark list " + markUIList.Count);
        for (int i = 0; i < totalDifference; i++)
        {
            markUIList[i].gameObject.SetActive(true);
        }
    }
    bool CheckDataValidation(LevelData data, Texture texture1, Texture texture2)
    {
        if (data == null)
        {
            Debug.LogError("Data is Null");
            return false;
        }
        if (texture1 == null)
        {
            Debug.LogError("Texture 1 is Null");
            return false;
        }
        if (texture2 == null)
        {
            Debug.LogError("Texture 1 is Null");
            return false;
        }

        return true;
    }

    private void DetectTap(Vector2 _touchPoint)
    {
        if (!haveLoadedPuzzle || GameManager.Instance.state != GameManager.GameState.GamePlay) return;
        HandleClickOnImages(_touchPoint);
    }
    private void HandleClickOnImages(Vector2 mousePos)
    {
        if (Input.touchCount > 1) return;
        // Check for click on image1
        if (RectTransformUtility.RectangleContainsScreenPoint(image1.transform.parent.GetComponent<RectTransform>(), mousePos) && IsPointerOverUIElement(mousePos, image1.rectTransform))
        {
            Vector2 normalizedPoint = GetNormalizedPoint(image1.rectTransform, mousePos);
            CheckMarkerProximity(normalizedPoint, image1.rectTransform);
        }
        // Check for click on image2
        else if (RectTransformUtility.RectangleContainsScreenPoint(image2.transform.parent.GetComponent<RectTransform>(), mousePos) && IsPointerOverUIElement(mousePos, image2.rectTransform))
        {
            Vector2 normalizedPoint = GetNormalizedPoint(image2.rectTransform, mousePos);
            CheckMarkerProximity(normalizedPoint, image2.rectTransform);
        }
    }
    private bool IsPointerOverUIElement(Vector2 mousePos, RectTransform clickedElement)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = mousePos
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults)
        {
            // Check if the raycast result is in the clickable layer
            if (result.gameObject.GetComponent<RectTransform>().Equals(clickedElement))
            {
                return true;
            }
            else
                return false;
        }
        return false;
    }

    // Calculate the normalized position of a mouse click on a RectTransform
    private Vector2 GetNormalizedPoint(RectTransform rectTransform, Vector2 screenPoint)
    {
        // Get the local point of the mouse click within the image
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out localPoint);

        // Original rect dimensions (without scale adjustments)
        float originalWidth = rectTransform.rect.width;
        float originalHeight = rectTransform.rect.height;

        Vector2 normalizedPointWithoutScale = new Vector2(
            (localPoint.x / originalWidth) + 0.5f,
            (localPoint.y / originalHeight) + 0.5f
        );

        return normalizedPointWithoutScale;
    }
    void SimulateZoomAndPanSibling()
    {
        zoomFactor = 1 + (image1.transform.localScale.x - 1);
        // Scale the images based on the zoom factor
        image2.rectTransform.localScale = image1.rectTransform.localScale;
        image2.rectTransform.anchoredPosition = image1.rectTransform.anchoredPosition;
    }
    private void CheckMarkerProximity(Vector2 normalizedPoint, RectTransform _parentImage)
    {
        MarkedDifferenceData markedPoint = markerData.points
        .Where(p => !IsPlayingTutorial(p.point))
        .OrderBy(p => Vector2.Distance(normalizedPoint, p.point))
        .FirstOrDefault();

        // foreach (MarkedDifferenceData markedPoint in markerData.points)
        // {
        float clickRadius = markedPoint.scale * clickTolerance;
        float distance = Vector2.Distance(normalizedPoint, markedPoint.point);

        // check for the hint click
        if (UIManager.Instance.showingHint && !markedPoint.point.Equals(GameManager.Instance.tutorialPoint)) return;

        if (distance <= clickRadius && !markerList.ContainsKey(markedPoint.point))
        {
            HideHint();
            if (enableVibration) TapticWave.TriggerHaptic(HapticModes.Select);
            // Correct click
            UIManager.Instance.UpdateMarkersCount(totalDifference, ++markedDifference);
            markerList.Add(markedPoint.point, InstantiateMarkerAtClick(markedPoint.point, markedPoint.scale, correctMarkerPrefab, image1.rectTransform));
            markerListDUMP.Add(InstantiateMarkerAtClick(markedPoint.point, markedPoint.scale, correctMarkerPrefab, image2.rectTransform));
            MarkParticle(markedPoint.point, _parentImage);
            unmarkedHintPoints.Remove(markedPoint);
            if (markedDifference >= totalDifference)
            {
                UIManager.Instance.mainImage.texture = image1.texture;
                GameManager.Instance.GameOverHandle(markerList.Count * 0.25f);
                LevelCompleteAnimation();
            }
            mark?.Invoke();
            return;
        }
        else if (distance <= clickRadius && markerList.ContainsKey(markedPoint.point))
        {
            if (enableVibration) TapticWave.TriggerHaptic(HapticModes.Confirm);

            var placedMarker = markerList[markedPoint.point];
            var dubPlacedMarker = markerListDUMP[markerList.Keys.ToList().IndexOf(markedPoint.point)];
            if (placedMarker != null)
            {
                placedMarker.transform.DOPunchScale(Vector3.one * 0.2f, 1);
            }
            if (dubPlacedMarker != null)
            {
                dubPlacedMarker.transform.DOPunchScale(Vector3.one * 0.2f, 1);
            }
            AudioManager.Instance.PlaySFX(AudioEvent.Tap);
            return;
        }
        //}

        if (GameManager.Instance.playTutorial || UIManager.Instance.showingHint)
            return;

        // Incorrect click
        wrongMark?.Invoke();
        if (enableVibration) TapticWave.TriggerHaptic(HapticModes.Failure);
        currentLives--;
        InstantiateMarkerAtClick(normalizedPoint, 1, errorMarkerPrefab, _parentImage, true);

        CrimsonEventsLogger.LogEvent(EPlayerEvent.life_lost);

        if (currentLives <= 0)
        {
            GameManager.Instance.GameOverHandle(1, false);
        }
    }
    private bool IsPlayingTutorial(Vector2 _point)
    {
        if (!GameManager.Instance.playTutorial)
        {
            return false;
        }
        else if (GameManager.Instance.playTutorial && _point.Equals(GameManager.Instance.tutorialPoint))
        {
            return false;
        }
        return true;
    }
    private void MarkParticle(Vector2 normalizedPoint, RectTransform _parentImage)
    {
        var markerUIPoint = markUIList[markedDifference - 1];
        // Convert normalized point to local position
        float adjustedWidth = _parentImage.rect.width;
        float adjustedHeight = _parentImage.rect.height;

        Vector2 localPoint = new Vector2(
            (normalizedPoint.x - 0.5f) * adjustedWidth,
            (normalizedPoint.y - 0.5f) * adjustedHeight
        );

        GameObject _markerParticle = Instantiate(markedParticle, _parentImage);
        _markerParticle.GetComponent<RectTransform>().anchoredPosition = localPoint;
        var particleScript = _markerParticle.GetComponent<MoveImageParticle>();
        particleScript.targetRectTransform = markerUIPoint.GetComponent<RectTransform>();
        _markerParticle.transform.SetParent(_parentImage.parent.parent);
        _markerParticle.transform.localScale = Vector3.one;
        float distance = Vector2.Distance(_markerParticle.transform.position, markerUIPoint.transform.position);
        particleScript.duration = (distance < 1000) ? (particleScript.duration * distance) / 1000 : particleScript.duration;
        _markerParticle.SetActive(true);
        DOVirtual.DelayedCall(_markerParticle.GetComponent<MoveImageParticle>().duration, () => markerUIPoint.MarkComplete());
    }
    private void LevelCompleteAnimation()
    {
        // Sort the dictionary by the x position of the Vector2 keys
        var sortedDictionary = markerList
            .OrderBy(pair => pair.Key.x)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var sortedDictionary2 = markerListDUMP
          .OrderBy(obj => obj.GetComponent<RectTransform>().anchoredPosition.x)
            .ToList();

        pinchZoomHandler.CompleteZoomOut();

        DOVirtual.DelayedCall(0.25f, () =>
        {
            float i = 0;
            foreach (var mark in markUIList)
            {
                DOVirtual.DelayedCall(i, () => { mark.gameObject.transform.DOPunchScale(Vector3.one * 1.01f, 0.25f); });
                i += 0.1f;
            }

            float index = 0;
            foreach (var item in sortedDictionary)
            {
                DOVirtual.DelayedCall(index, () => { item.Value.transform.DOPunchScale(Vector3.one * 1.1f, 0.25f); });
                index += 0.1f;
            }
            float index2 = 0;
            foreach (var item in sortedDictionary2)
            {
                DOVirtual.DelayedCall(index2, () => { item.transform.DOPunchScale(Vector3.one * 1.1f, 0.25f); });
                index2 += 0.1f;
            }
        });
    }
    private GameObject InstantiateMarkerAtClick(Vector2 normalizedPoint, float _scale, GameObject markerPrefab, RectTransform parentImage, bool wrong = false)
    {
        // Convert normalized point to local position
        float adjustedWidth = parentImage.rect.width;
        float adjustedHeight = parentImage.rect.height;

        Vector2 localPoint = new Vector2(
            (normalizedPoint.x - 0.5f) * adjustedWidth,
            (normalizedPoint.y - 0.5f) * adjustedHeight
        );

        // Instantiate marker at the click position
        GameObject marker = Instantiate(markerPrefab, parentImage);
        marker.GetComponent<RectTransform>().anchoredPosition = localPoint;
        marker.transform.localScale = Vector3.one * _scale;
        if (wrong)
        {
            Destroy(marker, 1f);
            UIManager.Instance.UpdateLive(currentLives);
            return null;
        }
        else
        {
            return marker;
        }
    }
    public void ShowHint()
    {
        GameManager.Instance.stopZoomAndPan = true;
        pinchZoomHandler.CompleteZoomOut();
        PanAndZoom.ZoomOutCompleted += FindHint;
    }
    private void FindHint()
    {
        GameManager.Instance.state = GameManager.GameState.GamePlay;
        // Pick a random unmarked difference
        Vector2 hintPoint = (GameManager.Instance.playTutorial) ? LoadingJsonData.Instance.markerData.points[4].point : unmarkedHintPoints[UnityEngine.Random.Range(0, unmarkedHintPoints.Count)].point;

        GameManager.Instance.tutorialPoint = hintPoint;

        var rectTrans = image1.rectTransform;
        // Convert normalized point to local position
        float adjustedWidth = rectTrans.rect.width;
        float adjustedHeight = rectTrans.rect.height;

        Vector2 localPoint = new Vector2(
            (hintPoint.x - 0.5f) * adjustedWidth,
            (hintPoint.y - 0.5f) * adjustedHeight
        );

        // Instantiate marker at the click position
        hint1 = Instantiate(hintHiglightPrefab, image1.rectTransform);
        hint2 = Instantiate(hintHiglightPrefab, image2.rectTransform);

        hint1.GetComponent<RectTransform>().anchoredPosition = localPoint;
        hint2.GetComponent<RectTransform>().anchoredPosition = localPoint;

        var hintParent = hintBackgroud.transform.parent;

        hint1.transform.SetParent(hintParent);
        hint2.transform.SetParent(hintParent);

        hintBackgroud.SetActive(true);
        hintBackgroud.transform.SetAsLastSibling();

        hint1.transform.localScale = hint2.transform.localScale = Vector3.one * 20;

        DOVirtual.DelayedCall(0.05f,()=>HintAnimation());

        PanAndZoom.ZoomOutCompleted -= FindHint;
    }
    private void HintAnimation()
    {
        int loopCount = (!GameManager.Instance.playTutorial) ? 10 : -1;
        hint1.transform.DOScale(Vector3.one, hintTransitionTime).OnComplete(() =>
        {
            // Create scale animation with ping-pong
            hint1.GetComponent<RectTransform>().DOScale(1.5f, 1).SetLoops(loopCount, LoopType.Yoyo).OnComplete(() =>
            {
                if (!GameManager.Instance.playTutorial)
                {
                    hint1.transform.DOScale(Vector3.one * 20, hintTransitionTime).OnComplete(() =>
                    {
                        GameManager.Instance.stopZoomAndPan = false;
                        Destroy(hint1); hintBackgroud.SetActive(false); UIManager.Instance.showingHint = false;
                    });
                }
            });
        });
        hint2.transform.DOScale(Vector3.one, hintTransitionTime).OnComplete(() =>
        {
            hint2.GetComponent<RectTransform>().DOScale(1.5f, 1).SetLoops(loopCount, LoopType.Yoyo).OnComplete(() =>
            {
                if (!GameManager.Instance.playTutorial)
                {
                    hint2.transform.DOScale(Vector3.one * 20, hintTransitionTime).OnComplete(() => Destroy(hint2));
                }
            });
        });
    }
    private void HideHint()
    {
        GameManager.Instance.stopZoomAndPan = false;
        UIManager.Instance.showingHint = false;
        if (hint1 == null || hint2 == null)
        {
            return;
        }

        hint1.transform.DOScale(Vector3.one * 20, hintTransitionTime / 2).OnComplete(() => { Destroy(hint1); hintBackgroud.SetActive(false); });
        hint2.transform.DOScale(Vector3.one * 20, hintTransitionTime / 2).OnComplete(() => Destroy(hint2));

        GameManager.Instance.IncrementHintUsed();

        CrimsonGames.Analytics.CrimsonEventsLogger.LogEvent(CrimsonGames.Analytics.EPlayerEvent.hint_used);
    }
    public void RefillLives()
    {
        currentLives = maxLives = GameManager.Instance.maxLives;
        UIManager.Instance.UpdateLive(currentLives);
    }
}

using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GamePlayUI : UIPanel
{
    [Space]
    [Header("Game Play UI")]
    [Space]
    public GameObject touchTutPanel;
    public GameObject tapGesturePrefab;
    public GameObject touchGesture_1;
    public GameObject touchGesture_2;

    public GameObject zoomTutorialPanel;
    public GameObject zoomGesture_1;
    public GameObject zoomGesture_2;

    bool called = false;

    public GameObject hintTutorialPanel;

    public GameObject zoomOutBtn;

    private void OnEnable()
    {
        PanAndZoom.Zoomin += ZoomIn;
        PanAndZoom.ZoomOut += ZoomOut;
    }

    private void OnDestroy()
    {
        PanAndZoom.Zoomin -= ZoomIn;
        PanAndZoom.ZoomOut -= ZoomOut;
    }
    public void InitGameUI()
    {
        UIManager.Instance.UpdateHintText();
        GameManager.LevelStart?.Invoke();
        if (GameManager.Instance.playTutorial)
        {
            StartTutorial();
        }
        else
        {
            InitGamePlayUI();
        }
    }

    void InitGamePlayUI()
    {
        zoomOutBtn.SetActive(false);
        UIManager.Instance.hintButton.interactable = true;
        UIManager.Instance.backButton.interactable = true;
        GameManager.Instance.stopZoomAndPan = false;

        touchGesture_1.SetActive(false);
        touchGesture_2.SetActive(false);
        zoomTutorialPanel.SetActive(false);
        hintTutorialPanel.SetActive(false);
    }
    private void ZoomOut(float scale)
    {
        if (GameManager.Instance.playTutorial) return;

        if (scale <= 1.01)
            zoomOutBtn.SetActive(false);
    }
    private void ZoomIn(float scale)
    {
        if (GameManager.Instance.playTutorial) return;

        zoomOutBtn.SetActive(true);
    }

    #region Tutorial
    public static Vector2 GetCenteredHintPoint(List<MarkedDifferenceData> points, float radius)
    {
        Vector2 center = new Vector2(0.5f, 0.5f); // Center of the image in normalized coordinates

        // Filter points within the specified radius
        List<MarkedDifferenceData> filteredPoints = points
            .Where(point => Vector2.Distance(point.point, center) <= radius)
            .ToList();

        // Return a random point from the filtered list or Vector2.zero if none found
        if (filteredPoints.Count > 0)
        {
            return filteredPoints[UnityEngine.Random.Range(0, filteredPoints.Count)].point;
        }
        else
        {
            Debug.LogWarning("No hint points are within the specified radius. Falling back to random selection.");
            return points[UnityEngine.Random.Range(0, points.Count)].point;
        }

        Debug.LogWarning("No points found within the specified radius. Returning Vector2.zero.");
        return Vector2.zero;
    }
    void StartTutorial()
    {
        UIManager.Instance.backButton.interactable = false;
        UIManager.Instance.hintButton.interactable = false;
        UIDifferenceMarker.Instance.pinchZoomHandler.enabled = false;

        UIDifferenceMarker.mark += Marked;
        var markedPoints = LoadingJsonData.Instance.markerData.points;
        // Pick a random unmarked difference
        Vector2 hintPoint = markedPoints[0].point;// GetCenteredHintPoint(markedPoints,0.2f);// markedPoints[UnityEngine.Random.Range(0, markedPoints.Count)];
        GameManager.Instance.tutorialPoint = hintPoint;

        var rectTrans = UIDifferenceMarker.Instance.image1.rectTransform;
        // Convert normalized point to local position
        float adjustedWidth = rectTrans.rect.width;
        float adjustedHeight = rectTrans.rect.height;

        Vector2 localPoint = new Vector2(
            (hintPoint.x - 0.5f) * adjustedWidth,
            (hintPoint.y - 0.5f) * adjustedHeight
        );

        // Instantiate marker at the click position
        touchGesture_1.transform.SetParent(UIDifferenceMarker.Instance.image1.transform);
        touchGesture_2.transform.SetParent(UIDifferenceMarker.Instance.image2.transform);

        touchGesture_1.GetComponent<RectTransform>().anchoredPosition = localPoint;
        touchGesture_2.GetComponent<RectTransform>().anchoredPosition = localPoint;

        touchGesture_1.transform.SetParent(touchTutPanel.transform);
        touchGesture_2.transform.SetParent(touchTutPanel.transform);

        touchGesture_1.transform.SetAsFirstSibling();
        touchGesture_2.transform.SetAsFirstSibling();

        var _hand1 = Instantiate(tapGesturePrefab, touchGesture_1.transform.position, Quaternion.identity);
        var _hand2 = Instantiate(tapGesturePrefab, touchGesture_2.transform.position, Quaternion.identity);

        _hand1.transform.SetParent(touchTutPanel.transform);
        _hand2.transform.SetParent(touchTutPanel.transform);

        touchGesture_1.transform.localScale = touchGesture_2.transform.localScale = Vector3.one * 20;

        touchTutPanel.GetComponent<UIPanel>().ActivatePanel();
        touchGesture_1.transform.DOScale(Vector3.one, 0.7f).OnComplete(() =>
        {
            touchGesture_1.GetComponent<RectTransform>().DOScale(1.5f, 1.5f).SetLoops(-1, LoopType.Yoyo);
        });
        touchGesture_2.transform.DOScale(Vector3.one, 0.7f).OnComplete(() =>
        {
            touchGesture_2.GetComponent<RectTransform>().DOScale(1.5f, 1.5f).SetLoops(-1, LoopType.Yoyo);
        });

    }
    void Marked()
    {
        touchTutPanel.GetComponent<UIPanel>().DeactivatePanel();
        zoomTutorialPanel.GetComponent<UIPanel>().ActivatePanel();
        GameManager.Instance.state = GameManager.GameState.Pause;
        Vector2 hintPoint = LoadingJsonData.Instance.markerData.points[2].point;// GetCenteredHintPoint(UIDifferenceMarker.Instance.unmarkedHintPoints, 0.4f); //UIDifferenceMarker.Instance.unmarkedHintPoint[UnityEngine.Random.Range(0, UIDifferenceMarker.Instance.unmarkedHintPoint.Count)];

        Debug.Log("Selected point :" + hintPoint);
        GameManager.Instance.tutorialPoint = hintPoint;
        var rectTrans = UIDifferenceMarker.Instance.image1.rectTransform;
        // Convert normalized point to local position
        float adjustedWidth = rectTrans.rect.width;
        float adjustedHeight = rectTrans.rect.height;

        Vector2 localPoint = new Vector2(
            (hintPoint.x - 0.5f) * adjustedWidth,
            (hintPoint.y - 0.5f) * adjustedHeight
        );

        // Instantiate marker at the click position
        zoomGesture_1.transform.SetParent(UIDifferenceMarker.Instance.image1.transform);
        zoomGesture_2.transform.SetParent(UIDifferenceMarker.Instance.image2.transform);

        zoomGesture_1.GetComponent<RectTransform>().anchoredPosition = localPoint;
        zoomGesture_2.GetComponent<RectTransform>().anchoredPosition = localPoint;

        UIDifferenceMarker.mark -= Marked;
        PanAndZoom.Zoomin += ZoomedIn;

        UIDifferenceMarker.Instance.pinchZoomHandler.enabled = true;
    }
    void ZoomedIn(float amount)
    {
        if (called) return;
        GameManager.Instance.stopZoomAndPan = true;
        GameManager.Instance.state = GameManager.GameState.GamePlay;
        UIDifferenceMarker.Instance.pinchZoomHandler.ZoomToPoint(GameManager.Instance.tutorialPoint, 2.5f, 0.25f);
        called = true;
        DOVirtual.DelayedCall(0.25f, () =>
        {
            zoomGesture_1.SetActive(false);
            zoomGesture_2.SetActive(false);
            PanAndZoom.Zoomin -= ZoomedIn;
            var _hand1 = Instantiate(tapGesturePrefab, zoomGesture_1.transform.position, Quaternion.identity);
            var _hand2 = Instantiate(tapGesturePrefab, zoomGesture_2.transform.position, Quaternion.identity);

            _hand1.transform.SetParent(zoomTutorialPanel.transform);
            _hand2.transform.SetParent(zoomTutorialPanel.transform);
        });

        UIDifferenceMarker.mark += GetTapZoomedDiffrence;
    }
    private void GetTapZoomedDiffrence()
    {
        UIDifferenceMarker.mark -= GetTapZoomedDiffrence;
        DOVirtual.DelayedCall(0.15f, () => UIDifferenceMarker.Instance.pinchZoomHandler.CompleteZoomOut(0.25f));
        DOVirtual.DelayedCall(0.5f, () =>
        {
            GameManager.Instance.stopZoomAndPan = true;
            zoomTutorialPanel.GetComponent<UIPanel>().DeactivatePanel();
        });
        DOVirtual.DelayedCall(1.3f, () =>
        {
            hintTutorialPanel.GetComponent<UIPanel>().ActivatePanel();
            UIDifferenceMarker.mark += TapHintPoint;
        });
    }
    private void TapHintPoint()
    {
        UIDifferenceMarker.mark -= TapHintPoint;
        GameManager.Instance.playTutorial = false;
        GameManager.Instance.stopZoomAndPan = false;
        UIManager.Instance.backButton.interactable = true;
        UIManager.Instance.hintButton.interactable = true;
        PlayerPrefs.SetInt("tutorial", 1);
        Debug.Log("PLay");
    }
    #endregion

}

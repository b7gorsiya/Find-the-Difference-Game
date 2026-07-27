using UnityEngine;

public class MarkUI : MonoBehaviour
{
    public GameObject markedUI;

    private void OnEnable()
    {
        markedUI.SetActive(false);
    }
    public void MarkComplete()
    {
        markedUI.SetActive(true);
    }
}

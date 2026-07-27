using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ToolEpisode : MonoBehaviour
{
    public TextMeshProUGUI episodeName;
    LevelInfo levelInfo;
    Button btn;

    ChapterEditUI chapterEditUI;
    private void Awake()
    {
        btn = GetComponent<Button>();
    }
    public void Init(LevelInfo _info,ChapterEditUI _script)
    {
        levelInfo = _info;
        episodeName.text ="<b>("+_info.episodeId+") | </b>"+ levelInfo.imageId;
        chapterEditUI = _script;
        btn.onClick.AddListener(LoadEpisodeDetails);
    }

    public void LoadEpisodeDetails()
    {
        chapterEditUI.currentEpisode = levelInfo.episodeId;
        chapterEditUI.currentLevel = levelInfo.levelNo;

        chapterEditUI.episodeEditUI.SetActive(true);
        chapterEditUI.episodeTitle.text = levelInfo.imageId;
        chapterEditUI.episodeID.text = levelInfo.episodeId.ToString();
    }
}

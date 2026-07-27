using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using UnityEngine;

public class ClaimOutroAnimation : UIPanel
{
    Animator animator;
    public GameObject collectionButton;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ClaimButtonClick()
    {
        collectionButton.SetActive(true);
        AudioManager.Instance.PlaySFX(AudioEvent.ClaimCard);
        animator.Play("ClaimOutro");
    }

    public void ShowHomeScreen()
    {
       // animator.enabled = false;

        DOVirtual.DelayedCall(0.3f, () =>
        {
            UIManager.Instance.Home();
            collectionButton.SetActive(false);
            this.gameObject.SetActive(false);
        });
    }
}

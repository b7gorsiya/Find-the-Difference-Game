using Newtonsoft.Json;
using System;

namespace CrimsonGames.Analytics
{
    [Serializable]
    public enum EPlayerEvent
    {
        menu_loaded,
        puzzle_tapped,
        puzzle_loaded,
        puzzle_viewed,
        puzzle_complete,
        hint_used,
        suggestion_submitted,
        updatePopUp_viewed,
        updatePopUp_updateSelected,
        inGame_rated,
        backButton_tapped,
        life_lost,
        puzzle_lost,
        noAds_buttonTapped,
        IAP_buyTap,
        IAP_purchaseFailed,
        IAP_purchaseSuccess,
        cg_session_start
    }

    public static class PlayerEvents
    {
        public static FirebaseEventData GenerateEvent(EPlayerEvent playerEvent)
        {
            switch (playerEvent)
            {
                case EPlayerEvent.menu_loaded:
                    return GetMenuLoadedEvent();
                case EPlayerEvent.puzzle_tapped:
                    return GetPuzzleTappedEvent();
                case EPlayerEvent.puzzle_loaded:
                    return GetPuzzleLoadedEvent();
                case EPlayerEvent.puzzle_viewed:
                    return GetPuzzleViewedEvent();
                case EPlayerEvent.puzzle_complete:
                    return GetPuzzleCompletedEvent();
                case EPlayerEvent.hint_used:
                    return GetHintUsedEvent();
                case EPlayerEvent.suggestion_submitted:
                    return null;
                case EPlayerEvent.updatePopUp_viewed:
                    return GetUpdatePopupViewedEvent();
                case EPlayerEvent.updatePopUp_updateSelected:
                    return GetUpdatePopupSelectedEvent();
                case EPlayerEvent.inGame_rated:
                    return GetInGameRatedEvent();
                case EPlayerEvent.backButton_tapped:
                    return GetBackButtonTapped();
                case EPlayerEvent.life_lost:
                    return GetLifeLostEvent();
                case EPlayerEvent.puzzle_lost:
                    return GetPuzzleLostEvent();
                case EPlayerEvent.noAds_buttonTapped:
                    return GetNoAdsButtonTappedEvent();
                case EPlayerEvent.IAP_buyTap:
                    return GetIAPBuyTapEvent();
                case EPlayerEvent.IAP_purchaseSuccess: 
                    return GetIAPPurchaseSuccessEvent();
                case EPlayerEvent.IAP_purchaseFailed:
                    return GetIAPPurchaseFailedEvent();
                case EPlayerEvent.cg_session_start:
                    return GetSessionStartEvent();
                default:
                    return null;
            }
        }

        private static FirebaseEventData GetMenuLoadedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.menu_loaded.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "menu");
            return fbevent;
        }

        private static FirebaseEventData GetPuzzleTappedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.puzzle_tapped.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "puzzle");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetPuzzleLoadedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.puzzle_loaded.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "puzzle");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetPuzzleViewedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.puzzle_viewed.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "puzzle");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetPuzzleCompletedEvent()
        {
            Order order = GameManager.Instance.GetGameOrder();
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.puzzle_complete.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "puzzle");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            fbevent.AddParameter("puzzle_mode", order.puzzle_mode);
            fbevent.AddParameter("hints_used", order.hints_used);
            fbevent.AddParameter("time_elapsed_sec", order.time_elapsed_sec);
            fbevent.AddParameter("differences_remaining", order.differences_remaining);
            fbevent.AddParameter("lives_remaining", order.lives_remaining);

            return fbevent;
        }

        private static FirebaseEventData GetHintUsedEvent()
        {
            Order order = GameManager.Instance.GetGameOrder();
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.hint_used.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "hint");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            fbevent.AddParameter("puzzle_mode", order.puzzle_mode);
            fbevent.AddParameter("hints_used", order.hints_used);
            fbevent.AddParameter("time_elapsed_sec", order.time_elapsed_sec);
            fbevent.AddParameter("differences_remaining", order.differences_remaining);
            fbevent.AddParameter("lives_remaining", order.lives_remaining);

            return fbevent;
        }

        private static FirebaseEventData GetUpdatePopupViewedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.updatePopUp_viewed.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "updatePopUp");

            return fbevent;
        }

        private static FirebaseEventData GetUpdatePopupSelectedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.updatePopUp_updateSelected.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "updatePopUp");

            return fbevent;
        }

        private static FirebaseEventData GetInGameRatedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.inGame_rated.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "rateUs");

            return fbevent;
        }

        private static FirebaseEventData GetBackButtonTapped()
        {
            Order order = GameManager.Instance.GetGameOrder();
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.backButton_tapped.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "back");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            fbevent.AddParameter("puzzle_mode", order.puzzle_mode);
            fbevent.AddParameter("hints_used", order.hints_used);
            fbevent.AddParameter("time_elapsed_sec", order.time_elapsed_sec);
            fbevent.AddParameter("differences_remaining", order.differences_remaining);
            fbevent.AddParameter("lives_remaining", order.lives_remaining);

            return fbevent;
        }

        private static FirebaseEventData GetLifeLostEvent()
        {
            Order order = GameManager.Instance.GetGameOrder();
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.life_lost.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "life");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            fbevent.AddParameter("puzzle_mode", order.puzzle_mode);
            fbevent.AddParameter("hints_used", order.hints_used);
            fbevent.AddParameter("time_elapsed_sec", order.time_elapsed_sec);
            fbevent.AddParameter("differences_remaining", order.differences_remaining);
            fbevent.AddParameter("lives_remaining", order.lives_remaining);

            return fbevent;
        }

        private static FirebaseEventData GetPuzzleLostEvent()
        {
            Order order = GameManager.Instance.GetGameOrder();
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.puzzle_lost.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "puzzle");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            fbevent.AddParameter("puzzle_mode", order.puzzle_mode);
            fbevent.AddParameter("hints_used", order.hints_used);
            fbevent.AddParameter("time_elapsed_sec", order.time_elapsed_sec);
            fbevent.AddParameter("differences_remaining", order.differences_remaining);
            fbevent.AddParameter("lives_remaining", order.lives_remaining);
            return fbevent;
        }

        private static FirebaseEventData GetNoAdsButtonTappedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.noAds_buttonTapped.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "IAP");
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetIAPBuyTapEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.IAP_buyTap.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "IAP");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetIAPPurchaseSuccessEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.IAP_purchaseSuccess.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "IAP");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetIAPPurchaseFailedEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.IAP_purchaseFailed.ToString());
            fbevent.AddParameter(FirebaseParameters.FirebaseEventDataPhylumParam, "IAP");
            fbevent.AddParameter("image_id", GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].imageId);
            fbevent.AddParameter("puzzle_num", GameManager.Instance.currentLevel);
            return fbevent;
        }

        private static FirebaseEventData GetSessionStartEvent()
        {
            FirebaseEventData fbevent = new FirebaseEventData(EPlayerEvent.cg_session_start.ToString());
            return fbevent;
        }
    }
}


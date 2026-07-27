using CrimsonLibrary.SupportLibrary.Utils.Generics;
using Newtonsoft.Json;
using System;
using UnityEngine;
using System.Threading.Tasks;

namespace CrimsonGames.Analytics
{
    public class InstallDataManager : GenericManager<InstallDataManager>
    {
        public CGInstallDataMongo installDataMongo;
        public bool hasFetchedInstallData = false;

        void Awake()
        {
            base.Awake();

            PlayFabPlayerManager.Instance.onLoginSuccess += () => GetPlayerInstallData(null, null);

            PlayFabPlayerManager.Instance.onLoginFail += () =>
            {
                if (PlayerPrefs.HasKey("playerinstallreferrerdata"))
                {
                    string jsonString = PlayerPrefs.GetString("playerinstallreferrerdata");
                    installDataMongo = JsonConvert.DeserializeObject<CGInstallDataMongo>(jsonString);
                    hasFetchedInstallData = true;
                }

                //Can't do anything if player starts game without internet
            };
        }

        public async void GetPlayerInstallData(Action onSuccess, Action onFailure)
        {
            if (PlayerPrefs.HasKey("playerinstallreferrerdata"))
            {
                string jsonString = PlayerPrefs.GetString("playerinstallreferrerdata");
                installDataMongo = JsonConvert.DeserializeObject<CGInstallDataMongo>(jsonString);
                onSuccess.SafeInvoke();
                hasFetchedInstallData = true;
                return;
            }

            while (true)
            {
                try
                {
                    FetchInstallDataParams installDataParams = new FetchInstallDataParams
                    {
                        playerID = PlayFabPlayerManager.Instance.PlayFabId,
                        database = SettingsManager.Instance.MongoSettingsData.database,
                        collection = "mmp_install_master"
                    };

                    installDataMongo = await MongoDBAPIManager.Instance.GetInstallData(installDataParams);

                    if (installDataMongo == null)
                    {
                        Debug.LogError("[InstallDataManager][GetPlayerInstallData] Install data was null, retrying...");
                        throw new Exception("Install data was null.");
                    }

                    PlayerPrefs.SetString("playerinstallreferrerdata", JsonConvert.SerializeObject(installDataMongo));
                    onSuccess.SafeInvoke();
                    Debug.Log("[InstallDataManager][GetPlayerInstallData] Player install data fetched and saved.");
                    hasFetchedInstallData = true;
                    break; // Exit loop on success
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InstallDataManager][GetPlayerInstallData] Exception while fetching install data: {ex.Message}");
                    await Task.Delay(1000); // Retry after 1 second
                }
            }
        }
    }
}



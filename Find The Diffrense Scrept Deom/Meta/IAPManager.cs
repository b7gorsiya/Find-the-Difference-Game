using CrimsonGames.Analytics;
using CrimsonGames.CBN.Managers;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System;
using System.Globalization;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : GenericManager<IAPManager>, IDetailedStoreListener
{
    private IStoreController controller;
    private IExtensionProvider extensions;

    public string environment = "production";

    public static Action<Product> OnIAPPurchaseSuccess;
    public static Action<Product, string> OnIAPPurchaseFailed;

    public string iapSource = "";
    protected async void Awake()
    {
        base.Awake();

        try
        {
            var options = new InitializationOptions().SetEnvironmentName(environment);

            await UnityServices.InitializeAsync(options);
        }
        catch (Exception exception)
        {
            // An error occurred during services initialization.
            Debug.LogError(exception);
        }

        if (SettingsManager.Instance.useFakeStore)
        {
            StandardPurchasingModule.Instance().useFakeStoreAlways = true;
            StandardPurchasingModule.Instance().useFakeStoreUIMode = FakeStoreUIMode.Default;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("no_ads_package", ProductType.NonConsumable, new IDs
        {
            {"no_ads_package", GooglePlay.Name},
        });

        UnityPurchasing.Initialize(this, builder);

        if (!PlayerPrefs.HasKey("noads"))
        {
            if (!HasPurchasedNoAds())
            {
                PlayerPrefs.SetInt("noads", 0);
            }
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        this.controller = controller;
        this.extensions = extensions;

        foreach (var product in controller.products.all)
        {
            Debug.Log("Product title :: " + product.metadata.localizedTitle + ", Product description :: " + product.metadata.localizedDescription +
                ", Product price :: " + product.metadata.localizedPriceString);
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("IAP Init failed :: " + error.ToString());
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("IAP Init failed :: " + error.ToString() + ", message :: " + message);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.Log("Purchase failed for :: " + product.metadata.localizedTitle + ", reason :: " + failureDescription.reason);

        double pricevalue = 21.00;
        CrimsonEventsLogger.LogEvent(EPlayerEvent.IAP_purchaseFailed, family: pricevalue, genus: iapSource, species: product.definition.id);

        OnIAPPurchaseFailed.SafeInvoke(product, failureDescription.reason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log("Purchase failed for :: " + product.metadata.localizedTitle + ", reason :: " + failureReason.ToString());

        double pricevalue = 21.00;
        CrimsonEventsLogger.LogEvent(EPlayerEvent.IAP_purchaseFailed, family: pricevalue, genus: iapSource, species: product.definition.id);

        OnIAPPurchaseFailed.SafeInvoke(product, failureReason.ToString());
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        Debug.Log("Purchase successful for product :: " + purchaseEvent.purchasedProduct.metadata.localizedTitle);

        string price = "";
        if (purchaseEvent.purchasedProduct.definition.id == "no_ads_package")
        {
            PlayerPrefs.SetInt("noads", 1);
            price = "21.00";
        }

        //CrimsonEventsLogger.LogPlayerIAPFirstBuySuccessful(price, purchaseEvent.purchasedProduct.definition.id, iapSource);
        double pricevalue = double.Parse(price, CultureInfo.InvariantCulture);
        CrimsonEventsLogger.LogEvent(EPlayerEvent.IAP_purchaseSuccess, family: pricevalue, genus: iapSource, species: purchaseEvent.purchasedProduct.definition.id);

        OnIAPPurchaseSuccess.SafeInvoke(purchaseEvent.purchasedProduct);

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseClicked(string productId)
    {
        Debug.Log("[OnPurchaseClicked] Product id for purchase :: " + productId);
        if (controller != null)
        {
            controller.InitiatePurchase(productId);
        }

        if (productId == "no_ads_package")
        {
            LogPlayerIAPFirstBuyTap("21.00", productId);
        }
    }

    public void OnPurchaseComplete(Product product) //Duplicate but needed for IAP Listener
    {
        Debug.Log("[OnPurchaseComplete] Purchase successful for product :: " + product.metadata.localizedTitle);
        string price = "";
        if (product.definition.id == "no_ads_package")
        {
            PlayerPrefs.SetInt("noads", 1);
            price = "21.00";
        }

        //CrimsonEventsLogger.LogPlayerIAPFirstBuySuccessful(price, product.definition.id, iapSource);
        double pricevalue = double.Parse(price, CultureInfo.InvariantCulture);
        CrimsonEventsLogger.LogEvent(EPlayerEvent.IAP_purchaseSuccess, family: pricevalue, genus: iapSource, species: product.definition.id);

        OnIAPPurchaseSuccess.SafeInvoke(product);
    }

    public void SetSource(string source)
    {
        iapSource = source;
    }

    public bool HasPurchasedNoAds()
    {
        if (PlayerPrefs.GetInt("noads") == 1)
        {
            return true;
        }

        return false;
    }


    public void LogPlayerIAPFirstBuyTap(string price, string productId)
    {
        double pricevalue = double.Parse(price, CultureInfo.InvariantCulture);
        CrimsonEventsLogger.LogEvent(EPlayerEvent.IAP_buyTap, family: pricevalue, genus: iapSource, species: productId);
    }

}
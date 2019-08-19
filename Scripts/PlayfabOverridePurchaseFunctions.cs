using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
using UnityEngine.Purchasing;
#endif

public class PlayfabOverridePurchaseFunctions : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
        InGameProductData.OverrideBuyFunction = BuyFunction;
        InGameProductData.OverrideBuyFunction = BuyWithCurrencyIdFunction;
        MonetizationManager.OverrideSaveAdsReward = SaveAdsReward;
        MonetizationManager.OverrideProcessPurchase = ProcessPurchase;
#endif
    }

#if UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
    private static void BuyFunction(InGameProductData productData, System.Action<bool, string> callback)
    {

    }

    private static void BuyWithCurrencyIdFunction(InGameProductData productData, System.Action<bool, string> callback)
    {

    }

    private static void SaveAdsReward(AdsReward adsReward)
    {

    }

    private static PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        return PurchaseProcessingResult.Complete;
    }
#endif
}

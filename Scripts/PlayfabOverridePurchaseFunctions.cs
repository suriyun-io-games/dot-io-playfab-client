using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !NO_IAP && UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
using UnityEngine.Purchasing;
#endif
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

public class PlayfabOverridePurchaseFunctions : MonoBehaviour
{
    private void Awake()
    {
        InGameProductData.OverrideBuyFunction = BuyFunction;
        InGameProductData.OverrideBuyWithCurrencyIdFunction = BuyWithCurrencyIdFunction;
        MonetizationManager.OverrideSaveAdsReward = SaveAdsReward;
#if !NO_IAP && UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
        MonetizationManager.OverrideProcessPurchase = ProcessPurchase;
#endif
    }

    private static void BuyFunction(InGameProductData productData, System.Action<bool, string> callback)
    {
        if (!productData.CanBuy())
        {
            if (callback != null)
                callback.Invoke(false, "Cannot buy item.");
            return;
        }

        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "buyItem",
            FunctionParameter = new
            {
                itemId = productData.GetId(),
            }
        }, (result) =>
        {
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            string error = (string)jsonResult["Error"];
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log("[Playfab Monetization] Your purchase was successful");
                List<object> itemId = (List<object>)jsonResult["ItemId"];
                if (callback != null)
                    callback.Invoke(true, string.Empty);
            }
            else
            {
                Debug.LogError("[Playfab Monetization] Your purchase was unsuccessful: " + error);
                if (callback != null)
                    callback.Invoke(false, error);
            }
        }, (error) =>
        {
            Debug.LogError("[Playfab Monetization] " + error.ErrorMessage);
            if (callback != null)
                callback.Invoke(false, error.ErrorMessage);
        });
    }

    private static void BuyWithCurrencyIdFunction(InGameProductData productData, string currencyId, System.Action<bool, string> callback)
    {
        if (!productData.CanBuy(currencyId))
        {
            if (callback != null)
                callback.Invoke(false, "Cannot buy item.");
            return;
        }

        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "buyItemWithCurrencyId",
            FunctionParameter = new
            {
                itemId = productData.GetId(),
                currencyId
            }
        }, (result) =>
        {
            Debug.Log("[Playfab Monetization] Your purchase was successful");
            JsonObject jsonResult = (JsonObject)result.FunctionResult;
            string error = (string)jsonResult["Error"];
            if (string.IsNullOrEmpty(error))
            {
                List<object> itemId = (List<object>)jsonResult["ItemId"];
                if (callback != null)
                    callback.Invoke(true, string.Empty);
            }
            else
            {
                if (callback != null)
                    callback.Invoke(false, error);
            }
        }, (error) =>
        {
            Debug.LogError("[Playfab Monetization] " + error.ErrorMessage);
            if (callback != null)
                callback.Invoke(false, error.ErrorMessage);
        });
    }

    private static void SaveAdsReward(AdsReward adsReward)
    {
        // WIP, may have to export data for cloud scripts
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "saveAdsReward"
        }, (result) =>
        {
            Debug.Log("[Playfab Monetization] Your purchase was successful");
        }, (error) =>
        {
            Debug.LogError("[Playfab Monetization] " + error.ErrorMessage);
        });
    }

#if !NO_IAP && UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
    private static PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        // NOTE: this code does not account for purchases that were pending and are
        // delivered on application start.
        // Production code should account for such case:
        // More: https://docs.unity3d.com/ScriptReference/Purchasing.PurchaseProcessingResult.Pending.html

        if (!MonetizationManager.IsPurchasingInitialized())
        {
            return PurchaseProcessingResult.Complete;
        }

        // Test edge case where product is unknown
        if (e.purchasedProduct == null)
        {
            Debug.LogWarning("Attempted to process purchasewith unknown product. Ignoring");
            return PurchaseProcessingResult.Complete;
        }

        // Test edge case where purchase has no receipt
        if (string.IsNullOrEmpty(e.purchasedProduct.receipt))
        {
            Debug.LogWarning("Attempted to process purchase with no receipt: ignoring");
            return PurchaseProcessingResult.Complete;
        }

        Debug.Log("Processing transaction: " + e.purchasedProduct.transactionID);

        // Deserialize receipt
        var receipt = PurchaseReceipts.FromJson(e.purchasedProduct.receipt);

        if (receipt.Store.Equals("GooglePlay"))
        {
            var payload = GooglePayloadData.FromJson(receipt.Payload);
            PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
            {
                // Pass in currency code in ISO format
                CurrencyCode = e.purchasedProduct.metadata.isoCurrencyCode,
                // Convert and set Purchase price
                PurchasePrice = (uint)(e.purchasedProduct.metadata.localizedPrice * 100),
                // Pass in the receipt
                ReceiptJson = payload.json,
                // Pass in the signature
                Signature = payload.signature
            }, result => Debug.Log("Validation successful!"),
               error => Debug.LogError("Validation failed: " + error.GenerateErrorReport())
            );
        }
        else if (receipt.Store.Equals("AppleAppStore"))
        {
            PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
            {
                // Pass in currency code in ISO format
                CurrencyCode = e.purchasedProduct.metadata.isoCurrencyCode,
                // Convert and set Purchase price
                PurchasePrice = (int)(e.purchasedProduct.metadata.localizedPrice * 100),
                // Pass in payload
                ReceiptData = receipt.Payload
            }, result => Debug.Log("Validation successful!"),
               error => Debug.LogError("Validation failed: " + error.GenerateErrorReport())
            );
        }

        return PurchaseProcessingResult.Complete;
    }
#endif

    /// <summary>
    /// https://docs.unity3d.com/Manual/UnityIAPPurchaseReceipts.html
    /// </summary>
    [System.Serializable]
    public struct GooglePayloadData
    {
        public string signature;
        public string json;

        public static GooglePayloadData FromJson(string json)
        {
            return JsonUtility.FromJson<GooglePayloadData>(json);
        }
    }

    /// <summary>
    /// https://docs.unity3d.com/Manual/UnityIAPPurchaseReceipts.html
    /// </summary>
    [System.Serializable]
    public struct PurchaseReceipts
    {
        /// <summary>
        /// The name of the store in use, such as GooglePlay or AppleAppStore
        /// </summary>
        public string Store;
        /// <summary>
        /// This transaction’s unique identifier, provided by the store
        /// </summary>
        public string TransactionID;
        /// <summary>
        /// Varies by platform, details below.
        /// </summary>
        public string Payload;

        public static PurchaseReceipts FromJson(string json)
        {
            return JsonUtility.FromJson<PurchaseReceipts>(json);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PlayFab;
using PlayFab.ClientModels;

public class PlayfabSave : BaseMonetizationSave
{
    public float refreshDuration = 3f;
    [System.Serializable]
    public class ErrorEvent : UnityEvent<string> { }

    public UnityEvent onRefreshing;
    public UnityEvent onRefresh;
    public UnityEvent onRefreshFirstTime;
    public ErrorEvent onError;

    private static Dictionary<string, int> currencies;
    private static readonly PurchasedItems items = new PurchasedItems();
    private static float lastRefreshTime;
    private static bool isRefreshing;
    private static bool isRefreshFirstTime;

    private void Update()
    {
        if (!PlayfabAuthClient.IsLoggedIn)
            return;

        if (Time.unscaledTime - lastRefreshTime >= refreshDuration)
        {
            lastRefreshTime = Time.unscaledTime;
            GetInventory();
        }
    }

    public override bool AddCurrency(string name, int amount)
    {
        PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest()
        {
            VirtualCurrency = name,
            Amount = amount,
        }, (result) =>
        {
            lastRefreshTime = Time.unscaledTime;
        }, (error) =>
        {
            Debug.LogError("[Playfab Save] " + error.ErrorMessage);
            onError.Invoke(error.ErrorMessage);
        });
        return true;
    }

    public override void AddPurchasedItem(string itemName)
    {
        // Do nothing, this will manage at Playfab service side
    }

    public override int GetCurrency(string name)
    {
        if (currencies != null && currencies.ContainsKey(name))
            return currencies[name];
        return 0;
    }

    public override PurchasedItems GetPurchasedItems()
    {
        return items;
    }

    public void GetInventory()
    {
        GetInventoryInternal(this);
    }

    public static void GetInventoryInternal(PlayfabSave saveInstance)
    {
        if (isRefreshing)
            return;
        isRefreshing = true;
        if (saveInstance != null)
            saveInstance.onRefreshing.Invoke();
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), (result) =>
        {
            isRefreshing = false;
            currencies = result.VirtualCurrency;
            items.Clear();
            foreach (var item in result.Inventory)
            {
                items.Add(item.ItemId);
            }
            if (saveInstance != null)
                saveInstance.onRefresh.Invoke();
            if (!isRefreshFirstTime)
            {
                isRefreshFirstTime = true;
                saveInstance.IsPurchasedItemsLoaded = true;
                if (saveInstance != null)
                    saveInstance.onRefreshFirstTime.Invoke();
            }
        }, (error) =>
        {
            isRefreshing = false;
            Debug.LogError("[Playfab Save] " + error.ErrorMessage);
            if (saveInstance != null)
                saveInstance.onError.Invoke(error.ErrorMessage);
        });
    }

    public override void RemovePurchasedItem(string itemName)
    {
        // Do nothing, this will manage at Playfab service side
    }

    public override void SetCurrency(string name, int amount)
    {
        // Do nothing, this will manage at Playfab service side
    }

    public override void SetPurchasedItems(PurchasedItems purchasedItems)
    {
        // Do nothing, this will manage at Playfab service side
    }
}

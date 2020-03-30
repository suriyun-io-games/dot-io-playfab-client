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

    public UnityEvent onRefresh;
    public UnityEvent onRefreshFirstTime;
    public ErrorEvent onError;

    private Dictionary<string, int> currencies;
    private readonly PurchasedItems items = new PurchasedItems();
    private float lastRefreshTime;
    private bool isRefreshing;
    private bool isRefreshFirstTime;

    private void Update()
    {
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
        if (isRefreshing)
            return;
        isRefreshing = true;
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), (result) =>
        {
            isRefreshing = false;
            currencies = result.VirtualCurrency;
            items.Clear();
            foreach (var item in result.Inventory)
            {
                items.Add(item.ItemId);
            }
            onRefresh.Invoke();
            if (!isRefreshFirstTime)
            {
                isRefreshFirstTime = true;
                onRefreshFirstTime.Invoke();
            }
        }, (error) =>
        {
            isRefreshing = false;
            Debug.LogError("[Playfab Save] " + error.ErrorMessage);
            onError.Invoke(error.ErrorMessage);
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

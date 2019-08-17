using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayfabMonetizationExporter : MonoBehaviour
{
    public MonetizationManager monetizationManager;

#if UNITY_EDITOR
    [ContextMenu("Export Products")]
    public void ExportProducts()
    {

    }

    [ContextMenu("Export Currencies")]
    public void ExportCurrencies()
    {
        var json = "[]";
        var i = 0;
        foreach (var currency in monetizationManager.currencies)
        {
            var code = currency.id.Substring(0, 2);
            var name = currency.name;
            var startAmount = currency.startAmount;
            if (i == 0)
            {
                json = "[";
            }
            else
            {
                json += ",";
            }

            json += JsonUtility.ToJson(new PlayfabCurrency()
            {
                CurrencyCode = code,
                DisplayName = name,
                InitialDeposit = startAmount,
                RechargeRate = 0,
                RechargeMax = 0,
                CurrencyCodeFull = code + " (" + name +")",
                GameManagerClassMetadata = null,
            });

            if (i == monetizationManager.currencies.Count - 1)
            {
                json += "]";
            }

            i++;
        }
        var path = EditorUtility.SaveFilePanel("Export Currencies", Application.dataPath, "CURRENCIES", "json");
        if (path.Length > 0)
            File.WriteAllText(path, json);
    }
#endif

    [System.Serializable]
    public struct PlayfabCurrency
    {
        public string CurrencyCode;     // GE
        public string DisplayName;      // Gem
        public int InitialDeposit;      // 0
        public int RechargeRate;        // 0
        public int RechargeMax;         // 0
        public string CurrencyCodeFull; // GE (Gem)
        public string GameManagerClassMetadata;     // null
    }
}

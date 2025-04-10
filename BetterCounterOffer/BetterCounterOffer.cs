using System;
using UnityEngine;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Messaging;
using MelonLoader;
using HarmonyLib;
using System.Collections.Generic;


public class BetterCounterOffer : MelonMod {

    [HarmonyPatch(typeof(CounterofferInterface), "Open")]

    static class CounterOfferOpenPatch {
        public static Il2CppSystem.Collections.Generic.List<ProductDefinition> prevList = new Il2CppSystem.Collections.Generic.List<ProductDefinition>();


        public static bool Prefix(CounterofferInterface __instance,ref ProductDefinition product, ref int quantity, ref float price, ref MSGConversation _conversation, ref Action<ProductDefinition, int, float> _orderConfirmedCallback) {
            __instance.IsOpen = true;
            __instance.product = product;
            __instance.quantity = Mathf.Clamp(quantity, 1, __instance.MaxQuantity);
            __instance.price = price;

            Il2CppSystem.Collections.Generic.List<ProductDefinition> currProd = ProductManager.ListedProducts;
            List<ProductDefinition> added = GetDifference(currProd,prevList);
            List<ProductDefinition> removed = GetDifference(prevList, currProd);

            foreach (ProductDefinition key in added) {
                if (!__instance.productEntries.ContainsKey(key)) {
                    __instance.CreateProductEntry(key);
                } else if (__instance.productEntries.ContainsKey(key)) {
                    RectTransform ob = __instance.productEntries[key];
                    ob.gameObject.SetActive(true);
                }
            }

            foreach (ProductDefinition key in removed) {
                if (__instance.productEntries.ContainsKey(key)) {
                    RectTransform ob = __instance.productEntries[key];
                    ob.gameObject.SetActive(false);
                    
                }
            }

            prevList.Clear();
            foreach (ProductDefinition key in currProd) {
                prevList.Add(key);
            }

            __instance.conversation = _conversation;
            MSGConversation msgconversation = __instance.conversation;
            Il2CppSystem.Action msgRenderedAciton = msgconversation.onMessageRendered;
            Action temp = delegate () { __instance.Close(); };
            msgconversation.onMessageRendered += (Il2CppSystem.Action)temp;

            __instance.orderConfirmedCallback = _orderConfirmedCallback;
            __instance.EntryContainer.gameObject.SetActive(false);
            __instance.Container.gameObject.SetActive(true);
            __instance.SetProduct(product);
            __instance.PriceInput.text = price.ToString();
            return false;
        }
    }

    public static List<ProductDefinition> GetDifference(Il2CppSystem.Collections.Generic.List<ProductDefinition> list1, Il2CppSystem.Collections.Generic.List<ProductDefinition> list2) {
        List<ProductDefinition> onlyInList1 = new List<ProductDefinition>();
        HashSet<String> existsInList2 = new HashSet<String>();

        foreach(ProductDefinition p in list2) {
            existsInList2.Add(p.Name);
        }

        foreach (ProductDefinition p in list1) {
            if (!existsInList2.Contains(p.Name)) {
                onlyInList1.Add(p);
            }
        }

        return onlyInList1;
    }

    public static void PrintList(List<ProductDefinition> l) {
        foreach (ProductDefinition p in l) {
            MelonLogger.Msg("- " + p.Name);
        }
    }

    public static void PrintList(Il2CppSystem.Collections.Generic.List<ProductDefinition> l) {
        foreach (ProductDefinition p in l) {
            MelonLogger.Msg("- " + p.Name);
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), "UpdateFairPrice")]
    static class CounterOfferUpdateFairPricePatch {

        public static bool Prefix(CounterofferInterface __instance) {
            ProductDefinition temp = __instance.product;
            float priceChange = (__instance.quantity * temp.Price) - __instance.price;
            __instance.ChangePrice(priceChange);
            return true;
        }
    }

    public override void OnInitializeMelon() {
        MelonLogger.Msg("Initializing Better Counter Offer...Go Make that Money");
        MelonLogger.Msg("If you find any bugs message me on nexus - OverweightUnicorn");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
        if(sceneName == "Main") {
            CounterOfferOpenPatch.prevList.Clear();
        }
    }

}

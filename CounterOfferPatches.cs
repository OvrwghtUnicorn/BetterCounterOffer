using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if IL2CPP
using Il2CppScheduleOne;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using GenericCol = Il2CppSystem.Collections.Generic;
#elif MONO
using ScheduleOne;
using ScheduleOne.Product;
using ScheduleOne.UI;
using ScheduleOne.UI.Phone;
using GenericCol = System.Collections.Generic;
#endif

namespace BetterCounterOffer
{

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Open))]
    static class CounterOfferInterfaceOpenPatch
    {

        public static void Postfix(CounterofferInterface __instance)
        {
            if (__instance != null)
            {
                CounterOfferUI.offerInterface = __instance;
                CounterOfferUI.OnPopupOpen(__instance);
            }
        }
    }

    // CounterofferInterface no longer exposes a `price` field or `ChangePrice` method.
    // All price changes now flow through the PriceSelector's AmountSelector.SetAmount,
    // so that is the single choke point for success-rate / fair-price recalculation.
    [HarmonyPatch(typeof(AmountSelector), nameof(AmountSelector.SetAmount))]
    static class CounterOfferPriceSelectorSetAmountPatch
    {

        public static void Postfix(AmountSelector __instance)
        {
            CounterofferInterface offer = CounterOfferUI.offerInterface;
            if (offer == null || offer.PriceSelector != __instance)
            {
                return;
            }

            if (!CounterOfferConfig.DisableSuccessRate.Value)
            {
                CounterOfferUI.UpdateSuccessRate(offer);
            }

            if (CounterOfferConfig.EnablePricePerUnit.Value)
            {
                CounterOfferUI.SetFairPriceText(__instance.SelectedAmount / offer.quantity);
            }
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.ChangeQuantity), new Type[] { typeof(float) })]
    static class CounterOfferInterfaceChangeQuantityPatch
    {

        public static bool Prefix(CounterofferInterface __instance, ref float change)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                int current = __instance.quantity;
                int target = current;

                if (change > 0)
                {
                    // Increase to the next multiple of 5
                    target = ((current / 5) + 1) * 5;
                }
                else if (change < 0)
                {
                    if (current <= 5)
                    {
                        target = 1;
                    }
                    else
                    {
                        // Decrease to the previous multiple of 5
                        target = ((current - 1) / 5) * 5;
                    }
                }
                else
                {
                    return true;
                }

                // Clamp to 1 - 9999 range
                target = Math.Max(1, Math.Min(9999, target));
                change = target - current;
            }

            return true;
        }

        public static void Postfix(CounterofferInterface __instance)
        {
            ProductDefinition temp = __instance.selectedProduct;
            float newTotalPrice = __instance.quantity * temp.Price;
            __instance.PriceSelector.SetAmount(newTotalPrice);
        }
    }


    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.SetProduct))]
    static class CounterOfferInterfaceSetProductPatch
    {

        public static void Postfix(CounterofferInterface __instance)
        {
            float newTotalPrice = __instance.quantity * __instance.selectedProduct.Price;
            __instance.PriceSelector.SetAmount(newTotalPrice);
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.Open))]
    static class CounterOfferProductSelectorOpenPatch
    {
        public static void Postfix(CounterOfferProductSelector __instance)
        {
            if (CounterOfferUI.selectorInterface == null)
            {
                CounterOfferUI.selectorInterface = __instance;
            }
        }
    }

    // The scene has a second, never-initialized CounterOfferProductSelector that the
    // Next/Prev buttons are actually wired to (pageCount/results stay at their default
    // 0/empty since Open()/RebuildResultsList() never run on it). Redirect any call that
    // lands on the wrong instance to the real one we're driving via search/tabs.
    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.ChangePage))]
    static class CounterOfferProductSelectorChangePageRedirectPatch
    {
        public static bool Prefix(CounterOfferProductSelector __instance, int change)
        {
            CounterOfferProductSelector real = CounterOfferUI.selectorInterface;
            if (real != null && real != __instance)
            {
                real.ChangePage(change);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.GetMatchingProducts))]
    static class CounterOfferProductSelectorGetMatchingProductsPatch
    {

        public static void Postfix(CounterOfferProductSelector __instance, ref GenericCol.List<ProductDefinition> __result, string searchTerm)
        {

            string term = searchTerm.ToLower();

            HashSet<EDrugType> drugTypes = new HashSet<EDrugType>();
            GenericCol.List<ProductDefinition> lp;
            if (CounterOfferUI.currTab == "Listed")
            {
                lp = ProductManager.ListedProducts;
            }
            else if (CounterOfferUI.currTab == "Favorites")
            {
                lp = ProductManager.FavouritedProducts;
            }
            else
            {
                lp = ProductManager.DiscoveredProducts;
            }
            GenericCol.List<ProductDefinition> newList = new GenericCol.List<ProductDefinition>();

            // Match against every drug type's own name (e.g. "cocaine"), plus the
            // base game's informal aliases, so tab-filtered search stays on par
            // with the base game's own GetMatchingProducts matching.
            foreach (EDrugType drugType in Enum.GetValues(typeof(EDrugType)))
            {
                if (term.Contains(drugType.ToString().ToLower())) { drugTypes.Add(drugType); }
            }
            if (term.Contains("weed")) { drugTypes.Add(EDrugType.Marijuana); }
            if (term.Contains("coke")) { drugTypes.Add(EDrugType.Cocaine); }
            if (term.Contains("meth")) { drugTypes.Add(EDrugType.Methamphetamine); }
            if (term.Contains("shroom")) { drugTypes.Add(EDrugType.Shrooms); }

            foreach (ProductDefinition p in lp)
            {
                if (drugTypes.Contains(p.DrugType) || p.Name.ToLower().Contains(term))
                {
                    newList.Add(p);
                }
            }

            __result = newList;
        }
    }

    // Base game hardcodes 25 entries/page in both RebuildResultsList's pageCount
    // math and SetPage's slicing, which overflows the grid by one row. SetPage is
    // fully reimplemented here (Prefix + skip original) around a smaller PageSize;
    // RebuildResultsList just needs its pageCount corrected afterward.
    [HarmonyPatch(typeof(CounterOfferProductSelector), "SetPage")]
    static class CounterOfferProductSelectorSetPagePatch
    {
        public const int PageSize = 24;

        public static bool Prefix(CounterOfferProductSelector __instance, int page)
        {
            __instance.pageIndex = Mathf.Clamp(page, 0, Mathf.Max(__instance.pageCount - 1, 0));
            int start = __instance.pageIndex * PageSize;
            int end = Mathf.Min(start + PageSize, __instance.results.Count);
            GenericCol.List<ProductDefinition> range = __instance.results.GetRange(start, end - start);

            foreach (var entry in __instance.productEntriesDict)
            {
                RectTransform rectTransform = entry.Value;
                if (range.Contains(entry.Key))
                {
                    rectTransform.gameObject.SetActive(true);
                    __instance.uiWindowPanel.AddSelectable(rectTransform.GetComponent<UISelectable>());
                }
                else
                {
                    rectTransform.gameObject.SetActive(false);
                    __instance.uiWindowPanel.RemoveSelectable(rectTransform.GetComponent<UISelectable>(), true);
                }
            }

            for (int j = 0; j < range.Count; j++)
            {
                __instance.productEntriesDict[range[j]].SetSiblingIndex(j);
            }

            __instance.PageLabel.text = $"{__instance.pageIndex + 1} / {__instance.pageCount}";
            return false;
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), "RebuildResultsList")]
    static class CounterOfferProductSelectorRebuildResultsListPatch
    {
        public static void Postfix(CounterOfferProductSelector __instance)
        {
            __instance.pageCount = Mathf.CeilToInt((float)__instance.results.Count / CounterOfferProductSelectorSetPagePatch.PageSize);
            __instance.SetPage(__instance.pageIndex);
        }
    }

}

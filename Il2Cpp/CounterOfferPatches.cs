using HarmonyLib;
using MelonLoader;
using UnityEngine;
#if IL2CPP
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using GenericCol = Il2CppSystem.Collections.Generic;
#elif MONO
using ScheduleOne.Product;
using ScheduleOne.UI.Phone;
using GenericCol = System.Collections.Generic;
#endif

namespace BetterCounterOffer {

    //[HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Awake))]
    //static class CounterOfferAwakePatch {
    //    public static bool Prefix(CounterofferInterface __instance) {
    //        MelonLogger.Msg("Waking Up, Lets Modify this UI and make it Better");
    //        CounterOfferUI.InitOnWake();
    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Open))]
    static class CounterOfferInterfaceOpenPatch {

        public static void Postfix(CounterofferInterface __instance) {
            if (__instance != null) {
                CounterOfferUI.OnPopupOpen(__instance);
            }
        }
    }


    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.ChangePrice))]
    static class CounterOfferInterfaceChangePricePatch {

        public static void Postfix(CounterofferInterface __instance) {
            if (!CounterOfferConfig.disableSuccessRate) {
                CounterOfferUI.UpdateSuccessRate(__instance);
            }

            if (CounterOfferConfig.enablePricePerUnit) {
                CounterOfferUI.SetFairPriceText(__instance.price / __instance.quantity);
            }
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.ChangeQuantity))]
    static class CounterOfferInterfaceChangeQuantityPatch {

        public static bool Prefix(CounterofferInterface __instance, ref int change) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                int current = __instance.quantity;
                int target = current;

                if (change > 0) {
                    // Increase to the next multiple of 5
                    target = ((current / 5) + 1) * 5;
                } else if (change < 0) {
                    if (current <= 5) {
                        target = 1;
                    } else {
                        // Decrease to the previous multiple of 5
                        target = ((current - 1) / 5) * 5;
                    }
                } else {
                    return true;
                }

                // Clamp to 1 - 9999 range
                target = Math.Max(1, Math.Min(9999, target));
                change = target - current;
            }

            return true;
        }

        public static void Postfix(CounterofferInterface __instance) {
            ProductDefinition temp = __instance.selectedProduct;
            float priceChange = __instance.quantity * temp.Price - __instance.price;
            __instance.ChangePrice(priceChange);
            if (!CounterOfferConfig.disableSuccessRate) {
                CounterOfferUI.UpdateSuccessRate(__instance);
            }
        }
    }


    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.SetProduct))]
    static class CounterOfferInterfaceSetProductPatch {

        public static void Postfix(CounterofferInterface __instance) {
            float priceChange = (__instance.quantity * __instance.selectedProduct.Price) - __instance.price;
            __instance.ChangePrice(priceChange);
            if (!CounterOfferConfig.disableSuccessRate) {
                CounterOfferUI.UpdateSuccessRate(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.UpdateFairPrice))]
    static class CounterofferInterface_UpdateFairPrice_Patch {
        public static void Postfix(CounterofferInterface __instance) {
            if (CounterOfferConfig.enablePricePerUnit) {
                CounterOfferUI.SetFairPriceText(__instance.price);
            }
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.Open))]
    static class CounterOfferProductSelectorOpenPatch {
        public static void Postfix(CounterOfferProductSelector __instance) {
            if (CounterOfferUI.selectorInterface == null) {
                CounterOfferUI.selectorInterface = __instance;
            }
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.GetMatchingProducts))]
    static class CounterOfferProductSelectorGetMatchingProductsPatch {

        public static void Postfix(CounterofferInterface __instance, ref GenericCol.List<ProductDefinition> __result, ref string searchTerm) {

            HashSet<EDrugType> drugTypes = new HashSet<EDrugType>();
            GenericCol.List<ProductDefinition> lp;
            if(CounterOfferUI.currTab == "Listed") {
                lp = ProductManager.ListedProducts;
            } else if(CounterOfferUI.currTab == "Favorites") {
                lp = ProductManager.FavouritedProducts;
            } else {
                lp = ProductManager.DiscoveredProducts;
            }
                GenericCol.List<ProductDefinition> newList = new GenericCol.List<ProductDefinition>();
            if (searchTerm.ToLower().Contains("weed")) { drugTypes.Add(EDrugType.Marijuana); }

            if (searchTerm.ToLower().Contains("coke")) { drugTypes.Add(EDrugType.Cocaine); }

            if (searchTerm.ToLower().Contains("meth")) { drugTypes.Add(EDrugType.Methamphetamine); }

            foreach (ProductDefinition p in lp) {
                if (drugTypes.Contains(p.DrugType) || p.Name.ToLower().Contains(searchTerm)) {
                    newList.Add(p);
                }
            }

            __result = newList;
        }
    }

}

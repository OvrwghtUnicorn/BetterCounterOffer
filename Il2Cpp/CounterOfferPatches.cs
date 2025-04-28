using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne.Economy;

namespace BetterCounterOffer {

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Awake))]
    static class CounterOfferAwakePatch {
        public static bool Prefix(CounterofferInterface __instance) {
            MelonLogger.Msg("Waking Up, Lets Modify this UI and make it Better");
            CounterOfferUI.InitOnWake();
            return true;
        }
    }

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
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.ChangeQuantity))]
    static class CounterOfferInterfaceChangeQuantityPatch {

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

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.Open))]
    static class CounterOfferProductSelectorOpenPatch {
        public static void Postfix(CounterOfferProductSelector __instance) {
            if (CounterOfferUI.selectorInterface == null) {
                MelonLogger.Msg(System.ConsoleColor.Magenta,"Attempting to caputre ProductSelector interface");
                CounterOfferUI.selectorInterface = __instance;
            }
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.GetMatchingProducts))]
    static class CounterOfferProductSelectorGetMatchingProductsPatch {

        public static void Postfix(CounterofferInterface __instance, ref Il2Generic.List<ProductDefinition> __result, ref string searchTerm) {

            HashSet<EDrugType> drugTypes = new HashSet<EDrugType>();
            Il2Generic.List<ProductDefinition> lp;
            if(CounterOfferUI.currTab == "Listed") {
                lp = ProductManager.ListedProducts;
            } else if(CounterOfferUI.currTab == "Favorites") {
                lp = ProductManager.FavouritedProducts;
            } else {
                lp = ProductManager.DiscoveredProducts;
            }
                Il2Generic.List<ProductDefinition> newList = new Il2Generic.List<ProductDefinition>();
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

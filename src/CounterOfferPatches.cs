using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Messaging;

namespace BetterCounterOffer {

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Awake))]
    static class CounterOfferAwakePatch {
        public static bool Prefix(CounterofferInterface __instance) {
            MelonLogger.Msg("Counteroffer Waking Up Trying to create field");
            CounterOfferUI.attemptToCreateField();
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
            CounterOfferUI.UpdateTextFields(__instance);
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.ChangeQuantity))]
    static class CounterOfferInterfaceChangeQuantityPatch {

        public static void Postfix(CounterofferInterface __instance) {
            ProductDefinition temp = __instance.selectedProduct;
            float priceChange = __instance.quantity * temp.Price - __instance.price;
            __instance.ChangePrice(priceChange);
            CounterOfferUI.UpdateTextFields(__instance);
        }
    }


    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.SetProduct))]
    static class CounterOfferInterfaceSetProductPatch {

        public static bool Prefix(CounterofferInterface __instance, ref ProductDefinition newProduct) {
            float priceChange = __instance.quantity * newProduct.Price - __instance.price;
            __instance.ChangePrice(priceChange);
            CounterOfferUI.UpdateTextFields(__instance);
            return true;
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.Open))]
    static class CounterOfferProductSelectorOpenPatch {
        public static void Postfix(CounterOfferProductSelector __instance) {
            MelonLogger.Msg(System.ConsoleColor.Green, "Getting the selector interface");
            if (CounterOfferUI.selectorInterface != null) {
                MelonLogger.Msg("Is Alive! has been captured by my hacking");
            } else {
                CounterOfferUI.selectorInterface = __instance;
                MelonLogger.Msg($"{__instance.gameObject.name} has been captured by my hacking");
            }
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.GetMatchingProducts))]
    static class CounterOfferProductSelectorGetMatchingProductsPatch {

        public static void Postfix(CounterofferInterface __instance, ref Il2Generic.List<ProductDefinition> __result, ref string searchTerm) {

            HashSet<EDrugType> drugTypes = new HashSet<EDrugType>();
            Il2Generic.List<ProductDefinition> lp = CounterOfferUI.displayAll ? ProductManager.DiscoveredProducts : ProductManager.ListedProducts;
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

using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2Generic = Il2CppSystem.Collections.Generic;

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

        public static bool Prefix(CounterofferInterface __instance) {
            MelonLogger.Msg("Yeet");
            return true;
        }
    }

    [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.UpdateFairPrice))]
    static class CounterOfferInterfaceUpdateFairPricePatch {

        public static bool Prefix(CounterofferInterface __instance) {
            ProductDefinition temp = __instance.selectedProduct;
            float priceChange = __instance.quantity * temp.Price - __instance.price;
            __instance.ChangePrice(priceChange);
            return true;
        }
    }

    [HarmonyPatch(typeof(CounterOfferProductSelector), nameof(CounterOfferProductSelector.GetMatchingProducts))]
    static class CounterOfferProductSelectorGetMatchingProductsPatch {

        public static void Postfix(CounterofferInterface __instance, ref Il2Generic.List<ProductDefinition> __result, ref string searchTerm) {

            HashSet<EDrugType> drugTypes = new HashSet<EDrugType>();
            Il2Generic.List<ProductDefinition> lp = ProductManager.ListedProducts;
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

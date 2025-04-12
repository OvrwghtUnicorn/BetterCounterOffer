using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Product;
using MelonLoader;
using HarmonyLib;
using System.Collections.Generic;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne.Messaging;


public class BetterCounterOffer : MelonMod {

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

    [HarmonyPatch(typeof(CounterofferInterface), "UpdateFairPrice")]
    static class CounterOfferUpdateFairPricePatch {

        public static bool Prefix(CounterofferInterface __instance) {
            ProductDefinition temp = __instance.selectedProduct;
            float priceChange = (__instance.quantity * temp.Price) - __instance.price;
            __instance.ChangePrice(priceChange);
            return true;
        }
    }

    public override void OnInitializeMelon() {
        MelonLogger.Msg("Initializing Better Counter Offer...Go Make that Money");
        MelonLogger.Msg("If you find any bugs message me on nexus - OverweightUnicorn");
    }

}

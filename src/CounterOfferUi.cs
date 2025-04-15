using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppSystem.Collections.Generic;

[assembly: MelonInfo(typeof(BetterCounterOffer.CounterOfferUi), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterCounterOffer
{
    public static class BuildInfo
    {
        public const string Name = "Better Counter Offer UI";
        public const string Description = "A mod that improves the Counter Offer Interface";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "1.1.0";
        public const string DownloadLink = "";
    }

    public class CounterOfferUi : MelonMod {

        static GameObject PlayerRef = null;
        static GameObject CounterOfferPopupRef = null;
        static GameObject productSelectorRef = null;


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

        [HarmonyPatch(typeof(CounterofferInterface), nameof(CounterofferInterface.Awake))]
        static class CounterOfferAwakePatch {

            public static bool Prefix(CounterofferInterface __instance) {
                MelonLogger.Msg("Counteroffer Waking Up Trying to create field");
                attemptToCreateField();
                return true;
            }
        }

        [HarmonyPatch(typeof(CounterofferInterface), "UpdateFairPrice")]
        static class CounterOfferUpdateFairPricePatch {

            public static bool Prefix(CounterofferInterface __instance) {
                ProductDefinition temp = __instance.selectedProduct;
                float priceChange = __instance.quantity * temp.Price - __instance.price;
                __instance.ChangePrice(priceChange);
                return true;
            }
        }

        public static (float maxSpend, float dailyAverage) CalculateSpendingLimits(Customer customer) {
            CustomerData customerData = customer.CustomerData;
            float adjustedWeeklySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            var orderDays = customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f);
            float dailyAverage = adjustedWeeklySpend / orderDays.Count;
            float maxSpend = dailyAverage * 3f;
            return (maxSpend, dailyAverage);
        }

        public static float CalculateSuccessProbability(Customer customer, ProductDefinition product, int quantity, float price) {
            CustomerData customerData = customer.CustomerData;

            float valueProposition = Customer.GetValueProposition(Registry.GetItem<ProductDefinition>(customer.OfferedContractInfo.Products.entries[0].ProductID),
                customer.OfferedContractInfo.Payment / customer.OfferedContractInfo.Products.entries[0].Quantity);
            float productEnjoyment = customer.GetProductEnjoyment(product, customerData.Standards.GetCorrespondingQuality());
            float enjoymentNormalized = Mathf.InverseLerp(-1f, 1f, productEnjoyment);
            float newValueProposition = Customer.GetValueProposition(product, price / quantity);
            float quantityRatio = Mathf.Pow(quantity / (float)customer.OfferedContractInfo.Products.entries[0].Quantity, 0.6f);
            float quantityMultiplier = Mathf.Lerp(0f, 2f, quantityRatio * 0.5f);
            float penaltyMultiplier = Mathf.Lerp(1f, 0f, Mathf.Abs(quantityMultiplier - 1f));

            if (newValueProposition * penaltyMultiplier > valueProposition) {
                return 1f;
            }
            if (newValueProposition < 0.12f) {
                return 0f;
            }

            float customerWeightedValue = productEnjoyment * valueProposition;
            float proposedWeightedValue = enjoymentNormalized * penaltyMultiplier * newValueProposition;

            if (proposedWeightedValue > customerWeightedValue) {
                return 1f;
            }

            float valueDifference = customerWeightedValue - proposedWeightedValue;
            float threshold = Mathf.Lerp(0f, 1f, valueDifference / 0.2f);
            float bonus = Mathf.Lerp(0f, 0.2f, Mathf.Max(customer.CurrentAddiction, customer.NPC.RelationData.NormalizedRelationDelta));

            float thresholdMinusBonus = threshold - bonus;
            return Mathf.Clamp01((0.9f - thresholdMinusBonus) / 0.9f);
        }

        public static void attemptToCreateField() {
            GameObject playerObject = GameObject.Find("Player_Local");
            if (playerObject != null) {
                MelonLogger.Msg("Player Object Found");
                PlayerRef = playerObject;
                Transform transform = PlayerRef.transform.Find("CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/AppsCanvas/Messages/Container/CounterofferInterface/Shade/Content");
                GameObject popupContent = transform != null ? transform.gameObject : null;
                if (popupContent != null) {
                    MelonLogger.Msg("CounterOffer container Found");
                    CounterOfferPopupRef = popupContent;

                    RectTransform CoPopupRect = transform.GetComponent<RectTransform>();
                    CoPopupRect.sizeDelta = new Vector2(-160f, -90f);

                    Rect parentRect = CoPopupRect.rect;
                    float width = parentRect.width;
                    float height = parentRect.height;

                    MelonLogger.Msg("Attempting to shift components");
                    shiftOfferElements(transform);

                    MelonLogger.Msg("Attempting to make searchbar");
                    createLabels(transform);

                    MelonLogger.Msg("Updating Selector Interface");
                    updateSelectorUi(transform);

                    Transform newComp = transform.Find("OfferInformation/SuccessRate");
                    GameObject ssRate = transform != null ? transform.gameObject : null;
                    if (ssRate != null) {
                        MelonLogger.Msg("Field Created Succesfully");
                    } else {
                        MelonLogger.Msg("Field Not Created Succesfully. FUUCKK!!!");
                    }
                }
            }
        }

        private static void adjustUiElements(Transform parent, string searchStr, float x, float y) {
            Transform fpTransform = parent.Find(searchStr);
            if (fpTransform != null) {
                MelonLogger.Msg(System.ConsoleColor.DarkGreen, $"{searchStr} Adjusted");
                RectTransform fpRect = fpTransform.GetComponent<RectTransform>();
                fpRect.anchoredPosition = new Vector2(x, y);
            } else {
                MelonLogger.Msg(System.ConsoleColor.Red,$"{searchStr} Couldn't be Found");
            }
        }

        private static void shiftOfferElements(Transform parent) {
            adjustUiElements(parent, "Fair price",0f,-450f);
            adjustUiElements(parent, "Price", 0f, -390f);
            adjustUiElements(parent, "Subtitle (1)", 0f, -335f);
            adjustUiElements(parent, "Product", 0f, 10f);
            adjustUiElements(parent, "Add", 210f, 10f);
            adjustUiElements(parent, "Remove", -210f, 10f);
            adjustUiElements(parent, "Subtitle", 0f, -220f);
            adjustUiElements(parent, "Selection", 0f, -250f);
        }

        private static void updateSelectorUi(Transform parent) {
            Transform selectorTrans = parent.Find("Selection");
            if (selectorTrans != null) {
                MelonLogger.Msg(System.ConsoleColor.DarkGreen, $"Selection Adjusted");
                RectTransform selectorRect = selectorTrans.GetComponent<RectTransform>();
                selectorRect.anchoredPosition = new Vector2(0f, -250f);

                MelonLogger.Msg(System.ConsoleColor.Cyan, "Attempting to Change SearchInput width");
                Transform searchTrans = selectorTrans.Find("SearchInput");
                GameObject SearchInput = searchTrans != null ? searchTrans.gameObject : null;
                if (SearchInput != null) { 
                    RectTransform searchRect = SearchInput.GetComponent<RectTransform>();
                    searchRect.SetWidth(200f);
                }

                MelonLogger.Msg(System.ConsoleColor.Cyan, "Attempting to Add Filter Button to parent");
                GameObject filterBtn = new GameObject("FilterButton");
                filterBtn.transform.SetParent(selectorTrans, false);
                var buttonText = filterBtn.AddComponent<Text>();
                buttonText.text = "Filter";
                buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonText.fontSize = 30;
                buttonText.color = Color.red;
                buttonText.alignment = TextAnchor.MiddleCenter;
                var buttonRect = filterBtn.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(40, 40);
                buttonRect.anchorMin = new Vector2(1, 1);
                buttonRect.anchorMax = new Vector2(1, 1);
                buttonRect.anchoredPosition = new Vector2(-15, -30);

            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, "Selection Couldn't be Found");
            }
        }

        private static void createLabels(Transform parent) {
            Text dupMe = null;
            Transform titleTransform = parent.Find("Title");
            if (titleTransform != null) {
                dupMe = titleTransform.GetComponent<Text>();
            }

            GameObject inputGO = new GameObject("OfferInformation");
            inputGO.transform.SetParent(parent, false);
            inputGO.transform.localPosition = new Vector3(181.5444f, 1.175f, -9.2547f);
            inputGO.AddComponent<CanvasRenderer>();

            var rect = inputGO.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-270.0037f, -150f);
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 100);

            var bg = inputGO.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 1.0f);

            // Text
            GameObject maxCashGO = new GameObject("MaxCash");
            maxCashGO.transform.SetParent(inputGO.transform, false);
            maxCashGO.transform.transform.localPosition = new Vector3(0, 20f, 0);
            var maxCashText = maxCashGO.AddComponent<Text>();
            maxCashText.text = "$1000 Max";
            maxCashText.font = dupMe != null ? dupMe.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            maxCashText.fontSize = 30;
            maxCashText.color = dupMe != null ? dupMe.color : Color.cyan;
            maxCashText.alignment = TextAnchor.MiddleCenter;
            RectTransform maxCashRect = maxCashGO.transform.GetComponent<RectTransform>();
            maxCashRect.sizeDelta = new Vector2(600, 100);

            // Placeholder
            GameObject successRateGO = new GameObject("SuccessRate");
            successRateGO.transform.SetParent(inputGO.transform, false);
            successRateGO.transform.localPosition = new Vector3(0, -20f, 0);
            var successRateText = successRateGO.AddComponent<Text>();
            successRateText.text = "100% Success Rate";
            successRateText.font = dupMe != null ? dupMe.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            successRateText.fontSize = 30;
            successRateText.color = dupMe != null ? dupMe.color : Color.cyan;
            successRateText.alignment = TextAnchor.MiddleCenter;
            RectTransform successRateRect = successRateGO.transform.GetComponent<RectTransform>();
            successRateRect.sizeDelta = new Vector2(600, 100);
        }


        public override void OnInitializeMelon() {
            MelonLogger.Msg("Initializing Better Counter Offer...Go Make that Money");
            MelonLogger.Msg("If you find any bugs message me on nexus - OverweightUnicorn");
        }

    }
}

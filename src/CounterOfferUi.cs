using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Economy;
using UnityEngine.Events;
using Il2CppFunly.SkyStudio;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.UI.Handover;

[assembly: MelonInfo(typeof(BetterCounterOffer.CounterOfferUI), BetterCounterOffer.BuildInfo.Name, BetterCounterOffer.BuildInfo.Version, BetterCounterOffer.BuildInfo.Author, BetterCounterOffer.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterCounterOffer {
    public static class BuildInfo {
        public const string Name = "Better Counter Offer UI";
        public const string Description = "A mod that improves the Counter Offer Interface";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "1.1.0";
        public const string DownloadLink = "";
    }

    public class CounterOfferUI : MelonMod {

        private static Color disabledBg = new Color(84, 84, 84);
        private static Color disabledText = new Color(64, 64, 64);

        public static GameObject PlayerRef = null;
        public static GameObject popupRef = null;
        public static GameObject productSelectorRef = null;
        public static Text initialOfferText = null;
        public static Text successRateText = null;
        public static Text maxCashText = null;
        public static Text btnText = null;
        public static Image btnBg = null; 
        public static bool displayAll = false;
        public static float prevTime = 0;
        public static Gradient colorMap = new Gradient();
        public static Il2Generic.List<ProductDefinition> currList = null;
        public static CounterOfferProductSelector selectorInterface = null;
        public static CounterofferInterface offerInterface = null;

        public static void OnPopupOpen(CounterofferInterface instance) {
            Customer currCustomer = instance.conversation.sender.GetComponent<Customer>();
            if (currCustomer == null) {
                offerInterface.conversation.sender.GetComponent<Customer>();
            }


            prevTime = 0;
            displayAll = false;

            ToggleButton();
            SetInitialPriceText(instance.price);

            float maxSpend = CalculateSpendingLimits(currCustomer);
            SetMaxCashText(maxSpend);

            float successChance = CalculateSuccessProbability(currCustomer, instance.selectedProduct, instance.quantity, instance.price);
            SetSuccessRateText(successChance);
        }

        public static float CalculateSpendingLimits(Customer customer) {
            CustomerData customerData = customer.CustomerData;
            float adjustedWeeklySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            var orderDays = customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f);
            float maxSpend = (adjustedWeeklySpend / orderDays.Count) * 3f;
            return maxSpend;
        }

        public static void SetSuccessRateText(float success) {
            if (successRateText == null) {
                return;
            }
            successRateText.text = $"<b>{Mathf.RoundToInt(success * 100)}% Chance of Success</b>";
            successRateText.color = colorMap.Evaluate(success);
        }

        public static void SetMaxCashText(float maxSpend) {
            if (maxCashText == null) {
                return;
            }
            maxCashText.text = $"<b>Spend Limit: ${Mathf.RoundToInt(maxSpend)}</b>";
        }

        public static void SetInitialPriceText(float initialPrice) {
            if (initialOfferText == null) {
                return;
            }
            initialOfferText.text = $"<b>Initial Offer ${Mathf.RoundToInt(initialPrice)}</b>";
        }

        public static void ToggleButton() {
            if (displayAll) {
                btnBg.color = Color.green;
                btnText.color = Color.white;
            } else {
                btnBg.color = disabledBg;
                btnText.color = disabledText;
            }
        }

        public static void UpdateTextFields(CounterofferInterface instance) {
            if (instance == null) {
                MelonLogger.Msg(System.ConsoleColor.Red, "CounterofferInterface Instance was Null!!");
                return;
            }

            if (successRateText == null) {
                MelonLogger.Msg(System.ConsoleColor.Red, "successRateText Instance was Null!!");
                return;
            }

            MelonLogger.Msg(System.ConsoleColor.Green, "Updating Text Fields");
            float probability = CalculateSuccessProbability(instance.conversation.sender.GetComponent<Customer>(), instance.selectedProduct, instance.quantity, instance.price);
            SetSuccessRateText(probability);

        }

        public static float CalculateSuccessProbability(Customer customer,ProductDefinition product, int quantity, float price) {
            float adjustedWeeklySpend = customer.customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            Il2Generic.List<EDay> orderDays = customer.customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f);
            float num = adjustedWeeklySpend / orderDays.Count;

            // Immediate rejection based on price threshold
            if (price >= num * 3f)
                return 0f;

            float valueProposition = Customer.GetValueProposition(
                Registry.GetItem<ProductDefinition>(customer.OfferedContractInfo.Products.entries[0].ProductID),
                customer.OfferedContractInfo.Payment / customer.OfferedContractInfo.Products.entries[0].Quantity
            );
            float productEnjoyment = customer.GetProductEnjoyment(product, customer.customerData.Standards.GetCorrespondingQuality());
            float num2 = Mathf.InverseLerp(-1f, 1f, productEnjoyment);
            float valueProposition2 = Customer.GetValueProposition(product, price / quantity);
            float num3 = Mathf.Pow(quantity / (float)customer.OfferedContractInfo.Products.entries[0].Quantity, 0.6f);
            float num4 = Mathf.Lerp(0f, 2f, num3 * 0.5f);
            float num5 = Mathf.Lerp(1f, 0f, Mathf.Abs(num4 - 1f));

            // High value proposition leads to acceptance
            if (valueProposition2 * num5 > valueProposition)
                return 1f;

            // Low value proposition leads to rejection
            if (valueProposition2 < 0.12f)
                return 0f;

            float num6 = productEnjoyment * valueProposition;
            float num7 = num2 * num5 * valueProposition2;

            // Better product enjoyment and proposition leads to acceptance
            if (num7 > num6)
                return 1f;

            float num8 = num6 - num7;
            float num9 = Mathf.Lerp(0f, 1f, num8 / 0.2f);
            float t = Mathf.Max(customer.CurrentAddiction, customer.NPC.RelationData.NormalizedRelationDelta);
            float num10 = Mathf.Lerp(0f, 0.2f, t);

            // Calculate probabilistic acceptance chance
            if (num9 <= num10)
                return 1f;
            if (num9 - num10 >= 0.9f)
                return 0f;

            float probability = (0.9f + num10 - num9) / 0.9f;
            return Mathf.Clamp(probability, 0f, 1f);
        }

        public static void attemptToCreateField() {
            GameObject handOverScreen = GameObject.Find("UI/HandoverScreen");
            if (handOverScreen != null) { 
                HandoverScreen hands = handOverScreen.GetComponent<HandoverScreen>();
                if (handOverScreen != null) {
                    colorMap = hands.SuccessColorMap;
                }
            }

            GameObject playerObject = GameObject.Find("Player_Local");
            if (playerObject != null) {
                PlayerRef = playerObject;

                Transform transform = PlayerRef.transform.Find("CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/AppsCanvas/Messages/Container/CounterofferInterface/Shade/Content");
                GameObject popupContent = transform != null ? transform.gameObject : null;
                if (popupContent != null) {
                    MelonLogger.Msg("CounterOffer container Found");

                    // Make pop-up bigger to support the new fields
                    RectTransform CoPopupRect = transform.GetComponent<RectTransform>();
                    CoPopupRect.sizeDelta = new Vector2(-160f, -90f);

                    MelonLogger.Msg("Attempting to shift components");
                    shiftOfferElements(transform);

                    MelonLogger.Msg("Attempting to make searchbar");
                    createLabels(transform);

                    MelonLogger.Msg("Updating Selector Interface");
                    updateSelectorUi(transform);
                }
            }
        }

        private static void adjustUiElements(Transform parent, string searchStr, float x, float y) {
            Transform fpTransform = parent.Find(searchStr);
            if (fpTransform != null) {
                RectTransform fpRect = fpTransform.GetComponent<RectTransform>();
                fpRect.anchoredPosition = new Vector2(x, y);
            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, $"{searchStr} Couldn't be Found");
            }
        }

        private static void shiftOfferElements(Transform parent) {
            adjustUiElements(parent, "Fair price", 0f, -450f);
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
                selectorInterface = selectorTrans.GetComponent<CounterOfferProductSelector>();

                RectTransform selectorRect = selectorTrans.GetComponent<RectTransform>();
                selectorRect.anchoredPosition = new Vector2(0f, -250f);
                Transform searchTrans = selectorTrans.Find("SearchInput");
                GameObject SearchInput = searchTrans != null ? searchTrans.gameObject : null;
                if (SearchInput != null) {
                    InputField searchField = SearchInput.GetComponent<InputField>();
                    Selectable.Transition hover = searchField.transition;
                    searchField.transition = Selectable.Transition.None;
                    Transform backgroundImage = searchTrans.Find("Image");
                    if (backgroundImage != null) {
                        MelonLogger.Msg(System.ConsoleColor.Cyan, "Attempting to Change Image width");
                        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                        bgRect.anchoredPosition = new Vector2(10f, -1);
                        bgRect.pivot = new Vector2(0, 0.5f);
                        bgRect.sizeDelta = new Vector2(-90f, 0);
                    }

                    Transform textArea = searchTrans.Find("Text Area");
                    if (textArea != null) {
                        MelonLogger.Msg(System.ConsoleColor.Cyan, "Attempting to Change Text Area width");
                        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
                        textAreaRect.anchoredPosition = new Vector2(5f, 0);
                        textAreaRect.pivot = new Vector2(0, 0.5f);
                        textAreaRect.sizeDelta = new Vector2(-90f, -18f);
                    }

                    MelonLogger.Msg(System.ConsoleColor.Cyan, "Attempting to Add Filter Button to parent");

                    GameObject filterGo = new GameObject("FilterButton");
                    filterGo.transform.SetParent(searchTrans, false);

                    btnBg = filterGo.AddComponent<Image>();
                    btnBg.color = disabledBg;

                    Button filterButton = filterGo.AddComponent<Button>();
                    filterButton.transition = hover;
                    filterButton.onClick.AddListener((UnityAction)handleClick);

                    GameObject filterTextGo = new GameObject("FilterText");
                    filterTextGo.transform.SetParent(filterGo.transform, false);
                    btnText = filterTextGo.AddComponent<Text>();
                    btnText.text = "<b>All</b>";
                    btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    btnText.fontSize = 30;
                    btnText.color = disabledText;
                    btnText.alignment = TextAnchor.MiddleCenter;
                    var buttonRect = filterGo.GetComponent<RectTransform>();
                    buttonRect.pivot = new Vector2(1, 1);
                    buttonRect.sizeDelta = new Vector2(60, 40);
                    buttonRect.anchorMin = new Vector2(1, 1);
                    buttonRect.anchorMax = new Vector2(1, 1);
                    buttonRect.anchoredPosition = new Vector2(-15f, -5f);

                }

            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, "Selection Couldn't be Found");
            }
        }

        static void handleClick() {
            float currTime = Time.time;
            MelonLogger.Msg($"Time passed: {currTime} - {prevTime} = {currTime - prevTime}");
            if (currTime - prevTime < 1) {
                MelonLogger.Msg(System.ConsoleColor.Blue, "Please Wait to Click Again");
            } else {
                if (displayAll) {
                    displayAll = false;
                } else {
                    displayAll = true;
                }
                ToggleButton();
                MelonLogger.Msg($"display all: {displayAll}");
                if (selectorInterface != null) {
                    MelonLogger.Msg($"Attempting to rebuild list");
                    selectorInterface.RebuildResultsList();
                }
                prevTime = currTime;
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

            // Success Rate
            GameObject initialOfferGO = new GameObject("InitialCash");
            initialOfferGO.transform.SetParent(inputGO.transform, false);
            initialOfferGO.transform.localPosition = new Vector3(0, 40f, 0);
            initialOfferText = initialOfferGO.AddComponent<Text>();
            initialOfferText.text = "Initial Offer Price: ";
            initialOfferText.font = dupMe != null ? dupMe.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            initialOfferText.fontSize = 30;
            initialOfferText.color = Color.gray;
            initialOfferText.alignment = TextAnchor.MiddleCenter;
            RectTransform initialOfferRect = initialOfferGO.transform.GetComponent<RectTransform>();
            initialOfferRect.sizeDelta = new Vector2(600, 100);

            // Max Cash
            GameObject maxCashGO = new GameObject("MaxCash");
            maxCashGO.transform.SetParent(inputGO.transform, false);
            maxCashGO.transform.transform.localPosition = new Vector3(0, 5f, 0);
            maxCashText = maxCashGO.AddComponent<Text>();
            maxCashText.text = "$1000 Max";
            maxCashText.font = dupMe != null ? dupMe.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            maxCashText.fontSize = 30;
            maxCashText.color = Color.gray;
            maxCashText.alignment = TextAnchor.MiddleCenter;
            RectTransform maxCashRect = maxCashGO.transform.GetComponent<RectTransform>();
            maxCashRect.sizeDelta = new Vector2(600, 100);

            // Success Rate
            GameObject successRateGO = new GameObject("SuccessRate");
            successRateGO.transform.SetParent(inputGO.transform, false);
            successRateGO.transform.localPosition = new Vector3(0, -30f, 0);
            successRateText = successRateGO.AddComponent<Text>();
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

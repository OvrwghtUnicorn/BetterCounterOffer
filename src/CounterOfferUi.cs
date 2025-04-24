using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Economy;
using UnityEngine.Events;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.UI.Handover;
using Il2CppInterop.Runtime;
using UnityEngine.EventSystems;

[assembly: MelonInfo(typeof(BetterCounterOffer.CounterOfferUI), BetterCounterOffer.BuildInfo.Name, BetterCounterOffer.BuildInfo.Version, BetterCounterOffer.BuildInfo.Author, BetterCounterOffer.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterCounterOffer {
    public static class BuildInfo {
        public const string Name = "Better Counter Offer UI";
        public const string Description = "A mod that improves the Counter Offer UI";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "2.0.0";
        public const string DownloadLink = "";
    }

    public class CounterOfferUI : MelonMod {

        private static Color disabledBg = new Color(0.4f, 0.4f, 0.4f);
        private static Color disabledText = new Color(0.2f, 0.2f, 0.2f);

        public static GameObject PlayerRef = null;
        public static GameObject popupRef = null;
        public static GameObject productSelectorRef = null;

        public static GameObject offerInfoGO = null;
        public static Text initialOfferText = null;
        public static Text successRateText = null;
        public static Text maxCashText = null;
        public static Text btnText = null;
        public static Image btnBg = null;
        public static Font gameFont = null;

        public static bool displayAll = false;
        public static float prevTime = 0;
        public static Gradient colorMap = new Gradient();
        public static Il2Generic.List<ProductDefinition> currList = null;
        public static CounterOfferProductSelector selectorInterface = null;
        public static CounterofferInterface offerInterface = null;

        public static int labelCount = 0;
        public static Dictionary<string, Vector2[]> uiPositions = new Dictionary<string, Vector2[]>
        {
            { "Shade/Content", new Vector2[] { new Vector2(-160, -160), new Vector2(-160, -180), new Vector2(-160 ,-140), new Vector2(-160f, -90f) } },
            { "Selection", new Vector2[] { new Vector2(0, -182), new Vector2(0,-230), new Vector2(0, -250), new Vector2(0, -250) } }, // Selection vector2 not retrieved for 1 element
            { "Subtitle", new Vector2[] { new Vector2(0, -117), new Vector2(0,-150), new Vector2(0, -190), new Vector2(0, -220) } },
            { "Remove", new Vector2[] { new Vector2(-210, 74), new Vector2(-210, 30), new Vector2(-210, 10), new Vector2(-210, 10) } },
            { "Add", new Vector2[] { new Vector2(210, 74), new Vector2(210, 30), new Vector2(210, 10), new Vector2(210, 10) } },
            { "Product", new Vector2[] { new Vector2(0, 74), new Vector2(0,30), new Vector2(0, 10), new Vector2(0, 10) } },
            { "Subtitle (1)", new Vector2[] { new Vector2(0, -258.12f), new Vector2(0,-260), new Vector2(0, -300), new Vector2(0, -335) } },
            { "Price", new Vector2[] { new Vector2(0, -313.12f), new Vector2(0,-310), new Vector2(0, -350), new Vector2(0, -390) } },
            { "Fair price", new Vector2[] { new Vector2(0, -362), new Vector2(0,-370), new Vector2(0, -405), new Vector2(0, -450) } },
        };

        public static void OnPopupOpen(CounterofferInterface instance) {
            Customer currCustomer = instance.conversation.sender.GetComponent<Customer>();
            if (currCustomer == null) {
                offerInterface.conversation.sender.GetComponent<Customer>();
            }


            prevTime = 0;
            displayAll = false;

            //ToggleButton();
            SetInitialPriceText(instance.price);

            float maxSpend = CalculateSpendingLimits(currCustomer);
            SetMaxCashText(maxSpend);

            float successChance = CalculateSuccessProbability(currCustomer, instance.selectedProduct, instance.quantity, instance.price);
            SetSuccessRateText(successChance);
        }

        public static void SetSuccessRateText(float success) {
            if (successRateText == null) {
                MelonLogger.Msg("successRateText is null??");
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

        private static void handleClick() {
            float currTime = Time.time;
            if (currTime - prevTime > 1) {
                if (displayAll) {
                    displayAll = false;
                } else {
                    displayAll = true;
                }

                // Update button color after display
                //ToggleButton();

                if (selectorInterface != null) {
                    selectorInterface.RebuildResultsList();
                }
                prevTime = currTime;
            }
        }

        public static float CalculateSpendingLimits(Customer customer) {
            CustomerData customerData = customer.CustomerData;
            float adjustedWeeklySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            var orderDays = customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f);
            float maxSpend = (adjustedWeeklySpend / orderDays.Count) * 3f;
            return maxSpend;
        }

        public static float CalculateSuccessProbability(Customer customer, ProductDefinition product, int quantity, float price) {
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

        public static void UpdateSuccessRate(CounterofferInterface instance) {
            if (instance == null) {
                MelonLogger.Msg(System.ConsoleColor.Red, "CounterofferInterface Instance was Null!!");
                return;
            }

            if (successRateText == null) {
                MelonLogger.Msg(System.ConsoleColor.Red, "successRateText Instance was Null!!");
                return;
            }

            float probability = CalculateSuccessProbability(instance.conversation.sender.GetComponent<Customer>(), instance.selectedProduct, instance.quantity, instance.price);
            SetSuccessRateText(probability);

        }

        public static void InitOnWake() {
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
                    MelonLogger.Msg(System.ConsoleColor.Magenta, "CounterOffer Container Found Lets Make it Better");

                    if (!CounterOfferConfig.disableAllLabels) {
                        CreateLabels(transform);
                        GrowPopUpWindow(transform);
                        ShiftOfferElements(transform);
                    }
                    UpdateSelectorUI(transform);
                }
            }
        }

        private static void GrowPopUpWindow(Transform transform) {
            if (transform != null) {
                Vector2 sizeDelta = uiPositions["Shade/Content"][labelCount];
                MelonLogger.Msg(System.ConsoleColor.Magenta, $"Growing Popup to {sizeDelta}");
                // Make pop-up bigger to support the new fields
                RectTransform CoPopupRect = transform.GetComponent<RectTransform>();
                CoPopupRect.sizeDelta = sizeDelta;
            }
        }

        private static void AdjustUiElements(Transform parent, string searchStr) {
            Transform fpTransform = parent.Find(searchStr);
            if (fpTransform != null) {
                Vector2 anchorPos = uiPositions[searchStr][labelCount];
                MelonLogger.Msg(System.ConsoleColor.Magenta, $"{searchStr} returned a value of {anchorPos}");
                RectTransform fpRect = fpTransform.GetComponent<RectTransform>();
                fpRect.anchoredPosition = anchorPos;
            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, $"{searchStr} Couldn't be Found");
            }
        }

        private static void ShiftOfferElements(Transform parent) {
            AdjustUiElements(parent, "Fair price");
            AdjustUiElements(parent, "Price");
            AdjustUiElements(parent, "Subtitle (1)");
            AdjustUiElements(parent, "Product");
            AdjustUiElements(parent, "Add");
            AdjustUiElements(parent, "Remove");
            AdjustUiElements(parent, "Subtitle");
            AdjustUiElements(parent, "Selection");
        }

        private static void UpdateSelectorUI(Transform parent) {
            Transform selectorTrans = parent.Find("Selection");
            if (selectorTrans != null) {
                MelonLogger.Msg(System.ConsoleColor.DarkGreen, $"Selection Adjusted");
                selectorInterface = selectorTrans.GetComponent<CounterOfferProductSelector>();

                //CreateFilterButton(selectorTrans);
                //ShrinkSearch(selectorTrans);
                Transform windowTrans = selectorTrans.Find("Window");
                if (windowTrans != null) { 
                    RectTransform windowRect = windowTrans.GetComponent<RectTransform>();
                    windowRect.anchoredPosition = new Vector2(0, -90);

                    GameObject btnContainerGO = new GameObject("Filter_Buttons");
                    btnContainerGO.transform.SetParent(selectorTrans, false);
                    btnContainerGO.AddComponent<CanvasRenderer>();
                    RectTransform containerRectTrans = btnContainerGO.AddComponent<RectTransform>();
                    containerRectTrans.anchorMin = new Vector2(0, 0.5f);
                    containerRectTrans.anchorMax = new Vector2(1, 0.5f);
                    containerRectTrans.anchoredPosition = new Vector2(0f, 0f);
                    containerRectTrans.sizeDelta = new Vector2(1, 50);
                    Image containerBg = btnContainerGO.AddComponent<Image>();
                    containerBg.color = Color.gray;

                    GridLayoutGroup containerGrid = btnContainerGO.AddComponent<GridLayoutGroup>();
                    containerGrid.cellSize = new Vector2(120, 45);
                    containerGrid.spacing = new Vector2(2, 0);
                    containerGrid.childAlignment = TextAnchor.LowerCenter;
                    containerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    containerGrid.constraintCount = 3;


                    TabController.font = gameFont != null ? gameFont : Resources.GetBuiltinResource<Font>("Arial.ttf"); ;
                    TabController.AddTab(btnContainerGO.transform, "Favorites", "<b>Fave</b>");
                    TabController.AddTab(btnContainerGO.transform, "Listed", "<b>Listed</b>");
                    TabController.AddTab(btnContainerGO.transform, "Discovered", "<b>All</b>");
                    TabController.SetSelected("Listed");
                }




            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, "Selection Couldn't be Found");
            }
        }

        //private static (TabButton, Image, Text) CreateBtnImgTxt(Transform parent, String title, Color bgColor) {
        //    GameObject buttonGo = new GameObject($"{title}_Button");
        //    buttonGo.transform.SetParent(parent, false);
        //    buttonGo.AddComponent<CanvasRenderer>();
        //    buttonGo.AddComponent<Button>();
        //    Image buttonImg = buttonGo.AddComponent<Image>();
        //    buttonImg.color = bgColor;

        //    TabButton tb = buttonGo.AddComponent<TabButton>();
        //    tb.background = buttonImg;

        //    GameObject buttonTextGo = new GameObject($"{title}_Button_Text");
        //    buttonTextGo.transform.SetParent(buttonGo.transform, false);
        //    Text buttonText = buttonTextGo.AddComponent<Text>();
        //    buttonText.text = "Test";
        //    buttonText.fontSize = 30;
        //    buttonText.font = gameFont != null ? gameFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        //    buttonText.color = Color.black;
        //    buttonText.alignment = TextAnchor.MiddleCenter;

        //    var textRect = buttonTextGo.GetComponent<RectTransform>();
        //    textRect.pivot = new Vector2(0.5f, 0.5f);
        //    textRect.sizeDelta = new Vector2(60, 40);
        //    textRect.anchorMin = new Vector2(0, 0);
        //    textRect.anchorMax = new Vector2(1, 1);
        //    textRect.anchoredPosition = new Vector2(0, 0);


        //    return (tb, buttonImg, buttonText);
        //}

        public static void TabSelected(Tab selected) {
            MelonLogger.Msg($"{selected.id} Selected");
        }

        private static void ShrinkSearch(Transform selectorTransform) {
            Transform searchTrans = selectorTransform.Find("SearchInput");
            GameObject SearchInput = searchTrans != null ? searchTrans.gameObject : null;
            if (SearchInput != null) {
                InputField searchField = SearchInput.GetComponent<InputField>();
                //Selectable.Transition hover = searchField.transition;
                //searchField.transition = Selectable.Transition.None;

                Transform backgroundImage = searchTrans.Find("Image");
                if (backgroundImage != null) {
                    MelonLogger.Msg(System.ConsoleColor.Magenta, "Shrinking Search Background Image ");
                    RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
                    bgRect.anchoredPosition = new Vector2(10f, -1);
                    bgRect.pivot = new Vector2(0, 0.5f);
                    bgRect.sizeDelta = new Vector2(-90f, 0);
                }

                Transform textArea = searchTrans.Find("Text Area");
                if (textArea != null) {
                    MelonLogger.Msg(System.ConsoleColor.Magenta, "Shrinking Search Text Area");
                    RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
                    textAreaRect.anchoredPosition = new Vector2(5f, 0);
                    textAreaRect.pivot = new Vector2(0, 0.5f);
                    textAreaRect.sizeDelta = new Vector2(-90f, -18f);
                }

            }
        }

        private static void CreateFilterButton(Transform parent) {
            MelonLogger.Msg(System.ConsoleColor.Magenta, "Trying to Create Filter Button");

            // Create a GameObject for the button
            GameObject filterGo = new GameObject("FilterButton");
            filterGo.transform.SetParent(parent, false);
            Button filterBtn = filterGo.AddComponent<Button>();
            btnBg = filterGo.AddComponent<Image>();
            btnBg.color = disabledBg;
            RectTransform filterBtnRect = filterBtn.GetComponent<RectTransform>();
            filterBtnRect.anchoredPosition = new Vector2(140, 202.5f);
            filterBtnRect.sizeDelta = new Vector2(60, 40);
            filterBtnRect.pivot = new Vector2(0.5f, 0.5f);

            filterBtn.onClick.AddListener((UnityAction)handleClick);

            // Create a GameObject for the Button Text
            // Text and Image componenets cannot be added to the same gameobject
            GameObject filterTextGo = new GameObject("FilterText");
            filterTextGo.transform.SetParent(filterGo.transform, false);
            btnText = filterTextGo.AddComponent<Text>();
            btnText.text = "<b>All</b>";
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 30;
            btnText.color = disabledText;
            btnText.alignment = TextAnchor.MiddleCenter;
            var textRect = filterTextGo.GetComponent<RectTransform>();
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(60, 40);
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.anchoredPosition = new Vector2(0, 0);
        }

        private static void CreateLabels(Transform parent) {
            Transform titleTransform = parent.Find("Title");
            if (titleTransform != null) {
                gameFont = titleTransform.GetComponent<Text>().font;
            }

            offerInfoGO = new GameObject("OfferInformation");
            offerInfoGO.transform.SetParent(parent, false);
            offerInfoGO.transform.localPosition = new Vector3(181.5444f, 1.175f, -9.2547f);
            offerInfoGO.AddComponent<CanvasRenderer>();

            var rect = offerInfoGO.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-270.0037f, -150f);
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 100);

            float startPosition = 40f;
            // Success Rate
            MelonLogger.Msg($"Disable Inital Price {CounterOfferConfig.disableInitialOffer}");
            if (initialOfferText == null && !CounterOfferConfig.disableInitialOffer) {
                initialOfferText = CreateLabel(offerInfoGO.transform, "InitialCash", "Initial Offer Price: ", new Vector3(0, startPosition, 0));
                MelonLogger.Msg($"Is initialOfferText initialized? {initialOfferText.text}");
                startPosition -= 35f;
                labelCount++;
            }

            // Max Cash
            MelonLogger.Msg($"Disable Max Limit {CounterOfferConfig.disableMaxLimit}");
            if (maxCashText == null && !CounterOfferConfig.disableMaxLimit) {
                maxCashText = CreateLabel(offerInfoGO.transform, "MaxCash", "$1000 Max", new Vector3(0, startPosition, 0));
                MelonLogger.Msg($"Is maxCashText initialized? {maxCashText.text}");
                startPosition -= 35f;
                labelCount++;
            }


            // Success Rate
            MelonLogger.Msg($"Disable Success Rate {CounterOfferConfig.disableSuccessRate}");
            if (successRateText == null && !CounterOfferConfig.disableSuccessRate) {
                successRateText = CreateLabel(offerInfoGO.transform, "SuccessRate", "100% Success Rate", new Vector3(0, startPosition, 0));
                MelonLogger.Msg($"Is successRateText initialized? {successRateText.text}");
                labelCount++;
            }
        }

        public static Text CreateLabel(Transform parent, string title, string text, Vector3 localPosition) {
            GameObject labelGo = new GameObject(title);
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = localPosition;
            Text textLabel = labelGo.AddComponent<Text>();
            textLabel.text = text;
            textLabel.font = gameFont != null ? gameFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            textLabel.fontSize = 30;
            textLabel.color = Color.gray;
            textLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform labelRect = labelGo.transform.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(600, 100);

            return textLabel;
        }


        public override void OnInitializeMelon() {
            MelonLogger.Msg(System.ConsoleColor.Magenta, "Initializing Better Counter Offer...Go Make that Money");
            MelonLogger.Msg(System.ConsoleColor.Magenta, "If you find any bugs,");
            MelonLogger.Msg(System.ConsoleColor.Magenta, "first check that your game is updated and on the main branch,");
            MelonLogger.Msg(System.ConsoleColor.Magenta, "Then if the bug persists message me on nexus");
            MelonLogger.Msg(System.ConsoleColor.Magenta, "- OverweightUnicorn\n");
            CounterOfferConfig.LoadConfig();
        }

    }
}

using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using ScheduleOne;
using ScheduleOne.UI.Phone;
using ScheduleOne.Product;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.UI.Handover;
using ScheduleOne.Law;
using ScheduleOne.Property.Utilities.Water;

[assembly: MelonInfo(typeof(BetterCounterOffer.CounterOfferUI), BetterCounterOffer.BuildInfo.Name, BetterCounterOffer.BuildInfo.Version, BetterCounterOffer.BuildInfo.Author, BetterCounterOffer.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 191, 0, 255)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BetterCounterOffer {
    public static class BuildInfo {
        public const string Name = "Better Counter Offer UI";
        public const string Description = "A mod that improves the Counter Offer UI";
        public const string Author = "OverweightUnicorn";
        public const string Company = "UnicornsCanMod";
        public const string Version = "3.2.0";
        public const string DownloadLink = "";
    }

    public class CounterOfferUI : MelonMod {

        public static GameObject PlayerRef = null;
        public static GameObject popupRef = null;
        public static GameObject productSelectorRef = null;

        public static GameObject offerInfoGO = null;
        public static Text initialOfferText = null;
        public static Text successRateText = null;
        public static Text maxCashText = null;
        public static Text fairPriceText = null;
        public static Text btnText = null;
        public static Image btnBg = null;
        public static Font gameFont = null;
        public static TabController selectorTabControl;

        public static bool displayAll = false;
        public static string currTab = "Favorites";
        public static float prevTime = 0;
        public static Gradient colorMap = new Gradient();
        public static CounterOfferProductSelector selectorInterface = null;
        public static CounterofferInterface offerInterface = null;

        public static int labelCount = 0;
        public static Dictionary<string, Vector2[]> uiPositions = new Dictionary<string, Vector2[]>
        {
            { "Shade/Content", new Vector2[] { new Vector2(-160, -160), new Vector2(-160, -180), new Vector2(-160 ,-140), new Vector2(-160f, -90f) } },
            { "Selection", new Vector2[] { new Vector2(0, -182), new Vector2(0,-230), new Vector2(0, -250), new Vector2(0, -250) } },
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

            if (!CounterOfferConfig.disableAllLabels) {
                if (!CounterOfferConfig.disableInitialOffer) {
                    if (CounterOfferConfig.enablePricePerUnit) {
                        SetInitialPriceText(instance.price / instance.quantity, true);
                        SetFairPriceText(instance.price / instance.quantity);
                    } else {
                        SetInitialPriceText(instance.price);
                    }
                }

                if (!CounterOfferConfig.disableMaxLimit) {
                    float maxSpend = CalculateSpendingLimits(currCustomer);
                    SetMaxCashText(maxSpend);
                }

                if (!CounterOfferConfig.disableSuccessRate) {
                    float successChance = CalculateSuccessProbability(currCustomer, instance.selectedProduct, instance.quantity, instance.price);
                    SetSuccessRateText(successChance);
                }
            }




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

        public static void SetInitialPriceText(float initialPrice, bool ppu = false) {
            if (initialOfferText == null) {
                return;
            }
            if (ppu) {
                initialOfferText.text = $"<b>Initial Offer: ${Mathf.RoundToInt(initialPrice)} per Unit</b>";
                return;
            }
            initialOfferText.text = $"<b>Initial Offer: ${Mathf.RoundToInt(initialPrice)}</b>";
        }

        public static void SetFairPriceText(float fairPrice) {
            if (fairPriceText == null) {
                return;
            }

            fairPriceText.text = $"<b>Price: ${Mathf.RoundToInt(fairPrice)} per Unit</b>";
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
            List<EDay> orderDays = customer.customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f);
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
                        GetAndShiftFairPrice(transform);
                    }
                    UpdateSelectorUI(transform);
                }
            }
        }

        private static void GrowPopUpWindow(Transform transform) {
            if (transform != null) {
                Vector2 sizeDelta = uiPositions["Shade/Content"][labelCount];
                // Make pop-up bigger to support the new fields
                RectTransform CoPopupRect = transform.GetComponent<RectTransform>();
                CoPopupRect.sizeDelta = sizeDelta;
            }
        }

        private static void AdjustUiElements(Transform parent, string searchStr) {
            Transform fpTransform = parent.Find(searchStr);
            if (fpTransform != null) {
                Vector2 anchorPos = uiPositions[searchStr][labelCount];
                RectTransform fpRect = fpTransform.GetComponent<RectTransform>();
                fpRect.anchoredPosition = anchorPos;
            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, $"{searchStr} Couldn't be Found");
            }
        }

        private static void ShiftOfferElements(Transform parent) {
            AdjustUiElements(parent, "Price");
            AdjustUiElements(parent, "Subtitle (1)");
            AdjustUiElements(parent, "Product");
            AdjustUiElements(parent, "Add");
            AdjustUiElements(parent, "Remove");
            AdjustUiElements(parent, "Subtitle");
            AdjustUiElements(parent, "Selection");
        }

        private static void GetAndShiftFairPrice(Transform parent) {
            Transform fpTransform = parent.Find("Fair price");
            if (fpTransform != null) {
                Vector2 anchorPos = uiPositions["Fair price"][labelCount];
                RectTransform fpRect = fpTransform.GetComponent<RectTransform>();
                fpRect.anchoredPosition = anchorPos;
                fairPriceText = fpTransform.GetComponent<Text>();
            }
        }

        private static void UpdateSelectorUI(Transform parent) {
            Transform selectorTrans = parent.Find("Selection");
            if (selectorTrans != null) {
                MelonLogger.Msg(System.ConsoleColor.Magenta, "The Selector UI now has Tabs....AWESOME");
                selectorInterface = selectorTrans.GetComponent<CounterOfferProductSelector>();

                Transform searchInputTrans = selectorTrans.Find("SearchInput");
                if (searchInputTrans != null) {
                    RectTransform searchInputRect = searchInputTrans.GetComponent<RectTransform>();
                    searchInputRect.anchoredPosition = new Vector2(0, -83);

                    InputField searchField = searchInputTrans.GetComponent<InputField>();
                }


                Transform windowTrans = selectorTrans.Find("Window");
                if (windowTrans != null) {
                    RectTransform windowRect = windowTrans.GetComponent<RectTransform>();
                    windowRect.anchoredPosition = new Vector2(0, -87);

                    //GridLayoutGroup windowGlg = windowTrans.GetComponent<GridLayoutGroup>();
                    //windowGlg.cellSize = new Vector2(87, 87);

                    if (selectorTabControl == null) {
                        selectorTabControl = new TabController(selectorTrans);
                        selectorTabControl.font = gameFont;
                        selectorTabControl.AddTab("Favorites", "<b>Fave</b>");
                        selectorTabControl.AddTab("Listed", "<b>Listed</b>");
                        selectorTabControl.AddTab("Discovered", "<b>All</b>");
                        selectorTabControl.SetSelected(currTab);
                    }
                }




            } else {
                MelonLogger.Msg(System.ConsoleColor.Red, "Selection Couldn't be Found");
            }
        }

        public static void TabSelected(Tab selected) {
            currTab = selected.id;
            if (selectorInterface != null) {
                selectorInterface.RebuildResultsList();
            }
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
            if (initialOfferText == null && !CounterOfferConfig.disableInitialOffer) {
                initialOfferText = CreateLabel(offerInfoGO.transform, "InitialCash", "Initial Offer Price: ", new Vector3(0, startPosition, 0));
                startPosition -= 35f;
                labelCount++;
            }

            // Max Cash
            if (maxCashText == null && !CounterOfferConfig.disableMaxLimit) {
                maxCashText = CreateLabel(offerInfoGO.transform, "MaxCash", "$1000 Max", new Vector3(0, startPosition, 0));
                startPosition -= 35f;
                labelCount++;
            }


            // Success Rate
            if (successRateText == null && !CounterOfferConfig.disableSuccessRate) {
                successRateText = CreateLabel(offerInfoGO.transform, "SuccessRate", "100% Success Rate", new Vector3(0, startPosition, 0));
                startPosition -= 35f;
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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            if (sceneName.ToLower() == "main") {
                labelCount = 0;
                selectorTabControl = null;
            }
        }
    }
}

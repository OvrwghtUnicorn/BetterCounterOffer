using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using GenericCol = Il2CppSystem.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.UI.Handover;
#elif MONO
using GenericCol = System.Collections.Generic;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Messages;
using ScheduleOne.Product;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.UI.Handover;
#endif

namespace BetterCounterOffer
{
    public static class CounterOfferUI
    {

        public static GameObject offerInfoGO = null;
        public static Text initialOfferText = null;
        public static Text successRateText = null;
        public static Text maxCashText = null;
        public static Text fairPriceText = null;
        public static Font gameFont = null;
        public static TabController selectorTabControl;

        public static string currTab = "Favorites";
        public static Gradient colorMap = new Gradient();
        public static CounterOfferProductSelector selectorInterface = null;
        public static CounterofferInterface offerInterface = null;

        public static int labelCount = 0;

        // Cached reference to Shade/Content transform, set once in InitOnWake
        private static Transform contentTransform = null;

        // MSGConversation.sender is gone; the sender is now a MessageContactInfo that
        // resolves to its NPC through TryGetNPC, and the Customer lives on that NPC.
        public static Customer GetCustomer(CounterofferInterface instance)
        {
            if (instance == null || instance.conversation == null)
            {
                return null;
            }

            if (!instance.conversation._sender.TryGetNPC(out var npc))
            {
                return null;
            }

            return npc.GetComponent<Customer>();
        }

        public static void OnPopupOpen(CounterofferInterface instance)
        {
            Customer currCustomer = GetCustomer(instance);
            if (currCustomer == null)
            {
                MelonLogger.Msg(System.ConsoleColor.Red, "Couldn't resolve the Customer for this counter offer");
                return;
            }

            if (!CounterOfferConfig.DisableAllLabels.Value)
            {
                if (!CounterOfferConfig.DisableInitialOffer.Value)
                {
                    float initialPrice = instance.PriceSelector.SelectedAmount;
                    if (CounterOfferConfig.EnablePricePerUnit.Value)
                    {
                        SetInitialPriceText(initialPrice / instance.quantity, true);
                        SetFairPriceText(initialPrice / instance.quantity);
                    }
                    else
                    {
                        SetInitialPriceText(initialPrice);
                    }

                }

                if (!CounterOfferConfig.DisableMaxLimit.Value)
                {
                    float maxSpend = CalculateSpendingLimits(currCustomer);
                    SetMaxCashText(maxSpend);
                }

                if (!CounterOfferConfig.DisableSuccessRate.Value)
                {
                    float successChance = CalculateSuccessProbability(currCustomer, instance.selectedProduct, instance.quantity, instance.PriceSelector.SelectedAmount);
                    SetSuccessRateText(successChance);
                }

            }




        }

        public static void SetSuccessRateText(float success)
        {
            if (successRateText == null)
            {
                Utility.Error("successRateText is null??");
                return;
            }
            successRateText.text = $"<b>{Mathf.RoundToInt(success * 100)}% Chance of Success</b>";
            successRateText.color = colorMap.Evaluate(success);
        }

        public static void SetMaxCashText(float maxSpend)
        {
            if (maxCashText == null)
            {
                return;
            }
            maxCashText.text = $"<b>Spend Limit: ${Mathf.RoundToInt(maxSpend)}</b>";
        }

        public static void SetInitialPriceText(float initialPrice, bool ppu = false)
        {
            if (initialOfferText == null)
            {
                return;
            }
            if (ppu)
            {
                initialOfferText.text = $"<b>Initial Offer: ${Mathf.RoundToInt(initialPrice)} per Unit</b>";
                return;
            }
            initialOfferText.text = $"<b>Initial Offer: ${Mathf.RoundToInt(initialPrice)}</b>";
        }

        public static void SetFairPriceText(float fairPrice)
        {
            if (fairPriceText == null)
            {
                return;
            }

            fairPriceText.text = $"<b>Price: ${Mathf.RoundToInt(fairPrice)} per Unit</b>";
        }

        public static float CalculateSpendingLimits(Customer customer)
        {
            CustomerData customerData = customer.CustomerData;
            float adjustedWeeklySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            var orderDays = new GenericCol.List<EDay>();
            customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f, orderDays);
            float maxSpend = (adjustedWeeklySpend / orderDays.Count) * 3f;
            return maxSpend;
        }

        public static float CalculateSuccessProbability(Customer customer, ProductDefinition product, int quantity, float price)
        {
            float adjustedWeeklySpend = customer.customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f);
            GenericCol.List<EDay> orderDays = new GenericCol.List<EDay>();
            customer.customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f, orderDays);
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

        public static void UpdateSuccessRate(CounterofferInterface instance)
        {
            if (instance == null)
            {
                Utility.Error("CounterofferInterface Instance was Null!!");
                return;
            }

            if (successRateText == null)
            {
                Utility.Error("successRateText Instance was Null!!");
                return;
            }

            Customer customer = GetCustomer(instance);
            if (customer == null)
            {
                return;
            }

            float probability = CalculateSuccessProbability(customer, instance.selectedProduct, instance.quantity, instance.PriceSelector.SelectedAmount);
            SetSuccessRateText(probability);

        }

        public static void InitOnWake()
        {
            Utility.Log("Initializing Counter Offer UI");
            GameObject handOverScreen = GameObject.Find("UI/HandoverScreen");
            if (handOverScreen != null)
            {
                Utility.Log("Found Handover Screen....Maybe Could Just search for the component?");
                HandoverScreen hands = handOverScreen.GetComponent<HandoverScreen>();
                if (handOverScreen != null)
                {
                    colorMap = hands._successColorMap;
                }
            }

            CounterofferInterface coInterface = PlayerSingleton<MessagesApp>.Instance.CounterofferInterface;

            if (coInterface != null)
            {
                
                Transform transform = coInterface.transform.Find("Content");
                GameObject popupContent = transform.gameObject;
                if (popupContent != null)
                {
                    contentTransform = transform;

                    // Always create all label GameObjects so they can be toggled at runtime
                    CreateLabels(transform);
                    GetFairPrice();

                    // Apply current pref values: sets active state, recalculates count, resizes popup, shifts elements


                    // Wire up runtime preference change callbacks
                    CounterOfferConfig.DisableAllLabels.OnEntryValueChangedUntyped.Subscribe((_,__) => ApplyLabelVisibility());
                    CounterOfferConfig.DisableInitialOffer.OnEntryValueChangedUntyped.Subscribe((_,__) => ApplyLabelVisibility());
                    CounterOfferConfig.DisableMaxLimit.OnEntryValueChangedUntyped.Subscribe((_, __) => ApplyLabelVisibility());
                    CounterOfferConfig.DisableSuccessRate.OnEntryValueChangedUntyped.Subscribe((_, __) => ApplyLabelVisibility());
                    CounterOfferConfig.EnablePricePerUnit.OnEntryValueChangedUntyped.Subscribe((_, __) => ApplyLabelVisibility());
                }

                UpdateSelectorUI(coInterface.transform);
                ApplyLabelVisibility();

                // EnsureAllEntriesExist() instantiates a button per discovered product the
                // first time it runs; doing that here (once, at load) instead of on the
                // popup's first Open() avoids the stutter when the player opens it in-game.
                if (selectorInterface != null)
                {
                    selectorInterface.EnsureAllEntriesExist();
                }

            }
        }

        /// <summary>
        /// Recomputes labelCount from the current preference values.
        /// </summary>
        private static void RecalculateLabelCount()
        {
            labelCount = 0;
            if (!CounterOfferConfig.DisableAllLabels.Value)
            {
                if (!CounterOfferConfig.DisableInitialOffer.Value) labelCount++;
                if (!CounterOfferConfig.DisableMaxLimit.Value)     labelCount++;
                if (!CounterOfferConfig.DisableSuccessRate.Value)  labelCount++;
            }
        }

        /// <summary>
        /// Activates/deactivates each label based on current prefs and shifts the
        /// product selector down to make room for however many are visible.
        /// </summary>
        public static void ApplyLabelVisibility()
        {
            if (contentTransform == null) return;

            RecalculateLabelCount();

            bool allDisabled = CounterOfferConfig.DisableAllLabels.Value;

            // Determine visibility of each label
            bool showInitial    = !allDisabled && !CounterOfferConfig.DisableInitialOffer.Value;
            bool showMaxCash    = !allDisabled && !CounterOfferConfig.DisableMaxLimit.Value;
            bool showSuccess    = !allDisabled && !CounterOfferConfig.DisableSuccessRate.Value;

            // Activate / deactivate
            if (initialOfferText != null) initialOfferText.gameObject.SetActive(showInitial);
            if (maxCashText != null) maxCashText.gameObject.SetActive(showMaxCash);
            if (successRateText != null)  successRateText.gameObject.SetActive(showSuccess);

            if(selectorInterface != null)
            {
                selectorInterface.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,-170 - (labelCount * 30));
            }

            GetFairPrice();
        }

        private static void GetFairPrice()
        {

            CounterofferInterface coInterface = PlayerSingleton<MessagesApp>.Instance.CounterofferInterface;
            if(coInterface != null)
            {
                fairPriceText = coInterface.FairPriceLabel;
            }
        }

        private static void UpdateSelectorUI(Transform parent)
        {
            Transform selectorTrans = parent.Find("Selection");
            if (selectorTrans != null)
            {
                Utility.Log("The Selector UI now has Tabs....AWESOME");
                selectorInterface = selectorTrans.GetComponent<CounterOfferProductSelector>();

                Transform searchInputTrans = selectorTrans.Find("Container/SearchInput");
                if (searchInputTrans != null)
                {
                    RectTransform searchInputRect = searchInputTrans.GetComponent<RectTransform>();
                    searchInputRect.anchoredPosition = new Vector2(0, -83);

                    InputField searchField = searchInputTrans.GetComponent<InputField>();
                }


                Transform windowTrans = selectorTrans.Find("Container");
                if (windowTrans != null)
                {
                    //RectTransform windowRect = windowTrans.GetComponent<RectTransform>();
                    //windowRect.anchoredPosition = new Vector2(0, -300);

                    //GridLayoutGroup windowGlg = windowTrans.GetComponent<GridLayoutGroup>();
                    //windowGlg.cellSize = new Vector2(87, 87);

                    if (selectorTabControl == null)
                    {
                        selectorTabControl = new TabController(windowTrans);
                        selectorTabControl.font = gameFont;
                        selectorTabControl.AddTab("Favorites", "<b>Fave</b>");
                        selectorTabControl.AddTab("Listed", "<b>Listed</b>");
                        selectorTabControl.AddTab("Discovered", "<b>All</b>");
                        selectorTabControl.SetSelected(currTab);
                    }
                }




            }
            else
            {
                Utility.Error("Selection Couldn't be Found");
            }
        }

        public static void TabSelected(Tab selected)
        {
            currTab = selected.id;
            if (selectorInterface != null)
            {
                selectorInterface.RebuildResultsList();
            }
        }

        private static void CreateLabels(Transform parent)
        {
            Transform titleTransform = parent.Find("Header/Title");
            if (titleTransform != null)
            {
                gameFont = titleTransform.GetComponent<Text>().font;
            }

            // Always create all three labels; ApplyLabelVisibility will show/hide them
            initialOfferText = CreateLabel(parent, "InitialCash",  "Initial Offer Price: ");
            initialOfferText.transform.SetSiblingIndex(3);
            maxCashText   = CreateLabel(parent, "MaxCash", "$1000 Max");
            maxCashText.transform.SetSiblingIndex(4);
            successRateText   = CreateLabel(parent, "SuccessRate", "100% Success Rate");
            successRateText.transform.SetSiblingIndex(5);
        }

        public static Text CreateLabel(Transform parent, string title, string text)
        {
            GameObject labelGo = new GameObject(title);
            labelGo.transform.SetParent(parent, false);
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
    }
}

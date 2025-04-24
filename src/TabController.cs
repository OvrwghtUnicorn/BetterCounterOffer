using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine.Events;

namespace BetterCounterOffer {

    public class Tab {
        public string id;
        public Button button;
        public Image background;
        public Text buttonText;

        public void SetColor(Color bgColor, Color textColor) {
            SetBgColor(bgColor);
            SetTextColor(textColor);
        }

        public void SetBgColor(Color newColor) {
            this.background.color = newColor;
        }

        public void SetTextColor(Color newColor) {
            this.buttonText.color = newColor;
        }
    }

    public static class TabController {
        public static Dictionary<string, Tab> allTabs = new Dictionary<string, Tab>();
        public static Tab selectedTab = null;

        public static Color textActive = Color.white;
        public static Color textDisabled = Color.black;
        public static Color tabIdle = new Color(0.349f, 0.349f, 0.349f);
        public static Color tabHover = new Color(0.5f, 0.5f, 0.5f);
        public static Color tabActive = new Color(0.549f, 0.929f, 0.137f);

        public static Font font;

        public static void AddTab(Transform parent, string id, string text) {
            if (allTabs.ContainsKey(id)) {
                throw new Exception($"The key {id} already exists in the tab list");
            }
            Tab newTab = CreateNewTab(parent, id, text);
            allTabs.Add(id, newTab);
        }

        public static void SetSelected(string key) {
            if (!allTabs.ContainsKey(key)) {
                throw new Exception($"{key} Does Not Exist in the TabController Dictionary");
            }
            Tab selected = allTabs[key];
            selectedTab = allTabs[key];
            selected.SetColor(tabActive, textActive);
        }

        public static Tab CreateNewTab(Transform parent, string title, string text) {
            GameObject buttonGo = new GameObject($"{title}_Button");
            buttonGo.transform.SetParent(parent, false);
            buttonGo.AddComponent<CanvasRenderer>();
            Button button = buttonGo.AddComponent<Button>();
            Image buttonImg = buttonGo.AddComponent<Image>();
            buttonImg.color = tabIdle;


            GameObject buttonTextGo = new GameObject($"{title}_Button_Text");
            buttonTextGo.transform.SetParent(buttonGo.transform, false);
            Text buttonText = buttonTextGo.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = font;
            buttonText.fontSize = 30;
            buttonText.color = textDisabled;
            buttonText.alignment = TextAnchor.MiddleCenter;

            var textRect = buttonTextGo.GetComponent<RectTransform>();
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(60, 40);
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.anchoredPosition = new Vector2(0, 0);

            Tab newTab = new Tab();
            newTab.id = title;
            newTab.button = button;
            newTab.background = buttonImg;
            newTab.buttonText = buttonText;

            EventTrigger.Entry eventEntryEnter = new EventTrigger.Entry();
            eventEntryEnter.eventID = EventTriggerType.PointerEnter;
            eventEntryEnter.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>((BaseEventData eventData) => HandleButtonEnter(newTab))); 

            EventTrigger.Entry eventEntryExit = new EventTrigger.Entry();
            eventEntryExit.eventID = EventTriggerType.PointerExit;
            eventEntryExit.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>((BaseEventData eventData) => HandleButtonExit(newTab)));

            EventTrigger.Entry eventEntryClick = new EventTrigger.Entry();
            eventEntryClick.eventID = EventTriggerType.PointerClick;
            eventEntryClick.callback.AddListener(DelegateSupport.ConvertDelegate<UnityAction<BaseEventData>>((BaseEventData eventData) => HandleButtonClick(newTab)));

            EventTrigger events = buttonGo.AddComponent<EventTrigger>();
            events.triggers.Add(eventEntryEnter);
            events.triggers.Add(eventEntryExit);
            events.triggers.Add(eventEntryClick);


            return newTab;
        }

        public static void HandleButtonEnter(Tab currTab) {
            ResetTabs();
            if (selectedTab != null && selectedTab == currTab) return;
            currTab.SetBgColor(tabHover);
        }

        public static void HandleButtonExit(Tab currTab) {
            ResetTabs();
        }

        public static void HandleButtonClick(Tab currTab) {
            CounterOfferUI.TabSelected(currTab);
            selectedTab = currTab;
            ResetTabs();
            currTab.SetColor(tabActive, textActive);
        }

        public static void ResetTabs() {
            foreach (KeyValuePair<string, Tab> tab in allTabs) {
                if (selectedTab != null && selectedTab == tab.Value) continue;
                tab.Value.SetColor(tabIdle, textDisabled);
            }
        }
    }

}

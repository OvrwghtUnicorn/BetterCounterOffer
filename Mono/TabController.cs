using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    public class TabController {
        public Dictionary<string, Tab> allTabs = new Dictionary<string, Tab>();
        public Tab selectedTab = null;

        // Used to prevent quick switching between tabs.
        public float clickBuffer = 0.7f;
        public float prevTime = 0;

        public GameObject filterbuttons;
        public Transform parent;
        public Font font;

        public Color textActive = new Color(0.961f, 0.961f, 0.961f);
        public Color textDisabled = new Color(0.686f, 0.686f, 0.686f);
        public Color tabIdle = new Color(0.051f, 0.286f, 0.451f);
        public Color tabHover = new Color(0.114f, 0.353f, 0.525f);
        public Color tabActive = new Color(0.204f, 0.522f, 0.737f);

        public TabController(Transform parent) {
            this.parent = parent;
            InitFilterButtons();
        }

        private void InitFilterButtons() {
            filterbuttons = new GameObject("Filter_Buttons");
            if (this.parent != null) {
                filterbuttons.transform.SetParent(this.parent, false);
            }
            filterbuttons.AddComponent<CanvasRenderer>();
            RectTransform containerRectTrans = filterbuttons.AddComponent<RectTransform>();
            containerRectTrans.anchorMin = new Vector2(0, 0.5f);
            containerRectTrans.anchorMax = new Vector2(1, 0.5f);
            containerRectTrans.anchoredPosition = new Vector2(0f, 200);
            containerRectTrans.sizeDelta = new Vector2(1, 60);
            Image containerBg = filterbuttons.AddComponent<Image>();
            containerBg.color = new Color(0.9608f, 0.9608f, 0.9608f);

            GridLayoutGroup containerGrid = filterbuttons.AddComponent<GridLayoutGroup>();
            containerGrid.cellSize = new Vector2(100, 45);
            containerGrid.spacing = new Vector2(15, 0);
            containerGrid.childAlignment = TextAnchor.MiddleCenter;
            containerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            containerGrid.constraintCount = 3;
        }

        public void AddTab(string id, string text) {
            if (allTabs.ContainsKey(id)) {
                throw new Exception($"The key {id} already exists in the tab list");
            }

            if (filterbuttons == null) {
                throw new Exception("Filter Buttons has not been initialized");
            }
            Tab newTab = CreateNewTab(filterbuttons.transform, id, text);
            allTabs.Add(id, newTab);
        }

        public void SetSelected(string key) {
            if (!allTabs.ContainsKey(key)) {
                throw new Exception($"{key} Does Not Exist in the TabController Dictionary");
            }
            Tab selected = allTabs[key];
            selectedTab = allTabs[key];
            selected.SetColor(tabActive, textActive);
        }

        public Tab CreateNewTab(Transform parent, string title, string text) {
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
            buttonText.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            eventEntryEnter.callback.AddListener((BaseEventData eventData) => HandleButtonEnter(newTab));

            EventTrigger.Entry eventEntryExit = new EventTrigger.Entry();
            eventEntryExit.eventID = EventTriggerType.PointerExit;
            eventEntryExit.callback.AddListener((BaseEventData eventData) => HandleButtonExit(newTab));

            EventTrigger.Entry eventEntryClick = new EventTrigger.Entry();
            eventEntryClick.eventID = EventTriggerType.PointerClick;
            eventEntryClick.callback.AddListener((BaseEventData eventData) => HandleButtonClick(newTab));

            EventTrigger events = buttonGo.AddComponent<EventTrigger>();
            events.triggers.Add(eventEntryEnter);
            events.triggers.Add(eventEntryExit);
            events.triggers.Add(eventEntryClick);


            return newTab;
        }

        public void HandleButtonEnter(Tab currTab) {
            ResetTabs();
            if (selectedTab != null && selectedTab == currTab) return;
            currTab.SetBgColor(tabHover);
        }

        public void HandleButtonExit(Tab currTab) {
            ResetTabs();
        }

        public void HandleButtonClick(Tab currTab) {
            float currTime = Time.time;
            if (currTime - prevTime > clickBuffer) {
                CounterOfferUI.TabSelected(currTab);
                selectedTab = currTab;
                ResetTabs();
                currTab.SetColor(tabActive, textActive);
                prevTime = currTime;
            }
        }

        public void ResetTabs() {
            foreach (KeyValuePair<string, Tab> tab in allTabs) {
                if (selectedTab != null && selectedTab == tab.Value) continue;
                tab.Value.SetColor(tabIdle, textDisabled);
            }
        }
    }

}

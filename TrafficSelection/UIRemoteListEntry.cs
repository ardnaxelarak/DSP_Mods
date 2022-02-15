using HarmonyLib;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TrafficSelection {
    public class UIRemoteListEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public RemoteIdentifier supply;
        public RemoteIdentifier demand;
        public StationComponent station;

        public int itemId;
        public int planetId;

        public UIFilterWindow window;

        public int stationMaxItemCount = 0;

        public ERemoteType remoteType;

        [SerializeField]
        public Image itemImage;

        [SerializeField]
        public Text stationText;

        [SerializeField]
        public Text starText;

        [SerializeField]
        public Text planetText;


        [SerializeField]
        public Image activeIcon;

        [SerializeField]
        public UIButton activeButton;

        [SerializeField]
        public Sprite toggleOnSprite;

        [SerializeField]
        public Sprite toggleOffSprite;

        public static UIRemoteListEntry CreatePrefab() {
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;

            UIStationStorage storageUIPrefab = stationWindow.storageUIPrefab;
            UIStationStorage src = GameObject.Instantiate<UIStationStorage>(storageUIPrefab);
            UIRemoteListEntry prefab = src.gameObject.AddComponent<UIRemoteListEntry>();
            prefab.itemImage = src.itemImage;

            GameObject.Destroy(src);

            GameObject go = prefab.gameObject;
            RectTransform rect = Util.NormalizeRectTopLeft(go);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + 14);
            GameObject.Destroy(go.transform.Find("bg/empty-tip")?.gameObject);
            GameObject.Destroy(go.transform.Find("storage-icon/take-button")?.gameObject);

            GameObject label = go.transform.Find("current-count-label/current-count-text")?.gameObject;
            if (label != null) {
                GameObject stationLabel = GameObject.Instantiate(label);
                stationLabel.name = "station-label";
                prefab.stationText = stationLabel.GetComponent<Text>();
                prefab.stationText.alignment = TextAnchor.MiddleLeft;
                stationLabel.transform.SetParent(go.transform, false);
                RectTransform rect2 = (RectTransform) stationLabel.transform;
                rect2.anchoredPosition = new Vector2(14f, 24f);

                GameObject starLabel = GameObject.Instantiate(label);
                starLabel.name = "star-label";
                prefab.starText = starLabel.GetComponent<Text>();
                prefab.starText.fontSize = 20;
                prefab.starText.alignment = TextAnchor.MiddleCenter;
                starLabel.transform.SetParent(go.transform, false);
                RectTransform rect3 = (RectTransform) starLabel.transform;
                rect3.anchoredPosition = new Vector2(-75, 4);

                GameObject planetLabel = GameObject.Instantiate(label);
                planetLabel.name = "planet-label";
                prefab.planetText = planetLabel.GetComponent<Text>();
                prefab.planetText.alignment = TextAnchor.MiddleCenter;
                planetLabel.transform.SetParent(go.transform, false);
                RectTransform rect4 = (RectTransform) planetLabel.transform;
                rect4.anchoredPosition = new Vector2(-75, -22);
            }

            //icon
            Transform bg = UIRoot.instance.uiGame.assemblerWindow.resetButton.transform.parent;
            if (bg != null) {
                GameObject activeBtn = GameObject.Instantiate(bg.gameObject);
                Transform btn = activeBtn.transform.Find("product-icon");
                if (btn != null) {
                    GameObject.Destroy(activeBtn.transform.Find("stop-btn")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("border")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("circle-fg-1")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("circle-fg")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("extra-circle-fg-1")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("extra-circle-fg")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("product-icon-1")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("cnt-text")?.gameObject);
                    GameObject.Destroy(activeBtn.transform.Find("cnt-text-1")?.gameObject);
                    GameObject.Destroy(activeBtn.GetComponent<Image>());

                    btn.gameObject.name = "check-icon";
                    prefab.activeButton = btn.GetComponent<UIButton>();
                    prefab.activeIcon = btn.GetComponent<Image>();
                    prefab.toggleOnSprite = UIRoot.instance.uiGame.mechaLab.toggleOnSprite;
                    prefab.toggleOffSprite = UIRoot.instance.uiGame.mechaLab.toggleOffSprite;
                    prefab.activeIcon.sprite = prefab.toggleOnSprite;
                    activeBtn.name = "active-btn";
                    activeBtn.transform.SetParent(go.transform, false);
                    RectTransform rect5 = (RectTransform)activeBtn.transform;
                    rect5.anchoredPosition = new Vector2(290f, 0f);
                    activeBtn.SetActive(true);
                }
            }

            GameObject.Destroy(go.transform.Find("current-count-label")?.gameObject);
            GameObject.Destroy(go.transform.Find("ordered-count-label")?.gameObject);
            GameObject.Destroy(go.transform.Find("max-count-label")?.gameObject);
            GameObject.Destroy(go.transform.Find("slider-bg")?.gameObject);

            go = prefab.gameObject;
            for (int i = go.transform.childCount - 1; i >= 0; --i) {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.name.Contains("button") || child.name.Contains("popup") || child.name.Contains("empty")) {
                    child.SetActive(false);
                    GameObject.Destroy(child);
                } else {
                    if (child.GetComponent<EventTrigger>() != null) {
                        GameObject.Destroy(child.GetComponent<EventTrigger>());
                    }
                    Vector3 lpos = child.transform.localPosition;
                    child.SetActive(true);
                    if (child.name == "storage-icon") {
                        child.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                        lpos.x = 36;
                        lpos.y = 30;
                        child.transform.localPosition = lpos;
                    }
                }
            }

            return prefab;
        }

        internal void Start() {
            activeButton.onClick += OnActiveButtonClick;
        }

        private void OnActiveButtonClick(int obj) {
            FilterValue value = FilterProcessor.Instance.GetValue(supply, demand);
            value.allowed = !value.allowed;
            FilterProcessor.Instance.SetValue(supply, demand, value);
            RefreshValue();
        }

        public void SetUpValues() {
            if (remoteType == ERemoteType.GasStub) {
                itemImage.sprite = UIFilterWindow.gasGiantSprite;
                itemImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            } else {
                ItemProto itemProto = LDB.items.Select(itemId);
                if (itemProto != null) {
                    itemImage.sprite = itemProto.iconSprite;
                }
            }

            int starId = planetId / 100;
            string distStr;
            float d = StarDistance.GetStarDistanceFromHere(starId);
            if (d > 0) {
                distStr = string.Format(" ({0:F1}ly)", d);
            } else {
                distStr = "";
            }
            StarData star = GameMain.galaxy.StarById(starId);
            starText.text = star?.displayName + distStr;

            PlanetData planet = GameMain.galaxy.PlanetById(planetId);
            planetText.text = planet?.displayName;

            if (station != null) {
                stationText.text = station.GetName();
            } else if (remoteType == ERemoteType.GasStub) {
                stationText.text = "Orbital Collection";
            } else {
                stationText.text = "";
            }

            RefreshValue();
         }

        private void RefreshValue() {
            FilterValue value = FilterProcessor.Instance.GetValue(supply, demand);
            if (value.allowed) {
                activeIcon.sprite = toggleOnSprite;
            } else {
                activeIcon.sprite = toggleOffSprite;
            }
        }

        public void OnPointerEnter(PointerEventData _eventData) {
        }

        public void OnPointerExit(PointerEventData _eventData) {
        }
    }
}

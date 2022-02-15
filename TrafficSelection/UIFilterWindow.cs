using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TrafficSelection {
    public class UIFilterWindow : ManualBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler {
        public RectTransform windowTrans;

        public StationComponent currentStation;
        public int itemId;
        public bool isPointEnter;
        private bool focusPointEnter;

        public UIListView remoteListView;
        private ELogisticStorage remoteType;

        public void SetUpAndOpen(StationComponent station, int index) {
            UIRoot.instance.uiGame.ShutPlayerInventory();
            stationText.text = station.GetName();
            if (station.storage[index].remoteLogic == ELogisticStorage.Demand) {
                remoteType = ELogisticStorage.Supply;
                supplyText.gameObject.SetActive(true);
                demandText.gameObject.SetActive(false);
            } else {
                remoteType = ELogisticStorage.Demand;
                supplyText.gameObject.SetActive(false);
                demandText.gameObject.SetActive(true);
            }

            currentStation = station;
            itemId = station.storage[index].itemId;
            if (itemId <= 0) {
                itemCircle.fillAmount = 0f;
                itemImage.sprite = defaultItemSprite;
                itemText.text = "";
            } else {
                itemCircle.fillAmount = 1f;
                ItemProto itemProto = LDB.items.Select(itemId);
                if (itemProto != null) {
                    itemImage.sprite = itemProto.iconSprite;
                    itemText.text = itemProto.name;
                }
            }

            SetUpData();

            base._Open();
            gameObject.SetActive(true);
            base.transform.SetAsLastSibling();
        }

        protected override void _OnCreate() {
            windowTrans = this.GetComponent<RectTransform>();
            windowTrans.sizeDelta = new Vector2(430, 640);
            CreateListViews();
            CreateUI();
        }

        protected override void _OnDestroy() {
        }

        protected override bool _OnInit() {
            windowTrans.anchoredPosition = new Vector2(-480, 0);
            return false;
        }

        protected override void _OnFree() {
        }

        protected override void _OnRegEvent() {
        }

        protected override void _OnUnregEvent() {
        }

        protected override void _OnOpen() {
        }

        protected override void _OnClose() {
        }

        protected override void _OnUpdate() {
            if (VFInput.escape && !UIRoot.instance.uiGame.starmap.active) {
                VFInput.UseEscape();
                Close();
            }
        }

        public void SetUpData() {
            _remoteList.Clear();
            _gasList.Clear();

            remoteListView.Clear();

            SetUpItemList();
            _remoteList.Sort((a, b) => a.distance - b.distance);

            AddToListView(remoteListView, 20, _remoteList);
        }

        internal void SetUpItemList() {
            HashSet<int> gasSupplyPlanets = new HashSet<int>();
            GalacticTransport galacticTransport = UIRoot.instance.uiGame.gameData.galacticTransport;
            StationComponent[] stationPool = galacticTransport.stationPool;
            int cursor = galacticTransport.stationCursor;

            for (int i = 1; i < cursor; i++) {
                if (stationPool[i] != null && stationPool[i].gid == i) {
                    StationComponent cmp = stationPool[i];
                    int length = cmp.storage.Length;
                    for (int j = 0; j < length; j++) {
                        if (!cmp.isStellar || cmp.storage[j].itemId != itemId || cmp.storage[j].remoteLogic != remoteType) {
                            continue;
                        }

                        if (cmp.isCollector) {
                            gasSupplyPlanets.Add(cmp.planetId);
                            AddStore(cmp, j, cmp.planetId, ERemoteType.Gas);
                        } else {
                            AddStore(cmp, j, cmp.planetId);
                        }
                        break;
                    }
                }
            }

            foreach (var gasPlanetId in gasSupplyPlanets) {
                AddStore(null, 0, gasPlanetId, ERemoteType.GasStub);
            }
        }

        internal List<RemoteData> _remoteList = new List<RemoteData>(200);
        internal List<RemoteData> _gasList = new List<RemoteData>(800);

        internal void AddStore(StationComponent station, int index, int planetId, ERemoteType remoteType = ERemoteType.Normal) {
            if (remoteType == ERemoteType.Gas) {
                return;
            }

            float distancef = StarDistance.GetStarDistanceFromHere(planetId / 100);
            int distance = (int) (distancef * 100);

            RemoteData d = new RemoteData() {
                station = station,
                index = index,
                planetId = planetId,
                distance = distance,
                remoteType = remoteType,
            };
            _remoteList.Add(d);
        }

        internal int AddToListView(UIListView listView, int count, List<RemoteData> list) {
            if (list.Count < count) {
                count = list.Count;
            }
            if (count == 0) {
                return count;
            }

            for (int i = 0; i < count; i++) {
                RemoteData d = list[0];
                list.RemoveAt(0);
                UIRemoteListEntry e = listView.AddItem<UIRemoteListEntry>();
                e.window = this;
                e.station = d.station;
                e.itemId = itemId;
                e.planetId = d.planetId;
                e.remoteType = d.remoteType;
                if (remoteType == ELogisticStorage.Demand) {
                    e.supply = FilterProcessor.GetIdentifier(currentStation, itemId);
                    e.demand = new RemoteIdentifier {
                        stationId = d.station == null ? -1 : d.station.gid,
                        planetId = d.planetId,
                        itemId = itemId,
                    };
                } else {
                    e.demand = FilterProcessor.GetIdentifier(currentStation, itemId);
                    e.supply = new RemoteIdentifier {
                        stationId = d.station == null ? -1 : d.station.gid,
                        planetId = d.planetId,
                        itemId = itemId,
                    };
                }
                e.SetUpValues();
            }
            return count;
        }

        public Text itemText;
        public UIButton itemButton;
        public Image itemImage;
        public Image itemCircle;
        public Text stationText;
        public Text supplyText;
        public Text demandText;

        public static Sprite defaultItemSprite = null;
        public static Sprite gasGiantSprite = null;

        internal void CreateListViews() {
            UIListView src = UIRoot.instance.uiGame.tutorialWindow.entryList;
            GameObject go = GameObject.Instantiate(src.gameObject);
            go.name = "list-view";

            RectTransform rect = Util.NormalizeRectCenter(go, 360, 444);
            rect.SetParent(windowTrans, false);
            rect.anchoredPosition = new Vector2(-5, -68);

            remoteListView = go.GetComponent<UIListView>();

            remoteListView.m_ItemRes.com_data = UIRemoteListEntry.CreatePrefab();
            Transform parent = remoteListView.m_ItemRes.gameObject.transform;
            Transform transform = remoteListView.m_ItemRes.com_data.gameObject.transform;

            for (int i = parent.childCount - 1; i >= 0; --i) {
                GameObject.Destroy(parent.GetChild(i).gameObject);
            }
            parent.DetachChildren();
            transform.SetParent(parent);

            InitListView(remoteListView);
        }

        internal void InitListView(UIListView listView) {
            listView.HorzScroll = false;
            listView.VertScroll = true;
            listView.m_ItemRes.sel_highlight = null;

            listView.CullOutsideItems = false;
            listView.ColumnSpacing = 0;
            listView.RowSpacing = 4;
            listView.m_ItemRes.com_data.gameObject.SetActive(true);

            Transform parent = listView.m_ItemRes.gameObject.transform;
            GameObject.Destroy(parent.GetComponent<Image>());
            GameObject.Destroy(parent.GetComponent<Button>());
            GameObject.Destroy(parent.GetComponent<UITutorialListEntry>());

            Transform transform = listView.m_ItemRes.com_data.gameObject.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            listView.RowHeight = (int)(transform as RectTransform).sizeDelta.y;
        }

        internal void CreateUI() {
            if (defaultItemSprite == null) {
                defaultItemSprite = Util.LoadSpriteResource("Icons/Tech/1414");
            }
            if (gasGiantSprite == null) {
                gasGiantSprite = Util.LoadSpriteResource("Icons/Tech/1606");
            }

            GameObject go;
            Transform bg;
            RectTransform rect;
            UIAssemblerWindow assemblerWindow = UIRoot.instance.uiGame.assemblerWindow;

            //icon
            bg = assemblerWindow.resetButton.transform.parent;
            if (bg != null) {
                go = GameObject.Instantiate(bg.gameObject);
                Transform btn = go.transform.Find("product-icon");
                if (btn != null) {
                    go.transform.Find("cnt-text")?.gameObject.SetActive(false);
                    GameObject.Destroy(go.transform.Find("stop-btn")?.gameObject);
                    GameObject.Destroy(go.transform.Find("circle-fg-1")?.gameObject);
                    GameObject.Destroy(go.transform.Find("product-icon-1")?.gameObject);
                    GameObject.Destroy(go.transform.Find("cnt-text-1")?.gameObject);

                    itemButton = btn.GetComponent<UIButton>();
                    itemCircle = go.transform.Find("circle-fg")?.GetComponent<Image>();
                    itemCircle.color = Util.DSPBlue;
                    itemImage = btn.GetComponent<Image>();
                    itemImage.sprite = defaultItemSprite;
                    rect = Util.NormalizeRectCenter(go);
                    rect.SetParent(windowTrans, false);
                    rect.anchoredPosition = new Vector2(-140f, 224f);
                    go.name = "item-button";
                    go.SetActive(true);
                }
            }

            //demand supply label
            Text stateText = assemblerWindow.stateText;

            go = GameObject.Instantiate(stateText.gameObject);
            go.name = "supply-label";
            supplyText = go.GetComponent<Text>();
            supplyText.text = "Supply".Translate();
            supplyText.color = Util.DSPBlue;
            rect = Util.NormalizeRectCenter(go);
            rect.SetParent(windowTrans, false);
            rect.sizeDelta = new Vector2(80, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(-185f, 168f);

            go = GameObject.Instantiate(go, windowTrans);
            go.name = "demand-label";
            demandText = go.GetComponent<Text>();
            demandText.text = "Demand".Translate();
            demandText.color = Util.DSPOrange;

            //name
            Text titleText = gameObject.transform.Find("panel-bg/title-text")?.gameObject.GetComponent<Text>();
            if (titleText != null) {
                go = GameObject.Instantiate(titleText.gameObject);
                go.name = "item-name";
                itemText = go.GetComponent<Text>();
                itemText.fontSize = 20;
                itemText.alignment = TextAnchor.MiddleCenter;

                rect = Util.NormalizeRectCenter(go);
                rect.SetParent(windowTrans, false);
                rect.sizeDelta = new Vector2(200f, rect.sizeDelta.y);
                rect.anchoredPosition = new Vector2(30f, 210f);
                go.SetActive(true);

                go = GameObject.Instantiate(go, windowTrans);
                rect.anchoredPosition = new Vector2(30f, 240f);
                go.name = "station-name";
                stationText = go.GetComponent<Text>();
                stationText.resizeTextForBestFit = true;
            }

            //frame
            Transform transform = UIRoot.instance.uiGame.inventory.transform;
            bg = transform.Find("content-bevel-bg");
            if (bg != null) {
                go = GameObject.Instantiate(bg.gameObject);
                rect = (RectTransform) go.transform;

                go.transform.SetParent(windowTrans, false);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMax = new Vector2(-26f, -160f);
                rect.offsetMin = new Vector2(24f, 26f);

                go.transform.SetSiblingIndex(2);
            }
        }

        protected override void _OnLateUpdate() {
        }

        public void OnPointerEnter(PointerEventData _eventData) {
            this.isPointEnter = true;
        }

        public void OnPointerExit(PointerEventData _eventData) {
            this.isPointEnter = false;
        }

        public void OnApplicationFocus(bool focus) {
            if (!focus) {
                this.focusPointEnter = this.isPointEnter;
                this.isPointEnter = false;
            } else {
                this.isPointEnter = this.focusPointEnter;
            }
        }

        public void Close() {
            base._Close();
            gameObject.SetActive(false);
        }

        public static UIFilterWindow CreateWindow(string name, string title = "") {
            var srcWin = UIRoot.instance.uiGame.tankWindow;
            GameObject src = srcWin.gameObject;
            GameObject go = GameObject.Instantiate(src, srcWin.transform.parent);
            go.name = name;
            go.SetActive(false);
            GameObject.Destroy(go.GetComponent<UITankWindow>());
            UIFilterWindow win = go.AddComponent<UIFilterWindow>();

            for (int i = 0; i < go.transform.childCount; i++) {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.name == "panel-bg") {
                    Button btn = child.GetComponentInChildren<Button>();
                    if (btn != null) {
                        btn.onClick.AddListener(win.Close);
                    }
                } else if (child.name != "shadow") {
                    GameObject.Destroy(child);
                }
            }

            Text text = win.gameObject.transform.Find("panel-bg/title-text")?.gameObject.GetComponent<Text>();
            if (text != null) {
                text.text = title;
            }

            win._Create();
            win._Init(win.data);
            return win;
        }
    }

    public enum ERemoteType {
        Normal,
        Gas,
        GasStub,
    }

    public struct RemoteData {
        public StationComponent station;
        public int index;
        public int planetId;
        public int itemId;
        public int distance;
        public ERemoteType remoteType;
    }
}
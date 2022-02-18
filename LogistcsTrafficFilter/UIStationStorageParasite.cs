using UnityEngine;
using UnityEngine.UI;

namespace LogisticsTrafficFilter {
    public class UIStationStorageParasite : MonoBehaviour {
        public static UIFilterWindow win;

        public UIStationStorage uiStorage;
 
        [SerializeField] public UIButton filterBtn;

        internal void Start() {
            if (filterBtn != null) {
                filterBtn.onClick += OpenFilter;
            }
        }

        public void RefreshValues() {
            if (uiStorage.station == null || uiStorage.index >= uiStorage.station.storage.Length || uiStorage.station.storage[uiStorage.index].itemId <= 0 || !uiStorage.station.isStellar) {
                filterBtn.gameObject.SetActive(false);
                return;
            }

            if (uiStorage.station.storage[uiStorage.index].remoteLogic == ELogisticStorage.Demand) {
                filterBtn.gameObject.SetActive(true);
                Image img = filterBtn.gameObject.transform.Find("icon")?.gameObject.GetComponent<Image>();
                if (img != null) {
                    img.color = Util.DSPBlue;
                }
            } else if (uiStorage.station.storage[uiStorage.index].remoteLogic == ELogisticStorage.Supply) {
                filterBtn.gameObject.SetActive(true);
                Image img = filterBtn.gameObject.transform.Find("icon")?.gameObject.GetComponent<Image>();
                if (img != null) {
                    img.color = Util.DSPOrange;
                }
            } else {
                filterBtn.gameObject.SetActive(false);
            }
        }

        public void OpenFilter(int obj) {
            win.SetUpAndOpen(uiStorage.station, uiStorage.index);
        }

        public static UIStationStorageParasite MakeUIStationStorageParasite(UIStationStorage stationStorage) {
            GameObject parent = stationStorage.gameObject;
            GameObject go = new GameObject("traffic-selection-filter");

            UIStationStorageParasite parasite = parent.AddComponent<UIStationStorageParasite>();
            go.transform.parent = parent.transform;
            go.transform.localPosition = new Vector3(409, -58, 0);
            go.transform.localScale = new Vector3(1, 1, 1);

            Sprite s = Util.LoadSpriteResource("ui/textures/sprites/icons/filter-icon");
            parasite.filterBtn = Util.MakeIconButton(go.transform, s, 0, 0);

            if (parasite.filterBtn != null) {
                parasite.filterBtn.gameObject.name = "traffic-selection-filter-button";
                parasite.uiStorage = stationStorage;
            }

            return parasite;
        }
    }
}

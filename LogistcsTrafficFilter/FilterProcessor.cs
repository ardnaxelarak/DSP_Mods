using NebulaAPI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LogisticsTrafficFilter {
    [Serializable]
    public struct StationIdentifier {
        public int stationId;
        public int planetId;
        public int itemId;
    }

    [Serializable]
    public struct FilterPair {
        public StationIdentifier supply;
        public StationIdentifier demand;

        public string ToDebugString() {
            return "<" + supply.stationId + " " + supply.planetId + " " + supply.itemId + " -> " + demand.stationId + " " + demand.planetId + " " + demand.itemId + ">";
        }

        public void Write(BinaryWriter writer) {
            writer.Write(supply.stationId);
            writer.Write(supply.planetId);
            writer.Write(supply.itemId);
            writer.Write(demand.stationId);
            writer.Write(demand.planetId);
            writer.Write(demand.itemId);
        }

        public static FilterPair Read(BinaryReader reader) {
            return new FilterPair {
                supply = new StationIdentifier {
                    stationId = reader.ReadInt32(),
                    planetId = reader.ReadInt32(),
                    itemId = reader.ReadInt32(),
                },
                demand = new StationIdentifier {
                    stationId = reader.ReadInt32(),
                    planetId = reader.ReadInt32(),
                    itemId = reader.ReadInt32(),
                },
            };
        }
    }

    [Serializable]
    public struct FilterValue {
        public bool allowed;

        public void Write(BinaryWriter writer) {
            writer.Write(JsonUtility.ToJson(this));
        }

        public static FilterValue Read(BinaryReader reader) {
            return JsonUtility.FromJson<FilterValue>(reader.ReadString());
        }
    }

    public class FilterProcessor {
        public const int saveVersion = 1;

        private static FilterProcessor _instance;

        private readonly Dictionary<FilterPair, FilterValue> filters;

        private FilterProcessor() {
            filters = new Dictionary<FilterPair, FilterValue>();
        }

        public FilterValue GetValue(FilterPair pair) {
            if (!filters.ContainsKey(pair)) {
                filters[pair] = new FilterValue { allowed = true };
            }
            return filters[pair];
        }

        public FilterValue GetValue(StationIdentifier supply, StationIdentifier demand) {
            FilterPair pair = new FilterPair {
                supply = supply,
                demand = demand,
            };
            return GetValue(pair);
        }

        public FilterValue GetValue(StationComponent supply, StationComponent demand, int itemId) {
            FilterPair pair = new FilterPair {
                supply = GetIdentifier(supply, itemId),
                demand = GetIdentifier(demand, itemId),
            };
            return GetValue(pair);
        }

        public static StationIdentifier GetIdentifier(StationComponent station, int itemId) {
            return new StationIdentifier {
                stationId = station.isCollector ? -1 : station.gid,
                planetId = station.planetId,
                itemId = itemId,
            };
        }

        private void UpdateStations(FilterPair pair) {
            GameData gameData = GameMain.data;
            if (gameData == null) {
                return;
            }
            GalacticTransport galacticTransport = gameData.galacticTransport;
            int shipCarries = gameData.history.logisticShipCarries;
            int cursor = galacticTransport.stationCursor;

            HashSet<int> toUpdate = new HashSet<int>();

            for (int i = 1; i < cursor; i++) {
                StationComponent station = galacticTransport.stationPool[i];
                if (station == null) {
                    continue;
                }
                if ((station.isCollector && station.planetId == pair.supply.planetId) || pair.supply.stationId == i || pair.demand.stationId == i) {
                    toUpdate.Add(i);
                }
            }

            foreach (int gid in toUpdate) {
                galacticTransport.stationPool[gid].ClearRemotePairs();
            }

            foreach (int gid in toUpdate) {
                StationComponent station = galacticTransport.stationPool[gid];
                station.RematchRemotePairs(galacticTransport.stationPool, cursor, 0, shipCarries);
            }
        }

        private void UpdateAllStations() {
            GameData gameData = GameMain.data;
            if (gameData == null) {
                return;
            }
            GalacticTransport galacticTransport = gameData.galacticTransport;
            int shipCarries = gameData.history.logisticShipCarries;
            int cursor = galacticTransport.stationCursor;

            for (int i = 1; i < cursor; i++) {
                galacticTransport.stationPool[i].ClearRemotePairs();
            }

            for (int i = 1; i < cursor; i++) {
                StationComponent station = galacticTransport.stationPool[i];
                station.RematchRemotePairs(galacticTransport.stationPool, cursor, 0, shipCarries);
            }
        }

        public void SetValue(FilterPair pair, FilterValue value) {
            filters[pair] = value;
            if (NebulaModAPI.IsMultiplayerActive) {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<FilterPacket>(new FilterPacket(pair, value));
            }
            UpdateStations(pair);
        }

        public void SetValue(StationIdentifier supply, StationIdentifier demand, FilterValue value) {
            FilterPair pair = new FilterPair {
                supply = supply,
                demand = demand,
            };
            SetValue(pair, value);
        }

        public void WriteSerialization(BinaryWriter writer) {
            writer.Write(filters.Count);
            foreach (KeyValuePair<FilterPair, FilterValue> kvp in filters) {
                kvp.Key.Write(writer);
                kvp.Value.Write(writer);
            }
        }

        public void ReadSerialization(BinaryReader reader, int saveVersion = saveVersion, bool fullList = true) {
            if (fullList) {
                filters.Clear();
            }
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                FilterPair pair = FilterPair.Read(reader);
                FilterValue value = FilterValue.Read(reader);
                filters[pair] = value;
            }
            UpdateAllStations();
        }

        public void Clear() {
            filters.Clear();
            UpdateAllStations();
        }

        public static FilterProcessor Instance {
            get {
                if (_instance == null) {
                    _instance = new FilterProcessor();
                }
                return _instance;
            }
        }
    }
}
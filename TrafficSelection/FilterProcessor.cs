using NebulaAPI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TrafficSelection {
    [Serializable]
    public struct RemoteIdentifier {
        public int stationId;
        public int planetId;
        public int itemId;
    }

    [Serializable]
    public struct FilterPair {
        public RemoteIdentifier supply;
        public RemoteIdentifier demand;

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
                supply = new RemoteIdentifier {
                    stationId = reader.ReadInt32(),
                    planetId = reader.ReadInt32(),
                    itemId = reader.ReadInt32(),
                },
                demand = new RemoteIdentifier {
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

        public FilterValue GetValue(RemoteIdentifier supply, RemoteIdentifier demand) {
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

        public static RemoteIdentifier GetIdentifier(StationComponent station, int itemId) {
            return new RemoteIdentifier {
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

            if (pair.demand.stationId > 0) {
                StationComponent station = galacticTransport.stationPool[pair.demand.stationId];
                station.ClearRemotePairs();
                station.RematchRemotePairs(galacticTransport.stationPool, galacticTransport.stationCursor, pair.supply.stationId > 0 ? pair.supply.stationId : 0, shipCarries);
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
                StationComponent station = galacticTransport.stationPool[i];
                station.ClearRemotePairs();
                station.RematchRemotePairs(galacticTransport.stationPool, galacticTransport.stationCursor, 0, shipCarries);
            }
        }

        public void SetValue(FilterPair pair, FilterValue value) {
            filters[pair] = value;
            if (NebulaModAPI.IsMultiplayerActive) {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<FilterPacket>(new FilterPacket(pair, value));
            }
            UpdateStations(pair);
        }

        public void SetValue(RemoteIdentifier supply, RemoteIdentifier demand, FilterValue value) {
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

        public void ReadSerialization(BinaryReader reader, bool fullList = true) {
            if (fullList) {
                filters.Clear();
            }
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                FilterPair pair = FilterPair.Read(reader);
                FilterValue value = FilterValue.Read(reader);
                filters[pair] = value;
            }
            Debug.Log("Read " + count + " filter rules");
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
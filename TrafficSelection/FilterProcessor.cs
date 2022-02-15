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
    }

    [Serializable]
    public struct FilterValue {
        public bool allowed;
    }

    public class FilterProcessor {
        private static FilterProcessor _instance;

        private Dictionary<FilterPair, FilterValue> filters;

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

        public void SetValue(FilterPair pair, FilterValue value) {
            filters[pair] = value;
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
                writer.Write(kvp.Key.supply.stationId);
                writer.Write(kvp.Key.supply.planetId);
                writer.Write(kvp.Key.supply.itemId);
                writer.Write(kvp.Key.demand.stationId);
                writer.Write(kvp.Key.demand.planetId);
                writer.Write(kvp.Key.demand.itemId);
                writer.Write(JsonUtility.ToJson(kvp.Value));
            }
        }

        public void ReadSerialization(BinaryReader reader) {
            filters.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                FilterPair pair = new FilterPair {
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
                FilterValue value = JsonUtility.FromJson<FilterValue>(reader.ReadString());
                filters[pair] = value;
            }
        }

        public void Clear() {
            filters.Clear();
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
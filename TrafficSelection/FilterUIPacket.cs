using NebulaAPI;
using System.Collections.Generic;

namespace TrafficSelection {
    public class FilterUIRequestPacket {
        public int StationId { get; set; }
        public int ItemId { get; set; }
        public bool ShowSuppliers { get; set; }
    }

    public class FilterUIResponsePacket {
        public int[] StationIds { get; set; }
        public int[] GasPlanetIds { get; set; }
    }

    [RegisterPacketProcessor]
    public class FilterUIRequestPacketProcessor : BasePacketProcessor<FilterUIRequestPacket> {
        public override void ProcessPacket(FilterUIRequestPacket packet, INebulaConnection conn) {
            HashSet<int> gasSupplyPlanets = new HashSet<int>();
            List<int> remoteStations = new List<int>();

            ELogisticStorage remoteType = packet.ShowSuppliers ? ELogisticStorage.Supply : ELogisticStorage.Demand;
            GalacticTransport galacticTransport = GameMain.data.galacticTransport;
            StationComponent[] stationPool = galacticTransport.stationPool;
            int cursor = galacticTransport.stationCursor;

            for (int i = 1; i < cursor; i++) {
                if (stationPool[i] != null && stationPool[i].gid == i) {
                    StationComponent cmp = stationPool[i];
                    int length = cmp.storage.Length;
                    for (int j = 0; j < length; j++) {
                        if (!cmp.isStellar || cmp.storage[j].itemId != packet.ItemId || cmp.storage[j].remoteLogic != remoteType) {
                            continue;
                        }

                        if (cmp.isCollector) {
                            gasSupplyPlanets.Add(cmp.planetId);
                        } else {
                            remoteStations.Add(cmp.gid);
                        }
                        break;
                    }
                }
            }

            List<int> gasList = new List<int>();
            foreach (int planetId in gasSupplyPlanets) {
                gasList.Add(planetId);
            }

            FilterUIResponsePacket response = new FilterUIResponsePacket {
                StationIds = remoteStations.ToArray(),
                GasPlanetIds = gasList.ToArray(),
            };

            conn.SendPacket<FilterUIResponsePacket>(response);
        }
    }

    [RegisterPacketProcessor]
    public class FilterUIResponsePacketProcessor : BasePacketProcessor<FilterUIResponsePacket> {
        public override void ProcessPacket(FilterUIResponsePacket packet, INebulaConnection conn) {
            UIFilterWindow.instance.SetUpItemList(packet);
        }
    }
}
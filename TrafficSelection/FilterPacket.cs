using NebulaAPI;

namespace TrafficSelection {
    public class FilterPacket {
        public bool FullList { get; set; }
        public byte[] Data { get; set; }

        public FilterPacket() { }
        
        public FilterPacket(FilterProcessor processor) {
            FullList = true;

            using IWriterProvider p = NebulaModAPI.GetBinaryWriter();
            processor.WriteSerialization(p.BinaryWriter);
            Data = p.CloseAndGetBytes();
        }

        public FilterPacket(FilterPair pair, FilterValue value) {
            FullList = false;

            using IWriterProvider p = NebulaModAPI.GetBinaryWriter();
            pair.Write(p.BinaryWriter);
            value.Write(p.BinaryWriter);
            Data = p.CloseAndGetBytes();
        }
    }

    [RegisterPacketProcessor]
    public class FilterPacketProcessor : BasePacketProcessor<FilterPacket> {
        public override void ProcessPacket(FilterPacket packet, INebulaConnection conn) {
            using IReaderProvider p = NebulaModAPI.GetBinaryReader(packet.Data);
            FilterProcessor.Instance.ReadSerialization(p.BinaryReader, packet.FullList);

            UIFilterWindow.instance.RefreshValues();

            if (IsHost) {
                NebulaModAPI.MultiplayerSession.Network.SendPacket<FilterPacket>(packet);
            }
        }
    }
}
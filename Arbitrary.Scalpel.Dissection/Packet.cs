using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Arbitrary.Scalpel.Dissection.Extentions;

namespace Arbitrary.Scalpel.Dissection
{
    [IdentifierAuto]
    public enum LinkLayer : byte
    {
        Null =            0,
        Ethernet =        1,
        PPP =             9,
        Raw =           101,
        IEEE80211 =     105,
        Loop =          108,
        SLL =           113,
        RadioTap =      127,
    }

    [IdentifierAuto]
    public enum EthernetType : ushort
    {
        None =  0x0000,
        Loop =  0x0060,
        IPv4 =  0x0800,
        ARP =   0x0806,
        IPv6 =  0x86dd,
    }

    [IdentifierAuto]
    public enum IPVersion : byte
    {
        IPv4 = 0x04,
        IPv6 = 0x06,
    }

    [IdentifierAuto]
    public enum ProtocolType : byte
    {
        ICMP =                                  0x01,
        IPv4 =                                  0x04,
        TCP =                                   0x06,
        UDP =                                   0x11,
        IPv6 =                                  0x29,
        [Identifier("ipv6_routing")]
        IPv6RoutingHeader =                     0x2b,
        [Identifier("ipv6_fragment")]
        IPv6FragmentHeader =                    0x2c,
        [Identifier("ipsec_esp")]
        IPSecEncapsulatingSecurityPayload =     0x32,
        [Identifier("ipsec_ah")]
        IPSecAuthenticationHeader =             0x33,
        ICMPv6 =                                0x3a,
        [Identifier("ipv6_no_next")]
        IPv6NoNextHeader =                      0x3b,
        [Identifier("ipv6_dest_options")]
        IPv6DestinationOptions =                0x3c,
        Raw =                                   0xff,
    }

    public enum ECNStatus : byte
    {
        [Identifier("non_ect")]
        NonECT =    0b00,
        [Identifier("ect")]
        [Identifier("ect0")]
        ECT0 =      0b01,
        [Identifier("ect")]
        [Identifier("ect1")]
        ECT1 =      0b10,
        [Identifier("ce")]
        CE =        0b11,
    }

    [Flags]
    public enum IPv4Flags : byte
    {
        [Identifier("zero")]
        Zero =              0b000,
        [Identifier("more_frags")]
        MoreFragments =     0b001,
        [Identifier("dont_frag")]
        DontFragment =      0b010,
        [Identifier("evil")]
        Evil =              0b100,
    }

    [Flags]
    [IdentifierAuto]
    public enum TCPFlags : ushort
    {
        None =      0b000_000000000,
        FIN =       0b000_000000001,
        SYN =       0b000_000000010,
        RST =       0b000_000000100,
        PSH =       0b000_000001000,
        ACK =       0b000_000010000,
        URG =       0b000_000100000,
        ECE =       0b000_001000000,
        CWR =       0b000_010000000,
        NS =        0b000_100000000,
        RSVD0 =     0b001_000000000,
        RSVD1 =     0b010_000000000,
        RSVD2 =     0b100_000000000,
        All =       0b111_111111111,
    }

    [Identifier("udp")]
    public sealed class UDPPacket : TransportPacket
    {
        protected override int MinimumHeaderLength => 0x08;

        public override ushort Source
        {
            get => BitConverter
                .ToUInt16(Data.Span)
                .ByteSwap();
        }

        public override ushort Destination
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x02).Span)
                .ByteSwap();
        }

        public ushort Length
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x04).Span)
                .ByteSwap();
        }

        public override ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x06).Span)
                .ByteSwap();
        }

        public UDPPacket(
            ReadOnlyMemory<byte> data,
            Packet parent)
            : base(data, parent)
        { 
            if (data.Length <= MinimumHeaderLength)
                return;
            PayloadData = Data.Slice(0x08);
        }
    }

    [Identifier("tcp")]
    public sealed class TCPPacket : TransportPacket
    {
        protected override int MinimumHeaderLength => 0x14;

        public override ushort Source
        {
            get => BitConverter
                .ToUInt16(Data.Span)
                .ByteSwap();
        }

        public override ushort Destination
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x02).Span)
                .ByteSwap();
        }

        [Identifier("sequence")]
        public uint Sequence
        {
            get => BitConverter
                .ToUInt32(Data
                    .Slice(0x04).Span)
                .ByteSwap();
        }

        [Identifier("ack")]
        public uint Acknowledgment
        {
            get => BitConverter
                .ToUInt32(Data
                    .Slice(0x04).Span)
                .ByteSwap();
        }

        [Identifier("data_offset")]
        public byte DataOffset
        {
            get => (byte)(Data.Span[0x0c]>>0x04);
        }

        [Identifier("header_length")]
        public int HeaderLength
        {
            get => DataOffset * sizeof(int);
        }

        [Identifier("flags")]
        public TCPFlags Flags
        {
            get => (TCPFlags)(BitConverter
                .ToUInt16(Data
                    .Slice(0x0c).Span)
                .ByteSwap()
                & (ushort)TCPFlags.All);
        }

        [Identifier("window")]
        public ushort WindowSize
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x0e).Span)
                .ByteSwap();
        }

        public override ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x10).Span)
                .ByteSwap();
        }

        [Identifier("urgent_pointer")]
        public ushort UrgentPointer
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x12).Span)
                .ByteSwap();
        }

        public TCPPacket(
            ReadOnlyMemory<byte> data,
            Packet parent)
            : base(data, parent)
        {
            var header_length = HeaderLength;
            if (data.Length <= header_length)
                return;
            var PayloadData = data.Slice(header_length);
        }
    }

    public abstract class TransportPacket : Packet
    {
        [Identifier("port")]
        [Identifier("source")]
        public abstract ushort Source { get; }
        [Identifier("port")]
        [Identifier("dest")]
        public abstract ushort Destination { get; }
        [Identifier("checksum")]
        public abstract ushort Checksum { get; }

        protected TransportPacket(
            ReadOnlyMemory<byte> data,
            Packet parent)
            : base(data, parent)
        { }
    }

    [Identifier("icmp")]
    public sealed class ICMPv4Packet : InternetPacket
    {
        protected override int MinimumHeaderLength => 0x08;

        [Identifier("typecode")]
        public ICMPv4TypeCode TypeCode
        {
            get => BitConverter
                .ToUInt16(Data.Span);
        }

        [Identifier("checksum")]
        public ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x02).Span)
                .ByteSwap();
        }

        [Identifier("rest_of_header")]
        public ReadOnlyMemory<byte> RestOfHeader
        {
            get => Data.Slice(0x04, 0x04);
        }

        [Identifier("id")]
        public ushort Identifier
        {
            get => BitConverter
                .ToUInt16(RestOfHeader.Span)
                .ByteSwap();
        }

        [Identifier("sequence")]
        public ushort Sequence
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x06).Span)
                .ByteSwap();
        }

        public ICMPv4Packet(
            ReadOnlyMemory<byte> data,
            Packet parent = null)
            : base(data, parent)
        { 
            if (data.Length <= MinimumHeaderLength)
                return;
            PayloadData = Data.Slice(0x08);
        }
    }

    [Identifier("ipv4")]
    public sealed class IPv4Packet : IPPacket
    {
        protected override int MinimumHeaderLength => 0x14;

        public override int HeaderLength
        {
            get => InternetHeaderLength * sizeof(int);
        }
        [Identifier("ihl")]
        public byte InternetHeaderLength 
        {
            get => unchecked((byte)(Data.Span[0] & 0x0f));
        }
        public override byte TrafficClass
        {
            get => Data.Span[0x01];
        }
        [Identifier("total_length")]
        public uint TotalLength
        { 
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x02).Span)
                .ByteSwap();
        }
        [Identifier("id")]
        public ushort Identification
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x04).Span)
                .ByteSwap();
        }
        [Identifier("flags")]
        public IPv4Flags Flags
        {
            get => (IPv4Flags)(Data.Span[0x06]>>0x05);
        }
        [Identifier("frag_offset")]
        public ushort FragmentOffset
        {
            get => unchecked((ushort)(BitConverter
                .ToUInt16(Data
                    .Slice(0x04).Span) & 0b0001111111111111)
                .ByteSwap());
        }
        public override byte TimeToLive
        {
            get => Data.Span[0x08];
        }
        public override ProtocolType Protocol 
        { 
            get => (ProtocolType)Data.Span[0x09];
        }
        [Identifier("checksum")]
        public ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Data
                    .Slice(0x0a).Span)
                .ByteSwap();
        }
        public override IPAddress Source 
        {
            get => ParseIPAddress(0x0c);
            // get => new IPAddress(BitConverter
            //     .ToInt32(Data
            //         .Slice(0x0c).Span));
        }
        public override IPAddress Destination 
        {
            get => ParseIPAddress(0x10);
            // get => new IPAddress(BitConverter
            //     .ToInt32(Data
            //         .Slice(0x10).Span));
        }
        
        public IPv4Packet(
            ReadOnlyMemory<byte> data,
            Packet parent = null)
            : base(data, parent)
        {
            var header_length = HeaderLength;
            if (data.Length <= header_length)
                return;
            var PayloadData = data.Slice(header_length);
            switch (Protocol)
            {
                case ProtocolType.ICMP:
                    Payload = new ICMPv4Packet(PayloadData, this);
                    return;
                case ProtocolType.TCP:
                    Payload = new TCPPacket(PayloadData, this);
                    return;
                case ProtocolType.UDP:
                    Payload = new UDPPacket(PayloadData, this);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    [Identifier("ip")]
    public abstract class IPPacket : InternetPacket
    {
        [Identifier("version")]
        public IPVersion Version 
        { 
            get => (IPVersion)(Data.Span[0]>>0x04); 
        }
        [Identifier("header_length")]
        public abstract int HeaderLength { get; }
        [Identifier("traffic_class")]
        public abstract byte TrafficClass { get; }
        [Identifier("ds")]
        public int DifferentiatedServices => TrafficClass>>0x02;
        [Identifier("ecn")]
        public ECNStatus ECN => (ECNStatus)(TrafficClass & 0x02);
        [Identifier("protocol")]
        public abstract ProtocolType Protocol { get; }
        public ProtocolType NextHeader => Protocol;
        [Identifier("ttl")]
        public abstract byte TimeToLive { get; }
        public byte HopLimit => TimeToLive;
        [Identifier("address")]
        [Identifier("source")]
        public abstract IPAddress Source { get; }
        [Identifier("address")]
        [Identifier("dest")]
        public abstract IPAddress Destination { get; }

        protected IPAddress ParseIPAddress(int offset)
            => new IPAddress(Data
                .Slice(offset,
                    Version == IPVersion.IPv4
                        ? 0x04
                    : Version == IPVersion.IPv6
                        ? 0x10
                    : throw new InvalidOperationException()).Span);
        
        protected IPPacket(
            ReadOnlyMemory<byte> data, 
            Packet parent = null)
            : base(data, parent)
        { }
    }

    public abstract class InternetPacket : Packet
    { 
        protected InternetPacket(
            ReadOnlyMemory<byte> data, 
            Packet parent = null)
            : base(data, parent)
        { }
    }

    [Identifier("eth")]
    public sealed class EthernetPacket : Packet
    {
        private const int AddressLength = 6;
        private const int DestinationOffset = 0;
        private const int SourceOffset = AddressLength;
        private const int TypeOffset = AddressLength * 2;
        private const int HeaderLength = TypeOffset + sizeof(EthernetType);
        protected override int MinimumHeaderLength => HeaderLength;

        [Identifier("address")]
        [Identifier("source")]
        public PhysicalAddress Source 
        { 
            get
            {
                var bytes = Data
                    .Slice(SourceOffset, AddressLength)
                    .ToArray();
                Array.Reverse(bytes);
                return new PhysicalAddress(bytes);
            }
        }

        [Identifier("address")]
        [Identifier("dest")]
        public PhysicalAddress Destination 
        { 
            get
            {
                var bytes = Data
                    .Slice(DestinationOffset, AddressLength)
                    .ToArray();
                Array.Reverse(bytes);
                return new PhysicalAddress(bytes);
            }
        }

        [Identifier("type")]
        public EthernetType Type 
        { 
            get => (EthernetType)(BitConverter
                .ToUInt16(Data
                    .Slice(TypeOffset).Span)
                .ByteSwap());
        }

        public EthernetPacket(
            ReadOnlyMemory<byte> data, 
            Packet parent = null)
            : base(data, parent)
        { 
            if (data.Length <= MinimumHeaderLength)
                return;
            PayloadData = data.Slice(0x0e);
            // we'll end up doing this twice arrrg
            switch (Type)
            {
                case EthernetType.IPv4:
                    Payload = new IPv4Packet(PayloadData, this);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public abstract class Packet
    {
        protected abstract int MinimumHeaderLength { get; }
        public readonly ReadOnlyMemory<byte> Data;
        public readonly Packet Parent;
        public virtual Packet Payload { get; protected set; }
        [Identifier("payload")]
        public virtual ReadOnlyMemory<byte> PayloadData 
            { get; protected set; }

        protected Packet(ReadOnlyMemory<byte> data, Packet parent = null)
        {
            Data = data.Length < MinimumHeaderLength
                ? throw new ArgumentException(nameof(data))
                : data;
            Parent = parent;
        }

        public static Packet Parse(LinkLayer link_layer, byte[] data)
        {
            switch (link_layer)
            {
                case LinkLayer.Ethernet:
                    return new EthernetPacket(
                        new ReadOnlyMemory<byte>(data));
                case LinkLayer.Raw:
                default:
                    throw new NotImplementedException(nameof(link_layer));
            }
        }
    }
}

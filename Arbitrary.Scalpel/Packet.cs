using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Arbitrary.Scalpel.Extentions;

namespace Arbitrary.Scalpel
{
    [AttributeUsage(0
        | AttributeTargets.Class
        | AttributeTargets.Property
        | AttributeTargets.Field, 
        AllowMultiple = true)]
    public class Identifier : Attribute
    {
        public readonly string Text;
        public Identifier(string text)
            => Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException(nameof(text))
                : text;
    }
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public class IdentifierAuto : Attribute
    { }

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
        Echo =  0x0200,
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
        UCP =                                   0x11,
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
    public enum IPv4Flag : byte
    {
        [Identifier("zero")]
        Zero =              0b000,
        [Identifier("dont_frag")]
        DontFragment =      0b001,
        [Identifier("more_frags")]
        MoreFragments =     0b010,
        [Identifier("evil")]
        Evil =              0b100,
    }

    [Identifier("icmp")]
    public sealed class ICMPv4Packet : InternetPacket
    {
        [Identifier("typecode")]
        public ICMPv4TypeCode TypeCode
        {
            get => BitConverter
                .ToUInt16(Segment
                    .Slice(0x02, sizeof(ushort))
                    .Span);
        }

        [Identifier("checksum")]
        public ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Segment
                    .Slice(0x02, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }

        [Identifier("rest_of_header")]
        public ReadOnlyMemory<byte> RestOfHeader
        {
            get => Segment.Slice(0x04, 0x04);
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
                .ToUInt16(Segment
                    .Slice(0x06, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }

        public ICMPv4Packet(ReadOnlyMemory<byte> segment)
            : base(segment)
        { }
    }

    [Identifier("ipv4")]
    public sealed class IPv4Packet : IPPacket
    {
        private const int MinimumHeaderLength = 0x14;

        public override int HeaderLength
        {
            get => InternetHeaderLength<<2;
        }
        [Identifier("ihl")]
        public byte InternetHeaderLength 
        {
            get => unchecked((byte)(Segment.Span[0] & 0x0f));
        }
        public override byte TrafficClass
        {
            get => Segment.Span[0x01];
        }
        [Identifier("total_length")]
        public uint TotalLength
        { 
            get => BitConverter
                .ToUInt16(Segment
                    .Slice(0x02, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }
        [Identifier("id")]
        public ushort Identification
        {
            get => BitConverter
                .ToUInt16(Segment
                    .Slice(0x04, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }
        [Identifier("flags")]
        public IPv4Flag Flags
        {
            get => (IPv4Flag)(Segment.Span[0x06]>>0x05);
        }
        [Identifier("frag_offset")]
        public ushort FragmentOffset
        {
            get => unchecked((ushort)(BitConverter
                .ToUInt16(Segment
                    .Slice(0x04, sizeof(ushort))
                    .Span) & 0b0001111111111111)
                .ByteSwap());
        }
        public override byte TimeToLive
        {
            get => Segment.Span[0x08];
        }
        public override ProtocolType Protocol 
        { 
            get => (ProtocolType)Segment.Span[0x09];
        }
        public ushort Checksum
        {
            get => BitConverter
                .ToUInt16(Segment
                    .Slice(0x0a, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }
        public override IPAddress Source 
        {
            get => new IPAddress(BitConverter
                .ToInt32(Segment
                    .Slice(0x0c, sizeof(int))
                    .Span));
        }
        public override IPAddress Destination 
        {
            get => new IPAddress(BitConverter
                .ToInt32(Segment
                    .Slice(0x10, sizeof(int))
                    .Span));
        }
        
        public IPv4Packet(Memory<byte> segment)
            : base(segment)
        { }
    }

    [Identifier("ip")]
    public abstract class IPPacket : InternetPacket
    {
        [Identifier("version")]
        public IPVersion Version 
        { 
            get => (IPVersion)(Segment.Span[0]>>4); 
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
        [Identifier("source")]
        public abstract IPAddress Source { get; }
        [Identifier("dest")]
        public abstract IPAddress Destination { get; }

        protected IPAddress ParseIPAddress(int offset)
            => new IPAddress(Segment
                .Slice(offset,
                    Version == IPVersion.IPv4
                        ? 0x04
                    : Version == IPVersion.IPv6
                        ? 0x10
                    : throw new InvalidOperationException())
                .Span);
        
        protected IPPacket(ReadOnlyMemory<byte> segment)
            : base(segment)
        { }
    }

    public abstract class InternetPacket : Packet
    { 
        protected InternetPacket(ReadOnlyMemory<byte> segment)
            : base(segment)
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

        [Identifier("source")]
        public PhysicalAddress Source 
        { 
            get
            {
                var bytes = Segment
                    .Slice(SourceOffset, AddressLength)
                    .ToArray();
                Array.Reverse(bytes);
                return new PhysicalAddress(bytes);
            }
        }

        [Identifier("dest")]
        public PhysicalAddress Destination 
        { 
            get
            {
                var bytes = Segment
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
                .ToUInt16(Segment
                    .Slice(TypeOffset, sizeof(EthernetType))
                    .Span)
                .ByteSwap());
        }

        public EthernetPacket(ReadOnlyMemory<byte> segment)
            : base(segment)
        { }
    }

    public abstract class Packet
    {
        protected readonly ReadOnlyMemory<byte> Segment;

        protected Packet(ReadOnlyMemory<byte> segment)
        {
            Segment = segment;
        }

        public static Packet Parse(LinkLayer link_layer, byte[] data)
        {
            switch (link_layer)
            {
                case LinkLayer.Ethernet:
                case LinkLayer.Raw:
                default:
                    throw new NotImplementedException(nameof(link_layer));
            }
            return null;
        }
    }
}

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
        AllowMultiple = false)]
    public class ShortName : Attribute
    {
        public readonly string Text;
        public ShortName(string text)
            => Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException(nameof(text))
                : text;
    }
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public class ShortNameAuto : Attribute
    { }

    [ShortNameAuto]
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

    [ShortNameAuto]
    public enum EthernetType : ushort
    {
        None =  0x0000,
        Loop =  0x0060,
        Echo =  0x0200,
        IPv4 =  0x0800,
        ARP =   0x0806,
        IPv6 =  0x86dd,
    }

    [ShortNameAuto]
    public enum IPVersion : byte
    {
        IPv4 = 0x04,
        IPv6 = 0x06,
    }

    [ShortNameAuto]
    public enum ProtocolType : byte
    {
        ICMP =                                  0x01,
        IPv4 =                                  0x04,
        TCP =                                   0x06,
        UCP =                                   0x11,
        IPv6 =                                  0x29,
        [ShortName("ipv6_routing")]
        IPv6RoutingHeader =                     0x2b,
        [ShortName("ipv6_fragment")]
        IPv6FragmentHeader =                    0x2c,
        [ShortName("ipsec_esp")]
        IPSecEncapsulatingSecurityPayload =     0x32,
        [ShortName("ipsec_ah")]
        IPSecAuthenticationHeader =             0x33,
        ICMPv6 =                                0x3a,
        [ShortName("ipv6_no_next")]
        IPv6NoNextHeader =                      0x3b,
        [ShortName("ipv6_dest_options")]
        IPv6DestinationOptions =                0x3c,
        Raw =                                   0xff,
    }

    [ShortName("ipv4")]
    public sealed class IPv4Packet : IPPacket
    {
        private const int MinimumHeaderLength = 0x14;

        public override int HeaderLength
        {
            get => InternetHeaderLength<<2;
        }

        [ShortName("ihl")]
        public int InternetHeaderLength 
        {
            get => Header.Span[0] & 0x0f; 
    
        }
        public override int TotalLength 
        { 
            get => BitConverter
                .ToUInt16(Header
                    .Slice(0x02, sizeof(ushort))
                    .Span)
                .ByteSwap();
        }
        public override ProtocolType Protocol 
        { 
            get => (ProtocolType)Header.Span[0x09];
        }
        public override int TimeToLive
        {
            get => Header.Span[0x08];
        }
        public override IPAddress Source 
        {
            get => new IPAddress(BitConverter
                .ToInt32(Header
                    .Slice(0x0c, sizeof(int))
                    .Span));
        }
        public override IPAddress Destination 
        {
            get => new IPAddress(BitConverter
                .ToInt32(Header
                    .Slice(0x10, sizeof(int))
                    .Span));
        }
        
        public IPv4Packet(Memory<byte> header)
            : base(header)
        { }
    }

    [ShortName("ip")]
    public abstract class IPPacket : Packet
    {
        [ShortName("version")]
        public IPVersion Version 
        { 
            get => (IPVersion)(Header.Span[0]>>4); 
        }
        [ShortName("header_length")]
        public abstract int HeaderLength { get; }
        [ShortName("total_length")]
        public abstract int TotalLength { get; }
        [ShortName("protocol")]
        public abstract ProtocolType Protocol { get; }
        public ProtocolType NextHeader => Protocol;
        [ShortName("ttl")]
        public abstract int TimeToLive { get; }
        public int HopLimit => TimeToLive;
        [ShortName("src")]
        public abstract IPAddress Source { get; }
        [ShortName("dest")]
        public abstract IPAddress Destination { get; }

        protected IPAddress ParseIPAddress(int offset)
            => new IPAddress(Header
                .Slice(offset,
                    Version == IPVersion.IPv4
                        ? 0x04
                    : Version == IPVersion.IPv6
                        ? 0x10
                    : throw new InvalidOperationException())
                .Span);
        
        protected IPPacket(ReadOnlyMemory<byte> header)
            : base(header)
        { }
    }

    [ShortName("eth")]
    public sealed class EthernetPacket : Packet
    {
        private const int AddressLength = 6;
        private const int DestinationOffset = 0;
        private const int SourceOffset = AddressLength;
        private const int TypeOffset = AddressLength * 2;
        private const int HeaderLength = TypeOffset + sizeof(EthernetType);

        [ShortName("src")]
        public PhysicalAddress Source 
        { 
            get
            {
                var bytes = Header
                    .Slice(SourceOffset, AddressLength)
                    .ToArray();
                Array.Reverse(bytes);
                return new PhysicalAddress(bytes);
            }
        }

        [ShortName("dest")]
        public PhysicalAddress Destination 
        { 
            get
            {
                var bytes = Header
                    .Slice(DestinationOffset, AddressLength)
                    .ToArray();
                Array.Reverse(bytes);
                return new PhysicalAddress(bytes);
            }
        }

        [ShortName("type")]
        public EthernetType Type 
        { 
            get => (EthernetType)(BitConverter
                .ToUInt16(Header
                    .Slice(TypeOffset, sizeof(EthernetType))
                    .Span)
                .ByteSwap());
        }

        public EthernetPacket(ReadOnlyMemory<byte> header)
            : base(header)
        { }
    }

    public abstract class Packet
    {
        protected readonly ReadOnlyMemory<byte> Header;

        protected Packet(ReadOnlyMemory<byte> header)
        {
            Header = header;
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

using System;
using Arbitrary.Scalpel.Extentions;

namespace Arbitrary.Scalpel
{
    public readonly ref struct ICMPv4TypeCode
    {
        private readonly ushort Value;

        private ICMPv4TypeCode(ushort value)
            => Value = value;

        public static implicit operator ICMPv4TypeCode(ushort value)
            => new ICMPv4TypeCode(value);
        public static implicit operator ushort(ICMPv4TypeCode typecode)
            => typecode.Value;
        public override bool Equals(object obj)
            => obj.GetType().IsAssignableFrom(typeof(ICMPv4TypeCode))
            && (ICMPv4TypeCode)obj == this;
        public override int GetHashCode()
            => this;

        [Identifier("echo_reply")]
        public static ICMPv4TypeCode EchoReply =>                       0x0000;
        [Identifier("unreachable")]
        public static class Unreachable
        {
            [Identifier("net")]
            public static ICMPv4TypeCode Network =>                     0x0300;
            [Identifier("host")]
            public static ICMPv4TypeCode Host =>                        0x0301;
            [Identifier("protocol")]
            public static ICMPv4TypeCode Protocol =>                    0x0302;
            [Identifier("port")]
            public static ICMPv4TypeCode Port =>                        0x0303;
            [Identifier("frag_needed")]
            public static ICMPv4TypeCode FragmentationNeeded =>         0x0304;
            [Identifier("source_route_failed")]
            public static ICMPv4TypeCode SourceRouteFailed =>           0x0305;
            [Identifier("net_unkown")]
            public static ICMPv4TypeCode NetworkUnknown =>              0x0306;
            [Identifier("host_unknown")]
            public static ICMPv4TypeCode HostUnknown =>                 0x0307;
            [Identifier("source_host_isolated")]
            public static ICMPv4TypeCode SourceHostIsolated =>          0x0308;
            [Identifier("net_prohibited")]
            public static ICMPv4TypeCode NetworkProhibited =>           0x0309;
            [Identifier("host_prohibited")]
            public static ICMPv4TypeCode HostProhibited =>              0x030a;
            [Identifier("net_tos")]
            public static ICMPv4TypeCode NetworkForTypeOfService =>     0x030b;
            [Identifier("host_tos")]
            public static ICMPv4TypeCode HostForTypeOfService =>        0x030c;
            [Identifier("comm_prohibited")]
            public static ICMPv4TypeCode CommunicationProhibited =>     0x030d;
            [Identifier("host_precedence_violation")]
            public static ICMPv4TypeCode HostPrecedenceViolation =>     0x030e;
            [Identifier("precedence_cutoff_in_effect")]
            public static ICMPv4TypeCode PrecedenceCutoffInEffect =>    0x030f;
        }
        [Identifier("source_quench")]
        public static ICMPv4TypeCode SourceQuench =>                    0x0400;
        [Identifier("redirect")]
        public static class Redirect
        {
            [Identifier("net")]
            public static ICMPv4TypeCode Network =>                     0x0500;
            [Identifier("host")]
            public static ICMPv4TypeCode Host =>                        0x0501;
            [Identifier("net_tos")]
            public static ICMPv4TypeCode NetworkForTypeOfService =>     0x0502;
            [Identifier("host_tos")]
            public static ICMPv4TypeCode HostForTypeOfService =>        0x0503;
        }
        [Identifier("alt_host_address")]
        public static ICMPv4TypeCode AlternateHostAddress =>            0x0600;
        [Identifier("echo_request")]
        public static ICMPv4TypeCode EchoRequest =>                     0x0800;
        [Identifier("advertisment")]
        public static ICMPv4TypeCode RouterAdvertisment =>              0x0900;
        [Identifier("solicitation")]
        public static ICMPv4TypeCode RouterSolicitation =>              0x0a00;
        [Identifier("time_exceeded")]
        public static class TimeExceeded
        {
            [Identifier("ttl")]
            public static ICMPv4TypeCode TimeToLive =>                  0x0b00;
            [Identifier("frag")]
            public static ICMPv4TypeCode FragmentReassembly =>          0x0b01;
        }
        [Identifier("param_problem")]
        public static class ParameterProblem
        {   
            [Identifier("pointer")]
            public static ICMPv4TypeCode PointerIndicatesError =>       0x0c00;
            [Identifier("required_option")]
            public static ICMPv4TypeCode MissingRequiredOption =>       0x0c01;
            [Identifier("bad_length")]
            public static ICMPv4TypeCode BadLength =>                   0x0c02;
        }
        [Identifier("timestamp")]
        public static ICMPv4TypeCode Timestamp =>                       0x0d00;
        [Identifier("timestamp_reply")]
        public static ICMPv4TypeCode TimestampReply =>                  0x0e00;
        [Identifier("info_request")]
        public static ICMPv4TypeCode InformationRequest =>              0x0f00;
        [Identifier("info_response")]
        public static ICMPv4TypeCode InformationReply =>                0x1000;
        [Identifier("address_mask_request")]
        public static ICMPv4TypeCode AddressMaskRequest =>              0x1100;
        [Identifier("address_mask_reply")]
        public static ICMPv4TypeCode AddressMaskReply =>                0x1200;
        [Identifier("traceroute")]
        public static ICMPv4TypeCode Traceroute =>                      0x1e00;
        [Identifier("datagram_conversion_error")]
        public static ICMPv4TypeCode DatagramConversionError =>         0x1f00;
        [Identifier("mobile_host_redirect")]
        public static ICMPv4TypeCode MobileHostRedirect =>              0x2000;
        [Identifier("where_are_you")]
        public static ICMPv4TypeCode WhereAreYou =>                     0x2100;
        [Identifier("here_i_am")]
        public static ICMPv4TypeCode HereIAm =>                         0x2200;
        [Identifier("mobile_reg_request")]
        public static ICMPv4TypeCode MobileRegistrationRequest =>       0x2300;
        [Identifier("mobile_reg_response")]
        public static ICMPv4TypeCode MobileRegistrationReply =>         0x2400;
        [Identifier("skip")]
        public static ICMPv4TypeCode SKIP =>                            0x2500;
        [Identifier("skip")]
        public static class Photuris
        {
            [Identifier("bad_spi")]
            public static ICMPv4TypeCode BadSPI =>                      0x2600;
            [Identifier("auth_failed")]
            public static ICMPv4TypeCode AuthenticationFailed =>        0x2601;
            [Identifier("decompression_failed")]
            public static ICMPv4TypeCode DecompressionFailed =>         0x2602;
            [Identifier("decrypt_failed")]
            public static ICMPv4TypeCode DecryptionFailed =>            0x2603;
            [Identifier("need_authentication")]
            public static ICMPv4TypeCode NeedAuthentication =>          0x2604;
            [Identifier("need_authorization")]
            public static ICMPv4TypeCode NeedAuthorization =>           0x2605;
        }
        [Identifier("experimental_mobility")]
        public static ICMPv4TypeCode ExperimentalMobility =>            0x2700;
        [Identifier("ext_echo_request")]
        public static ICMPv4TypeCode ExtendedEchoRequest =>             0x2800;
        [Identifier("ext_echo_response")]
        public static class ExtendedEchoReply
        {
            [Identifier("no_error")]
            public static ICMPv4TypeCode NoError =>                     0x2900;
            [Identifier("malformed")]
            public static ICMPv4TypeCode MalformedQuery =>              0x2901;
            [Identifier("no_such_interface")]
            public static ICMPv4TypeCode NoSuchInterface =>             0x2902;
            [Identifier("no_such_table_entry")]
            public static ICMPv4TypeCode NoSuchTableEntry =>            0x2903;
            [Identifier("multiple_interfaces")]
            public static ICMPv4TypeCode MultipleInterfaces =>          0x2904;
        }
        [Identifier("experiment1")]
        public static ICMPv4TypeCode Experiment1 =>                     0xfd00;
        [Identifier("experiment2")]
        public static ICMPv4TypeCode Experiment2 =>                     0xfe00;
    }
}
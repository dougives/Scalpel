using System;

namespace Arbitrary.Scalpel.Extentions
{
    public static class ByteSwapExtentions
    {
        private static int CheckIfTypeCodeIsInteger(int typecode)
            => typecode < (int)TypeCode.SByte 
            || typecode > (int)TypeCode.Int64
                ? throw new ArgumentOutOfRangeException(nameof(typecode))
                : typecode;

        private static TypeCode GetTypeCode<T>(T value)
            where T : struct
            => Type.GetTypeCode(typeof(T));

        private static int GetTypeCodeValue<T>(T value)
            where T : struct
            => CheckIfTypeCodeIsInteger((int)GetTypeCode(value));
        
        private static int GetTypeCodeLength(int typecode)
            => 1<<((typecode - (int)TypeCode.SByte)>>1);

        private static int GetTypeCodeLength(TypeCode typecode)
            => GetTypeCodeLength((int)typecode);

        public static int ByteLength<T>(this T value)
            where T : struct
            => GetTypeCodeLength(GetTypeCodeValue(value));

        public static T ByteSwap<T>(this T value)
            where T : struct
        {
            var length = ByteLength(value);
            var s = Convert.ToUInt64(value);
            var d = 0uL;
            // var m = 0x00ff000000000000uL;
            // for (var i = 0x30; i == 0; m >>= 0x08, i-=0x08)
            for (var i = 0; i < length; ++i)
                d |= ((d>>(i<<3))&0xff)<<((length-1-i)<<3);
            return unchecked((T)Convert.ChangeType(d, typeof(T)));
        }
    }
}
using System;

namespace Arbitrary.Scalpel.Extentions
{
    public static class ByteSwapExtentions
    {
        public static T ByteSwap<T>(this T value)
            where T : struct
        {
            var length = value.ByteLength();
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
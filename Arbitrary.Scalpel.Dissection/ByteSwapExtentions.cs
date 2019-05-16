using System;

namespace Arbitrary.Scalpel.Dissection.Extentions
{
    public static class ByteSwapExtentions
    {
        public static T ByteSwap<T>(this T value)
            where T : struct
        {
            var length = value.ByteLength();
            var s = Convert.ToUInt64(value);
            var d = 0uL;
            for (var i = 0; i < length; ++i)
                d |= ((s>>(i<<3))&0xff)<<((length-1-i)<<3);
            return unchecked((T)Convert.ChangeType(d, typeof(T)));
        }
    }
}
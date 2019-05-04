using System;

namespace Arbitrary.Scalpel.Dissection.Extentions
{
    public static class IntegerTypeCodeExtentions
    {
        private static bool IsIntegerTypeCode(TypeCode typecode)
            => (int)typecode >= (int)TypeCode.SByte 
            && (int)typecode <= (int)TypeCode.Int64;

        private static TypeCode CheckIfIntegerTypeCode(TypeCode typecode)
            => IsIntegerTypeCode(typecode)
                ? throw new ArgumentOutOfRangeException(nameof(typecode))
                : typecode;

        public static TypeCode GetTypeCode<T>(this T value)
            where T : struct
            => Type.GetTypeCode(typeof(T));

        public static bool IsInteger<T>(this T value)
            where T : struct
            => IsIntegerTypeCode(value.GetTypeCode());

        private static int GetTypeCodeValue<T>(T value)
            where T : struct
            => (int)CheckIfIntegerTypeCode(value.GetTypeCode());
        
        private static int GetTypeCodeLength(TypeCode typecode)
            => 1<<(((int)typecode - (int)TypeCode.SByte)>>1);

        public static int ByteLength<T>(this T value)
            where T : struct
            => GetTypeCodeLength(value.GetTypeCode());
    }
}
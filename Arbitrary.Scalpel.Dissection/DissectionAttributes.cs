using System;

namespace Arbitrary.Scalpel.Dissection
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

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class HierarchicalEnum : Attribute
    { }
}
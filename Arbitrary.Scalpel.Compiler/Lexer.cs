using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace Arbitrary.Scalpel.Compiler
{
    public readonly struct KernelSource
    {
        public readonly string Path;
        public readonly string Text;
        public bool IsFromMemory => Path is null;

        private KernelSource(string text, string path = null)
        {
            Text = String.IsNullOrWhiteSpace(text)
                ? throw new ArgumentException(nameof(text))
                : text;
            Path = path;
        }
        
        public static KernelSource FromString(string text)
            => new KernelSource(text);
        public static KernelSource FromPath(string path)
            => new KernelSource(File.ReadAllText(path), path);
    }

    public class Token
    {

    }

    public class Lexer : IEnumerable<Token>
    {
        //public readonly ref struct Operator
        //{
        //    private static readonly string[] Table = new []
        //    {
        //        ""
        //    };
        //
        //    private readonly string Value;
        //
        //    private Operator(string value)
        //        => Value = value;
        //    public static implicit operator string(Operator keyword)
        //        => keyword.Value;
        //    public static implicit operator Operator(string value)
        //        => new Operator(value);
        //}

        public readonly KernelSource Source;

        public Lexer(KernelSource source)
            => Source = source;

        private IEnumerable<Token> GetEnumerable()
            => Enumerable.Empty<Token>();

        public IEnumerator<Token> GetEnumerator()
            => GetEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}

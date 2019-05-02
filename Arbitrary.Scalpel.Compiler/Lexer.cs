using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Arbitrary.Scalpel.Compiler
{
    public readonly struct KernelSource
    {
        public readonly string Path;
        public readonly ReadOnlyMemory<char> Buffer;
        public bool IsFromMemory => Path is null;

        // should only be called by exceptions
        internal (int, int) GetOffsetLineInfo(int offset)
        {
            if (offset > Buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            var buffer = Buffer.Span;
            var count = 0;
            var column = 0;
            for (int i = 0; i < offset && i < Buffer.Length; ++i, ++column)
            {
                if (buffer[i] == '\n')
                {
                    ++count;
                    column = 0;
                    continue;
                }
                if (buffer[i] == '\r')
                {
                    ++count;
                    column = 0;
                    if (i >= Buffer.Length)
                        break;
                    if (buffer[i] == '\n')
                        ++i;
                    continue;
                }
            }
            return (
                offset == buffer.Length && count > 0
                    ? count - 1
                    : count, 
                column);
        }

        private KernelSource(
            ReadOnlyMemory<char> buffer,
            string path = null)
        {
            Buffer = buffer;
            Path = path;
        }

        public static KernelSource FromFile(string path)
        {
            return new KernelSource(
                File.ReadAllText(path).AsMemory(),
                path);
        }
    }

    public enum TokenType
    {
        CloseParens,
        OpenParens,
        Title,
        Selection,
        Name,
        StringLiteral,
        DecimalIntegerLiteral,
        HexadecimalIntegerLiteral,
        Operator,
        Comment,
    }
    
    public readonly struct Token
    {
        public readonly TokenType Type;
        public readonly KernelSource Source;
        public readonly int Offset;
        public readonly int Length;
    
        private Token(
            TokenType type, 
            KernelSource source, 
            int offset, 
            int length)
        {
            Type = type;
            Source = source;
            Offset = offset < 0
                ? throw new ArgumentOutOfRangeException(nameof(offset))
                : offset;
            Length = length <= 0 || offset + length > Source.Buffer.Length
                ? throw new ArgumentOutOfRangeException(nameof(length))
                : length;
        }
        // public static implicit operator TokenType(Token token)
        //     => token.Type;
        public static implicit operator ReadOnlySpan<char>(Token token)
            => token.Source.Buffer.Span
                .Slice(token.Offset, token.Length);
        // public static implicit operator string(Token token)
        //     => ((ReadOnlySpan<char>)token).ToString();
        // public static implicit operator (TokenType, string)(Token token)
        //     => (token, token);
        public static implicit operator Token(
            (TokenType, KernelSource, int, int) value)
            => new Token(
                value.Item1, 
                value.Item2, 
                value.Item3, 
                value.Item4);
        public override string ToString()
            => Source.Buffer.Span.ToString();
    }

    public class Lexer : IEnumerable<Token>
    {
        public class LexerException : Exception
        {
            private static string LocationMessage(Lexer lexer)
            {
                var (line, column) = lexer.Source
                    .GetOffsetLineInfo(lexer.Offset);
                return $"({line}, {column})";
            }

            public LexerException(string message, Lexer lexer)
                : base(String
                    .IsNullOrWhiteSpace(message)
                        ? string.Empty
                        : $"{message}: " 
                    + LocationMessage(lexer))
            { }
        }

        private static readonly char TitlePrefix = '$';
        private static readonly char LineCommentPrefix = '#';
        private static readonly char SelectionPrefix = ':';
        private static readonly char OpenParens = '(';
        private static readonly char CloseParens = ')';
        private static readonly char StringDelimiter = '"';
        private static readonly HashSet<string> Operators = 
            new HashSet<string>(new []
            {
                "|", "&", "==", "!=", "<", ">", "<=", ">=", "!",  
            }); 
        private static readonly HashSet<char> OperatorsOne =
            new HashSet<char>(Operators
                .Where(s => s.Length == 1)
                .Select(s => s[0]));
        private static readonly HashSet<string> OperatorsTwo =
            new HashSet<string>(Operators
                .Where(s => s.Length == 2));
        private static readonly string OperatorSet = new string(string
            .Concat(Operators)
            .ToHashSet()
            .ToArray());

        private int Offset = 0;

        public readonly KernelSource Source;

        public Lexer(KernelSource source)
            => Source = source;

        private bool OffsetIsAtEnd(ReadOnlySpan<char> buffer)
            => Offset >= Source.Buffer.Length;

        private void TrimStart(ReadOnlySpan<char> buffer)
        {
            for (; 
                !OffsetIsAtEnd(buffer) 
                    && Char.IsWhiteSpace(buffer[Offset]); 
                ++Offset)
            { }
        }

        private void TrimRestOfLine(ReadOnlySpan<char> buffer)
        {
            for (; !OffsetIsAtEnd(buffer); ++Offset)
            {
                if (buffer[Offset] == '\n')
                {
                    ++Offset;
                    return;
                }
                if (buffer[Offset] == '\r')
                {
                    ++Offset;
                    if (OffsetIsAtEnd(buffer))
                        return;
                    if (buffer[Offset] == '\n')
                        ++Offset;
                    return;
                }
            } 
        }

        private bool IsIdentifierHead(ReadOnlySpan<char> buffer)
            => buffer[Offset] == '_' || !char.IsLetter(buffer[Offset]);

        private bool IsIdentifierBody(ReadOnlySpan<char> buffer)
            => buffer[Offset] == '_' 
            || !char.IsLetterOrDigit(buffer[Offset]);

        private bool IsOperatorHead(ReadOnlySpan<char> buffer)
            => OperatorSet.Contains(buffer[Offset]);

        private (int, int) ScanIdentifier(ReadOnlySpan<char> buffer)
        {            
            if (OffsetIsAtEnd(buffer))
                throw new LexerException(
                    "Unexpected end of source, expected identifier",
                    this);
            var start = Offset;
            if (!IsIdentifierHead(buffer))
                throw new LexerException(
                    "Identifier must start with letter or '_'",
                    this);
            ++Offset;
            for (; 
                !OffsetIsAtEnd(buffer) && IsIdentifierBody(buffer);
                ++Offset)
            { }
            return (start, Offset - start);
        }

        private Token ScanName(ReadOnlySpan<char> buffer)
        {
            var start = Offset;
            do
            {
                ScanIdentifier(buffer);
            } while (buffer[Offset++] == '.');
            return (TokenType.Name, Source, start, Offset - start);
        }

        private Token ScanSelection(ReadOnlySpan<char> buffer)
            => ScanName(buffer);

        private Token ScanTitle(ReadOnlySpan<char> buffer)
        {
            var (start, length) = ScanIdentifier(buffer);
            return (TokenType.Title, Source, start, length);
        }

        private Token ScanOperator(ReadOnlySpan<char> buffer)
        {
            if (buffer.Length < Offset + 1)
                throw new LexerException(
                    "Operator cannot immediatly precede end of source",
                    this);
            var two = buffer.Slice(Offset, 2).ToString();

            foreach (var op in OperatorsTwo)
            {
                if (two == op)
                {
                    Offset += 2;
                    return (TokenType.Operator, Source, Offset - 2, 2);
                }
            }

            foreach (var op in OperatorsOne)
                if (buffer[Offset] == op)
                    return (TokenType.Operator, Source, Offset++, 1);

            throw new LexerException("Unknown operator", this);
        }

        private Token ScanIntegerLiteral(ReadOnlySpan<char> buffer)
        {
            var type = TokenType.DecimalIntegerLiteral;
            var start = Offset;

            if (buffer[Offset] == '0')
            {
                Offset++;
                if (OffsetIsAtEnd(buffer))
                    return (type, Source, start, Offset - start);
                if (char.IsDigit(buffer[Offset]))
                    throw new LexerException(
                        "Octal integer literal not supported",
                        this);
                if (buffer[Offset] == 'x')
                {
                    type = TokenType.HexadecimalIntegerLiteral;
                    start = Offset;
                    Offset++;
                    if (!char.IsDigit(buffer[Offset]) 
                        || OffsetIsAtEnd(buffer))
                        throw new LexerException(
                            "Unexpected end of integer literal",
                            this);
                }
            }
            
            for (; 
                !OffsetIsAtEnd(buffer) && char.IsDigit(buffer[Offset]);
                ++Offset)
            { }
            return (type, Source, start, Offset - start);
        }

        private IEnumerable<Token> EnumerateTokens()
        {
            var buffer = Source.Buffer.Span;
            
            TrimStart(buffer);

            while (!OffsetIsAtEnd(buffer))
            {
                if (buffer[Offset] == LineCommentPrefix)
                {
                    TrimRestOfLine(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (buffer[Offset] == TitlePrefix)
                {
                    ++Offset;
                    TrimStart(buffer);
                    yield return ScanTitle(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (buffer[Offset] == SelectionPrefix)
                {
                    ++Offset;
                    TrimStart(buffer);
                    yield return ScanSelection(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (buffer[Offset] == OpenParens)
                {
                    yield return (TokenType.OpenParens, Source, Offset, 1);
                    ++Offset;
                    TrimStart(buffer);
                    continue;
                }
                if (buffer[Offset] == CloseParens)
                {
                    yield return (TokenType.CloseParens, Source, Offset, 1);
                    ++Offset;
                    TrimStart(buffer);
                    continue;
                }
                if (IsIdentifierHead(buffer))
                {
                    yield return ScanName(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (IsOperatorHead(buffer))
                {
                    yield return ScanOperator(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (char.IsDigit(buffer[Offset]))
                {
                    yield return ScanIntegerLiteral(buffer);
                    TrimStart(buffer);
                    continue;
                }
                if (buffer[Offset] == StringDelimiter)
                {
                    throw new NotImplementedException();
                }
                throw new LexerException("Unexpected character", this);
            }
            yield break;
        }

        public IEnumerator<Token> GetEnumerator()
            => EnumerateTokens().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pidgin;
using Pidgin.Expression;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Arbitrary.Scalpel.Compiler
{
    public enum UnaryOperationType
    {
        Not,
    }

    public enum BinaryOperationType
    {
        Or,
        And,
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
    }

    public interface ISyntax : IEquatable<ISyntax>
    { }

    public interface IPredicate : ISyntax
    { }

    public interface ILiteral : IPredicate
    { }

    public class TitleSyntax : ISyntax
    {
        public readonly string Name;

        public TitleSyntax(string name)
            => Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(nameof(name))
                : name;

        public bool Equals(ISyntax other)
            => other is TitleSyntax o 
            && Name == o.Name;
    }

    public class SelectorSyntax : ISyntax
    {
        public readonly NameSyntax Name;

        public SelectorSyntax(NameSyntax name)
            => Name = name
                ?? throw new ArgumentNullException(nameof(name));

        public bool Equals(ISyntax other)
            => other is SelectorSyntax o 
            && Name == o.Name;
    }

    public class NameSyntax : IPredicate
    {
        public readonly IReadOnlyList<string> Identifiers;

        public NameSyntax(IEnumerable<string> identifiers)
            => Identifiers = new List<string>(identifiers
                ?? throw new ArgumentNullException(nameof(identifiers)))
                as IReadOnlyList<string>;
        
        public bool Equals(ISyntax other)
            => other is NameSyntax o
            && Identifiers.SequenceEqual(o.Identifiers);
    }

    public class UnaryOperationSyntax<T> : IPredicate
        where T : IPredicate
    {
        public readonly UnaryOperationType Type;
        public readonly T Operand;

        public UnaryOperationSyntax(UnaryOperationType type, T operand)
        {
            Type = type;
            Operand = operand == null
                ? throw new ArgumentNullException(nameof(operand))
                : operand;
        }

        public bool Equals(ISyntax other)
            => other is UnaryOperationSyntax<T> o 
            && Type == o.Type
            && Operand.Equals(o.Operand);
    }

    public class BinaryOperationSyntax<T, U> : IPredicate
        where T : IPredicate
        where U : IPredicate
    {
        public readonly BinaryOperationType Type;
        public readonly T LeftOperand;
        public readonly U RightOperand;

        public BinaryOperationSyntax(
            BinaryOperationType type,
            T left_operand,
            U right_operand)
        {
            if (left_operand is ILiteral && right_operand is ILiteral)
                throw new InvalidOperationException();
            Type = type;
            LeftOperand = left_operand == null
                ? throw new ArgumentNullException(nameof(left_operand))
                : left_operand;
            RightOperand = right_operand == null
                ? throw new ArgumentNullException(nameof(right_operand))
                : right_operand;
        }

        public bool Equals(ISyntax other)
            => other is BinaryOperationSyntax<T, U> o 
            && Type == o.Type
            && LeftOperand.Equals(o.LeftOperand)
            && RightOperand.Equals(o.RightOperand);
    }

    public class StringLiteralSyntax : ILiteral
    {
        public readonly string Text;

        public StringLiteralSyntax(string text)
            => Text = string.IsNullOrWhiteSpace(text)
                ? throw new ArgumentNullException(nameof(text))
                : text;

        public bool Equals(ISyntax other)
            => other is StringLiteralSyntax o 
            && Text == o.Text;
    }

    public class IntegerLiteralSyntax : ILiteral
    {
        public readonly int Value;

        public IntegerLiteralSyntax(int value)
            => Value = value;

        public bool Equals(ISyntax other)
            => other is IntegerLiteralSyntax o 
            && Value == o.Value;
    }

    public class KernelSyntax : ISyntax
    {
        public readonly TitleSyntax Title;
        public readonly IPredicate Predicate;
        public readonly IReadOnlyList<SelectorSyntax> Selectors;

        public KernelSyntax(
            TitleSyntax title,
            IPredicate predicate,
            IEnumerable<SelectorSyntax> selectors)
        {
            Title = title
                ?? throw new ArgumentNullException(nameof(title));
            Predicate = predicate
                ?? throw new ArgumentNullException(nameof(predicate));
            Selectors = new List<SelectorSyntax>(selectors
                ?? throw new ArgumentNullException(nameof(selectors)));
            if (Selectors.Count == 0)
                throw new ArgumentException(nameof(selectors));
        }

        public bool Equals(ISyntax other)
            => other is KernelSyntax o
            && Title == o.Title
            && Predicate == o.Predicate
            && Selectors.SequenceEqual(o.Selectors);
    }

    public static class ScalpelParser
    {

        private static readonly Parser<char, char> TitlePrefix = 
            Char('$');
        private static readonly Parser<char, char> LineCommentPrefix = 
            Char('#');
        private static readonly Parser<char, char> SelectionPrefix = 
            Char(':');
        private static readonly Parser<char, char> StringDelimiter = 
            Char('"');
        private static readonly Parser<char, char> OpenParens = 
            Char('(');
        private static readonly Parser<char, char> CloseParens = 
            Char(')');

        private static Parser<char, T> Tok<T>(Parser<char, T> op)
            => Try(op).Before(SkipWhitespaces);
        private static Parser<char, string> Tok(string op)
            => Tok(String(op));

        private static readonly Parser<char, char> IdentifierHead =
            Letter.Or(Char('_'));
        private static readonly Parser<char, char> IdentifierBody =
            LetterOrDigit.Or(Char('_'));
        private static readonly Parser<char, string> Identifier =
            Map((a, b) => a + b, 
                IdentifierHead, 
                IdentifierBody.ManyString());

        private static readonly Parser<char, IPredicate> Name =
            Tok(Identifier.SeparatedAtLeastOnce(Char('.')))
                .Select(ids => new NameSyntax(ids) as IPredicate);

        private static readonly Parser<char, Unit> LineComment =
            LineCommentPrefix
                .Between(SkipWhitespaces)
                .SkipUntil(EndOfLine);

        private static readonly Parser<char, TitleSyntax> Title =
            TitlePrefix
                .Between(SkipWhitespaces)
                .Then(Identifier, (c, id) 
                    => new TitleSyntax(id))
                .Before(Whitespace
                    .SkipUntil(EndOfLine));

        private static readonly Parser<char, SelectorSyntax> Selector =
            SelectionPrefix
                .Between(SkipWhitespaces)
                .Then(Name, (c, name)
                    => new SelectorSyntax(name as NameSyntax));

        private static Parser<char, T> Parenthesised<T>(
            Parser<char, T> parser)
            => parser.Between(
                OpenParens.Before(SkipWhitespaces), 
                CloseParens.Before(SkipWhitespaces));

        private static Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            BinaryOperation(Parser<char, BinaryOperationType> op)
            => op.Select<Func<IPredicate, IPredicate, IPredicate>>(
                t => (a, b) => 
                    new BinaryOperationSyntax<IPredicate, IPredicate>(
                        t, a, b));
        private static Parser<char, Func<IPredicate, IPredicate>> 
            UnaryOperation(Parser<char, UnaryOperationType> op)
            => op.Select<Func<IPredicate, IPredicate>>(
                t => o => new UnaryOperationSyntax<IPredicate>(t, o));
        
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            OrOperation = BinaryOperation(
                Tok("||").ThenReturn(
                    BinaryOperationType.Or));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            AndOperation = BinaryOperation(
                Tok("&&").ThenReturn(
                    BinaryOperationType.And));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            EqualOperation = BinaryOperation(
                Tok("==").ThenReturn(
                    BinaryOperationType.Equal));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            NotEqualOperation = BinaryOperation(
                Tok("!=").ThenReturn(
                    BinaryOperationType.NotEqual));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            LessThanOperation = BinaryOperation(
                Tok("<").ThenReturn(
                    BinaryOperationType.LessThan));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            GreaterThanOperation = BinaryOperation(
                Tok(">").ThenReturn(
                    BinaryOperationType.GreaterThan));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            LessThanOrEqualOperation = BinaryOperation(
                Tok("<=").ThenReturn(
                    BinaryOperationType.LessThanOrEqual));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate, IPredicate>> 
            GreaterThanOrEqualOperation = BinaryOperation(
                Tok(">=").ThenReturn(
                    BinaryOperationType.GreaterThanOrEqual));
        private static readonly Parser<
            char, Func<IPredicate, IPredicate>>
            NotOperation = UnaryOperation(
                Tok("!").ThenReturn(
                    UnaryOperationType.Not));

        private static readonly Parser<char, ILiteral> 
            IntegerLiteral =
            DecimalNum.Or(HexNum).Or(OctalNum)
                .Select(x => new IntegerLiteralSyntax(x) as ILiteral);
        private static readonly Parser<char, ILiteral> 
            StringLiteral =
            Token(c => c != '"')
                .ManyString()
                .Between(StringDelimiter)
                .Select(s => new StringLiteralSyntax(s) as ILiteral);
        private static readonly Parser<char, ILiteral> Literal =
            IntegerLiteral.Or(StringLiteral);

        private static readonly Parser<char, IPredicate> Predicate =
            Tok("||").Optional().Then(ExpressionParser.Build(
                p => (OneOf(Name, Parenthesised(p))),
                new []
                {
                    Operator.Prefix(NotOperation),
                    Operator.InfixL(LessThanOperation)
                        .And(Operator.InfixL(GreaterThanOperation))
                        .And(Operator.InfixL(LessThanOrEqualOperation))
                        .And(Operator.InfixL(
                            GreaterThanOrEqualOperation)),
                    Operator.InfixL(EqualOperation)
                        .And(Operator.InfixL(NotEqualOperation)),
                    Operator.InfixL(AndOperation),
                    Operator.InfixL(OrOperation),
                }));
        
        private static readonly Parser<char, KernelSyntax> Kernel =
            Map((t, p, s) => new KernelSyntax(t, p, s),
                Title, 
                Predicate, 
                Selector.AtLeastOnce());

        //public readonly KernelSyntax ParserResult;

        // public ScalpelParser(string source)
        // {
        //     ParserResult = Kernel.ParseOrThrow(source);
        // }

        public static KernelSyntax ParseFromString(string source)
            => Kernel.ParseOrThrow(source);
        public static KernelSyntax ParseFromFile(string path)
            => ParseFromString(File.ReadAllText(path));
        async public static Task<KernelSyntax> ParseFromFileAsync(
            string path)
            => ParseFromString(await File.ReadAllTextAsync(path));
    }
}
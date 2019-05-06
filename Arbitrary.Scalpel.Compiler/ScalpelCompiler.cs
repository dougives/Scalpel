using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Arbitrary.Scalpel.Dissection;

namespace Arbitrary.Scalpel.Compiler
{
    public struct SymbolState
    {
        public string Name { get; set; }
        public int PredicateCounter { get; set; }
        public int SelectionCounter { get; set; }
        public object Value { get; set; }
    }

    public class ScalpelProgram
    {
        private static readonly IReadOnlyDictionary<string, Type>
            SymbolTypeMap;

        private readonly Dictionary<string, SymbolState> Symbols =
            new Dictionary<string, SymbolState>();

        private readonly Dictionary<string, ScalpelKernel> Kernels =
            new Dictionary<string, ScalpelKernel>();

        private void AddSymbol(
            string name,
            bool is_in_predicate,
            bool is_in_selection)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(nameof(name));
            if (!is_in_predicate && !is_in_selection)
                throw new ArgumentException();

            // there can be a state where a symbol is added, but the value
            // has not yet been updated before the program calls a kernel.
            // the program must only call kernels between whole cycles.
            lock (Symbols)
            {
                if (Symbols.ContainsKey(name))
                {
                    var state = Symbols[name];
                    if (is_in_predicate)
                        state.PredicateCounter++;
                    if (is_in_selection)
                        state.SelectionCounter++;
                    return;
                }

                Symbols.Add(name, new SymbolState
                {
                    Name = name,
                    PredicateCounter = is_in_predicate ? 1 : 0,
                    SelectionCounter = is_in_selection ? 1 : 0,
                    Value = default(object),
                });
            }
        }

        private void AddKernel(ScalpelKernel kernel)
        {
            throw new NotImplementedException();
            lock (Kernels)
            {
                if (Kernels.ContainsKey(kernel.Title))
                    throw new InvalidOperationException(nameof(kernel));
                foreach (var symbol in kernel.AllSymbols)
                    AddSymbol(
                        symbol,
                        kernel.PredicateSymbols.Contains(symbol),
                        kernel.SelectionSymbols.Contains(symbol));
            }
        }

        private void Cycle(Packet packet)
        {
        }

        public ScalpelProgram(IEnumerable<ScalpelKernel> kernels)
        {
            foreach (var kernel in kernels)
                AddKernel(kernel);
        }
    }

    public class ScalpelKernel
    {
        public readonly string Title;
        
        public readonly Predicate<Dictionary<string, SymbolState>> 
            Predicate;
        public readonly HashSet<string> PredicateSymbols;
        public readonly HashSet<string> SelectionSymbols;
        public readonly HashSet<string> AllSymbols;

        private ScalpelKernel(KernelSyntax syntax)
        {
            Title = string.IsNullOrWhiteSpace(syntax.Title.Name)
                ? throw new ArgumentException(nameof(syntax))
                : syntax.Title.Name;

            string JoinName(NameSyntax name)
                => string.Join('.', name.Identifiers);
            
            IEnumerable<string> FindPredicateSymbols(IPredicate ps)
            {
                switch (ps)
                {
                    case NameSyntax o:
                        yield return JoinName(o);
                        yield break;
                    case UnaryOperationSyntax<IPredicate> o:
                        foreach (var p 
                            in FindPredicateSymbols(o.Operand))
                            yield return p;
                            yield break;
                    case BinaryOperationSyntax<IPredicate, IPredicate> o:
                        foreach (var p 
                            in FindPredicateSymbols(o.LeftOperand))
                            yield return p;
                        foreach (var p 
                            in FindPredicateSymbols(o.RightOperand))
                            yield return p;
                        yield break;
                    default:
                        yield break;
                }
            }

            PredicateSymbols = new HashSet<string>(
                FindPredicateSymbols(syntax.Predicate));

            SelectionSymbols = new HashSet<string>(
                syntax.Selectors.Select(s => JoinName(s.Name)));

            AllSymbols = new HashSet<string>(
                PredicateSymbols.Union(SelectionSymbols));

            Predicate<Dictionary<string, SymbolState>> CompilePredicate(
                IPredicate predicate)
            {
                const string param_name = "symbols";
                Type param_type = typeof(Dictionary<string, SymbolState>);

                var param_var = Expression.Variable(
                    param_type,
                    param_name);

                Expression PredicateExpression(IPredicate pe)
                {
                    switch (predicate)
                    {
                        case IntegerLiteralSyntax o:
                            return Expression.Constant(o.Value);
                        case StringLiteralSyntax o:
                            return Expression.Constant(o.Text);
                        case NameSyntax o:
                            return Expression.Call(
                                param_var,
                                param_type.GetMethod("Get"),
                                Expression.Constant(JoinName(o)));
                        case UnaryOperationSyntax<IPredicate> o:
                            switch (o.Type)
                            {
                                case UnaryOperationType.Not:
                                    return Expression.IsFalse(
                                        PredicateExpression(o.Operand));
                                default:
                                    throw new InvalidOperationException();
                            }
                        case BinaryOperationSyntax<IPredicate, IPredicate> o:
                            switch (o.Type)
                            {
                                case BinaryOperationType.Or:
                                    return Expression.Or(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.And:
                                    return Expression.And(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.NotEqual:
                                    return Expression.NotEqual(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.Equal:
                                    return Expression.Equal(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.GreaterThanOrEqual:
                                    return Expression.GreaterThanOrEqual(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.LessThanOrEqual:
                                    return Expression.LessThanOrEqual(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                case BinaryOperationType.GreaterThan:
                                    return Expression.GreaterThan(
                                        PredicateExpression(o.LeftOperand),
                                        PredicateExpression(o.RightOperand));
                                default:
                                    throw new InvalidOperationException();
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }

                var parameter = Expression.Parameter(
                    param_type,
                    param_name);
                var lambda = Expression.Lambda(
                    param_type,
                    PredicateExpression(predicate),
                    parameter);

                while (lambda.CanReduce)
                    lambda = lambda.ReduceAndCheck() as LambdaExpression;

                return lambda.Compile()
                    as Predicate<Dictionary<string, SymbolState>>;
            }

            Predicate = CompilePredicate(syntax.Predicate);
        }
    }
}
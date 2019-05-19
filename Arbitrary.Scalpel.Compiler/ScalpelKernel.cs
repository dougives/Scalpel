using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Arbitrary.Scalpel.Compiler
{
    public class ScalpelKernel
    {
        public readonly string Title;
        
        public readonly Predicate<Dictionary<string, SymbolState>> 
            Predicate;
        public readonly HashSet<string> PredicateSymbols;
        public readonly HashSet<string> SelectionSymbols;
        public readonly HashSet<string> AllSymbols;
        public readonly HashSet<string> LayerIdentifiers;

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

            LayerIdentifiers = new HashSet<string>(
                AllSymbols.Select(s => s.Substring(0, s.IndexOf('.'))));

            Predicate<Dictionary<string, SymbolState>> CompilePredicate(
                IPredicate predicate)
            {
                const string param_name = "symbols";
                Type param_type = typeof(Dictionary<string, SymbolState>);
                Type lambda_type = 
                    typeof(Predicate<Dictionary<string, SymbolState>>);

                var parameter = Expression.Parameter(
                    param_type,
                    param_name);

                Expression PredicateExpression(IPredicate pe)
                {
                    switch (pe)
                    {
                        case IntegerLiteralSyntax o:
                            return Expression.Constant(o.Value);
                        case StringLiteralSyntax o:
                            return Expression.Constant(o.Text);
                        case NameSyntax o:
                            return Expression.Property( 
                                Expression.Call(
                                    parameter,
                                    param_type.GetMethod("get_Item"),
                                    Expression.Constant(JoinName(o))),
                                "Value");
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

                var lambda = Expression.Lambda(
                    lambda_type,
                    PredicateExpression(predicate),
                    parameter);

                while (lambda.CanReduce)
                    lambda = lambda.ReduceAndCheck() as LambdaExpression;

                return lambda.Compile()
                    as Predicate<Dictionary<string, SymbolState>>;
            }

            Predicate = CompilePredicate(syntax.Predicate);
        }

        public static ScalpelKernel CompileFromFile(string path)
            => new ScalpelKernel(ScalpelParser.ParseFromFile(path));
    }
}

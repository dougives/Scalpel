using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        private static readonly IReadOnlyDictionary<string, HashSet<object>>
            SymbolMap;

        private static IReadOnlyDictionary<string, HashSet<object>>
            BuildSymbolMap()
        {
            var symbol_map = 
                new Dictionary<string, HashSet<object>>();

            void AddSymbolMapEntry(string key, object value)
            {                    
                if (symbol_map.ContainsKey(key))
                    {
                        symbol_map[key].Add(value);
                        return;
                    }
                    symbol_map.Add(
                        key, 
                        new HashSet<object>( new [] { value }));
            }

            var packet_assembly = Assembly.GetAssembly(typeof(Packet));
            foreach (var packet_type in packet_assembly.GetTypes()
                .Where(t => typeof(Packet).IsAssignableFrom(t)))
            {
                var packet_ids = packet_type
                    .GetCustomAttributes<Identifier>(true);
                if (!packet_ids.Any())
                    continue;
                foreach (var packet_id in packet_ids.Select(id => id.Text))
                {
                    AddSymbolMapEntry(packet_id, packet_type);

                    var props = packet_type.GetProperties();
                    foreach (var prop in props)
                    {
                        var prop_ids = prop
                            .GetCustomAttributes<Identifier>(true);
                        if (!prop_ids.Any())
                            continue;
                        foreach (var prop_id in prop_ids
                            .Select(id => id.Text))
                        {
                            var symbol = string
                                .Join('.', packet_id, prop_id);
                            
                            // if prop is an enum, add the enum symbols
                            // this is kind of a disaster
                            if (typeof(Enum)
                                .IsAssignableFrom(prop.PropertyType))
                            {
                                var fields = prop.PropertyType
                                    .GetFields()
                                    .ToHashSet();
                                foreach (var field in fields)
                                {
                                    var field_ids = field
                                        .GetCustomAttributes<Identifier>();
                                    if (!field_ids.Any())
                                        continue;
                                    foreach (var field_id in field_ids
                                        .Select(id => id.Text))
                                    {
                                        var enum_symbol = string
                                            .Join('.', symbol, field_id);
                                        AddSymbolMapEntry(
                                            enum_symbol,
                                            field);
                                    }
                                }
                                var has_auto_id = prop.PropertyType
                                    .GetCustomAttribute<IdentifierAuto>();
                                if (has_auto_id != null)
                                {
                                    var auto_fields = fields.Except(
                                        fields.Where(f
                                            => f.GetCustomAttributes<
                                                Identifier>()
                                                .Any()));
                                    foreach (var field in auto_fields)
                                    {
                                        var enum_symbol = string
                                            .Join('.', 
                                                symbol, 
                                                field.Name.ToLower());
                                        AddSymbolMapEntry(
                                            enum_symbol,
                                            field);
                                    }
                                }
                            }

                            // if prop is HierarchicalEnum, add all of it
                            var has_hierarchical_enum = prop.PropertyType
                                .GetCustomAttribute<HierarchicalEnum>(true);
                            if (has_hierarchical_enum != null)
                            {
                                throw new NotImplementedException();
                                // IEnumerable<(string, object)>
                                //     EnumerateHierarchy(
                                //         string base_symbol, 
                                //         Type type)
                                // {
                                //     var children = type.GetNestedTypes(
                                //         BindingFlags.Public
                                //         | BindingFlags.Static);
                                //     foreach (var child in children)
                                //     {
                                //         foreach (var 
                                //     }
                                // }
                            }

                            AddSymbolMapEntry(symbol, prop);
                        }
                    }
                }
            }
            return symbol_map;
        }

        static ScalpelProgram()
        {
            SymbolMap = BuildSymbolMap();
        }

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
            //throw new NotImplementedException();
            lock (Kernels)
            {
                if (Kernels.ContainsKey(kernel.Title))
                    throw new InvalidOperationException(nameof(kernel));
                foreach (var symbol in kernel.AllSymbols)
                    AddSymbol(
                        symbol,
                        kernel.PredicateSymbols.Contains(symbol),
                        kernel.SelectionSymbols.Contains(symbol));
                Kernels.Add(kernel.Title, kernel);
            }
        }

        public void Cycle(Packet packet)
        {
            IEnumerable<string> PacketLayerIdentifiers()
            {
                for (var layer = packet; 
                    layer != null; 
                    layer = packet.Parent)
                {
                    foreach (var id in layer.GetType()
                        .GetCustomAttributes<Identifier>(true))
                        yield return id.Text;
                }
            }
            var packet_layer_ids = 
                new HashSet<string>(PacketLayerIdentifiers());
            var relevant_kernels = Kernels.Values
                .Where(k => k.LayerIdentifiers
                    .IsSubsetOf(packet_layer_ids));
            var selected_kernels = relevant_kernels
                .Where(k => k.Predicate(Symbols));
            throw new NotImplementedException();
        }

        public ScalpelProgram(IEnumerable<ScalpelKernel> kernels)
        {
            // add layer id's to symbols
            // this allows a kernel to specify a predicate
            // that just checks for the existance of a layer.
            // kernel predicates that don't use the layers
            // aren't even called by the program cycle.
            // but it's a little weird since
            // eth == tcp.flags.syn becomes a valid operation,
            // equivalent to eth && tcp.flags.syn
            foreach (var layer_id in SymbolMap.Keys
                .Where(s => !s.Contains('.')))
                Symbols.Add(
                    layer_id,
                    new SymbolState
                    {
                        Name = layer_id,
                        PredicateCounter = int.MaxValue,
                        SelectionCounter = int.MaxValue,
                        Value = true,
                    });

            // add enum constants

            
            foreach (var kernel in kernels)
                AddKernel(kernel);
        }

        public static ScalpelProgram FromKernelFiles(
            IEnumerable<string> paths)
            => !paths.Any()
                ? throw new ArgumentException(nameof(paths))
                : new ScalpelProgram(
                    paths.Select(p 
                        => ScalpelKernel.CompileFromFile(p)));
        public static ScalpelProgram FromKernelFiles(params string[] paths)
            => FromKernelFiles(paths as IEnumerable<string>);
    }

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
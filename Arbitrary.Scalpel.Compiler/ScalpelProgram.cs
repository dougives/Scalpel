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
                                IEnumerable<(string, object)>
                                    EnumerateHierarchy(
                                        string base_symbol, 
                                        Type type)
                                {
                                    var names = type
                                        .GetProperties(
                                            BindingFlags.Public
                                            | BindingFlags.Static)
                                        .Where(t 
                                            => t.PropertyType 
                                            == prop.PropertyType);
                                    foreach (var name in names)
                                    {
                                        var name_ids = 
                                            name.GetCustomAttributes<
                                                Identifier>(true);
                                        foreach (var name_id in name_ids)
                                            yield return (string
                                                .Join('.',
                                                    base_symbol,
                                                    name_id.Text),
                                                name);
                                    }
                                    var children = type.GetNestedTypes(
                                        BindingFlags.Public
                                        | BindingFlags.Static);
                                    foreach (var child in children)
                                    {
                                        var child_ids =
                                            child.GetCustomAttributes<
                                                Identifier>(true);
                                        foreach (var child_id in child_ids)
                                        {
                                            foreach (var (
                                                child_name, 
                                                child_type)
                                                in EnumerateHierarchy(string
                                                    .Join('.',
                                                        base_symbol,
                                                        child_id.Text),
                                                    child))
                                                yield return (
                                                    child_name,
                                                    child_type);
                                        }
                                    }
                                }
                                foreach (var (name, type)
                                    in EnumerateHierarchy(
                                        symbol,
                                        prop.PropertyType))
                                    AddSymbolMapEntry(name, type);
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
            if (SymbolMap[name].All(v 
                => typeof(Type).IsAssignableFrom(v.GetType())))
                return;

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

        public IReadOnlyDictionary<
            string, 
            IReadOnlyDictionary<string, object>> 
            Cycle(Packet packet)
        {
            IEnumerable<Packet> EnumerateLayers()
            {
                for (var layer = packet; 
                    layer != null; 
                    layer = layer.Payload)
                    yield return layer;
            }
            IReadOnlyList<Packet> layers = EnumerateLayers()
                .ToList();

            IEnumerable<string> LayerIdentifiers(Packet layer)
            {
                foreach (var id in layer.GetType()
                    .GetCustomAttributes<Identifier>(true))
                    yield return id.Text;
            }
            var packet_layer_ids = 
                new HashSet<string>(layers
                    .SelectMany(LayerIdentifiers));
            var relevant_kernels = Kernels.Values
                .Where(k => k.LayerIdentifiers
                    .IsSubsetOf(packet_layer_ids));
            var matched_kernels = relevant_kernels
                .Where(k => k.Predicate(Symbols));
            object GetSymbolPacketValue(object o)
            {
                switch (o)
                {
                    case Type _:
                        return true;
                    case PropertyInfo pi:
                        return pi.GetValue(
                            layers.First(l 
                                => pi.ReflectedType
                                    .IsInstanceOfType(l)));
                    default:
                        throw new ArgumentException(nameof(o));
                }
            }
            object GetSymbolValues(string symbol)
            {
                var range = SymbolMap[symbol];
                if (range.Count == 1)
                    return GetSymbolPacketValue(
                        range.First());
                return range.Select(GetSymbolPacketValue)
                    as IReadOnlyCollection<object>;
            }
            var selection_set = matched_kernels
                .SelectMany(k => k.SelectionSymbols)
                .ToHashSet()
                .ToDictionary(
                    s => s,
                    GetSymbolValues);
            IReadOnlyDictionary<string, object> MapKernelSelection(
                ScalpelKernel kernel)
                {
                    var mapping = kernel.SelectionSymbols
                        .ToDictionary(
                            s => s,
                            s => selection_set[s]);
                    //mapping.Add("kernel_title", kernel.Title);
                    return mapping;
                }
            return matched_kernels
                .ToDictionary(
                    k => k.Title,
                    MapKernelSelection);
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
                .Where(s 
                    => SymbolMap[s].All(v 
                        => typeof(Type).IsAssignableFrom(v.GetType()))))
                Symbols.Add(
                    layer_id,
                    new SymbolState
                    {
                        Name = layer_id,
                        PredicateCounter = int.MaxValue,
                        SelectionCounter = int.MaxValue,
                        Value = true,
                    });
            
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
}
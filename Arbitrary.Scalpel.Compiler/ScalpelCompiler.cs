using System;
using System.Collections.Generic;
using System.Linq;
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
            }
        }

        public ScalpelProgram(IEnumerable<ScalpelKernel> kernels)
        {
            throw new NotImplementedException();
        }
    }

    public class ScalpelKernel
    {
        public readonly string Title;
        
        private readonly Predicate<Dictionary<string, SymbolState>> 
            Predicate;
        private readonly HashSet<string> PredicateSymbols;
        private readonly HashSet<string> SelectionSymbols;
        private readonly HashSet<string> DifferenceSymbols;

        private ScalpelKernel(KernelSyntax syntax)
        {
            Title = string.IsNullOrWhiteSpace(syntax.Title.Name)
                ? throw new ArgumentException(nameof(syntax))
                : syntax.Title.Name;
            
        }
    }
}
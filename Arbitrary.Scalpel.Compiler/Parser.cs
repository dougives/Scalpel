using System;

namespace Arbitrary.Scalpel.Compiler
{
    public class ScalpelKernel
    {
        public string Title { get; set; } = null;

    }



    public class Parser
    {    
        public class ParserException : Exception
        {
            private static string LocationMessage(
                Parser parser, 
                Token token)
            {
                var (line, column) = parser.Lexer.Source
                    .GetOffsetLineInfo(token.Offset);
                return $"({line}, {column})";
            }

            public ParserException(
                string message, 
                Parser parser, 
                Token token)
                : base(String
                    .IsNullOrWhiteSpace(message)
                        ? string.Empty
                        : $"{message}: " 
                    + LocationMessage(parser, token))
            { }

            public ParserException(
                string message, 
                Parser parser)
                : base($"{message}.")
            { }
        }

        private readonly Lexer Lexer;

        public Parser(Lexer lexer)
        {
            Lexer = lexer
                ?? throw new ArgumentNullException(nameof(lexer));
        }

        public ScalpelKernel Parse()
        {
            var kernel = new ScalpelKernel();
            var enumerator = Lexer.GetEnumerator();
            var moved = enumerator.MoveNext();
            if (!moved)
                throw new ParserException(
                    "Kernel is empty",
                    this);

            var token = enumerator.Current;
            if (token.Type != TokenType.Title)
                throw new ParserException(
                    "Expected title at start of kernel",
                    this, token);
            kernel.Title = token.ToString();

            throw new NotImplementedException();
        }
    }
}
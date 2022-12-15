﻿using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using Rubberduck.Parsing.Abstract;
using Rubberduck.Parsing.Grammar;

namespace Rubberduck.Parsing.Parsers
{
    public class VBAPreprocessorParser : TokenStreamParserBase
    {
        public VBAPreprocessorParser(IParsePassErrorListenerFactory sllErrorListenerFactory, IParsePassErrorListenerFactory llErrorListenerFactory)
            : base(sllErrorListenerFactory, llErrorListenerFactory)
        {
        }

        protected override IParseTree Parse(ITokenStream tokenStream, PredictionMode predictionMode, IParserErrorListener errorListener)
        {
            var parser = new VBAConditionalCompilationParser(tokenStream);
            parser.Interpreter.PredictionMode = predictionMode;
            parser.AddErrorListener(errorListener);
            return parser.compilationUnit();
        }
    }
}

﻿using Antlr4.Runtime;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.Parsing.Exceptions;

public class ReportingSyntaxErrorListener : RubberduckParseErrorListenerBase
{
    public ReportingSyntaxErrorListener(WorkspaceFileUri uri, ISyntaxErrorMessageService messageService) :base(uri, messageService) { }

    protected override List<AntlrSyntaxErrorInfo> Errors { get; } = [];
}

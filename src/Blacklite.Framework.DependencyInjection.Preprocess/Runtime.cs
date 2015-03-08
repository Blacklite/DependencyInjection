using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Runtime
{
    public interface ICompileModule
    {
        void BeforeCompile(IBeforeCompileContext context);

        void AfterCompile(IAfterCompileContext context);
    }

    public interface IBeforeCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<ResourceDescription> Resources { get; }

        IList<Diagnostic> Diagnostics { get; }
    }

    public interface IAfterCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<Diagnostic> Diagnostics { get; }
    }
}

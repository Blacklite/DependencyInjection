// TODO: To renable with Beta 2 and see if Vs support is working.
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.Framework.Runtime;

namespace Blacklite.Framework.DependencyInjection.compiler.preprocess
{
    public class Container<TDeclaration, TSymbol>
    {
        public Container(TDeclaration declaration, TSymbol symbol)
        {
            Declaration = declaration;
            Symbol = symbol;
        }

        public TDeclaration Declaration { get; }

        public TSymbol Symbol { get; }
    }

    public static class Diagnostics
    {
        class ImplementationMissMatchRule
        {
            const string DiagnosticId = "DI0001";
            const string Description = "Implementation miss-match.";
            const string MessageFormat = "The implementation '{0}' does not implement the service '{1}'.";
            const string Category = "Dependency Injection";

            internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                DiagnosticId,
                Description,
                MessageFormat,
                Category,
                DiagnosticSeverity.Error,
                true,
                /*description:*/ string.Empty,
                /*helpLink:*/ string.Empty);
        }

        public static DiagnosticDescriptor ImplementationMissMatch { get; } = ImplementationMissMatchRule.Rule;
    }

    public class PreprocessAnnotation : ICompileModule
    {
        private IEnumerable<Container<InvocationExpressionSyntax, IMethodSymbol>> GetAddAssemblyMethodCall(SyntaxTree syntaxTree, SemanticModel model)
        {
            // Find all classes that have our attribute.
            var addAssemblyExpressions = syntaxTree
                       .GetRoot()
                       .DescendantNodes()
                       .OfType<MemberAccessExpressionSyntax>()
                       .Where(declaration => declaration.Name.ToString().Contains("AddAssembly"))
                       .Where(declaration => declaration.Parent is InvocationExpressionSyntax)
                       .Where(declaration => model.GetSymbolInfo(declaration.Parent).Symbol is IMethodSymbol)
                       .Select(declaration => new Container<InvocationExpressionSyntax, IMethodSymbol>((InvocationExpressionSyntax)declaration.Parent, (IMethodSymbol)model.GetSymbolInfo(declaration.Parent).Symbol))
                       .Where(x => x.Symbol.IsExtensionMethod)
                       .Where(x => x.Symbol.ReceiverType.ToString() == "Microsoft.Framework.DependencyInjection.IServiceCollection");

            // Each return the container for each class
            foreach (var expression in addAssemblyExpressions)
            {
                if (expression.Declaration.ArgumentList.Arguments.Count() == 1)
                {
                    ClassDeclarationSyntax classSyntax;
                    SyntaxNode parent = expression.Declaration;
                    while (!(parent is ClassDeclarationSyntax))
                    {
                        parent = parent.Parent;
                    }
                    classSyntax = parent as ClassDeclarationSyntax;

                    var replace = false;

                    var typeofExpression = expression.Declaration.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax;
                    if (typeofExpression != null)
                    {
                        if (typeofExpression.Type.ToString() == classSyntax.Identifier.ToString())
                        {
                            replace = true;
                        }
                    }

                    var thisExpression = expression.Declaration.ArgumentList.Arguments[0].Expression as ThisExpressionSyntax;
                    if (thisExpression != null)
                    {
                        replace = true;
                    }

                    if (replace)
                    {
                        yield return expression;
                    }
                }
            }
        }

        private IEnumerable<Container<InvocationExpressionSyntax, IMethodSymbol>> GetFromAssemblyMethodCall(SyntaxTree syntaxTree, SemanticModel model)
        {
            // Find all classes that have our attribute.
            var fromAssemblyExpressions = syntaxTree
                       .GetRoot()
                       .DescendantNodes()
                       .OfType<MemberAccessExpressionSyntax>()
                       .Where(declaration => declaration.Name.ToString().Contains("FromAssembly"))
                       .Where(declaration => declaration.Parent is InvocationExpressionSyntax)
                       .Where(declaration => model.GetSymbolInfo(declaration.Parent).Symbol is IMethodSymbol)
                       .Select(declaration => new Container<InvocationExpressionSyntax, IMethodSymbol>((InvocationExpressionSyntax)declaration.Parent, (IMethodSymbol)model.GetSymbolInfo(declaration.Parent).Symbol))
                       .Where(x => x.Symbol.IsExtensionMethod)
                       .Where(x => x.Symbol.ReceiverType.ToString() == "Microsoft.Framework.DependencyInjection.ServiceDescriber");

            // Each return the container for each class
            foreach (var expression in fromAssemblyExpressions)
            {
                if (expression.Declaration.ArgumentList.Arguments.Count() == 1)
                {
                    ClassDeclarationSyntax classSyntax;
                    SyntaxNode parent = expression.Declaration;
                    while (!(parent is ClassDeclarationSyntax))
                    {
                        parent = parent.Parent;
                    }
                    classSyntax = parent as ClassDeclarationSyntax;

                    var replace = false;

                    var typeofExpression = expression.Declaration.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax;
                    if (typeofExpression != null)
                    {
                        if (typeofExpression.Type.ToString() == classSyntax.Identifier.ToString())
                        {
                            replace = true;
                        }
                    }

                    var thisExpression = expression.Declaration.ArgumentList.Arguments[0].Expression as ThisExpressionSyntax;
                    if (thisExpression != null)
                    {
                        replace = true;
                    }

                    if (replace)
                    {
                        yield return expression;
                    }
                }
            }
        }

        private IEnumerable<Container<ClassDeclarationSyntax, INamedTypeSymbol>> GetClassesWithImplementationAttribute(SyntaxTree syntaxTree, SemanticModel model)
        {
            // Find all classes that have our attribute.
            return syntaxTree
                       .GetRoot()
                       .DescendantNodes()
                       .OfType<ClassDeclarationSyntax>()
                       .Select(declaration => new Container<ClassDeclarationSyntax, INamedTypeSymbol>(declaration, model.GetDeclaredSymbol(declaration)))
                       .Where(x => x.Symbol.GetAttributes()
                           .Any(z => z.AttributeClass.Name.ToString().Contains("ServiceDescriptorAttribute")));
        }

        private NameSyntax BuildQualifiedName(string type, int genericTypeParamCount)
        {
            // Create a qualified name for every piece of the type.
            // If you don't do this... bad things happen.
            NameSyntax nameSyntax;
            var parts = type.Split('.');

            var identifiers = parts.Select(SyntaxFactory.IdentifierName).Cast<SimpleNameSyntax>();

            if (genericTypeParamCount > 0)
            {
                var name = parts.Last();
                name = name.Substring(0, name.IndexOf("<"));

                var ommitedTypeArguments = new List<TypeSyntax>();
                for (var i = 0; i < genericTypeParamCount; i++)
                {
                    ommitedTypeArguments.Add(SyntaxFactory.OmittedTypeArgument());
                }

                identifiers = identifiers.Take(parts.Count() - 1).Concat(new[] {
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier(name),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList(ommitedTypeArguments)
                        )
                    )
                });
            }

            if (identifiers.Count() > 1)
            {
                QualifiedNameSyntax qns = SyntaxFactory.QualifiedName(
                    identifiers.First(),
                    identifiers.Skip(1).First()
                );

                foreach (var usingId in identifiers.Skip(2))
                {
                    qns = SyntaxFactory.QualifiedName(qns, usingId);
                }

                nameSyntax = qns;
            }
            else
            {
                nameSyntax = identifiers.Single();
            }

            return nameSyntax;
        }

        private static string GetDisplayString(INamedTypeSymbol symbol)
        {
            string implementationType;
            if (symbol.IsGenericType)
                implementationType = symbol.ConstructUnboundGenericType().ToDisplayString();
            else
                implementationType = symbol.ToDisplayString();
            return implementationType;
        }

        private IEnumerable<StatementResult> GetStatements(IBeforeCompileContext context, IEnumerable<Container<ClassDeclarationSyntax, INamedTypeSymbol>> containers)
        {
            foreach (var container in containers)
            {
                var symbol = container.Symbol;
                var declaration = container.Declaration;

                var attributeSymbol = symbol.GetAttributes().Single(x => x.AttributeClass.Name.ToString().Contains("ServiceDescriptorAttribute"));
                var attributeDeclaration = declaration.AttributeLists
                    .SelectMany(z => z.Attributes)
                    .Single(z => z.Name.ToString().Contains("ServiceDescriptor"));

                string implementationType = GetDisplayString(symbol);

                var implementationQualifiedName = BuildQualifiedName(implementationType, symbol.TypeParameters.Count());

                string serviceType = null;
                IEnumerable<NameSyntax> serviceQualifiedNames = symbol.AllInterfaces
                    .Select(z => BuildQualifiedName(GetDisplayString(z), z.TypeParameters.Count()));

                if (declaration.Modifiers.Any(z => z.RawKind == (int)SyntaxKind.PublicKeyword))
                {
                    // TODO:  Should this include all base types?  Should it be the lowest base type (HttpContext for example)?
                    serviceQualifiedNames = serviceQualifiedNames.Union(new NameSyntax[] { implementationQualifiedName });
                }

                if (attributeSymbol.ConstructorArguments.Count() > 0 && attributeSymbol.ConstructorArguments[0].Value != null)
                {
                    var serviceNameTypedSymbol = (INamedTypeSymbol)attributeSymbol.ConstructorArguments[0].Value;
                    if (serviceNameTypedSymbol == null)
                        throw new Exception("Could not infer service symbol");
                    serviceType = GetDisplayString(serviceNameTypedSymbol);
                    serviceQualifiedNames = new NameSyntax[] { BuildQualifiedName(serviceType, serviceNameTypedSymbol.TypeParameters.Count()) };
                }


                var baseTypes = new List<string>();
                var impType = symbol;
                while (impType != null)
                {
                    baseTypes.Add(impType.ToDisplayString());
                    impType = impType.BaseType;
                }

                // TODO: Enforce implementation is assignable to service
                // Diagnostic error?
                var potentialBaseTypes = baseTypes.Concat(symbol.AllInterfaces.Select(GetDisplayString));

                // This is where some of the power comes out.
                // We now have the ability to throw compile time errors if we believe something is wrong.
                // This could be extended to support generic types, and potentially matching compatible open generic types together to build a list.
                if (serviceType != null && !potentialBaseTypes.Any(z => serviceType.Equals(z, StringComparison.OrdinalIgnoreCase)))
                {
                    var serviceName = serviceType.Split('.').Last();
                    var implementationName = implementationType.Split('.').Last();

                    context.Diagnostics.Add(
                        Diagnostic.Create(
                            Diagnostics.ImplementationMissMatch,
                            attributeDeclaration.GetLocation(),
                            implementationName,
                            serviceName
                        )
                    );
                }

                var hasLifecycle = attributeSymbol.NamedArguments.Any(z => z.Key == "Lifecycle");
                var lifecycle = "Transient";

                if (hasLifecycle)
                {
                    lifecycle = GetLifecycle((int)attributeSymbol.NamedArguments.Single(z => z.Key == "Lifecycle").Value.Value);
                }

                // Build the Statement
                yield return new StatementResult()
                {
                    implementationQualifiedName = implementationQualifiedName,
                    lifecycle = lifecycle,
                    serviceQualifiedNames = serviceQualifiedNames
                };
            }
        }

        class StatementResult
        {
            public string lifecycle;
            public IEnumerable<NameSyntax> serviceQualifiedNames;
            public NameSyntax implementationQualifiedName;
        }

        public string GetLifecycle(int enumValue)
        {
            string lifecycle = "Transient";
            switch (enumValue)
            {
                case 1:
                    lifecycle = "Scoped";
                    break;
                case 0:
                    lifecycle = "Singleton";
                    break;
            }

            return lifecycle;
        }

        private Func<string, IEnumerable<StatementSyntax>> GetCollectionAddAssemblyExpressionStatement(StatementResult result)
        {
            // I hear there is a better way to do this... that will be released sometime.
            return (string identifierName) =>
            {
                return result.serviceQualifiedNames.Select(serviceName => SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(identifierName),
                                name: SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Add" + result.lifecycle))
                            ),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    new[] {
                                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(serviceName)),
                                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(result.implementationQualifiedName))
                                    })
                            )
                        )
                    )
                );
            };
        }

        private Func<string, IEnumerable<ExpressionSyntax>> GetCollectionFromAssemblyExpressionStatement(StatementResult result)
        {
            // I hear there is a better way to do this... that will be released sometime.
            return (string identifierName) =>
            {
                return result.serviceQualifiedNames.Select(serviceName =>
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(identifierName),
                            name: SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(result.lifecycle))
                        ),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new[] {
                                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(serviceName)),
                                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(result.implementationQualifiedName))
                                })
                        )
                    )
                );
            };
        }

        public void BeforeCompile(IBeforeCompileContext context)
        {
            var newCompilation = context.CSharpCompilation;

            // Find all the class containers we care about.
            var containers = newCompilation.SyntaxTrees
                .Select(tree => new
                {
                    Model = newCompilation.GetSemanticModel(tree),
                    SyntaxTree = tree,
                    Root = tree.GetRoot()
                });

            // Build the registration statements out of the containers.
            var addAssemblyNodes = GetStatements(context, containers.SelectMany(ctx => GetClassesWithImplementationAttribute(ctx.SyntaxTree, ctx.Model)).ToArray()).ToArray();
            var addAssemblyMethodCalls = containers.SelectMany(ctx => GetAddAssemblyMethodCall(ctx.SyntaxTree, ctx.Model));
            newCompilation = ProcessAddAssemblyMethodCalls(newCompilation, addAssemblyNodes, addAssemblyMethodCalls);

            containers = newCompilation.SyntaxTrees
                .Select(tree => new
                {
                    Model = newCompilation.GetSemanticModel(tree),
                    SyntaxTree = tree,
                    Root = tree.GetRoot()
                });

            var fromAssemblyNodes = GetStatements(context, containers.SelectMany(ctx => GetClassesWithImplementationAttribute(ctx.SyntaxTree, ctx.Model)).ToArray()).ToArray();
            var fromAssemblyMethodCalls = containers.SelectMany(ctx => GetFromAssemblyMethodCall(ctx.SyntaxTree, ctx.Model));
            newCompilation = ProcessFromAssemblyMethodCalls(newCompilation, fromAssemblyNodes, fromAssemblyMethodCalls);

            context.CSharpCompilation = newCompilation;
        }

        private CSharpCompilation ProcessAddAssemblyMethodCalls(CSharpCompilation compilation, IEnumerable<StatementResult> nodes, IEnumerable<Container<InvocationExpressionSyntax, IMethodSymbol>> containers)
        {
            foreach (var methods in containers.GroupBy(m => m.Declaration.SyntaxTree))
            {
                SyntaxNode syntaxRoot = null;
                foreach (var method in methods)
                {
                    if (syntaxRoot == null)
                        syntaxRoot = method.Declaration.SyntaxTree.GetRoot();

                    var identifierName = ((MemberAccessExpressionSyntax)method.Declaration.Expression).Expression.ToString();

                    var methodNodes = nodes.Select(GetCollectionAddAssemblyExpressionStatement).SelectMany(x => x(identifierName));

                    syntaxRoot = syntaxRoot
                        .ReplaceNode(method.Declaration.Parent, methodNodes)
                        .NormalizeWhitespace();
                }

                var syntaxTree = methods.Key;
                var newSyntaxTree = syntaxTree.WithRootAndOptions(syntaxRoot, syntaxTree.Options);

                compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
            }

            return compilation;
        }

        private CSharpCompilation ProcessFromAssemblyMethodCalls(CSharpCompilation compilation, IEnumerable<StatementResult> nodes, IEnumerable<Container<InvocationExpressionSyntax, IMethodSymbol>> containers)
        {
            foreach (var methods in containers.GroupBy(m => m.Declaration.SyntaxTree))
            {
                SyntaxNode syntaxRoot = null;
                foreach (var method in methods)
                {
                    if (syntaxRoot == null)
                    {
                        syntaxRoot = method.Declaration.SyntaxTree.GetRoot();
                    }

                    var identifierName = ((MemberAccessExpressionSyntax)method.Declaration.Expression).Expression.ToString();

                    var methodNodes = nodes.Select(GetCollectionFromAssemblyExpressionStatement).SelectMany(x => x(identifierName));
                    var node = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.ImplicitArrayCreationExpression(
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList(methodNodes)
                            )
                        )
                    );

                    syntaxRoot = syntaxRoot
                       .ReplaceNode(method.Declaration, node.Expression)
                       .NormalizeWhitespace();
                }

                var syntaxTree = methods.Key;
                var newSyntaxTree = syntaxTree.WithRootAndOptions(syntaxRoot, syntaxTree.Options);

                compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
            }

            return compilation;
        }

        public void AfterCompile(IAfterCompileContext context)
        {
            // Not Used
        }
    }
}

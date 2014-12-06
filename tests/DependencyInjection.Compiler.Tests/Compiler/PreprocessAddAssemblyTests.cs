using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.Runtime;
using System;
using System.Reflection;
using System.Linq;
using Xunit;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Blacklite.Framework.DependencyInjection.compiler.preprocess;

namespace DependencyInjection.Compiler.Tests.Compiler
{

    public class PreprocessAddAssemblyTests
    {
        private CSharpCompilation GetCompilation()
        {
            return CSharpCompilation.Create("PreprocessAnnotationTests.dll",
                references: new[] {
                // This isn't very nice...
                    MetadataReference.CreateFromStream(System.IO.File.OpenRead(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll"))
                })
                // This way we don't have to reference anything but mscorlib.
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                    using System;

                    namespace Microsoft.Framework.DependencyInjection {
                        public enum LifecycleKind {
                            Singleton,
                            Scoped,
	                        Transient
                        }

                        [AttributeUsage(AttributeTargets.Class)]
                        public class ServiceDescriptorAttribute : Attribute
                        {
                            public ServiceDescriptorAttribute(Type serviceType = null)
                            {
                                ServiceType = serviceType;
                            }

                            public Type ServiceType { get; set; }
                            public LifecycleKind Lifecycle { get; set; } = LifecycleKind.Transient;
                        }

                        public interface IServiceCollection {}

                        public static class ServiceCollectionExtensions {
                            public static IServiceCollection AddAssembly(this IServiceCollection collection, Assembly assembly) {
                                return collection;
                            }

                            public static IServiceCollection AddAssembly(this IServiceCollection collection, Type type) {
                                return collection;
                            }

                            public static IServiceCollection AddAssembly(this IServiceCollection collection, object @this) {
                                return collection;
                            }
                        }
                    }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                    using Microsoft.Framework.DependencyInjection;
                    namespace Temp {
                        public class Startup {
                            void Configure(IServiceCollection services) {
                                services.AddAssembly(typeof(Startup));
                            }

                            public static int Main() { return 0; }
                        }
                    }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact]
        public void Compiles()
        {
            var compilation = GetCompilation();

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void DefaultsToTransient()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA))]
                    public class ProviderA : IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddTransient(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }


        [Fact]
        public void UnderstandsTransient()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Transient)]
                    public class ProviderA : IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddTransient(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }


        [Fact]
        public void UnderstandsScoped()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Scoped)]
                    public class ProviderA : IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddScoped(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void UnderstandsSingleton()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA : IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddSingleton(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void UnderstandsEverything()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA : IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderB1
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderB1))]
                    public class ProviderB1 : IProviderB1
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderB
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderB), Lifecycle = LifecycleKind.Transient)]
                    public class ProviderB : IProviderB
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderC
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderC), Lifecycle = LifecycleKind.Scoped)]
                    public class ProviderC : IProviderC
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddSingleton(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
            services.AddTransient(typeof (Temp.Providers.IProviderB1), typeof (Temp.Providers.ProviderB1));
            services.AddTransient(typeof (Temp.Providers.IProviderB), typeof (Temp.Providers.ProviderB));
            services.AddScoped(typeof (Temp.Providers.IProviderC), typeof (Temp.Providers.ProviderC));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void ReportsDiagnostics()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA : IProviderB
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));


            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.True(context.Diagnostics.Any());
        }

        [Fact]
        public void AddAssemblyWorksWithThisExpression()
        {
            var compilation = GetCompilation();

            compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                        using Microsoft.Framework.DependencyInjection;
                        namespace Temp {
                            public class Startup {
                                void Configure(IServiceCollection services) {
                                    services.AddAssembly(this);
                                }

                                public static int Main() { return 0; }
                            }
                        }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());
        }

        [Fact]
        public void AddAssemblyWorksWithTypeExpression()
        {
            var compilation = GetCompilation();

            compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                        using Microsoft.Framework.DependencyInjection;
namespace Temp {
                            public class Startup {
                                void Configure(IServiceCollection services) {
                                    services.AddAssembly(typeof(Startup));
                                }

                                public static int Main() { return 0; }
                            }
                        }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());
        }

        [Fact]
        public void AddAssemblyWorksWithAssemblyExpression()
        {
            var compilation = GetCompilation();

            compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                        using Microsoft.Framework.DependencyInjection;
namespace Temp {
                            public class Startup {
                                void Configure(IServiceCollection services) {
                                    services.AddAssembly(typeof(Startup).GetTypeInfo().Assembly);
                                }

                                public static int Main() { return 0; }
                            }
                        }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());
        }

        [Fact]
        public void AddAssemblyIsLeftAloneForOtherTypes()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                    using Microsoft.Framework.DependencyInjection;
namespace Temp {
                        public class NotStartup {
                        }

                        public class Startup2 {
                            void Configure(IServiceCollection collection) {
                                collection.AddAssembly(typeof(NotStartup));
                            }
                        }
                    }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));


            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());

            Assert.Equal(@"
                    using Microsoft.Framework.DependencyInjection;
namespace Temp {
                        public class NotStartup {
                        }

                        public class Startup2 {
                            void Configure(IServiceCollection collection) {
                                collection.AddAssembly(typeof(NotStartup));
                            }
                        }
                    }", context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());
        }

        [Fact]
        public void AddAssemblyIsReplacedByDefault()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"
                    using Microsoft.Framework.DependencyInjection;
namespace Temp {
                        public class Startup2 {
                            void Configure(IServiceCollection collection) {
                                collection.AddAssembly(typeof(Startup2));
                            }
                        }
                    }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));


            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup2
    {
        void Configure(IServiceCollection collection)
        {
        }
    }
}", context.CSharpCompilation.SyntaxTrees.Last().GetText().ToString());
        }

        [Fact]
        public void DiagnosticsStillFunctionEvenIfReplacementNeverHappens()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA : IProviderB
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.True(context.Diagnostics.Any());
        }

        [Fact]
        public void RegistersAllInterfacesIfServiceTypeIsNotDefined()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB
                    {
                        decimal GetValue2();
                    }

                    [ServiceDescriptor(Lifecycle = LifecycleKind.Scoped)]
                    class ProviderA : IProviderB, IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }

                        public decimal GetValue2()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddScoped(typeof (Temp.Providers.IProviderB), typeof (Temp.Providers.ProviderA));
            services.AddScoped(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void RegistersAllInterfacesAndTheClassIfTheClassIsPublicIfServiceTypeIsNotDefined()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB
                    {
                        decimal GetValue2();
                    }

                    [ServiceDescriptor( Lifecycle = LifecycleKind.Singleton )]
                    public class ProviderA : IProviderB, IProviderA
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }

                        public decimal GetValue2()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddSingleton(typeof (Temp.Providers.IProviderB), typeof (Temp.Providers.ProviderA));
            services.AddSingleton(typeof (Temp.Providers.IProviderA), typeof (Temp.Providers.ProviderA));
            services.AddSingleton(typeof (Temp.Providers.ProviderA), typeof (Temp.Providers.ProviderA));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void UnderstandsOpenEverything()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA<T,Y>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA<,>), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA<T,Y> : IProviderA<T,Y>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderB1<T>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderB1<>))]
                    public class ProviderB1<T> : IProviderB1<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderB<T>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderB<T>), Lifecycle = LifecycleKind.Transient)]
                    public class ProviderB<T> : IProviderB<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)))
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderC<T>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderC<>), Lifecycle = LifecycleKind.Scoped)]
                    public class ProviderC<T> : IProviderC<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddSingleton(typeof (Temp.Providers.IProviderA<, >), typeof (Temp.Providers.ProviderA<, >));
            services.AddTransient(typeof (Temp.Providers.IProviderB1<>), typeof (Temp.Providers.ProviderB1<>));
            services.AddTransient(typeof (Temp.Providers.IProviderB<>), typeof (Temp.Providers.ProviderB<>));
            services.AddScoped(typeof (Temp.Providers.IProviderC<>), typeof (Temp.Providers.ProviderC<>));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void ReportsOpenDiagnostics()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA<T>
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB<T>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA<>), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA<T> : IProviderB<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));


            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.True(context.Diagnostics.Any());
        }

        [Fact]
        public void OpenDiagnosticsStillFunctionEvenIfReplacementNeverHappens()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA<T>
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB<T>
                    {
                        decimal GetValue();
                    }

                    [ServiceDescriptor(typeof(IProviderA<>), Lifecycle = LifecycleKind.Singleton)]
                    public class ProviderA<T> : IProviderB<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.True(context.Diagnostics.Any());
        }

        [Fact]
        public void RegistersAllInterfacesIfOpenServiceTypeIsNotDefined()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA<T>
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB<T>
                    {
                        decimal GetValue2();
                    }

                    [ServiceDescriptor(Lifecycle = LifecycleKind.Scoped)]
                    class ProviderA<T> : IProviderB<T>, IProviderA<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }

                        public decimal GetValue2()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddScoped(typeof (Temp.Providers.IProviderB<>), typeof (Temp.Providers.ProviderA<>));
            services.AddScoped(typeof (Temp.Providers.IProviderA<>), typeof (Temp.Providers.ProviderA<>));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }

        [Fact]
        public void RegistersAllInterfacesAndTheClassIfTheClassIsPublicIfOpenServiceTypeIsNotDefined()
        {
            var compilation = GetCompilation()
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(SourceText.From(@"using Microsoft.Framework.DependencyInjection;
                namespace Temp.Providers
                {
                    public interface IProviderA<T>
                    {
                        decimal GetValue();
                    }

                    public interface IProviderB<T>
                    {
                        decimal GetValue2();
                    }

                    [ServiceDescriptor( Lifecycle = LifecycleKind.Singleton )]
                    public class ProviderA<T> : IProviderB<T>, IProviderA<T>
                    {
                        public decimal GetValue()
                        {
                            return 9000.99M;
                        }

                        public decimal GetValue2()
                        {
                            return 9000.99M;
                        }
                    }
                }", Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

            var context = new BeforeCompileContext()
            {
                CSharpCompilation = compilation
            };

            var unit = new PreprocessAnnotation();

            unit.BeforeCompile((Microsoft.Framework.Runtime.IBeforeCompileContext)context);
            Assert.False(context.Diagnostics.Any());

            //Console.WriteLine(context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());

            Assert.Equal(@"using Microsoft.Framework.DependencyInjection;

namespace Temp
{
    public class Startup
    {
        void Configure(IServiceCollection services)
        {
            services.AddSingleton(typeof (Temp.Providers.IProviderB<>), typeof (Temp.Providers.ProviderA<>));
            services.AddSingleton(typeof (Temp.Providers.IProviderA<>), typeof (Temp.Providers.ProviderA<>));
            services.AddSingleton(typeof (Temp.Providers.ProviderA<>), typeof (Temp.Providers.ProviderA<>));
        }

        public static int Main()
        {
            return 0;
        }
    }
}", context.CSharpCompilation.SyntaxTrees.First(x => x.GetText().ToString().Contains("class Startup")).GetText().ToString());
        }
    }
}

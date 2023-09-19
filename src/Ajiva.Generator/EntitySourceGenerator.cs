using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Ajiva.Generator;

[Generator]
public class EntitySourceGenerator : ISourceGenerator
{
    public const string AttributeName = @"EntityComponent";

    public void Initialize(GeneratorInitializationContext context)
    {
        // register receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new EntitySyntaxReceiver());
    }

    private static string PropName(string type)
    {
        return /*"Com" + */Regex.Replace(type.StartsWith("I") ? type.Substring(1) : type,
            "(?:^|_| +)(.)", match => match.Groups[1].Value.ToUpper());
    }

    // determine the namespace the class/enum/struct is declared in, if any
    static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        // If we don't have a namespace at all we'll return an empty string
        // This accounts for the "default namespace" case
        string nameSpace = string.Empty;

        // Get the containing syntax node for the type declaration
        // (could be a nested type, for example)
        SyntaxNode? potentialNamespaceParent = syntax.Parent;

        // Keep moving "out" of nested classes etc until we get to a namespace
        // or until we run out of parents
        while (potentialNamespaceParent != null &&
               potentialNamespaceParent is not NamespaceDeclarationSyntax
               && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        // Build up the final namespace by looping until we no longer have a namespace declaration
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // We have a namespace. Use that as the type
            nameSpace = namespaceParent.Name.ToString();

            // Keep moving "out" of the namespace declarations until we 
            // run out of nested namespace declarations
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                // Add the outer namespace as a prefix to the final namespace
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        // return the final namespace
        return nameSpace;
    }

    public void Execute(GeneratorExecutionContext context)
    {
        //Debugger.Launch();
        //add error to the compilation
        Compilation? compilation = context.Compilation;
        AddHelpers(context);

        // get the populated receiver
        if (context.SyntaxReceiver is not EntitySyntaxReceiver receiver) return;

        List<string> usings = new();
        List<string> names = new();

        // Extract all tyoes from [EntityComponentAttribute(typeof(T), typeof(T2), ...)]
        // all Classes that have this attribute will generate properties for each type
        // found classes are stored in receiver.EntityComponents
        foreach (var classDeclaration in receiver.EntityComponents)
        {
            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDeclaration);
            names.Add(symbol!.Name);

            var attributeList = classDeclaration.AttributeLists
                .FirstOrDefault(x =>
                    x.Attributes.Any(y => y.Name.ToString().StartsWith(AttributeName))
                );
            var attribute = attributeList?.Attributes.FirstOrDefault(x => x.Name.ToString().StartsWith(AttributeName));
            if (attribute is null) continue;

            var types = attribute.ArgumentList?.Arguments.Select(
                x =>
                {
                    if (x.Expression is TypeOfExpressionSyntax typeOfExpressionSyntax)
                        return typeOfExpressionSyntax.Type.ToString();
                    //if (x.Expression is NameOfExpressionSyntax typeOfExpressionSyntax)
                    //  return typeOfExpressionSyntax.Type.ToString();
                    return x.Expression.ToString();
                }).ToArray();
            if (types is null) continue;

            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("AJ10001", "Generateing Entity: " + symbol.Name, "Generateing Entity: " + symbol.Name, "Generateing Entity: " + symbol.Name, DiagnosticSeverity.Info, true, "Generateing Entity: " + symbol.Name), Location.None));

            using var writer = new IndentedTextWriter(new StringWriter(), "    ");

            void Indent()
            {
                writer.WriteLine("{");
                writer.Indent++;
            }

            void UnIndent()
            {
                writer.Indent--;
                writer.WriteLine("}");
            }

            writer.WriteLine("// <auto-generated />");
            //writer.WriteLine("#nullable enable");
            writer.WriteLine("#pragma warning disable CS0105");
            writer.WriteLine("#pragma warning disable CS8669");
            writer.WriteLine("#pragma warning disable CS8618");
            writer.WriteLine("#pragma warning disable CS0162");
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Diagnostics.CodeAnalysis;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using Ajiva.Ecs.Entity.Helper;");
            writer.WriteLine("using Autofac;");
            writer.WriteLine("using Autofac.Builder;");
            writer.WriteLine("using Autofac.Core;");
            writer.WriteLine("using Ajiva.Ecs;");

            //copy all usings from the class
            foreach (var usingDirectiveSyntax in classDeclaration.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                writer.WriteLine(usingDirectiveSyntax.ToString());
            }
            usings.Add(symbol.ContainingNamespace.ToString());
            writer.WriteLine("namespace {0};", symbol.ContainingNamespace);

            if (symbol.ContainingType is not null)
            {
                writer.WriteLine("{0} {1} partial {2} {3}", symbol.ContainingType.DeclaredAccessibility.ToString().ToLower(), symbol.ContainingType.IsStatic ? "static" : "", symbol.ContainingType.TypeKind.ToString().ToLower(), symbol.ContainingType.Name);
                Indent();
            }

            writer.WriteLine("[SuppressMessage(\"ReSharper\", \"InconsistentNaming\")]");
            writer.WriteLine("[SuppressMessage(\"ReSharper\", \"UnusedMember.Global\")]");
            writer.WriteLine("[SuppressMessage(\"ReSharper\", \"MemberCanBePrivate.Global\")]");

            writer.WriteLine("public partial class {0} : IEntity", symbol.Name);
            Indent();

            writer.WriteLine("public Guid Id { get; } = Guid.NewGuid();");

            foreach (var type in types)
            {
                writer.WriteLine("public {0} {1} {{ get; protected set; }}", type, PropName(type));
            }

            writer.WriteLine("public bool TryGetComponent<TComponent>([MaybeNullWhen(false)] out TComponent value) where TComponent : IComponent");
            Indent();

            foreach (var type in types)
            {
                Indent();
                writer.WriteLine("if ({0} is TComponent val and not null)", PropName(type));
                Indent();
                writer.WriteLine("value = val;");
                writer.WriteLine("return true;");
                UnIndent();
                UnIndent();
            }
            writer.WriteLine("value = default;");
            writer.WriteLine("return false;");
            UnIndent();

            writer.WriteLine("public bool HasComponent<TComponent>() where TComponent : IComponent");
            Indent();

            foreach (var type in types)
            {
                writer.WriteLine("if ({0} is TComponent and not null)", PropName(type));
                Indent();
                writer.WriteLine("return true;");
                UnIndent();
            }
            writer.WriteLine("return false;");
            UnIndent();

            writer.WriteLine("public TComponent Get<TComponent>() where TComponent : IComponent");
            Indent();
            foreach (var type in types)
            {
                Indent();
                writer.WriteLine("if ({0} is TComponent val and not null)", PropName(type));
                Indent();
                writer.WriteLine("return val;");
                UnIndent();
                UnIndent();
            }
            writer.WriteLine("throw new KeyNotFoundException(typeof(TComponent).Name);");
            writer.WriteLine("return default;");
            UnIndent();

            writer.WriteLine("public {0} Configure<TComponent>(Action<TComponent> configuration) where TComponent : IComponent", symbol.Name);
            Indent();
            foreach (var type in types)
            {
                Indent();
                writer.WriteLine("if ({0} is TComponent val and not null)", PropName(type));
                Indent();
                writer.WriteLine("configuration(val);");
                writer.WriteLine("return this;");
                UnIndent();
                UnIndent();
            }
            writer.WriteLine("throw new KeyNotFoundException(typeof(TComponent).Name);");
            writer.WriteLine("return this;");
            UnIndent();

            //partial dispose
            //writer.WriteLine("public partial void Dispose();");

            //IEnumerator<IComponent>
            writer.WriteLine("public IEnumerable<IComponent> GetComponents()");
            Indent();
            foreach (var type in types)
            {
                writer.WriteLine("yield return " + PropName(type) + ";");
            }
            UnIndent();

            writer.WriteLine("public IEnumerable<Type> GetComponentTypes()");
            Indent();
            foreach (var type in types)
            {
                writer.WriteLine("yield return typeof(" + type + ");");
            }
            UnIndent();

            writer.WriteLine("protected {0}() {{}}", symbol.Name);
            writer.WriteLine("internal static {0} CreateEmpty() {{ return new(); }}", symbol.Name);

            writer.WriteLine("public ref struct Creator");
            Indent();
            writer.WriteLine("public {0}FactoryData FactoryData;", symbol.Name);
            foreach (var t in types)
            {
                writer.WriteLine("public {0}? {1};", t, PropName(t));
            }
            writer.WriteLine("public {0} Create()", symbol.Name);
            Indent();
            writer.WriteLine("var entity = new {0}();", symbol.Name);
            /*foreach (var t in types)
            {
                writer.WriteLine("entity.{0} = {0} is not null ? {0} : FactoryData.{0}.CreateComponent();", PropName(t));
            }*/
            foreach (var t in types)
            {
                writer.WriteLine("if({0} is not null) entity.{0} = {0};", PropName(t));
            }
            if (classDeclaration.Members.OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.ToString() == "InitializeDefault"))
                writer.WriteLine("entity.InitializeDefault();");
            foreach (var t in types)
            {
                writer.WriteLine("if(entity.{0} is null) entity.{0} = FactoryData.{0}.CreateComponent(entity);", PropName(t));
            }
            writer.WriteLine("return entity;");
            UnIndent();
            writer.WriteLine("public {0} Finalize()", symbol.Name);
            Indent();
            writer.WriteLine("var entity = Create();");
            foreach (var type in types)
            {
                writer.WriteLine("FactoryData.{0}.RegisterComponent(entity, entity.{0});", PropName(type));
            }
            writer.WriteLine("FactoryData.EntityRegistry.RegisterEntity(entity);");
            writer.WriteLine("return entity;");
            UnIndent();
            foreach (var t in types)
            {
                writer.WriteLine("public {0}.Creator With({1} val) {{ {2} = val; return this; }}", symbol.Name, t, PropName(t));
            }
            UnIndent();

            if (symbol.ContainingType is not null)
            {
                UnIndent();
            }
            UnIndent();

            writer.WriteLine("public partial record {0}FactoryData(", symbol.Name);
            foreach (var type in types)
            {
                writer.WriteLine("IComponentSystem<{0}> {1}, ", type, PropName(type));
            }
            writer.WriteLine("IEntityRegistry EntityRegistry) : IFactoryData");
            Indent();
            writer.WriteLine("public {0}.Creator Begin() => new() {{ FactoryData = this }};", symbol.Name);
            UnIndent();

            //Debugger.Launch();
            //Console.WriteLine(writer.InnerWriter.ToString());
            writer.WriteLine("#pragma warning restore CS0105");
            writer.WriteLine("#pragma warning restore CS8669");
            writer.WriteLine("#pragma warning restore CS0162");
            writer.WriteLine("#pragma warning restore CS8618");
            //writer.WriteLine("#nullable restore");
            
            context.AddSource($"{symbol?.Name}_EntityComponent.cs", SourceText.From(writer.InnerWriter.ToString(), Encoding.UTF8));
        }

        AddFactory(context, usings, names);
    }

    private static void AddFactory(GeneratorExecutionContext context, List<string> usings, List<string> names)
    {
        using var cWriter = new IndentedTextWriter(new StringWriter(), "    ");

        void CIndent()
        {
            cWriter.WriteLine("{");
            cWriter.Indent++;
        }

        void CUnIndent()
        {
            cWriter.Indent--;
            cWriter.WriteLine("}");
        }

        cWriter.WriteLine("// <auto-generated />");
        cWriter.WriteLine("using System;");
        cWriter.WriteLine("using System.Collections.Generic;");
        cWriter.WriteLine("using System.Diagnostics.CodeAnalysis;");
        cWriter.WriteLine("using System.Linq;");
        cWriter.WriteLine("using Ajiva.Ecs.Entity.Helper;");
        cWriter.WriteLine("using Autofac;");
        cWriter.WriteLine("using Autofac.Builder;");
        cWriter.WriteLine("using Autofac.Core;");
        cWriter.WriteLine("");
        foreach (var @using in usings)
        {
            cWriter.WriteLine("using {0};", @using);
        }
        cWriter.WriteLine("");

        cWriter.WriteLine("namespace {0}.Extensions", context.Compilation.AssemblyName);
        CIndent();
        cWriter.WriteLine("public static class EntityComponentHelperExtensions");
        CIndent();

        cWriter.WriteLine("public static ContainerBuilder AddFactoryData(this ContainerBuilder builder)");
        CIndent();
        cWriter.WriteLine("builder.RegisterType<EntityFactory>().AsSelf().SingleInstance();");
        foreach (var name in names)
        {
            cWriter.WriteLine("builder.RegisterType<{0}FactoryData>().AsSelf().SingleInstance();", name);
        }
        cWriter.WriteLine("return builder;");
        CUnIndent();

        foreach (var name in names)
        {
            cWriter.WriteLine("public static {0}.Creator Create{0}(this IContainer container)", name);
            CIndent();
            cWriter.WriteLine("return new()");
            CIndent();
            cWriter.WriteLine("FactoryData = container.Resolve<{0}FactoryData>(),", name);
            CUnIndent();
            cWriter.WriteLine(";");
            CUnIndent();
        }
        CUnIndent();

        cWriter.WriteLine("public partial record EntityFactory(");
        cWriter.Indent++;
        var first = true;
        foreach (var name in names)
        {
            if (first) first = false;
            else cWriter.WriteLine(",");
            cWriter.WriteLine("{0}FactoryData {0}FactoryData", name);
        }
        cWriter.Indent--;
        cWriter.WriteLine(")");
        CIndent();
        foreach (var name in names)
        {
            cWriter.WriteLine("public {0}.Creator Create{0}()", name);
            CIndent();
            cWriter.WriteLine("return new()");
            CIndent();
            cWriter.WriteLine("FactoryData = {0}FactoryData,", name);
            CUnIndent();
            cWriter.WriteLine(";");
            CUnIndent();
        }
        CUnIndent();

        CUnIndent();
        //Debugger.Launch();
        context.AddSource("EntityContainerHelper.cs", SourceText.From(cWriter.InnerWriter.ToString(), Encoding.UTF8));
    }

    private void AddHelpers(GeneratorExecutionContext context)
    {
        // add attribute
        context.AddSource("EntityComponentAttribute", SourceText.From($$"""
            using System;
            
            namespace Ajiva.Ecs.Entity.Helper
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class {{AttributeName}}Attribute : Attribute
                {
                    public Type[] Types { get; }
            
                    public EntityComponentAttribute(params Type[] types)
                    {
                        Types = types;
                    }
                }
            }
            """, Encoding.UTF8));
    }
}
public class EntitySyntaxReceiver : ISyntaxReceiver
{
    /// <inheritdoc />
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // check if the class has the attribute
        if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax ||
            !classDeclarationSyntax.AttributeLists.Any()) return;

        // check if the attribute is the one we are looking for
        if (!classDeclarationSyntax.AttributeLists
                .SelectMany(x => x.Attributes)
                .Any(x => x.Name.ToString().StartsWith(EntitySourceGenerator.AttributeName))) return;

        EntityComponents.Add(classDeclarationSyntax);
    }

    public List<ClassDeclarationSyntax> EntityComponents { get; set; } = new();
}

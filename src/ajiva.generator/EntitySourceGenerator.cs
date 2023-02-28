using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ajiva.generator;

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

        // Extract all tyoes from [EntityComponentAttribute(typeof(T), typeof(T2), ...)]
        // all Classes that have this attribute will generate properties for each type
        // found classes are stored in receiver.EntityComponents
        foreach (var classDeclaration in receiver.EntityComponents)
        {
            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDeclaration);

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

            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("ajiva.ecs.info", "Generateing Entity: " + symbol.Name, "Generateing Entity: " + symbol.Name, "Generateing Entity: " + symbol.Name, DiagnosticSeverity.Warning, true, "Generateing Entity: " + symbol.Name), Location.None));

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
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Diagnostics.CodeAnalysis;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using ajiva.Ecs.Entity.Helper;");
            //copy all usings from the class
            foreach (var usingDirectiveSyntax in classDeclaration.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                writer.WriteLine(usingDirectiveSyntax.ToString());
            }

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

            if (symbol.ContainingType is not null)
            {
                UnIndent();
            }
            UnIndent();

            //Debugger.Launch();
            context.AddSource($"{symbol?.Name}_EntityComponent.cs", SourceText.From(writer.InnerWriter.ToString(), Encoding.UTF8));
        }
    }

    private void AddHelpers(GeneratorExecutionContext context)
    {
        // add attribute
        context.AddSource("EntityComponentAttribute", SourceText.From($$"""
            using System;
            
            namespace ajiva.Ecs.Entity.Helper
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

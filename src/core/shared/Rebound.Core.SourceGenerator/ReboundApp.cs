using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Rebound.Generators;

[Generator]
public class ReboundAppSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

        var classSymbol = receiver.CandidateClasses[0];
        {
            try
            {
                var attribute = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "ReboundAppAttribute");

                var singleInstanceTaskName = attribute?.ConstructorArguments[0].Value?.ToString() ?? "";

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                var appClass = GenerateAppClass(classSymbol, singleInstanceTaskName);
                var programClass = GenerateProgramClass();

                var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName))
                    .AddMembers(appClass, programClass);

                var compilationUnit = CompilationUnit()
                    .AddUsings(
                        UsingDirective(IdentifierName("System")),
                        UsingDirective(IdentifierName("System.Threading")),
                        UsingDirective(IdentifierName("System.Collections.Concurrent")),
                        UsingDirective(IdentifierName("Rebound.Core.Helpers.Services"))
                    )
                    .AddMembers(namespaceDecl)
                    .NormalizeWhitespace();

                context.AddSource("App_Generated.g.cs", compilationUnit.ToFullString());
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REBOUND001",
                        "Error in ReboundAppSourceGenerator",
                        ex.Message,
                        "CodeGeneration",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        }
    }

    private ClassDeclarationSyntax GenerateAppClass(INamedTypeSymbol classSymbol, string singleInstanceTaskName)
    {
        var className = classSymbol.Name;

        // Constructor wires up the _singleInstanceAppService to the existing OnSingleInstanceLaunched method
        var constructor = ConstructorDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithBody(Block(
                ParseStatement($@"_singleInstanceAppService = new SingleInstanceAppService(""{singleInstanceTaskName}"");"),
                ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;")//,
                //ParseStatement(@"_singleInstanceAppService.Launch(string.Join("" "", Environment.GetCommandLineArgs()));") // <-- launch it
            ));

        // Field for single instance service
        var singleInstanceField = FieldDeclaration(
            VariableDeclaration(IdentifierName("SingleInstanceAppService"))
                .AddVariables(VariableDeclarator("_singleInstanceAppService")))
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        // Compose the App class (partial)
        var classDecl = ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddMembers(singleInstanceField, constructor);

        return classDecl;
    }

    private ClassDeclarationSyntax GenerateProgramClass()
    {
        // Program class with _actions queue and QueueAction helper
        return ParseMemberDeclaration(@"
internal class Program
{
    public static BlockingCollection<Action> _actions = new();
    public static void QueueAction(Func<System.Threading.Tasks.Task> action) => _actions.Add(() => _ = action());

    [STAThread]
    static unsafe void Main(string[] args)
    {
        TerraFX.Interop.WinRT.WinRT.RoInitialize(TerraFX.Interop.WinRT.RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

        var app = new App();

        app._singleInstanceAppService.Launch(string.Join("" "", args));

        // Main queued actions loop
        while (true)
        {
            if (_actions.TryTake(out var action, Timeout.Infinite))
            {
                try { action(); } catch { }
            }
        }
    }
}").NormalizeWhitespace() as ClassDeclarationSyntax ?? throw new Exception("Failed to generate Program class");
    }
}

// SyntaxReceiver
internal class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (symbol?.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ReboundAppAttribute") == true)
            {
                CandidateClasses.Add(symbol);
            }
        }
    }
}

public class LegacyLaunchItem(string name, string launchArg, string iconPath)
{
    public string Name { get; } = name;
    public string LaunchArg { get; } = launchArg;
    public string IconPath { get; } = iconPath;
}
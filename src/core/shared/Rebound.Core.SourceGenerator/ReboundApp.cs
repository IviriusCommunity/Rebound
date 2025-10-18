using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
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
                ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;")
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
    public static void QueueAction(Func<System.Threading.Tasks.Task> action)
    {
        System.Diagnostics.Debug.WriteLine($""[Program] QueueAction called, current queue size: {_actions.Count}"");
        _actions.Add(() =>
        {
            System.Diagnostics.Debug.WriteLine($""[Program] Executing queued action"");
            try
            {
                _ = action();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($""[Program] Exception in queued action: {ex.Message}"");
            }
        });
        System.Diagnostics.Debug.WriteLine($""[Program] Action queued, new queue size: {_actions.Count}"");
    }

    [STAThread]
    static unsafe void Main(string[] args)
    {
        System.Diagnostics.Debug.WriteLine($""[Program] Main started"");
        TerraFX.Interop.WinRT.WinRT.RoInitialize(TerraFX.Interop.WinRT.RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

        var app = new App();
        System.Diagnostics.Debug.WriteLine($""[Program] App instance created"");

        // Start processing actions on a background thread
        var processingStarted = new System.Threading.ManualResetEventSlim(false);
        var processingThread = new System.Threading.Thread(() =>
        {
            System.Diagnostics.Debug.WriteLine($""[Program] Processing thread started, Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}"");
            processingStarted.Set();
            System.Diagnostics.Debug.WriteLine($""[Program] About to enter blocking Take() loop..."");
            while (true)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($""[Program] Calling Take() - will block until item available. Queue count: {_actions.Count}"");
                    // Take blocks until an item is available
                    var action = _actions.Take();
                    System.Diagnostics.Debug.WriteLine($""[Program] Got action from queue (remaining: {_actions.Count}), executing..."");
                    try 
                    { 
                        action(); 
                    } 
                    catch (Exception ex) 
                    { 
                        System.Diagnostics.Debug.WriteLine($""[Program] Exception executing action: {ex.Message}"");
                        System.Diagnostics.Debug.WriteLine($""[Program] Stack trace: {ex.StackTrace}"");
                    }
                    System.Diagnostics.Debug.WriteLine($""[Program] Action executed"");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($""[Program] Exception in processing loop: {ex.Message}"");
                    System.Diagnostics.Debug.WriteLine($""[Program] Stack trace: {ex.StackTrace}"");
                }
            }
        })
        {
            IsBackground = false
        };
        processingThread.SetApartmentState(System.Threading.ApartmentState.STA);
        processingThread.Start();

        // Wait for the processing thread to be ready
        System.Diagnostics.Debug.WriteLine($""[Program] Waiting for processing thread to be ready..."");
        processingStarted.Wait();
        System.Diagnostics.Debug.WriteLine($""[Program] Processing thread ready"");

        // Now launch - this will either start the server (first instance) or send message and exit (second instance)
        System.Diagnostics.Debug.WriteLine($""[Program] Calling Launch with args: {string.Join("" "", args)}"");
        app._singleInstanceAppService.Launch(string.Join("" "", args));
        System.Diagnostics.Debug.WriteLine($""[Program] Launch returned"");

        // Keep the main thread alive for the first instance
        System.Diagnostics.Debug.WriteLine($""[Program] Main thread waiting on processing thread..."");
        processingThread.Join();
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
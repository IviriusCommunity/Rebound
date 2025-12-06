// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Rebound.Generators;

// Change 1: Implement IIncrementalGenerator
[Generator]
public class ReboundAppSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Define a provider for all ClassDeclarationSyntax nodes
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    // Ensure we get the symbol from the syntax context
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node) is INamedTypeSymbol symbol)
                    {
                        return symbol;
                    }
                    return null;
                }
            )
            // Filter: Only keep symbols that are classes and have the ReboundAppAttribute
            // FIX: 's' is now directly an INamedTypeSymbol (or null) because of the transform above.
            .Where(static s => s is not null && s.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "ReboundAppAttribute" || attr.AttributeClass?.Name == "ReboundApp"));

        // 2. Combine all found symbols into a single, post-filter item (if any exist)
        // FIX: The type is directly INamedTypeSymbol?
        var classToGenerate = classDeclarations.Collect()
            .Select(static (items, _) => items.FirstOrDefault());


        // 3. Register a source output
        // FIX: Remove 'static' from the lambda here OR make all called methods static.
        // We will make the called methods static.
        context.RegisterSourceOutput(classToGenerate, (ctx, classSymbol) =>
        {
            if (classSymbol is null) return;

            try
            {
                // FIX: 'classSymbol' is now INamedTypeSymbol? and can directly use GetAttributes()
                var attribute = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "ReboundAppAttribute" || attr.AttributeClass?.Name == "ReboundApp");

                // FIX: Added safer check for ConstructorArguments
                var singleInstanceTaskName = attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";

                // FIX: 'classSymbol' is INamedTypeSymbol? and can directly use ContainingNamespace
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                // GenerateAppClass and GenerateProgramClass MUST now be static.
                var appClass = GenerateAppClass(classSymbol, singleInstanceTaskName);
                var programClass = GenerateProgramClass();

                // ... (rest of the generation logic remains the same)
                var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName))
                    .AddMembers(appClass, programClass);

                var compilationUnit = CompilationUnit()
                    .AddUsings(
                        UsingDirective(IdentifierName("System")),
                        UsingDirective(IdentifierName("System.Threading")),
                        UsingDirective(IdentifierName("System.Threading.Tasks")),
                        UsingDirective(IdentifierName("System.Collections.Concurrent")),
                        UsingDirective(IdentifierName("System.Collections.Generic")),
                        UsingDirective(IdentifierName("TerraFX.Interop.Windows")),
                        UsingDirective(IdentifierName("Rebound.Core.UI")),
                        UsingDirective(IdentifierName("Rebound.Core.IPC")),
                        UsingDirective(IdentifierName("Rebound.Core.Helpers"))
                    )
                    .AddMembers(namespaceDecl)
                    .NormalizeWhitespace();

                ctx.AddSource("App_Generated.g.cs", compilationUnit.ToFullString());
            }
            catch (System.Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REBOUND001",
                        "Error in ReboundAppSourceGenerator",
                        ex.Message,
                        "CodeGeneration",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        });
    }

    // FIX: Must be 'static' to be called from the non-static RegisterSourceOutput lambda
    private static ClassDeclarationSyntax GenerateAppClass(INamedTypeSymbol classSymbol, string singleInstanceTaskName)
    {
        // ... (implementation remains the same)
        var className = classSymbol.Name;

        var constructor = ConstructorDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithBody(Block(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName("_singleInstanceAppService"),
                        ObjectCreationExpression(
                            IdentifierName("global::Rebound.Core.UI.SingleInstanceAppService"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(singleInstanceTaskName)))))))),
                ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;")
            ));

        var singleInstanceField = FieldDeclaration(
            VariableDeclaration(IdentifierName("SingleInstanceAppService"))
                .AddVariables(VariableDeclarator("_singleInstanceAppService")))
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        var classDecl = ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddMembers(singleInstanceField, constructor);

        return classDecl;
    }

    private static ClassDeclarationSyntax GenerateProgramClass()
    {
        return ParseMemberDeclaration(@"
internal static partial class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
#if UNPACKAGED
        Environment.SetEnvironmentVariable(""MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY"", AppContext.BaseDirectory);
#endif

        TerraFX.Interop.WinRT.WinRT.RoInitialize(
            TerraFX.Interop.WinRT.RO_INIT_TYPE.RO_INIT_SINGLETHREADED);

        // SET THE MAIN THREAD ID FOR UIThreadQueue
        Rebound.Core.UI.UIThreadQueue.SetMainThreadId(
            TerraFX.Interop.Windows.Windows.GetCurrentThreadId());

        var app = new App();

        Task.Run(() => app._singleInstanceAppService.Launch(string.Join("" "", args)));

        MSG msg;
        // CHANGED: Use GetMessage instead of the busy-wait loop
        while (TerraFX.Interop.Windows.Windows.GetMessageW(&msg, default, 0, 0) > 0)
        {
            foreach (var window in WindowList.OpenWindows.ToArray())
            {
                if (!window.Closed)
                {
                    if (!window.PreTranslateMessage(&msg))
                    {
                        TerraFX.Interop.Windows.Windows.TranslateMessage(&msg);
                        TerraFX.Interop.Windows.Windows.DispatchMessageW(&msg);
                    }
                }
            }

            // Process queued UI actions after each message
            var actionsToRun = new System.Collections.Generic.List<System.Func<System.Threading.Tasks.Task>>();
            while (Rebound.Core.UI.UIThreadQueue._actions.TryDequeue(out var action))
                actionsToRun.Add(action);

            foreach (var action in actionsToRun)
            {
                try { _ = action(); }
                catch (Exception ex) { Rebound.Core.ReboundLogger.Log(""[UIThreadQueue] UI thread crash."", ex); }
            }
        }
    }
}").NormalizeWhitespace() as ClassDeclarationSyntax ?? throw new System.Exception("Failed to generate Program class");
    }
}
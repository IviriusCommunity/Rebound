using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Rebound.Generators
{
    [Generator]
    public class ReboundAppSourceGenerator : ISourceGenerator
    {
        internal static List<LegacyLaunchItem> GetLegacyLaunchItems(string legacyLaunchItems)
        {
            List<string> values = [.. legacyLaunchItems.Split('|')];
            List<LegacyLaunchItem> items = [];

            for (var i = 0; i < values.Count; i++)
            {
                List<string> parts = [.. values[i].Split('*')];
                if (parts.Count != 3)
                {
                    throw new ArgumentException($"Invalid legacy launch item format: {values[i]}");
                }
                items.Add(new LegacyLaunchItem(parts[0], parts[1], parts[2]));
            }

            return items;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register for syntax notifications to track relevant class declarations
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

            foreach (var classSymbol in receiver.CandidateClasses)
            {
                try
                {
                    // Get the ReboundAppAttribute data
                    var attribute = classSymbol.GetAttributes()
                        .FirstOrDefault(attr => attr.AttributeClass?.Name == "ReboundAppAttribute");

                    // Extract the parameters from the attribute's constructor
                    var singleProcessTaskName = attribute?.ConstructorArguments[0].Value?.ToString() ?? "";
                    List<LegacyLaunchItem>? legacyLaunchCommandTitle;
                    if (attribute?.ConstructorArguments[1].Value?.ToString() != "") legacyLaunchCommandTitle = GetLegacyLaunchItems(attribute?.ConstructorArguments[1].Value?.ToString() ?? "");
                    else legacyLaunchCommandTitle = null;

                        // Get the namespace of the class
                        var namespaceSymbol = classSymbol.ContainingNamespace;
                    var namespaceName = namespaceSymbol.ToDisplayString(); // This gives the full namespace path

                    // Process the class and generate the necessary code
                    var classDeclaration = GenerateClass(classSymbol, singleProcessTaskName, legacyLaunchCommandTitle);

                    // Use the existing namespace name instead of hardcoding "Rebound.Run"
                    var namespaceDeclaration = NamespaceDeclaration(IdentifierName(namespaceName))
                        .AddMembers(classDeclaration);

                    // Add the necessary namespaces to the generated code
                    var compilationUnit = CompilationUnit()
                        .AddUsings(
                            UsingDirective(IdentifierName("System")),
                            UsingDirective(IdentifierName("Windows.ApplicationModel.Activation")),
                            UsingDirective(IdentifierName("Microsoft.UI.Xaml")),
                            UsingDirective(IdentifierName("Windows.UI.StartScreen")),
                            UsingDirective(IdentifierName("System.Threading.Tasks")),
                            UsingDirective(IdentifierName("Windows.Foundation")),
                            UsingDirective(IdentifierName("Rebound.Core.Helpers")),
                            UsingDirective(IdentifierName("Rebound.Core.Helpers.Services"))
                        )
                        .AddMembers(namespaceDeclaration)
                        .NormalizeWhitespace();

                    // Output the generated file
                    context.AddSource($"{classSymbol.Name}_Generated.g.cs", compilationUnit.ToFullString());
                    break;
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "REBOUND001", // Example ID
                            "Error in ReboundAppSourceGenerator",
                            ex.Message,
                            "CodeGeneration",
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
                    break;
                }
            }
        }

        private ClassDeclarationSyntax GenerateClass(
            INamedTypeSymbol classSymbol,
            string singleProcessTaskName,
            List<LegacyLaunchItem>? legacyLaunchItems)
        {
            var className = classSymbol.Name;

            // --- Constructor setup ---
            var constructorStatements = new List<StatementSyntax>
            {
                ParseStatement($@"_singleInstanceAppService = new SingleInstanceAppService(""{singleProcessTaskName}"");"),
                ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;")
            };

            if (legacyLaunchItems != null)
            {
                constructorStatements.Add(ParseStatement("InitializeJumpList();"));
            }

            var constructor = ConstructorDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBody(Block(constructorStatements));

            // --- Optional InitializeJumpList() ---
            MethodDeclarationSyntax? initializeJumpListMethod = null;
            if (legacyLaunchItems != null)
            {
                var legacyLaunchCode = string.Empty;

                for (int i = 0; i < legacyLaunchItems.Count; i++)
                {
                    var x = legacyLaunchItems[i];
                    legacyLaunchCode += $@"
    var item{i} = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(""{x.LaunchArg}"", ""{x.Name}"");
    item{i}.Logo = new Uri(""{x.IconPath}"");
    jumpList.Items.Add(item{i});
";
                }

                var existingInitializeJumpListMethod = classSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => m.Name == "InitializeJumpList" && m.Parameters.Length == 0 &&
                                         m.ReturnType.Name == "Void");
                initializeJumpListMethod = existingInitializeJumpListMethod == null
                    ? MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "InitializeJumpList")
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.AsyncKeyword))
                        .WithBody(Block(
                            ParseStatement(@$"
    var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
    jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;
    jumpList.Items.Clear();
    {legacyLaunchCode}
    await jumpList.SaveAsync();")
                        ))
                    : null;
            }

            // --- Static fields ---
            var singleInstanceAppField = FieldDeclaration(
                    VariableDeclaration(IdentifierName("SingleInstanceAppService"))
                    .AddVariables(VariableDeclarator("_singleInstanceAppService"))
                )
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword));

            var mainWindowField = FieldDeclaration(
                    VariableDeclaration(IdentifierName("IslandsWindow"))
                    .AddVariables(VariableDeclarator("MainWindow"))
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

            // --- Class Declaration ---
            var classDeclaration = ClassDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddMembers(constructor, singleInstanceAppField, mainWindowField);

            if (initializeJumpListMethod != null)
                classDeclaration = classDeclaration.AddMembers(initializeJumpListMethod);

            return classDeclaration;
        }
    }
}

// Collects candidate classes annotated with [ReboundApp]
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
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
                            UsingDirective(IdentifierName("Windows.ApplicationModel")),
                            UsingDirective(IdentifierName("Microsoft.UI.Xaml")),
                            UsingDirective(IdentifierName("Rebound.Helpers.Services")),
                            UsingDirective(IdentifierName("Windows.UI.StartScreen")),
                            UsingDirective(IdentifierName("System.Threading.Tasks")),
                            UsingDirective(IdentifierName("Windows.Foundation")),
                            UsingDirective(IdentifierName("WinUIEx"))
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

        private ClassDeclarationSyntax GenerateClass(INamedTypeSymbol classSymbol, string singleProcessTaskName, List<LegacyLaunchItem>? legacyLaunchItems)
        {
            var className = classSymbol.Name;

            // Check if the OnLaunched method already exists
            var existingOnLaunchedMethod = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == "OnLaunched" && m.Parameters.Length == 1 &&
                                     m.Parameters[0].Type.Name == "LaunchActivatedEventArgs");
            var onLaunchedMethod = existingOnLaunchedMethod == null
                ? MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "OnLaunched")
                    .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier("args")).WithType(IdentifierName("LaunchActivatedEventArgs"))
                    )
                    .WithBody(Block(
                        // Initialize the list of statements
                        new List<StatementSyntax>
                        {
                ParseStatement($@"_singleInstanceAppService = new SingleInstanceAppService(""{singleProcessTaskName}"");"),
                ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;"),
                ParseStatement(@"_singleInstanceAppService.Launch(args.Arguments);")
                        }
                        .Concat(legacyLaunchItems != null ? [ParseStatement("InitializeJumpList();")] : [])
                        .ToArray()  // Convert to an array to be passed to Block
                    ))
                : null;

            MethodDeclarationSyntax? initializeJumpListMethod = null;
            if (legacyLaunchItems != null)
            {
                var legacyLaunchCode = string.Empty;

                foreach (var x in legacyLaunchItems)
                {
                    legacyLaunchCode += $@"
    var item{legacyLaunchItems.IndexOf(x)} = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(""{x.LaunchArg}"", ""{x.Name}"");
    item{legacyLaunchItems.IndexOf(x)}.Logo = new Uri(""{x.IconPath}"");
    jumpList.Items.Add(item{legacyLaunchItems.IndexOf(x)});
";
                }

                // Check if the InitializeJumpList method already exists
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

            // Create a static field for SingleInstanceApp
            var singleInstanceAppField = FieldDeclaration(
                    VariableDeclaration(IdentifierName("SingleInstanceAppService"))
                    .AddVariables(VariableDeclarator("_singleInstanceAppService"))
                )
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword));

            // Create a static field for MainWindow
            var mainWindowField = FieldDeclaration(
                    VariableDeclaration(IdentifierName("WindowEx"))
                    .AddVariables(VariableDeclarator("MainAppWindow"))
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword));

            // Create the class and add members to it
            var classDeclaration = ClassDeclaration(className)
                    .AddModifiers(Token(SyntaxKind.PartialKeyword));

            // Only add the members if they don't already exist
            if (onLaunchedMethod != null) classDeclaration = classDeclaration.AddMembers(onLaunchedMethod);
            if (initializeJumpListMethod != null && legacyLaunchItems != null) classDeclaration = classDeclaration.AddMembers(initializeJumpListMethod);

            // Iterate over the class members to find the method where InitializeComponent should be added
            foreach (var member in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                // Check if the method has a body (it should for this use case)
                if (member.Body != null)
                {
                    var initializeComponentStatement = ParseStatement("InitializeComponent();");

                    // Add the InitializeComponent() statement to the method's body
                    var updatedBody = member.Body.AddStatements(initializeComponentStatement);

                    // Replace the old body with the updated one
                    classDeclaration = classDeclaration.ReplaceNode(member.Body, updatedBody);
                }
            }

            // Add the static field for SingleInstanceApp
            classDeclaration = classDeclaration.AddMembers(singleInstanceAppField);
            classDeclaration = classDeclaration.AddMembers(mainWindowField);

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
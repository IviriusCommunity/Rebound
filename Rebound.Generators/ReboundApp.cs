using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Rebound.Generators
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ReboundAppAttribute : Attribute
    {
        public string SingleProcessTaskName { get; }
        public string LegacyLaunchCommandTitle { get; }

        public ReboundAppAttribute(string singleProcessTaskName, string legacyLaunchCommandTitle)
        {
            SingleProcessTaskName = singleProcessTaskName;
            LegacyLaunchCommandTitle = legacyLaunchCommandTitle;
        }
    }

    [Generator]
    public class ReboundAppSourceGenerator : ISourceGenerator
    {
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
                    var singleProcessTaskName = (string)attribute.ConstructorArguments[0].Value;
                    var legacyLaunchCommandTitle = (string)attribute.ConstructorArguments[1].Value;

                    // Get the namespace of the class
                    var namespaceSymbol = classSymbol.ContainingNamespace;
                    string namespaceName = namespaceSymbol.ToDisplayString(); // This gives the full namespace path

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
                            UsingDirective(IdentifierName("Windows.Foundation"))
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

        private ClassDeclarationSyntax GenerateClass(INamedTypeSymbol classSymbol, string singleProcessTaskName, string legacyLaunchCommandTitle)
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
                        ParseStatement(@$"_singleInstanceAppService = new SingleInstanceAppService(""{singleProcessTaskName}"");"),
                        ParseStatement(@"_singleInstanceAppService.Launched += OnSingleInstanceLaunched;"),
                        ParseStatement(@"_singleInstanceAppService.Launch(args.Arguments);"),
                        //ParseStatement("Current.UnhandledException += App_UnhandledException;"),
                        ParseStatement("InitializeJumpList();")
                    ))
                : null;

            // Check if the InitializeJumpList method already exists
            var existingInitializeJumpListMethod = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == "InitializeJumpList" && m.Parameters.Length == 0 &&
                                     m.ReturnType.Name == "Void");
            var initializeJumpListMethod = existingInitializeJumpListMethod == null
                ? MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "InitializeJumpList")
                    .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.AsyncKeyword))
                    .WithBody(Block(
                        ParseStatement(@$"
    var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
    jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;
    jumpList.Items.Clear();
    var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(""legacy"", ""{legacyLaunchCommandTitle}"");
    item.Logo = new Uri(""ms-appx:///Assets/Computer disk.png"");
    jumpList.Items.Add(item);
    await jumpList.SaveAsync();")
                    ))
                : null;

            // Create a static field for SingleInstanceApp
            var singleInstanceAppField = FieldDeclaration(
                    VariableDeclaration(IdentifierName("SingleInstanceAppService"))
                    .AddVariables(VariableDeclarator("_singleInstanceAppService"))
                )
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword));

            // Create the class and add members to it
            var classDeclaration = ClassDeclaration(className)
                .AddModifiers(Token(SyntaxKind.PartialKeyword));

            // Only add the members if they don't already exist
            if (onLaunchedMethod != null) classDeclaration = classDeclaration.AddMembers(onLaunchedMethod);
            if (initializeJumpListMethod != null) classDeclaration = classDeclaration.AddMembers(initializeJumpListMethod);

            // Add the static field for SingleInstanceApp
            classDeclaration = classDeclaration.AddMembers(singleInstanceAppField);

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
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol?.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ReboundAppAttribute") == true)
            {
                CandidateClasses.Add(symbol);
            }
        }
    }
}
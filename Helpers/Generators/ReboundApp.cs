using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rebound.Helpers.Generators;

[Generator]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReboundAppAttribute : Attribute, ISourceGenerator
{
    public string SingleProcessTaskName { get; }
    public string LegacyLaunchCommandTitle { get; }

    public ReboundAppAttribute(string singleProcessTaskName, string legacyLaunchCommandTitle)
    {
        SingleProcessTaskName = singleProcessTaskName;
        LegacyLaunchCommandTitle = legacyLaunchCommandTitle;
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

        foreach (var classSymbol in receiver.CandidateClasses)
        {
            string className = classSymbol.Name;

            // Find the constructor (if any)
            var constructor = classSymbol.Constructors.FirstOrDefault();
            if (constructor is null)
                continue; // Skip if no constructor exists

            // Get the source location
            var syntaxRef = constructor.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef is null)
                continue;

            if (syntaxRef.GetSyntax() is not ConstructorDeclarationSyntax constructorNode)
                continue;

            // Build the modified constructor with additional logic
            var modifiedConstructor = SyntaxFactory.ConstructorDeclaration(constructorNode.Identifier)
                .AddModifiers(constructorNode.Modifiers.ToArray())
                .AddParameterListParameters(constructorNode.ParameterList.Parameters.ToArray())
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement(@"
                        SingleInstanceAppService.Launched += SingleInstanceApp_Launched;
                        Current.UnhandledException += App_UnhandledException;

                        var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
                        jumpList.SystemGroupKind = Windows.UI.StartScreen.JumpListSystemGroupKind.None;
                        jumpList.Items.Clear();

                        var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(""legacy"", name);
                        item.Logo = new Uri(""ms-appx:///Assets/Computer disk.png"");

                        jumpList.Items.Add(item);
                        await jumpList.SaveAsync();
                    ")
                ))
                .NormalizeWhitespace();

            // Generate additional code to be added to the class
            var additionalCode = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "OnLaunched")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("args"))
                    .WithType(SyntaxFactory.ParseTypeName("LaunchActivatedEventArgs")))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.ParseExpression("_singleInstanceApp.Launch(args.Arguments)"))
                    )
                ));

            // Add namespaces and class-level declarations
            string namespaces = @$"
                using Rebound.Helpers.Services;
            ";

            var classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .AddMembers(modifiedConstructor, additionalCode);

            // Build the final syntax tree for the class
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("Rebound.Helpers"))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Rebound.Helpers.Services")))
                .AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.CodeAnalysis")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Windows.UI.StartScreen")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Windows.Foundation")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Windows.ApplicationModel")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Rebound.Helpers.Services")))
                .AddMembers(namespaceDeclaration);

            var formattedCode = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Rebound.Helpers.Services")))
                .NormalizeWhitespace();

            Debug.Write(formattedCode.ToFullString());
            context.AddSource($"{className}_Generated.g.cs", formattedCode.ToFullString());
        }
    }
}

// Receiver to collect candidate classes
class SyntaxReceiver : ISyntaxContextReceiver
{
    public List<INamedTypeSymbol> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol is not null &&
                symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ReboundAppAttribute"))
            {
                CandidateClasses.Add(symbol);
            }
        }
    }
}
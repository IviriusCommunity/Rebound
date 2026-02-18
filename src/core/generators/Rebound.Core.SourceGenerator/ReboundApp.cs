// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Rebound.Generators;

[Generator]
public class ReboundAppSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node) is INamedTypeSymbol symbol)
                    {
                        return symbol;
                    }
                    return null;
                }
            )
            .Where(static s => s is not null && s.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "ReboundAppAttribute" || attr.AttributeClass?.Name == "ReboundApp"));

        var classToGenerate = classDeclarations.Collect()
            .Select(static (items, _) => items.FirstOrDefault());

        context.RegisterSourceOutput(classToGenerate, (ctx, classSymbol) =>
        {
            if (classSymbol is null) return;

            try
            {
                var attribute = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name == "ReboundAppAttribute" || attr.AttributeClass?.Name == "ReboundApp");

                var singleInstanceTaskName = attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                var appClass = GenerateAppClass(classSymbol, singleInstanceTaskName);
                var programClass = GenerateProgramClass();

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
                        UsingDirective(IdentifierName("Rebound.Core.IPC"))
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

    private static ClassDeclarationSyntax GenerateAppClass(INamedTypeSymbol classSymbol, string singleInstanceTaskName)
    {
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
        try
        {
            var mainMethod = MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    "Main")
                .AddModifiers(
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(IdentifierName("STAThread")))))
                .WithParameterList(GenerateMainMethodParameters())
                .WithBody(GenerateMainMethodBody());

            var programClass = ClassDeclaration("Program")
                .AddModifiers(
                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.PartialKeyword))
                .AddMembers(mainMethod);

            return programClass;
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateProgramClass] Failed to generate Program class: {ex.Message}", ex);
        }
    }

    private static ParameterListSyntax GenerateMainMethodParameters()
    {
        try
        {
            return ParameterList(
                SingletonSeparatedList(
                    Parameter(Identifier("args"))
                    .WithType(
                        ArrayType(
                            PredefinedType(Token(SyntaxKind.StringKeyword)))
                        .WithRankSpecifiers(
                            SingletonList(
                                ArrayRankSpecifier(
                                    SingletonSeparatedList<ExpressionSyntax>(
                                        OmittedArraySizeExpression())))))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateMainMethodParameters] Failed to generate parameters: {ex.Message}", ex);
        }
    }

    private static BlockSyntax GenerateMainMethodBody()
    {
        try
        {
            return Block(
                GenerateUnpackagedIfStatement(),
                GenerateRoInitializeStatement(),
                GenerateSetMainThreadIdStatement(),
                GenerateAppCreationStatement(),
                GenerateLaunchTaskStatement(),
                GenerateMsgDeclaration(),
                GenerateMessageLoop()
            );
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateMainMethodBody] Failed to generate method body: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateUnpackagedIfStatement()
    {
        try
        {
            // Create the statement with preprocessor directives as trivia
            var statement = ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Environment"),
                        IdentifierName("SetEnvironmentVariable")))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(new[]
                        {
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                                Literal("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY"))),
                            Argument(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("AppContext"),
                                IdentifierName("BaseDirectory")))
                        }))));

            // Add #if UNPACKAGED as leading trivia
            statement = statement.WithLeadingTrivia(
                Trivia(IfDirectiveTrivia(IdentifierName("UNPACKAGED"), true, false, false)),
                CarriageReturnLineFeed);

            // Add #endif as trailing trivia
            statement = statement.WithTrailingTrivia(
                CarriageReturnLineFeed,
                Trivia(EndIfDirectiveTrivia(true)));

            return statement;
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateUnpackagedIfStatement] Failed to generate UNPACKAGED conditional: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateRoInitializeStatement()
    {
        try
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("TerraFX"),
                                    IdentifierName("Interop")),
                                IdentifierName("WinRT")),
                            IdentifierName("WinRT")),
                        IdentifierName("RoInitialize")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                // FIX: Just use RO_INIT_SINGLETHREADED directly
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("TerraFX"),
                                                IdentifierName("Interop")),
                                            IdentifierName("WinRT")),
                                        IdentifierName("RO_INIT_TYPE")),
                                    IdentifierName("RO_INIT_SINGLETHREADED")))))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateRoInitializeStatement] Failed to generate RoInitialize call: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateSetMainThreadIdStatement()
    {
        try
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("Rebound"),
                                    IdentifierName("Core")),
                                IdentifierName("UI")),
                            IdentifierName("UIThreadQueue")),
                        IdentifierName("SetMainThreadId")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("TerraFX"),
                                                    IdentifierName("Interop")),
                                                IdentifierName("Windows")),
                                            IdentifierName("Windows")),
                                        IdentifierName("GetCurrentThreadId"))))))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateSetMainThreadIdStatement] Failed to generate SetMainThreadId call: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateAppCreationStatement()
    {
        try
        {
            return LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName("var"))
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator("app")
                        .WithInitializer(
                            EqualsValueClause(
                                ObjectCreationExpression(IdentifierName("App"))
                                .WithArgumentList(ArgumentList()))))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateAppCreationStatement] Failed to generate app creation: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateLaunchTaskStatement()
    {
        try
        {
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Task"),
                        IdentifierName("Run")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                ParenthesizedLambdaExpression()
                                .WithExpressionBody(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("app"),
                                                IdentifierName("_singleInstanceAppService")),
                                            IdentifierName("Launch")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("string"),
                                                            IdentifierName("Join")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SeparatedList(new[]
                                                            {
                                                            Argument(LiteralExpression(
                                                                SyntaxKind.StringLiteralExpression,
                                                                Literal(" "))),
                                                            Argument(IdentifierName("args"))
                                                            })))))))))))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateLaunchTaskStatement] Failed to generate Task.Run launch: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateMsgDeclaration()
    {
        try
        {
            return LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("MSG"))
                .WithVariables(SingletonSeparatedList(VariableDeclarator("msg"))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateMsgDeclaration] Failed to generate MSG declaration: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateMessageLoop()
    {
        try
        {
            ExpressionSyntax condition;
            StatementSyntax windowLoop;
            StatementSyntax actionsDecl;
            StatementSyntax dequeueLoop;
            StatementSyntax executionLoop;

            try { condition = GenerateWhileCondition(); }
            catch (System.Exception ex) { throw new System.Exception($"Failed at GenerateWhileCondition: {ex.Message}", ex); }

            try { windowLoop = GenerateWindowProcessingLoop(); }
            catch (System.Exception ex) { throw new System.Exception($"Failed at GenerateWindowProcessingLoop: {ex.Message}", ex); }

            try { actionsDecl = GenerateActionsDeclaration(); }
            catch (System.Exception ex) { throw new System.Exception($"Failed at GenerateActionsDeclaration: {ex.Message}", ex); }

            try { dequeueLoop = GenerateActionsDequeueLoop(); }
            catch (System.Exception ex) { throw new System.Exception($"Failed at GenerateActionsDequeueLoop: {ex.Message}", ex); }

            try { executionLoop = GenerateActionsExecutionLoop(); }
            catch (System.Exception ex) { throw new System.Exception($"Failed at GenerateActionsExecutionLoop: {ex.Message}", ex); }

            try
            {
                return WhileStatement(
                    condition,
                    Block(windowLoop, actionsDecl, dequeueLoop, executionLoop));
            }
            catch (System.Exception ex) { throw new System.Exception($"Failed at WhileStatement construction: {ex.Message}", ex); }
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateMessageLoop] Failed to generate message loop: {ex.Message}", ex);
        }
    }

    private static ExpressionSyntax GenerateWhileCondition()
    {
        try
        {
            return BinaryExpression(
                SyntaxKind.GreaterThanExpression,
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("TerraFX"),
                                    IdentifierName("Interop")),
                                IdentifierName("Windows")),
                            IdentifierName("Windows")),
                        IdentifierName("GetMessageW")))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(new[]
                        {
                        Argument(
                            PrefixUnaryExpression(
                                SyntaxKind.AddressOfExpression,
                                IdentifierName("msg"))),
                        Argument(LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))
                        }))),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateWhileCondition] Failed to generate while condition: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateWindowProcessingLoop()
    {
        try
        {
            return ForEachStatement(
                IdentifierName("var"),
                Identifier("window"),
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("WindowList"),
                            IdentifierName("OpenWindows")),
                        IdentifierName("ToArray"))),
                Block(
                    IfStatement(
                        PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("window"),
                                IdentifierName("Closed"))),
                        Block(
                            GeneratePreTranslateMessageCheck()))));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateWindowProcessingLoop] Failed to generate window processing loop: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GeneratePreTranslateMessageCheck()
    {
        try
        {
            return IfStatement(
                PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("window"),
                            IdentifierName("PreTranslateMessage")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    PrefixUnaryExpression(
                                        SyntaxKind.AddressOfExpression,
                                        IdentifierName("msg"))))))),
                Block(
                    GenerateTranslateMessageCall(),
                    GenerateDispatchMessageCall()));
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GeneratePreTranslateMessageCheck] Failed to generate PreTranslateMessage check: {ex.Message}", ex);
        }
    }

    private static StatementSyntax GenerateTranslateMessageCall()
    {
        return ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("TerraFX"),
                                IdentifierName("Interop")),
                            IdentifierName("Windows")),
                        IdentifierName("Windows")),
                    IdentifierName("TranslateMessage")))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            PrefixUnaryExpression(
                                SyntaxKind.AddressOfExpression,
                                IdentifierName("msg")))))));
    }

    private static StatementSyntax GenerateDispatchMessageCall()
    {
        return ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("TerraFX"),
                                IdentifierName("Interop")),
                            IdentifierName("Windows")),
                        IdentifierName("Windows")),
                    IdentifierName("DispatchMessageW")))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            PrefixUnaryExpression(
                                SyntaxKind.AddressOfExpression,
                                IdentifierName("msg")))))));
    }

    private static StatementSyntax GenerateActionsDeclaration()
    {
        return LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
            .WithVariables(
                SingletonSeparatedList(
                    VariableDeclarator("actionsToRun")
                    .WithInitializer(
                        EqualsValueClause(
                            ObjectCreationExpression(
                                GenericName("List")
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SingletonSeparatedList<TypeSyntax>(
                                            GenericName("Func")
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList<TypeSyntax>(
                                                        IdentifierName("Task"))))))))
                            .WithArgumentList(ArgumentList()))))));
    }

    private static StatementSyntax GenerateActionsDequeueLoop()
    {
        return WhileStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("Rebound"),
                                    IdentifierName("Core")),
                                IdentifierName("UI")),
                            IdentifierName("UIThreadQueue")),
                        IdentifierName("_actions")),
                    IdentifierName("TryDequeue")))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            DeclarationExpression(
                                IdentifierName("var"),
                                SingleVariableDesignation(Identifier("action"))))
                        .WithRefKindKeyword(Token(SyntaxKind.OutKeyword))))),
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("actionsToRun"),
                        IdentifierName("Add")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(IdentifierName("action")))))));
    }

    private static StatementSyntax GenerateActionsExecutionLoop()
    {
        try
        {
            // FIX: Just call action() without the suppression operator
            var tryBlock = Block(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName("_"),
                        InvocationExpression(IdentifierName("action")))));

            var logCall = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Rebound"),
                            IdentifierName("Core")),
                        IdentifierName("ReboundLogger")),
                    IdentifierName("Log")))
            .WithArgumentList(
                ArgumentList(
                    SeparatedList(new[]
                    {
                        Argument(LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal("[UIThreadQueue] UI thread crash."))),
                        Argument(IdentifierName("ex"))
                    })));

            var catchBlock = Block(ExpressionStatement(logCall));

            var catchClause = CatchClause()
                .WithDeclaration(
                    CatchDeclaration(IdentifierName("Exception"))
                    .WithIdentifier(Identifier("ex")))
                .WithBlock(catchBlock);

            var tryStatement = TryStatement()
                .WithBlock(tryBlock)
                .WithCatches(SingletonList(catchClause));

            var forEachBody = Block(tryStatement);

            return ForEachStatement(
                IdentifierName("var"),
                Identifier("action"),
                IdentifierName("actionsToRun"),
                forEachBody);
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"[GenerateActionsExecutionLoop] {ex.Message}", ex);
        }
    }
}
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeAutoGenerator.SyntaxAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAutoGenerator.CodeFixGen;

// [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PlayerHandlerCodeFixProvider)), Shared]
// public class PlayerHandlerCodeFixProvider : CodeFixProvider
// {
//     public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PlayerHandlerAnalyzer.DiagnosticId);
//
//     public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
//
//     public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
//     {
//         var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
//         var diagnostic = context.Diagnostics.First();
//         var diagnosticSpan = diagnostic.Location.SourceSpan;
//
//         var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
//
//         context.RegisterCodeFix(
//             CodeAction.Create(
//                 title: "Generate missing method",
//                 createChangedDocument: c => GenerateMethodAsync(context.Document, declaration, diagnostic, c),
//                 equivalenceKey: "Generate missing method"),
//             diagnostic);
//     }
//
//     private async Task<Document> GenerateMethodAsync(Document document, ClassDeclarationSyntax classDeclaration, Diagnostic diagnostic, CancellationToken cancellationToken)
//     {
//         var methodName = diagnostic.Properties["MethodName"];
//
//         // Generate the method
//         var method = SyntaxFactory.MethodDeclaration(
//             SyntaxFactory.ParseTypeName("Task"),
//             methodName)
//             .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
//             .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("throw new NotImplementedException();")));
//
//         var newClassDeclaration = classDeclaration.AddMembers(method);
//
//         var root = await document.GetSyntaxRootAsync(cancellationToken);
//         var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
//
//         return document.WithSyntaxRoot(newRoot);
//     }
// }
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeAutoGenerator.SyntaxAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ActorHandlerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ActorHandlerMissingMethod";
    private static readonly LocalizableString Title = "ActorHandler is missing required methods";
    private static readonly LocalizableString MessageFormat = "ActorHandler is missing method: {0}";
    private static readonly LocalizableString Description = "ActorrHandler must implement all methods.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);


    private static List<InterfaceDeclarationSyntax> _actorInterfaces = new();
    private static List<ClassDeclarationSyntax> _actorClasss = new();
    

    public override void Initialize(AnalysisContext context)
    {
        
        // Thread.Sleep(30000);
        
        // Debugger.Launch()
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // // 注册对接口声明的动作
        context.RegisterSyntaxNodeAction(AnalyzeDefinition, SyntaxKind.InterfaceDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDefinition, SyntaxKind.ClassDeclaration);
        context.RegisterCompilationStartAction(start =>
        {
            start.RegisterCompilationEndAction(AnalyzeCompilationEnd);
        });
        
    }

    private static void AnalyzeDefinition(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case InterfaceDeclarationSyntax interfaceDeclaration:
                var attrs = interfaceDeclaration.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Where(attr => attr.Name.ToString() == "ActorDefine")
                    .ToList();

                if (attrs.Count > 0)
                {
                    _actorInterfaces.Add(interfaceDeclaration);
                }
                break;
            case ClassDeclarationSyntax classDeclaration:
                if (classDeclaration.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
                {
                    _actorClasss.Add(classDeclaration);
                }
                break;
        }
    }
    
    
    
    /// <summary>
    /// 找到符合条件的第一个 ClassDeclarationSyntax
    /// </summary>
    /// <param name="context"></param>
    /// <param name="fullClassName"></param>
    /// <returns></returns>
    private static ClassDeclarationSyntax FindClassDeclaration(CompilationAnalysisContext context, string fullClassName)
    {
        var compilation = context.Compilation;
        
        var firstClassDeclaration = _actorClasss
            .Where(actorCls =>
            {
                var clsSemanticModel = compilation.GetSemanticModel(actorCls.SyntaxTree);
                var classSymbol = clsSemanticModel.GetDeclaredSymbol(actorCls);
                var fullstr = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        
                // 添加过滤条件
                return fullstr == fullClassName;

                // return false;
            }).FirstOrDefault(); // 获取第一个符合条件的 ClassDeclarationSyntax

        return firstClassDeclaration;
    }



    /// <summary>
    /// 找到符合条件的类定义
    /// </summary>
    /// <param name="context"></param>
    /// <param name="actorDefine"></param>
    /// <param name="fullClassName"></param>
    /// <returns></returns>
    private static ClassDeclarationSyntax FindClassDeclaration(CompilationAnalysisContext context,
        AttributeSyntax actorDefine, string fullClassName)
    {
        var compilation = context.Compilation;
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var firstClassDeclaration = _actorClasss
            .Where(actorCls =>
            {
                // var clsSemanticModel = compilation.GetSemanticModel(actorCls.SyntaxTree);
                var classSymbol = semanticModel.GetDeclaredSymbol(actorCls);
                var fullstr = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        
                // 添加过滤条件
                return fullstr == fullClassName;

                // return false;
            }).FirstOrDefault(); // 获取第一个符合条件的 ClassDeclarationSyntax
        return firstClassDeclaration;
    }
    
    /// <summary>
    /// 寻找actorHandler所有符合条件的方法
    /// </summary>
    /// <param name="context"></param>
    /// <param name="actorDefine"></param>
    /// <param name="actorHandler"></param>
    /// <returns></returns>
    private static List<MethodDeclarationSyntax> FindMethods(CompilationAnalysisContext context, AttributeSyntax actorDefine, ClassDeclarationSyntax actorHandler)
    {
        var compilation = context.Compilation;
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        var methods = actorHandler.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
            {
                if (method.SyntaxTree == null)
                {
                    return false;
                }
                // 检查方法是否为 async
                if (!method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)))
                {
                    return false; // 不是 async 方法，跳过
                }

                // 检查方法是否有参数
                if (method.ParameterList.Parameters.Count == 0)
                {
                    return false; // 没有参数，跳过
                }

                // 获取第一个参数
                var firstParameter = method.ParameterList.Parameters[0];

                // 获取第一个参数的类型信息
                var parameterTypeInfo = semanticModel.GetTypeInfo(firstParameter.Type);

                // 获取 actorDefine.ArgumentList?.Arguments[0] 的类型信息
                if (actorDefine.ArgumentList?.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    var argumentTypeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);

                    // 比较参数类型是否匹配
                    return SymbolEqualityComparer.Default.Equals(parameterTypeInfo.Type, argumentTypeInfo.Type);
                }

                return false;
            })
            .ToList(); // 将符合条件的方法转换为 List<MethodDeclarationSyntax>

        return methods;
    }
    
    private static void AnalyzeCompilationEnd(CompilationAnalysisContext context)
    {
        var compilation = context.Compilation;
        // 获取 SyntaxTree
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        
        foreach (var actorInterface in _actorInterfaces)
        {
            var actorDefine = actorInterface.AttributeLists
                .SelectMany(a => a.Attributes)
                .First();
            
            if (actorDefine == null || actorDefine.SyntaxTree == null)
            {
                continue;
            }
            
            //索引0: model，1: handler, 2: keyType
            if (actorDefine.ArgumentList?.Arguments[1].Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                if (typeOfExpression.Type == null)
                {
                    continue;
                }
                
                // 获取 TypeOfExpressionSyntax 的语义信息
                var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
                if (typeInfo.Type == null)
                {
                    continue;
                }

                // 获取完整类名（包括命名空间）
                var fullClassName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (string.IsNullOrEmpty(fullClassName))
                {
                    continue;
                }
                
                var actorHandler = FindClassDeclaration(context, actorDefine, fullClassName);
                if (actorHandler == null)
                {
                    var diagnostic = Diagnostic.Create(Rule, actorInterface.Identifier.GetLocation(), "没有handler类");
                    context.ReportDiagnostic(diagnostic);
                }
                
                
                var methods = FindMethods(context, actorDefine, actorHandler);
                if (methods == null || methods.Count <= 0)
                {
                    var diagnostic = Diagnostic.Create(Rule, actorHandler.Identifier.GetLocation(), "没有实现接口方法");
                    context.ReportDiagnostic(diagnostic);
                }

                // 拿到接口的所有方法
                var interfaceMethods = actorInterface.Members
                    .OfType<MethodDeclarationSyntax>()
                    .ToList();
                
                foreach (var interfaceMethod in interfaceMethods) 
                { 
                    var methodName = interfaceMethod.Identifier.Text;

                    // 在 methods 中查找同名方法
                    var implementedMethod = methods.FirstOrDefault(m => m.Identifier.Text == methodName);
                    if (implementedMethod == null)
                    {
                        // 如果未找到同名方法，报告诊断信息
                        var diagnostic = Diagnostic.Create(Rule, actorHandler.Identifier.GetLocation(), $"未实现接口方法: {methodName}");
                        context.ReportDiagnostic(diagnostic);
                        continue;
                    }

                    // 比较从第二个参数开始的参数类型
                    var interfaceParameters = interfaceMethod.ParameterList.Parameters.ToList();
                    var implementedParameters = implementedMethod.ParameterList.Parameters.Skip(1).ToList();

                    if (interfaceParameters.Count != implementedParameters.Count)
                    {
                        // 如果参数数量不一致，报告诊断信息
                        var diagnostic = Diagnostic.Create(Rule, implementedMethod.Identifier.GetLocation(), $"方法 {methodName} 参数数量不一致");
                        context.ReportDiagnostic(diagnostic);
                        continue;
                    }

                    for (int i = 0; i < interfaceParameters.Count; i++)
                    {
                        var interfaceParamType = semanticModel.GetTypeInfo(interfaceParameters[i].Type).Type;
                        var implementedParamType = semanticModel.GetTypeInfo(implementedParameters[i].Type).Type;

                        if (!SymbolEqualityComparer.Default.Equals(interfaceParamType, implementedParamType))
                        {
                            // 如果参数类型不一致，报告诊断信息
                            var diagnostic = Diagnostic.Create(Rule, implementedMethod.Identifier.GetLocation(), $"方法 {methodName} 参数类型不一致");
                            context.ReportDiagnostic(diagnostic);
                            break;
                        }
                    }
                }
            }
        }
    }
}
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
public class PlayerHandlerAnalyzer : DiagnosticAnalyzer
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
        
        Thread.Sleep(30000);
        
        // Debugger.Launch()
        // context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // context.EnableConcurrentExecution();
        // context.RegisterSyntaxNodeAction(AnalyzePlayerHandler, SyntaxKind.ClassDeclaration);
        // 注册对类声明的动作
        // context.RegisterSyntaxNodeAction(AnalyzePlayerHandler, SyntaxKind.ClassDeclaration);
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
    // private static void AnalyzeInterfaceDefinition(SyntaxNodeAnalysisContext context)
    // {
    //     var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
    //     
    //     var attrs = interfaceDeclaration.AttributeLists
    //         .SelectMany(a => a.Attributes)
    //         .Where(attr => attr.Name.ToString() == "ActorDefine")
    //         .ToList();
    //
    //     if (attrs.Count <= 0)
    //     {
    //         return;
    //     }
    //
    //     var actorDefine = attrs[0];
    //
    //     if (actorDefine.ArgumentList?.Arguments[1].Expression is TypeOfExpressionSyntax typeOfExpression)
    //     {
    //         typeOfExpression.Type
    //     }
    //     
    //     // var str1 = actorDefine.ArgumentList.Arguments.ToString();
    //     var str2 = actorDefine.ArgumentList.Arguments[0].ToString();
    //     var str3 = actorDefine.ArgumentList.Arguments[1].ToString();
    //     var str4 = actorDefine.ArgumentList.Arguments[2].ToString();
    //
    //     var str5 = actorDefine.ArgumentList.Arguments[2].FullSpan.ToString();
    //     
    //     // var str6 = actorDefine.ArgumentList.Arguments[2].NameEquals.Name.Identifier.ValueText;
    //     
    //     var str7 = actorDefine.ArgumentList.Arguments[2].Expression.ToString();
    //
    //     var e1 = actorDefine.ArgumentList.Arguments[0].Expression;
    //     var e2 = actorDefine.ArgumentList.Arguments[1].Expression;
    //
    //     // if (e2 is TypeOfExpressionSyntax typeOfExpression)
    //     // {
    //     //     var tt = typeOfExpression.Type;
    //     //     var xx = tt.GetText().ToString();
    //     //     
    //     //     var modelTypeName = typeOfExpression.Type.ToString();
    //     //     var xm = typeOfExpression.Type.ToFullString();
    //     //     
    //     //     _ = actorDefine.ArgumentList.Arguments[2].Expression;
    //     //     // Console.WriteLine($"modelTypeName: {modelTypeName}");
    //     // }
    //     
    //     var e3 = actorDefine.ArgumentList.Arguments[2].Expression;
    //     
    //     var modelType = attrs
    //         .Select(attr => attr.ArgumentList?.Arguments
    //             .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == "model")?.Expression)
    //         .FirstOrDefault() as TypeOfExpressionSyntax;
    //     
    //     var handlerType = attrs
    //         .Select(attr => attr.ArgumentList?.Arguments
    //             .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == "handler")?.Expression)
    //         .FirstOrDefault() as TypeOfExpressionSyntax;
    //     var handler = handlerType?.Type.ToString();
    //     
    //     // var handlerType = attrs
    //     //     .Select(attr => attr.ArgumentList?.Arguments
    //     //         .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == "handler")?.Expression)
    //     //     .FirstOrDefault() as TypeOfExpressionSyntax;
    //     // var handler = handlerType?.Type.ToString();
    //     
    //     var interfaceSymbol = (INamedTypeSymbol)ModelExtensions.GetDeclaredSymbol(context.SemanticModel, interfaceDeclaration);
    //     
    //     // 检查接口是否带有 ActorDefine 特性
    //     var actorDefineAttribute = interfaceSymbol.GetAttributes()
    //         .FirstOrDefault(attr => attr.AttributeClass?.Name == "ActorDefine");
    //
    //     if (actorDefineAttribute == null)
    //         return;
    //
    //     // 获取 Handler 类型名称
    //     var handlerTypeName = actorDefineAttribute.ConstructorArguments
    //         .First(arg => arg.Type?.Name == "Type").Value?.ToString();
    //
    //     if (handlerTypeName == null)
    //         return;
    //
    //     // 缓存接口和 Handler 类型信息
    //     _actorInterfaces[interfaceSymbol.ToDisplayString()] = handlerTypeName;
    // }


    // private static void AnalyzePlayerHandler(SyntaxNodeAnalysisContext context)
    // {
    //     var classDeclaration = (ClassDeclarationSyntax)context.Node;
    //     var classSymbol = (INamedTypeSymbol)ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration);
    //
    //     // 获取所有带有 ActorDefine 特性的接口
    //     var actorInterfaces = context.SemanticModel.Compilation.GetTypesByMetadataName("Server.Hotfix.ActorDemo.IPlayer")
    //         .Where(type => type.GetAttributes()
    //             .Any(attr => attr.AttributeClass?.Name == "ActorDefine"));
    //
    //     foreach (var actorInterface in actorInterfaces)
    //     {
    //         // 获取 ActorDefine 特性中定义的 Handler 类型
    //         var actorDefineAttribute = actorInterface.GetAttributes()
    //             .First(attr => attr.AttributeClass?.Name == "ActorDefine");
    //         var handlerTypeName = actorDefineAttribute.ConstructorArguments
    //             .First(arg => arg.Type?.Name == "Type").Value?.ToString();
    //
    //         if (handlerTypeName == null)
    //             continue;
    //
    //         var handlerType = context.SemanticModel.Compilation.GetTypeByMetadataName(handlerTypeName);
    //         if (handlerType == null)
    //             continue;
    //
    //         // 检查当前类是否为 Handler 类型
    //         if (classSymbol?.Name != handlerType.Name)
    //             continue;
    //
    //         // 获取接口中的所有方法
    //         var interfaceMethods = actorInterface.GetMembers().OfType<IMethodSymbol>();
    //
    //         // 检查 Handler 类是否实现了接口中的所有方法
    //         foreach (var method in interfaceMethods)
    //         {
    //             var methodName = method.Name;
    //             var extensionMethodName = $"{methodName}";
    //
    //             var methodExists = classSymbol.GetMembers()
    //                 .OfType<IMethodSymbol>()
    //                 .Any(m => m.Name == extensionMethodName && m.IsExtensionMethod);
    //
    //             if (!methodExists)
    //             {
    //                 var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), methodName);
    //                 context.ReportDiagnostic(diagnostic);
    //             }
    //         }
    //     }
    // }
    
    
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
    
    private static List<MethodDeclarationSyntax> FindMethods(CompilationAnalysisContext context,
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
        if (firstClassDeclaration == null)
        {
            return null;
        }
        
        var methods = firstClassDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
            {
                if (method.SyntaxTree == null)
                {
                    return false;
                }
                var semanticModel = compilation.GetSemanticModel(method.SyntaxTree);
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

            // SemanticModel semanticModel = null;
            // try
            // {
            //     semanticModel = compilation.GetSemanticModel(actorInterface.SyntaxTree);
            //     if (semanticModel == null)
            //     {
            //         continue;
            //     }
            // }
            // catch (Exception e)
            // {
            //     var diagnostic = Diagnostic.Create(Rule, Location.None, e.ToString());
            //     context.ReportDiagnostic(diagnostic);
            //     throw e;
            // }
           
            
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

                var methods = FindMethods(context, actorDefine, fullClassName);

                // var firstClassDeclaration = FindClassDeclaration(context, fullClassName);
                // // 不存在没定义的。
                // if (firstClassDeclaration == null)
                // {
                //     continue;
                // }
                
                
                _ = semanticModel.GetTypeInfo(typeOfExpression.Type);
                
                //
                // foreach (var actorCls in _actorClasss)
                // {
                //     var clsSemanticModel = compilation.GetSemanticModel(actorCls.SyntaxTree);
                //     var classSymbol = clsSemanticModel.GetDeclaredSymbol(actorCls);
                //     
                //     var fullstr = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                //     
                //     // clsSemanticModel.GetTypeInfo(classSymbol);
                //     //
                //     // var fullstr = clsTypeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                //     //
                //     if (fullstr == fullClassName)
                //     {
                //         break;
                //     }
                // }
            }
            
            
            // 索引0: model，1: handler, 2: keyType
            // if (actorDefine.ArgumentList?.Arguments[1].Expression is TypeOfExpressionSyntax typeOfExpression)
            // {
            //     var handlerTypeName = typeOfExpression.Type.ToString();
            //     if (handlerTypeName == null)
            //     {
            //         continue;
            //     }
            // }
            
            // 从 actorClasss中找到对应的handler
            // _actorClasss.Where(actorClass => actorClass)
            //
            //
            // // 检查当前类是否为 Handler 类型
            // if (classSymbol?.Name != handlerType.Name)
            //     continue;
            //
            // // 获取接口中的所有方法
            // var interfaceMethods = actorInterface.GetMembers().OfType<IMethodSymbol>();
            //
            // // 检查 Handler 类是否实现了接口中的所有方法
            // foreach (var method in interfaceMethods)
            // {
            //     var methodName = method.Name;
            //     var extensionMethodName = $"{methodName}";
            //
            //     var methodExists = classSymbol.GetMembers()
            //         .OfType<IMethodSymbol>()
            //         .Any(m => m.Name == extensionMethodName && m.IsExtensionMethod);
            //
            //     if (!methodExists)
            //     {
            //         var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), methodName);
            //         context.ReportDiagnostic(diagnostic);
            //     }
            // }
        }
        
        _ = _actorClasss.Count;
        _ = _actorInterfaces.Count;

        _ = _actorInterfaces.Count;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DynamicData.Kernel;
using NexusMods.MnemonicDB.QueryableParser.AST;

namespace NexusMods.MnemonicDB.QueryableParser;

public class Parser
{
    private readonly HashSet<MethodInfo> _rootMethods;

    public Parser(IEnumerable<MethodInfo> rootMethods)
    {
        _rootMethods = rootMethods.ToHashSet();
    }
    public INode Parse(Expression expression)
    {
        var context = new ParseContext();
        ParseNode(expression, context);
        return new Conjunction(context.Nodes.ToArray());
    }

    private void ParseNode(Expression expression, ParseContext context)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Call:
                ParseMethodCall((MethodCallExpression)expression, context);
                break;
            case ExpressionType.Quote:
                ParseNode(((UnaryExpression)expression).Operand, context);
                break;
            case ExpressionType.Lambda:
                ParseLambda((LambdaExpression)expression, context);
                break;
            case ExpressionType.Equal:
                ParseEqual((BinaryExpression)expression, context);
                break;
            case ExpressionType.MemberAccess:
                ParseMemberAccess((MemberExpression)expression, context);
                break;
            case ExpressionType.Parameter:
                ParseParameter ((ParameterExpression)expression, context);
                break;
            case ExpressionType.Constant:
                ParseConstant((ConstantExpression)expression, context);
                break;
            default:
                throw new NotSupportedException($"Expression type '{expression.NodeType}' is not supported.");
        };

    }

    private void ParseConstant(ConstantExpression expression, ParseContext context)
    {
        var output = LVar.New();
        context.Add(new Constant<object>(expression.Value!, output));
        context.Push(output);
        
    }

    private void ParseParameter(ParameterExpression expression, ParseContext context)
    {
        if (context.TryGetMapping(expression.Name!, out var variable))
            context.ReturnVar = variable;
    }
    

    private void ParseMemberAccess(MemberExpression expression, ParseContext context)
    {
        ParseNode(expression.Expression!, context);
        
        if (expression.Member is PropertyInfo property)
        {
            var source = context.ReturnVar;
            var output = LVar.New();
            context.Add(new PropertyAccess(source, property, output));
            context.Push(output);
            return;
        }

        throw new NotImplementedException();
    }

    private void ParseEqual(BinaryExpression expression, ParseContext context)
    {
        ParseNode(expression.Left, context);
        ParseNode(expression.Right, context);
        context.Add(new Unify(context.Pop(), context.Pop()));
    }

    private void ParseLambda(LambdaExpression expression, ParseContext context)
    {
        foreach (var parameter in expression.Parameters)
        {
            if (parameter.Name == null) 
                continue;
            context.PushMapping(parameter.Name!, context.Variables.Peek());
        }
        
        ParseNode(expression.Body, context);
        
        foreach (var parameter in expression.Parameters)
        {
            if (parameter.Name == null) 
                continue;
            context.PopMapping(parameter.Name!);
        }
    }

    private void ParseMethodCall(MethodCallExpression expression, ParseContext context)
    {
        var method = expression.Method;
        if (_rootMethods.Contains(method))
        {
            var lvar = LVar.New();
            context.Push(lvar);
            context.Add(new Root(method, lvar));
            return;
        }

        if (method.IsGenericMethod)
        {
            var genericMethod = method.GetGenericMethodDefinition();

            switch (genericMethod.Name, genericMethod.DeclaringType!.FullName)
            {
                case ("Where", "System.Linq.Queryable"):
                    ParseWhere(expression, context);
                    return;

                
                default :
                    throw new NotImplementedException($"Generic method '{genericMethod.Name}' in '{genericMethod.DeclaringType!.FullName}' is not supported.");
                
            }
        }

        throw new NotImplementedException();
    }

    private void ParseWhere(MethodCallExpression expression, ParseContext context)
    {
        ParseNode(expression.Arguments[0], context);
        var prev = context.Peek();
        ParseNode(expression.Arguments[1], context);
        context.Push(prev);
    }
}

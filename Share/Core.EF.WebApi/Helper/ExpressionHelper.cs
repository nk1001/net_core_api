using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using Core.Helper.APIMessage;
using Core.Helper.Extend;

namespace Core.EF.WebApi.Helper
{

    class ParameterlessExpressionSearcher : ExpressionVisitor
    {
        public HashSet<Expression> ParameterlessExpressions { get; } = new HashSet<Expression>();
        private bool containsParameter = false;

        public override Expression Visit(Expression node)
        {
            bool originalContainsParameter = containsParameter;
            containsParameter = false;
            base.Visit(node);
            if (!containsParameter)
            {
                if (node?.NodeType == ExpressionType.Parameter)
                    containsParameter = true;
                else
                    if (node != null)
                    ParameterlessExpressions.Add(node);
            }
            containsParameter |= originalContainsParameter;

            return node;
        }
    }
    class ParameterlessExpressionEvaluator : ExpressionVisitor
    {
        public Dictionary<string, object> Paramaters = new Dictionary<string, object>();
        private HashSet<Expression> parameterlessExpressions;
        bool IsParams = false;
        public ParameterlessExpressionEvaluator(HashSet<Expression> parameterlessExpressions, bool isParams = false)
        {
            this.parameterlessExpressions = parameterlessExpressions;
            IsParams = isParams;
        }
        public override Expression Visit(Expression node)
        {
            if (parameterlessExpressions.Contains(node))
                return Evaluate(node);
            else
                return base.Visit(node);
        }
        private Expression Evaluate(Expression node)
        {
            try
            {
                if (node.NodeType == ExpressionType.Constant)
                {
                    if (IsParams)
                    {
                        if (node.Type == typeof(DateTime) || node.Type == typeof(DateTime?))
                        {
                            var guid = Guid.NewGuid().ToString("N");
                            var result = Expression.Variable(node.Type, $"@{guid}");
                            Expression.Assign(result, node);
                            Paramaters.Add($"@{guid}", ((ConstantExpression)node).Value);
                            return result;
                        }

                    }

                    return node;
                }
                object value = Expression.Lambda(node).Compile().DynamicInvoke();
                if (value != null && (value.GetType().IsGenericList() || value.GetType().IsArray) && IsParams)
                {
                    string key = "@";
                    int index = 0;
                    while (true)
                    {
                        if (!Paramaters.ContainsKey(key + index))
                        {
                            key = key + index;
                            Paramaters.Add(key, value);
                            break;
                        }
                        index++;
                    }
                    ParameterExpression parameterExpression = Expression.Parameter(node.Type, key);

                    return parameterExpression;
                }
                else
                {
                    return Expression.Constant(value, node.Type);
                }
            }
            catch (Exception)
            {

                return node;
            }

        }
    }
    public static class ExpressionExtensions
    {
        static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Expression NotEqual(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.NotEqual(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.NotEqual(memberExpression, constantToCompare);
            }

        }
        public static Expression Equal(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.Equal(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.Equal(memberExpression, constantToCompare);
            }

        }

        public static bool ExecuteExpression<T>(T o, Expression? expression)
        {
            if (expression != null)
            {
                return ((Expression<Func<T, bool>>)expression).Compile().Invoke(o);
            }
            return false;
        }

        public static Expression Simplify(this Expression expression)
        {
            var searcher = new ParameterlessExpressionSearcher();
            searcher.Visit(expression);
            return new ParameterlessExpressionEvaluator(searcher.ParameterlessExpressions).Visit(expression);
        }

        public static Expression<T> Simplify<T>(this Expression<T> expression)
        {
            return (Expression<T>)Simplify((Expression)expression);
        }
        public static Expression<T> SimplifyParams<T>(this Expression<T> expression, out Dictionary<string, object> paramaters)
        {
            return (Expression<T>)SimplifyParams((Expression)expression, out paramaters);
        }
        public static Expression SimplifyParams(this Expression expression, out Dictionary<string, object> paramaters)
        {
            var searcher = new ParameterlessExpressionSearcher();
            searcher.Visit(expression);
            var evaluator = new ParameterlessExpressionEvaluator(searcher.ParameterlessExpressions, true);
            var ex = evaluator.Visit(expression);
            paramaters = evaluator.Paramaters;
            return ex;
        }
        //all previously shown code goes here

    }
   
    public class ExpressionHelper
    {
        public static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static Expression Equal(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.Equal(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.Equal(memberExpression, constantToCompare);
            }

        }

        public static Expression GreaterThanOrEqual(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.GreaterThanOrEqual(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.GreaterThanOrEqual(memberExpression, constantToCompare);
            }

        }

        public static Expression LessThanOrEqual(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.LessThanOrEqual(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.LessThanOrEqual(memberExpression, constantToCompare);
            }

        }
        public static Expression GreaterThan(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.GreaterThan(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.GreaterThan(memberExpression, constantToCompare);
            }

        }

        public static Expression LessThan(Expression memberExpression,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var notEqual = Expression.LessThan(valueExpression, constantToCompare);
                return Expression.AndAlso(hasValueExpression, notEqual);
            }
            else
            {
                return Expression.LessThan(memberExpression, constantToCompare);
            }

        }

        public static Expression CallMethod(Expression memberExpression,string methodName,
            ConstantExpression constantToCompare)
        {
            if (IsNullableType(memberExpression.Type))
            {
                var hasValueExpression = Expression.Property(memberExpression, "HasValue");
                var valueExpression = Expression.Property(memberExpression, "Value");
                var method = valueExpression.Type.GetMethod(methodName, new[] { valueExpression.Type });
                var exp = Expression.Call(valueExpression, method, constantToCompare);
                return Expression.AndAlso(hasValueExpression, exp);
            }
            else
            {
                var method = memberExpression.Type.GetMethod(methodName, new[] { memberExpression.Type });
                return Expression.Call(memberExpression, method, constantToCompare);
            }

        }
        public static Expression In(Expression expression, IList? iList)
        {
            if (IsNullableType(expression.Type))
            {
               
                var hasValueExpression = Expression.Property(expression, "HasValue");
                var valueExpression = Expression.Property(expression, "Value");
                var methodInfo = iList.GetType().GetMethod("Contains", new []{ valueExpression.Type});
                ConstantExpression foreignKeysParameter = Expression.Constant(iList);
                var exp = Expression.Call(foreignKeysParameter, methodInfo, valueExpression);
                return Expression.AndAlso(hasValueExpression, exp);
            }
            else
            {
                var methodInfo = iList.GetType().GetMethod("Contains", iList.GetType().GetGenericArguments());
                ConstantExpression foreignKeysParameter = Expression.Constant(iList, iList.GetType());
                MethodCallExpression exp = Expression.Call(foreignKeysParameter
                    , methodInfo, expression);
                return exp;
            }
        }



        public static Expression? BuildExpression<T>(List<FilterParameter> filterParameters,
            ParameterExpression pe) where T : class
        {
         
            Expression? all = null;
            foreach (var filterParameter in filterParameters)
            {
                if (filterParameter.Name != null)
                {
                    if (typeof(T).GetProperty(filterParameter.Name) != null)
                    {
                        Expression expression = Expression.Property(pe, filterParameter.Name);
                        Expression? exp = null;
                        TypeConverter vConverter = TypeDescriptor.GetConverter(expression.Type);
                        switch (filterParameter.Type)
                        {
                            case "equal":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }
                                var v = vConverter.ConvertFromString(filterParameter.Value);
                                exp = Equal(expression, Expression.Constant(v));
                                break;
                            case "begins_with":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }
                                v = vConverter.ConvertFromString(filterParameter.Value);
                                exp = CallMethod(expression, "StartsWith",
                                    Expression.Constant(v));
                                break;
                            case "ends_with":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }
                                v = vConverter.ConvertFromString(filterParameter.Value);
                                exp = CallMethod(expression, "EndsWith",
                                    Expression.Constant(v));
                                break;
                            case "contains":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }
                                v = vConverter.ConvertFromString(filterParameter.Value);
                                exp = CallMethod(expression, "Contains",
                                    Expression.Constant(v));
                                break;
                            case "is_null":
                                exp = Expression.Equal(expression, Expression.Constant(null, expression.Type));
                                break;
                            case "greater_than_or_equal":
                                exp = GreaterThanOrEqual(expression, Expression.Constant(null, expression.Type));
                                break;
                            case "greater_than":
                                exp = GreaterThan(expression, Expression.Constant(null, expression.Type));
                                break;
                            case "less_than_or_equal":
                                exp = LessThanOrEqual(expression, Expression.Constant(null, expression.Type));
                                break;
                            case "less_than":
                                exp = LessThan(expression, Expression.Constant(null, expression.Type));
                                break;
                            case "between":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }

                                v = vConverter.ConvertFromString(filterParameter.Value.Split('|')[0]);
                                TypeConverter converter = TypeDescriptor.GetConverter(expression.Type);
                                var e = converter.ConvertFromString(filterParameter.Value.Split('|')[1]);
                                exp = GreaterThanOrEqual(expression, Expression.Constant(v));
                                exp = Expression.AndAlso(exp, LessThanOrEqual(expression, Expression.Constant(e)));
                                break;
                            case "in":
                                if (filterParameter.Value == null)
                                {
                                    throw new Exception($"{filterParameter.Name} => Value is NULL");
                                }
                                var lstValue = filterParameter.Value.Split('|');
                                var type = expression.Type;
                                if (IsNullableType(expression.Type))
                                {
                                    type = expression.Type.GetGenericArguments()[0];
                                }
                                IList iList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;
                                foreach (var val in lstValue)
                                {
                                    TypeConverter vvConverter = TypeDescriptor.GetConverter(type);

                                    var nVal = vvConverter.ConvertFromString(val);
                                    iList.Add(nVal);
                                }
                                exp = In(expression, iList);
                                break;


                        }

                        if (filterParameter.FilterParameters is { Count: > 0 })
                        {
                            var expChild = BuildExpression<T>(filterParameter.FilterParameters, pe);
                            if (exp != null && expChild != null)
                            {
                                exp = filterParameter.ConditionGroup == "AND" ? Expression.AndAlso(exp, expChild) : Expression.OrElse(exp, expChild);
                            }
                            else
                            {
                                if (expChild!=null)
                                {
                                    exp = expChild;
                                }
                            }
                        }
                        var condition = filterParameter.Condition;
                        if (exp != null)
                        {
                            if (all == null)
                            {

                                all = exp;
                            }
                            else
                            {
                                all = condition == "AND" ? Expression.AndAlso(all, exp) : Expression.OrElse(all, exp);
                            }
                        }
                    }
                    else
                    {
                        Expression? exp = null;
                        if (filterParameter.FilterParameters is { Count: > 0 })
                        {
                            var expChild = BuildExpression<T>(filterParameter.FilterParameters, pe);
                            if (exp != null && expChild != null)
                            {
                                exp = filterParameter.ConditionGroup == "AND" ? Expression.AndAlso(exp, expChild) : Expression.OrElse(exp, expChild);
                            }
                            else
                            {
                                if (expChild != null)
                                {
                                    exp = expChild;
                                }
                            }
                        }
                        var condition = filterParameter.Condition;
                        if (exp != null)
                        {
                            if (all == null)
                            {

                                all = exp;
                            }
                            else
                            {
                                all = condition == "AND" ? Expression.AndAlso(all, exp) : Expression.OrElse(all, exp);
                            }
                        }
                    }
                   
                   
                }
               
            }
            
            return all;
        }
    }
}

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal static class FishingNativeAccessors
    {
        internal static MemberInfo? FindMember(Type? type, string name, bool isStatic = false)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
                PropertyInfo? property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property?.GetGetMethod(true) != null)
                    return property;
            }
            return null;
        }

        internal static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.Name == name && !method.IsGenericMethodDefinition && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }

        internal static Func<object?>? CreateStaticObjectGetter(MemberInfo? member)
        {
            if (!TryGetMemberType(member, out Type? valueType) || valueType!.IsValueType)
                return null;
            Expression read = MakeRead(member!, null);
            return Expression.Lambda<Func<object?>>(Expression.Convert(read, typeof(object))).Compile();
        }

        internal static Func<bool>? CreateStaticBoolGetter(MemberInfo? member)
        {
            if (!TryGetMemberType(member, out Type? valueType) || valueType != typeof(bool))
                return null;
            return Expression.Lambda<Func<bool>>(MakeRead(member!, null)).Compile();
        }

        internal static Func<int, bool>? CreateStaticBoolIntMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || !target.IsStatic || target.ReturnType != typeof(bool) ||
                parameters.Length != 1 || parameters[0].ParameterType != typeof(int))
            {
                return null;
            }
            ParameterExpression value = Expression.Parameter(typeof(int), "value");
            return Expression.Lambda<Func<int, bool>>(Expression.Call(target, value), value).Compile();
        }

        internal static Func<float>? CreateStaticFloatGetter(MemberInfo? member)
        {
            if (!TryGetMemberType(member, out Type? valueType) || valueType != typeof(float))
                return null;
            return Expression.Lambda<Func<float>>(MakeRead(member!, null)).Compile();
        }

        internal static Func<object, object?>? CreateObjectGetter(MemberInfo? member)
        {
            if (!TryGetMemberType(member, out Type? valueType) || valueType!.IsValueType)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            Expression read = MakeRead(member!, Expression.Convert(instance, member!.DeclaringType!));
            return Expression.Lambda<Func<object, object?>>(Expression.Convert(read, typeof(object)), instance).Compile();
        }

        internal static Func<object, bool>? CreateBoolGetter(MemberInfo? member)
        {
            return CreateValueGetter<bool>(member);
        }

        internal static Func<object, float>? CreateFloatGetter(MemberInfo? member)
        {
            return CreateValueGetter<float>(member);
        }

        internal static Func<object, double>? CreateDoubleGetter(MemberInfo? member)
        {
            return CreateValueGetter<double>(member);
        }

        internal static Func<object, int>? CreateIntGetter(MemberInfo? member)
        {
            return CreateValueGetter<int>(member);
        }

        internal static Func<object, int>? CreateEnumIntGetter(MemberInfo? member)
        {
            if (!TryGetMemberType(member, out Type? valueType) || !valueType!.IsEnum)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            Expression read = MakeRead(member!, Expression.Convert(instance, member!.DeclaringType!));
            return Expression.Lambda<Func<object, int>>(Expression.Convert(read, typeof(int)), instance).Compile();
        }

        internal static Action<object, bool>? CreateBoolSetter(MemberInfo? member)
        {
            return CreateValueSetter<bool>(member);
        }

        internal static Action<object, float>? CreateFloatSetter(MemberInfo? member)
        {
            return CreateValueSetter<float>(member);
        }

        internal static Action<object, double>? CreateDoubleSetter(MemberInfo? member)
        {
            return CreateValueSetter<double>(member);
        }

        internal static Func<object, Type, object?>? CreateObjectTypeMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || target.ReturnType.IsValueType || parameters.Length != 1 || parameters[0].ParameterType != typeof(Type))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression type = Expression.Parameter(typeof(Type), "type");
            MethodCallExpression call = Expression.Call(Expression.Convert(instance, target.DeclaringType!), target, type);
            return Expression.Lambda<Func<object, Type, object?>>(Expression.Convert(call, typeof(object)), instance, type).Compile();
        }

        internal static Func<object, object?>? CreateObjectMethod(MethodInfo? target)
        {
            if (target == null || target.ReturnType.IsValueType || target.GetParameters().Length != 0)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            MethodCallExpression call = Expression.Call(Expression.Convert(instance, target.DeclaringType!), target);
            return Expression.Lambda<Func<object, object?>>(Expression.Convert(call, typeof(object)), instance).Compile();
        }

        internal static Func<object, bool>? CreateBoolMethod(MethodInfo? target)
        {
            if (target == null || target.ReturnType != typeof(bool) || target.GetParameters().Length != 0)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Func<object, bool>>(Expression.Call(Expression.Convert(instance, target.DeclaringType!), target), instance).Compile();
        }

        internal static Action<object>? CreateVoidMethod(MethodInfo? target)
        {
            if (target == null || target.ReturnType != typeof(void) || target.GetParameters().Length != 0)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Action<object>>(Expression.Call(Expression.Convert(instance, target.DeclaringType!), target), instance).Compile();
        }

        internal static Action<object, float>? CreateVoidFloatMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || target.ReturnType != typeof(void) || parameters.Length != 1 || parameters[0].ParameterType != typeof(float))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(float), "value");
            return Expression.Lambda<Action<object, float>>(
                Expression.Call(Expression.Convert(instance, target.DeclaringType!), target, value),
                instance,
                value).Compile();
        }

        internal static Func<object, int, object?>? CreateObjectIntMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || target.ReturnType.IsValueType || parameters.Length != 1 || parameters[0].ParameterType != typeof(int))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(int), "value");
            MethodCallExpression call = Expression.Call(Expression.Convert(instance, target.DeclaringType!), target, value);
            return Expression.Lambda<Func<object, int, object?>>(Expression.Convert(call, typeof(object)), instance, value).Compile();
        }

        internal static Action<object, object>? CreateVoidObjectMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || target.ReturnType != typeof(void) || parameters.Length != 1 || parameters[0].ParameterType.IsValueType)
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(object), "value");
            MethodCallExpression call = Expression.Call(
                Expression.Convert(instance, target.DeclaringType!),
                target,
                Expression.Convert(value, parameters[0].ParameterType));
            return Expression.Lambda<Action<object, object>>(call, instance, value).Compile();
        }

        internal static Action<object, object>? CreateVoidObjectForceMethod(MethodInfo? target)
        {
            ParameterInfo[] parameters = target?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (target == null || target.ReturnType != typeof(void) || parameters.Length < 1 || parameters.Length > 2 || parameters[0].ParameterType.IsValueType)
                return null;
            if (parameters.Length == 2 && parameters[1].ParameterType != typeof(bool))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(object), "value");
            Expression[] arguments = parameters.Length == 1
                ? new Expression[] { Expression.Convert(value, parameters[0].ParameterType) }
                : new Expression[] { Expression.Convert(value, parameters[0].ParameterType), Expression.Constant(true) };
            MethodCallExpression call = Expression.Call(Expression.Convert(instance, target.DeclaringType!), target, arguments);
            return Expression.Lambda<Action<object, object>>(call, instance, value).Compile();
        }

        internal static Action<object, object, float>? CreateObjectFloatResultSetter(MethodInfo? source, MemberInfo? target)
        {
            ParameterInfo[] parameters = source?.GetParameters() ?? Array.Empty<ParameterInfo>();
            if (source == null || parameters.Length != 1 || parameters[0].ParameterType != typeof(float) ||
                !TryGetWritableMemberType(target, out Type? targetType) || targetType != source.ReturnType)
            {
                return null;
            }
            ParameterExpression sourceInstance = Expression.Parameter(typeof(object), "source");
            ParameterExpression targetInstance = Expression.Parameter(typeof(object), "target");
            ParameterExpression value = Expression.Parameter(typeof(float), "value");
            MethodCallExpression call = Expression.Call(Expression.Convert(sourceInstance, source.DeclaringType!), source, value);
            Expression writeTarget = target is FieldInfo field
                ? Expression.Field(Expression.Convert(targetInstance, field.DeclaringType!), field)
                : Expression.Property(Expression.Convert(targetInstance, target!.DeclaringType!), (PropertyInfo)target);
            return Expression.Lambda<Action<object, object, float>>(
                Expression.Assign(writeTarget, call),
                sourceInstance,
                targetInstance,
                value).Compile();
        }

        private static Func<object, T>? CreateValueGetter<T>(MemberInfo? member) where T : struct
        {
            if (!TryGetMemberType(member, out Type? valueType) || valueType != typeof(T))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            Expression read = MakeRead(member!, Expression.Convert(instance, member!.DeclaringType!));
            return Expression.Lambda<Func<object, T>>(read, instance).Compile();
        }

        private static Action<object, T>? CreateValueSetter<T>(MemberInfo? member) where T : struct
        {
            if (!TryGetWritableMemberType(member, out Type? valueType) || valueType != typeof(T))
                return null;
            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(T), "value");
            Expression target = member is FieldInfo field
                ? Expression.Field(Expression.Convert(instance, field.DeclaringType!), field)
                : Expression.Property(Expression.Convert(instance, member!.DeclaringType!), (PropertyInfo)member);
            return Expression.Lambda<Action<object, T>>(Expression.Assign(target, value), instance, value).Compile();
        }

        private static Expression MakeRead(MemberInfo member, Expression? instance)
        {
            return member is FieldInfo field
                ? Expression.Field(instance, field)
                : Expression.Property(instance, (PropertyInfo)member);
        }

        private static bool TryGetMemberType(MemberInfo? member, out Type? valueType)
        {
            valueType = member is FieldInfo field ? field.FieldType : (member as PropertyInfo)?.PropertyType;
            return valueType != null;
        }

        private static bool TryGetWritableMemberType(MemberInfo? member, out Type? valueType)
        {
            valueType = null;
            if (member is FieldInfo field && !field.IsInitOnly)
            {
                valueType = field.FieldType;
                return true;
            }
            if (member is PropertyInfo property && property.GetSetMethod(true) != null)
            {
                valueType = property.PropertyType;
                return true;
            }
            return false;
        }
    }
}

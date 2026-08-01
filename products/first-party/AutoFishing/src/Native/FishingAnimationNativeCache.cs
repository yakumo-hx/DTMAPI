using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static Yuuka.DTMAPI.AutoFishing.FishingNativeAccessors;
using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingAnimationNativeCache
    {
        private static readonly string[] BodyMemberNames = { "body", "Body" };
        private static readonly string[] RodRendererMemberNames = { "fishRodRenderer", "FishRodRenderer" };
        private static readonly string[] AnimatorMemberNames = { "animator", "_animator", "Animator" };
        private static readonly string[] AnimatorSpeedMemberNames = { "speed", "Speed" };
        private static readonly string[] HookMemberNames = { "Hook", "_hook" };
        private static readonly string[] RigidbodyMemberNames = { "rigidbody", "Rigidbody", "body", "Body" };
        private static readonly string[] VelocityMemberNames = { "Velocity", "velocity" };
        private static readonly string[] GravityMemberNames = { "gravityScale", "GravityScale" };
        private static readonly string[] PullDurationMemberNames = { "_pullDuration", "pullDuration", "PullDuration" };
        private readonly Dictionary<Type, OwnerAccessors> ownerAccessors = new Dictionary<Type, OwnerAccessors>();
        private readonly Dictionary<Type, ScalarAccessors> animatorAccessors = new Dictionary<Type, ScalarAccessors>();
        private readonly Dictionary<Type, HookRendererAccessors> hookRendererAccessors = new Dictionary<Type, HookRendererAccessors>();
        private readonly Dictionary<Type, HookAccessors> hookAccessors = new Dictionary<Type, HookAccessors>();
        private readonly Dictionary<Type, ScalarAccessors> gravityAccessors = new Dictionary<Type, ScalarAccessors>();
        private readonly Dictionary<Type, ScalarAccessors> pullDurationAccessors = new Dictionary<Type, ScalarAccessors>();
        private readonly object?[] candidates = new object?[16];
        private readonly Type? animatorType;
        private readonly Type? rigidbodyType;
        private Func<object?>? staticAgentGetter;
        private bool staticAgentResolved;
        private int candidateCount;

        internal FishingAnimationNativeCache()
        {
            animatorType = ResolveType("UnityEngine.Animator, UnityEngine.AnimationModule") ?? ResolveType("UnityEngine.Animator, UnityEngine");
            rigidbodyType = ResolveType("UnityEngine.Rigidbody2D, UnityEngine.Physics2DModule") ?? ResolveType("UnityEngine.Rigidbody2D, UnityEngine");
        }

        internal int Builds { get; private set; }
        internal int Rebuilds { get; private set; }
        internal int BuildFailures { get; private set; }
        internal int InvocationFailures { get; private set; }

        internal int CollectAnimatorCandidates(object source)
        {
            ClearCandidates();
            AddOwnerAndNested(source);
            object? agent = TryGetStaticAgent();
            if (agent != null && !ReferenceEquals(agent, source))
                AddOwnerAndNested(agent);
            return candidateCount;
        }

        internal object GetAnimatorCandidate(int index)
        {
            return candidates[index] ?? throw new InvalidOperationException("Fishing animation candidate was cleared.");
        }

        internal void ClearCandidates()
        {
            for (int i = 0; i < candidateCount; i++)
                candidates[i] = null;
            candidateCount = 0;
        }

        internal bool TryReadAnimatorSpeed(object animator, out double value)
        {
            return TryReadScalar(GetAnimatorAccessors(animator.GetType()), animator, out value);
        }

        internal bool TryWriteAnimatorSpeed(object animator, double value)
        {
            return TryWriteScalar(GetAnimatorAccessors(animator.GetType()), animator, value);
        }

        internal bool TryGetHook(object renderer, out object? hook)
        {
            hook = null;
            HookRendererAccessors accessors = GetHookRendererAccessors(renderer.GetType());
            if (accessors.Hook == null)
                return false;
            try
            {
                hook = accessors.Hook(renderer);
                return hook != null;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        internal bool TryScaleHookVelocity(object hook, double multiplier, out float originalX, out float originalY)
        {
            originalX = 0f;
            originalY = 0f;
            HookAccessors accessors = GetHookAccessors(hook.GetType());
            if (accessors.ReadVelocityX == null || accessors.ReadVelocityY == null || accessors.ScaleVelocity == null)
                return false;
            try
            {
                originalX = accessors.ReadVelocityX(hook);
                originalY = accessors.ReadVelocityY(hook);
                accessors.ScaleVelocity(hook, (float)multiplier);
                return true;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        internal bool TryRestoreHookVelocity(object hook, float x, float y)
        {
            HookAccessors accessors = GetHookAccessors(hook.GetType());
            if (accessors.WriteVelocity == null)
                return false;
            try
            {
                accessors.WriteVelocity(hook, x, y);
                return true;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        internal bool TryGetHookBody(object hook, out object? body)
        {
            body = null;
            HookAccessors accessors = GetHookAccessors(hook.GetType());
            try
            {
                if (accessors.Rigidbody != null)
                    body = accessors.Rigidbody(hook);
                if (body == null && rigidbodyType != null && accessors.GetComponent != null)
                    body = accessors.GetComponent(hook, rigidbodyType);
                return body != null;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        internal bool TryReadGravity(object body, out double value)
        {
            return TryReadScalar(GetGravityAccessors(body.GetType()), body, out value);
        }

        internal bool TryWriteGravity(object body, double value)
        {
            return TryWriteScalar(GetGravityAccessors(body.GetType()), body, value);
        }

        internal bool TryReadPullDuration(object state, out double value)
        {
            return TryReadScalar(GetPullDurationAccessors(state.GetType()), state, out value);
        }

        internal bool TryWritePullDuration(object state, double value)
        {
            return TryWriteScalar(GetPullDurationAccessors(state.GetType()), state, value);
        }

        private void AddOwnerAndNested(object owner)
        {
            AddAnimators(owner);
            OwnerAccessors accessors = GetOwnerAccessors(owner.GetType());
            object? body = InvokeOptional(accessors.Body, owner);
            object? rod = InvokeOptional(accessors.RodRenderer, owner);
            if (body != null && !ReferenceEquals(body, owner))
            {
                AddAnimators(body);
                OwnerAccessors bodyAccessors = GetOwnerAccessors(body.GetType());
                object? nestedRod = InvokeOptional(bodyAccessors.RodRenderer, body);
                if (nestedRod != null)
                    AddAnimators(nestedRod);
            }
            if (rod != null && !ReferenceEquals(rod, owner))
                AddAnimators(rod);
        }

        private void AddAnimators(object owner)
        {
            OwnerAccessors accessors = GetOwnerAccessors(owner.GetType());
            if (accessors.IsAnimator && GetAnimatorAccessors(owner.GetType()).Available)
                AddCandidate(owner);
            object? animator = InvokeOptional(accessors.Animator, owner);
            if (animator != null && GetAnimatorAccessors(animator.GetType()).Available)
                AddCandidate(animator);
            if (animatorType != null && accessors.GetComponent != null)
            {
                try
                {
                    object? component = accessors.GetComponent(owner, animatorType);
                    if (component != null && GetAnimatorAccessors(component.GetType()).Available)
                        AddCandidate(component);
                }
                catch
                {
                    InvocationFailures++;
                }
            }
        }

        private void AddCandidate(object value)
        {
            for (int i = 0; i < candidateCount; i++)
            {
                if (ReferenceEquals(candidates[i], value))
                    return;
            }
            if (candidateCount < candidates.Length)
                candidates[candidateCount++] = value;
        }

        private object? TryGetStaticAgent()
        {
            if (!staticAgentResolved)
            {
                staticAgentResolved = true;
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MemberInfo? agent = FindMember(dolocApi, "agent", isStatic: true);
                if (agent != null)
                {
                    try
                    {
                        staticAgentGetter = CreateStaticObjectGetter(agent);
                        Builds++;
                        if (staticAgentGetter == null)
                            BuildFailures++;
                    }
                    catch
                    {
                        Builds++;
                        BuildFailures++;
                    }
                }
            }
            if (staticAgentGetter == null)
                return null;
            try
            {
                return staticAgentGetter();
            }
            catch
            {
                InvocationFailures++;
                return null;
            }
        }

        private OwnerAccessors GetOwnerAccessors(Type type)
        {
            if (ownerAccessors.TryGetValue(type, out OwnerAccessors accessors))
                return accessors;
            Builds++;
            try
            {
                accessors = new OwnerAccessors(
                    FindFirstObjectGetter(type, BodyMemberNames),
                    FindFirstObjectGetter(type, RodRendererMemberNames),
                    FindFirstObjectGetter(type, AnimatorMemberNames),
                    CreateObjectTypeMethod(FindTypeMethod(type, "GetComponent")),
                    (animatorType != null && animatorType.IsAssignableFrom(type)) || FindFirstMember(type, AnimatorSpeedMemberNames) != null);
            }
            catch
            {
                BuildFailures++;
                accessors = OwnerAccessors.Unavailable;
            }
            ownerAccessors[type] = accessors;
            return accessors;
        }

        private ScalarAccessors GetAnimatorAccessors(Type type)
        {
            if (animatorAccessors.TryGetValue(type, out ScalarAccessors accessors))
                return accessors;
            accessors = BuildScalarAccessors(type, AnimatorSpeedMemberNames);
            animatorAccessors[type] = accessors;
            return accessors;
        }

        private HookRendererAccessors GetHookRendererAccessors(Type type)
        {
            if (hookRendererAccessors.TryGetValue(type, out HookRendererAccessors accessors))
                return accessors;
            Builds++;
            try
            {
                Func<object, object?>? hook = FindFirstObjectGetter(type, HookMemberNames);
                if (hook == null)
                    BuildFailures++;
                accessors = new HookRendererAccessors(hook);
            }
            catch
            {
                BuildFailures++;
                accessors = HookRendererAccessors.Unavailable;
            }
            hookRendererAccessors[type] = accessors;
            return accessors;
        }

        private HookAccessors GetHookAccessors(Type type)
        {
            if (hookAccessors.TryGetValue(type, out HookAccessors accessors))
                return accessors;
            Builds++;
            try
            {
                MemberInfo? velocity = FindFirstMember(type, VelocityMemberNames);
                CreateVectorAccessors(velocity, out Func<object, float>? readX, out Func<object, float>? readY, out Action<object, float, float>? write, out Action<object, float>? scale);
                Func<object, object?>? body = FindFirstObjectGetter(type, RigidbodyMemberNames);
                Func<object, Type, object?>? getComponent = CreateObjectTypeMethod(FindTypeMethod(type, "GetComponent"));
                if (velocity == null || readX == null || readY == null || write == null || scale == null)
                    BuildFailures++;
                accessors = new HookAccessors(readX, readY, write, scale, body, getComponent);
            }
            catch
            {
                BuildFailures++;
                accessors = HookAccessors.Unavailable;
            }
            hookAccessors[type] = accessors;
            return accessors;
        }

        private ScalarAccessors GetGravityAccessors(Type type)
        {
            if (gravityAccessors.TryGetValue(type, out ScalarAccessors accessors))
                return accessors;
            accessors = BuildScalarAccessors(type, GravityMemberNames);
            gravityAccessors[type] = accessors;
            return accessors;
        }

        private ScalarAccessors GetPullDurationAccessors(Type type)
        {
            if (pullDurationAccessors.TryGetValue(type, out ScalarAccessors accessors))
                return accessors;
            accessors = BuildScalarAccessors(type, PullDurationMemberNames);
            pullDurationAccessors[type] = accessors;
            return accessors;
        }

        private ScalarAccessors BuildScalarAccessors(Type type, string[] names)
        {
            Builds++;
            try
            {
                MemberInfo? member = FindFirstMember(type, names);
                var accessors = new ScalarAccessors(
                    CreateFloatGetter(member),
                    CreateFloatSetter(member),
                    CreateDoubleGetter(member),
                    CreateDoubleSetter(member));
                if (!accessors.Available)
                    BuildFailures++;
                return accessors;
            }
            catch
            {
                BuildFailures++;
                return ScalarAccessors.Unavailable;
            }
        }

        private bool TryReadScalar(ScalarAccessors accessors, object instance, out double value)
        {
            value = 0d;
            if (!accessors.Available)
                return false;
            try
            {
                value = accessors.ReadFloat != null ? accessors.ReadFloat(instance) : accessors.ReadDouble!(instance);
                return true;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        private bool TryWriteScalar(ScalarAccessors accessors, object instance, double value)
        {
            if (!accessors.Available)
                return false;
            try
            {
                if (accessors.WriteFloat != null)
                    accessors.WriteFloat(instance, (float)value);
                else
                    accessors.WriteDouble!(instance, value);
                return true;
            }
            catch
            {
                InvocationFailures++;
                return false;
            }
        }

        private object? InvokeOptional(Func<object, object?>? getter, object instance)
        {
            if (getter == null)
                return null;
            try
            {
                return getter(instance);
            }
            catch
            {
                InvocationFailures++;
                return null;
            }
        }

        private static Func<object, object?>? FindFirstObjectGetter(Type type, string[] names)
        {
            MemberInfo? member = FindFirstMember(type, names);
            return CreateObjectGetter(member);
        }

        private static MemberInfo? FindFirstMember(Type type, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                MemberInfo? member = FindMember(type, names[i]);
                if (member != null)
                    return member;
            }
            return null;
        }

        private static MethodInfo? FindTypeMethod(Type type, string name)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? method = current.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(Type) }, null);
                if (method != null)
                    return method;
            }
            return null;
        }

        private static void CreateVectorAccessors(
            MemberInfo? velocityMember,
            out Func<object, float>? readX,
            out Func<object, float>? readY,
            out Action<object, float, float>? write,
            out Action<object, float>? scale)
        {
            readX = null;
            readY = null;
            write = null;
            scale = null;
            Type? vectorType = GetMemberType(velocityMember);
            if (velocityMember == null || vectorType == null)
                return;
            MemberInfo? xMember = FindMember(vectorType, "x") ?? FindMember(vectorType, "X");
            MemberInfo? yMember = FindMember(vectorType, "y") ?? FindMember(vectorType, "Y");
            if (GetMemberType(xMember) != typeof(float) || GetMemberType(yMember) != typeof(float) || !IsWritable(velocityMember) || !IsWritable(xMember) || !IsWritable(yMember))
                return;

            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            Expression typedInstance = Expression.Convert(instance, velocityMember.DeclaringType!);
            Expression velocityRead = Read(velocityMember, typedInstance);
            readX = Expression.Lambda<Func<object, float>>(Read(xMember!, velocityRead), instance).Compile();
            readY = Expression.Lambda<Func<object, float>>(Read(yMember!, velocityRead), instance).Compile();

            ParameterExpression x = Expression.Parameter(typeof(float), "x");
            ParameterExpression y = Expression.Parameter(typeof(float), "y");
            ParameterExpression multiplier = Expression.Parameter(typeof(float), "multiplier");
            ParameterExpression vector = Expression.Variable(vectorType, "vector");
            Expression assignVector = Expression.Assign(vector, velocityRead);
            Expression assignX = Assign(xMember!, vector, x);
            Expression assignY = Assign(yMember!, vector, y);
            Expression writeVector = Assign(velocityMember, typedInstance, vector);
            write = Expression.Lambda<Action<object, float, float>>(
                Expression.Block(new[] { vector }, assignVector, assignX, assignY, writeVector),
                instance,
                x,
                y).Compile();
            scale = Expression.Lambda<Action<object, float>>(
                Expression.Block(
                    new[] { vector },
                    assignVector,
                    Assign(xMember!, vector, Expression.Multiply(Read(xMember!, vector), multiplier)),
                    Assign(yMember!, vector, Expression.Multiply(Read(yMember!, vector), multiplier)),
                    writeVector),
                instance,
                multiplier).Compile();
        }

        private static Type? GetMemberType(MemberInfo? member)
        {
            return member is FieldInfo field ? field.FieldType : (member as PropertyInfo)?.PropertyType;
        }

        private static bool IsWritable(MemberInfo? member)
        {
            return member is FieldInfo field ? !field.IsInitOnly : (member as PropertyInfo)?.GetSetMethod(true) != null;
        }

        private static Expression Read(MemberInfo member, Expression instance)
        {
            return member is FieldInfo field ? Expression.Field(instance, field) : Expression.Property(instance, (PropertyInfo)member);
        }

        private static Expression Assign(MemberInfo member, Expression instance, Expression value)
        {
            Expression target = member is FieldInfo field ? Expression.Field(instance, field) : Expression.Property(instance, (PropertyInfo)member);
            return Expression.Assign(target, value);
        }

        private sealed class OwnerAccessors
        {
            internal static readonly OwnerAccessors Unavailable = new OwnerAccessors(null, null, null, null, false);
            internal OwnerAccessors(Func<object, object?>? body, Func<object, object?>? rodRenderer, Func<object, object?>? animator, Func<object, Type, object?>? getComponent, bool isAnimator)
            {
                Body = body;
                RodRenderer = rodRenderer;
                Animator = animator;
                GetComponent = getComponent;
                IsAnimator = isAnimator;
            }
            internal Func<object, object?>? Body { get; }
            internal Func<object, object?>? RodRenderer { get; }
            internal Func<object, object?>? Animator { get; }
            internal Func<object, Type, object?>? GetComponent { get; }
            internal bool IsAnimator { get; }
        }

        private sealed class ScalarAccessors
        {
            internal static readonly ScalarAccessors Unavailable = new ScalarAccessors(null, null, null, null);
            internal ScalarAccessors(Func<object, float>? readFloat, Action<object, float>? writeFloat, Func<object, double>? readDouble, Action<object, double>? writeDouble)
            {
                ReadFloat = readFloat;
                WriteFloat = writeFloat;
                ReadDouble = readDouble;
                WriteDouble = writeDouble;
            }
            internal bool Available => (ReadFloat != null && WriteFloat != null) || (ReadDouble != null && WriteDouble != null);
            internal Func<object, float>? ReadFloat { get; }
            internal Action<object, float>? WriteFloat { get; }
            internal Func<object, double>? ReadDouble { get; }
            internal Action<object, double>? WriteDouble { get; }
        }

        private sealed class HookRendererAccessors
        {
            internal static readonly HookRendererAccessors Unavailable = new HookRendererAccessors(null);
            internal HookRendererAccessors(Func<object, object?>? hook) => Hook = hook;
            internal Func<object, object?>? Hook { get; }
        }

        private sealed class HookAccessors
        {
            internal static readonly HookAccessors Unavailable = new HookAccessors(null, null, null, null, null, null);
            internal HookAccessors(Func<object, float>? readVelocityX, Func<object, float>? readVelocityY, Action<object, float, float>? writeVelocity, Action<object, float>? scaleVelocity, Func<object, object?>? rigidbody, Func<object, Type, object?>? getComponent)
            {
                ReadVelocityX = readVelocityX;
                ReadVelocityY = readVelocityY;
                WriteVelocity = writeVelocity;
                ScaleVelocity = scaleVelocity;
                Rigidbody = rigidbody;
                GetComponent = getComponent;
            }
            internal Func<object, float>? ReadVelocityX { get; }
            internal Func<object, float>? ReadVelocityY { get; }
            internal Action<object, float, float>? WriteVelocity { get; }
            internal Action<object, float>? ScaleVelocity { get; }
            internal Func<object, object?>? Rigidbody { get; }
            internal Func<object, Type, object?>? GetComponent { get; }
        }
    }
}

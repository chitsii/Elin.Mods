using System;
using System.Linq;
using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal readonly struct CompatMethodSignature
    {
        public string MethodName { get; }
        public Type? ReturnType { get; }
        public bool AllowDerivedReturnType { get; }
        public Type[] ParameterTypes { get; }

        public CompatMethodSignature(
            string methodName,
            Type? returnType,
            Type[] parameterTypes,
            bool allowDerivedReturnType = false)
        {
            MethodName = methodName;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            AllowDerivedReturnType = allowDerivedReturnType;
        }

        public bool Matches(MethodInfo method)
        {
            if (!string.Equals(method.Name, MethodName, StringComparison.Ordinal))
                return false;

            if (ReturnType != null)
            {
                if (AllowDerivedReturnType)
                {
                    if (!ReturnType.IsAssignableFrom(method.ReturnType))
                        return false;
                }
                else if (method.ReturnType != ReturnType)
                {
                    return false;
                }
            }

            var p = method.GetParameters();
            if (p.Length != ParameterTypes.Length)
                return false;

            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].ParameterType != ParameterTypes[i])
                    return false;
            }

            return true;
        }

        public string ToDisplayText()
        {
            string returnLabel = ReturnType?.Name ?? "any";
            string args = string.Join(", ", ParameterTypes.Select(t => t.Name));
            return $"{returnLabel} {MethodName}({args})";
        }
    }
}

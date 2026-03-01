using System;
using System.Collections.Generic;
using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal sealed class CompatSymbol
    {
        public string Id { get; }
        public Type OwnerType { get; }
        public IReadOnlyList<string> CandidateNames { get; }
        public bool IsStatic { get; }
        public Func<MethodInfo, bool> Predicate { get; }
        public IReadOnlyList<CompatMethodSignature> StrictSignatures { get; }

        public CompatSymbol(
            string id,
            Type ownerType,
            string[] candidateNames,
            bool isStatic,
            Func<MethodInfo, bool> predicate,
            CompatMethodSignature[] strictSignatures)
        {
            Id = id;
            OwnerType = ownerType;
            CandidateNames = candidateNames;
            IsStatic = isStatic;
            Predicate = predicate;
            StrictSignatures = strictSignatures;
        }

        public static readonly CompatSymbol CharaSetMainElement = new(
            id: "Chara.SetMainElement",
            ownerType: typeof(Chara),
            candidateNames: new[] { "SetMainElement" },
            isStatic: false,
            predicate: method =>
            {
                if (method.ReturnType != typeof(void)) return false;
                var p = method.GetParameters();
                return p.Length >= 1 && p[0].ParameterType == typeof(int);
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("SetMainElement", typeof(void), new[] { typeof(int), typeof(int), typeof(bool) }),
                new CompatMethodSignature("SetMainElement", typeof(void), new[] { typeof(int), typeof(int) }),
                new CompatMethodSignature("SetMainElement", typeof(void), new[] { typeof(int) }),
            });

        public static readonly CompatSymbol CharaDie = new(
            id: "Chara.Die",
            ownerType: typeof(Chara),
            candidateNames: new[] { "Die" },
            isStatic: false,
            predicate: method =>
            {
                if (method.ReturnType != typeof(void)) return false;
                var p = method.GetParameters();
                if (p.Length < 3) return false;
                if (p[0].ParameterType != typeof(Element)) return false;
                if (p[1].ParameterType != typeof(Card)) return false;
                if (p[2].ParameterType != typeof(AttackSource)) return false;
                if (p.Length >= 4 && p[3].ParameterType != typeof(Chara)) return false;
                return true;
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("Die", typeof(void), new[] { typeof(Element), typeof(Card), typeof(AttackSource), typeof(Chara) }),
                new CompatMethodSignature("Die", typeof(void), new[] { typeof(Element), typeof(Card), typeof(AttackSource) }),
            });

        public static readonly CompatSymbol CharaDetachMinion = new(
            id: "Chara.ReleaseMinion",
            ownerType: typeof(Chara),
            candidateNames: new[] { "UnmakeMinion", "ReleaseMinion" },
            isStatic: false,
            predicate: method =>
            {
                if (method.ReturnType != typeof(void)) return false;
                return method.GetParameters().Length == 0;
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("UnmakeMinion", typeof(void), Type.EmptyTypes),
                new CompatMethodSignature("ReleaseMinion", typeof(void), Type.EmptyTypes),
            });

        public static readonly CompatSymbol QuestCreate = new(
            id: "Quest.Create",
            ownerType: typeof(Quest),
            candidateNames: new[] { "Create", "CreateQuest" },
            isStatic: true,
            predicate: method =>
            {
                if (!typeof(Quest).IsAssignableFrom(method.ReturnType)) return false;
                var p = method.GetParameters();
                return p.Length >= 1 && p[0].ParameterType == typeof(string);
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("Create", typeof(Quest), new[] { typeof(string), typeof(string), typeof(Chara), typeof(bool) }, allowDerivedReturnType: true),
                new CompatMethodSignature("Create", typeof(Quest), new[] { typeof(string), typeof(string), typeof(Chara) }, allowDerivedReturnType: true),
                new CompatMethodSignature("Create", typeof(Quest), new[] { typeof(string) }, allowDerivedReturnType: true),
                new CompatMethodSignature("CreateQuest", typeof(Quest), new[] { typeof(string), typeof(string), typeof(Chara), typeof(bool) }, allowDerivedReturnType: true),
                new CompatMethodSignature("CreateQuest", typeof(Quest), new[] { typeof(string), typeof(string), typeof(Chara) }, allowDerivedReturnType: true),
                new CompatMethodSignature("CreateQuest", typeof(Quest), new[] { typeof(string) }, allowDerivedReturnType: true),
            });

        public static readonly CompatSymbol ActPlanUpdate = new(
            id: "ActPlan._Update",
            ownerType: typeof(ActPlan),
            candidateNames: new[] { "_Update", "Update" },
            isStatic: false,
            predicate: method =>
            {
                if (method.ReturnType != typeof(void)) return false;
                var p = method.GetParameters();
                return p.Length == 1 && p[0].ParameterType == typeof(PointTarget);
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("_Update", typeof(void), new[] { typeof(PointTarget) }),
                new CompatMethodSignature("Update", typeof(void), new[] { typeof(PointTarget) }),
            });

        public static readonly CompatSymbol PointGetNearestPoint = new(
            id: "Point.GetNearestPoint",
            ownerType: typeof(Point),
            candidateNames: new[] { "GetNearestPoint" },
            isStatic: false,
            predicate: method =>
            {
                if (method.ReturnType != typeof(Point)) return false;
                var p = method.GetParameters();
                return p.Length >= 2
                    && p[0].ParameterType == typeof(bool)
                    && p[1].ParameterType == typeof(bool);
            },
            strictSignatures: new[]
            {
                new CompatMethodSignature("GetNearestPoint", typeof(Point), new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int) }),
                new CompatMethodSignature("GetNearestPoint", typeof(Point), new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) }),
            });

        public static IReadOnlyList<CompatSymbol> All { get; } = new[]
        {
            CharaSetMainElement,
            CharaDie,
            CharaDetachMinion,
            QuestCreate,
            ActPlanUpdate,
            PointGetNearestPoint,
        };
    }
}

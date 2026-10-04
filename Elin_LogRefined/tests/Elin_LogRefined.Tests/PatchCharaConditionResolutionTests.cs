using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elin_LogRefined.Tests
{
    [TestFixture]
    public class PatchCharaConditionResolutionTests
    {
        [SetUp]
        public void ResetConditionArgumentIndex()
        {
            typeof(PatchChara)
                .GetField("_idxConditionArg", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, 0);
            typeof(PatchChara)
                .GetField("_conditionScopes", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }

        [Test]
        public void ResolveCondition_returns_null_for_null_result_even_when_input_condition_exists()
        {
            var attempted = new Condition();

            var resolved = InvokeLegacyResolveCondition(null, new object[] { attempted });

            Assert.That(resolved, Is.Null);
        }

        [Test]
        public void ResolveCondition_returns_stacked_condition_when_scope_observed_OnStacked()
        {
            var owner = new Chara { uid = 42 };
            var attempted = new Condition { id = 123, owner = owner };
            var existing = new Condition { id = 123, owner = owner };

            var scope = CreateScope(owner, new object[] { attempted });
            try
            {
                PushScope(scope);
                InvokeRecordStacked(existing);

                var resolved = InvokeResolveCondition(null, new object[] { attempted }, scope);

                Assert.That(resolved, Is.SameAs(existing));
            }
            finally
            {
                PopScope(scope);
            }
        }

        [Test]
        public void ResolveCondition_ignores_stacked_evidence_from_other_owner()
        {
            var owner = new Chara { uid = 42 };
            var otherOwner = new Chara { uid = 77 };
            var attempted = new Condition { id = 123, owner = owner };
            var otherExisting = new Condition { id = 123, owner = otherOwner };

            var scope = CreateScope(owner, new object[] { attempted });
            try
            {
                PushScope(scope);
                InvokeRecordStacked(otherExisting);

                var resolved = InvokeResolveCondition(null, new object[] { attempted }, scope);

                Assert.That(resolved, Is.Null);
            }
            finally
            {
                PopScope(scope);
            }
        }

        [Test]
        public void Patch_prefix_and_OnStacked_prefix_resolve_existing_stacked_condition()
        {
            var owner = new Chara { uid = 42 };
            var attempted = new Condition { id = 123, owner = owner };
            var existing = new Condition { id = 123, owner = owner };

            var scope = InvokeAddConditionPrefix(owner, new object[] { attempted });
            try
            {
                InvokeOnStackedEvidencePrefix(existing);

                var resolved = InvokeResolveCondition(null, new object[] { attempted }, scope);

                Assert.That(resolved, Is.SameAs(existing));
            }
            finally
            {
                InvokeFinalizer(scope);
            }
        }

        [Test]
        public void Nested_scopes_record_inner_and_outer_OnStacked_evidence_independently()
        {
            var outerOwner = new Chara { uid = 42 };
            var innerOwner = new Chara { uid = 77 };
            var outerAttempted = new Condition { id = 123, owner = outerOwner };
            var innerAttempted = new Condition { id = 456, owner = innerOwner };
            var outerExisting = new Condition { id = 123, owner = outerOwner };
            var innerExisting = new Condition { id = 456, owner = innerOwner };

            var outerScope = InvokeAddConditionPrefix(outerOwner, new object[] { outerAttempted });
            try
            {
                var innerScope = InvokeAddConditionPrefix(innerOwner, new object[] { innerAttempted });
                try
                {
                    InvokeOnStackedEvidencePrefix(innerExisting);
                    Assert.That(InvokeResolveCondition(null, new object[] { innerAttempted }, innerScope), Is.SameAs(innerExisting));
                    Assert.That(InvokeResolveCondition(null, new object[] { outerAttempted }, outerScope), Is.Null);
                }
                finally
                {
                    InvokeFinalizer(innerScope);
                }

                InvokeOnStackedEvidencePrefix(outerExisting);
                Assert.That(InvokeResolveCondition(null, new object[] { outerAttempted }, outerScope), Is.SameAs(outerExisting));
            }
            finally
            {
                InvokeFinalizer(outerScope);
            }

            Assert.That(GetScopeStackCount(), Is.Zero);
        }

        [Test]
        public void Finalizer_cleanup_prevents_late_OnStacked_from_marking_exception_scope()
        {
            var owner = new Chara { uid = 42 };
            var attempted = new Condition { id = 123, owner = owner };
            var existing = new Condition { id = 123, owner = owner };

            var scope = InvokeAddConditionPrefix(owner, new object[] { attempted });
            Assert.That(GetScopeStackCount(), Is.EqualTo(1));

            InvokeFinalizer(scope);
            InvokeOnStackedEvidencePrefix(existing);

            Assert.That(GetScopeStackCount(), Is.Zero);
            Assert.That(InvokeResolveCondition(null, new object[] { attempted }, scope), Is.Null);
        }

        [Test]
        public void ResolveCondition_returns_result_condition_for_successful_refresh()
        {
            var owner = new Chara { uid = 42 };
            var attempted = new Condition { id = 123, owner = owner };

            var scope = CreateScope(owner, new object[] { attempted });
            var resolved = InvokeResolveCondition(attempted, new object[] { attempted }, scope);

            Assert.That(resolved, Is.SameAs(attempted));
        }

        [Test]
        public void ResolveCondition_treats_unsupported_true_bool_result_as_unknown()
        {
            var resolved = InvokeResolveCondition(true, Array.Empty<object>(), null);

            Assert.That(resolved, Is.Null);
        }

        private static Condition InvokeLegacyResolveCondition(object result, object[] args)
        {
            var method = typeof(PatchChara).GetMethod(
                "ResolveCondition",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(object), typeof(object[]) },
                null);

            Assert.That(method, Is.Not.Null);
            return (Condition)method.Invoke(null, new object[] { result, args });
        }

        private static object CreateScope(Chara owner, object[] args)
        {
            var method = typeof(PatchChara).GetMethod(
                "CreateConditionApplicationScope",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            return method.Invoke(null, new object[] { owner, args });
        }

        private static object InvokeAddConditionPrefix(Chara owner, object[] args)
        {
            var method = typeof(PatchChara).GetMethod(
                "Prefix",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            var parameters = new object[] { owner, args, null };
            method.Invoke(null, parameters);
            Assert.That(parameters[2], Is.Not.Null);
            return parameters[2];
        }

        private static void InvokeFinalizer(object scope)
        {
            var method = typeof(PatchChara).GetMethod(
                "Finalizer",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new[] { scope });
        }

        private static void InvokeOnStackedEvidencePrefix(Condition condition)
        {
            var method = typeof(PatchConditionOnStackedEvidence).GetMethod(
                "Prefix",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { condition });
        }

        private static int GetScopeStackCount()
        {
            var field = typeof(PatchChara).GetField(
                "_conditionScopes",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            var stack = field.GetValue(null) as System.Collections.ICollection;
            return stack?.Count ?? 0;
        }

        private static void PushScope(object scope)
        {
            var method = typeof(PatchChara).GetMethod(
                "PushConditionApplicationScope",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new[] { scope });
        }

        private static void PopScope(object scope)
        {
            var method = typeof(PatchChara).GetMethod(
                "PopConditionApplicationScope",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new[] { scope });
        }

        private static void InvokeRecordStacked(Condition condition)
        {
            var method = typeof(PatchChara).GetMethod(
                "RecordStackedCondition",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { condition });
        }

        private static Condition InvokeResolveCondition(object result, object[] args, object scope)
        {
            var method = typeof(PatchChara)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .SingleOrDefault(m => m.Name == "ResolveCondition" && m.GetParameters().Length == 3);

            Assert.That(method, Is.Not.Null);
            return (Condition)method.Invoke(null, new[] { result, args, scope });
        }
    }
}

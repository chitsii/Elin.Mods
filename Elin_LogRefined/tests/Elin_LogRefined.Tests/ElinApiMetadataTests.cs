using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Elin_LogRefined.Tests
{
    [TestFixture]
    public class ElinApiMetadataTests
    {
        [Test]
        public void Chara_AddCondition_condition_bool_remains_the_Harmony_target()
        {
            var method = typeof(Chara).GetMethod(
                "AddCondition",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Condition), typeof(bool) },
                null);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Condition)));
        }

        [Test]
        public void Condition_OnStacked_int_is_available_as_positive_stack_evidence()
        {
            var method = typeof(Condition).GetMethod(
                "OnStacked",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(int) },
                null);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(method.IsVirtual, Is.True);
        }

        [Test]
        public void AddCondition_p2_overloads_still_delegate_to_condition_bool_target_shape()
        {
            var overloads = typeof(Chara)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "AddCondition")
                .Select(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)))
                .ToArray();

            Assert.That(overloads, Does.Contain("String,Int32,Int32,Boolean"));
            Assert.That(overloads, Does.Contain("Condition,Boolean"));
        }
    }
}

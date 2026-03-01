using System;

namespace Elin_SukutsuArena.Attributes
{
    /// <summary>
    /// Marks code that depends on Elin game APIs or CWL.
    /// Used for dependency tracking and maintenance documentation.
    /// Claude Code can use these annotations to identify and fix breakages after game updates.
    /// </summary>
    /// <remarks>
    /// Dependency types:
    /// - "Inheritance": Class inherits from game class (Zone_Civilized, ZoneInstance, etc.)
    /// - "Direct": Direct API call to game classes (EClass.pc, Chara.ShowDialog, etc.)
    /// - "Patch": Harmony patch targeting game method
    /// - "Reflection": Runtime type/member access via reflection
    ///
    /// Risk levels:
    /// - "High": Likely to break on game updates, requires immediate attention
    /// - "Medium": May break, should be checked after updates
    /// - "Low": Stable API, unlikely to change
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class GameDependencyAttribute : Attribute
    {
        /// <summary>
        /// Type of dependency: "Inheritance", "Direct", "Patch", "Reflection"
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Target API, class name, method signature, or type name being depended upon.
        /// Examples: "Zone_Civilized", "EClass.pc", "Zone.Activate", "Cwl.API.Custom.CustomZone.Managed"
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Risk level: "High", "Medium", "Low"
        /// Indicates likelihood of breakage on game updates.
        /// </summary>
        public string Risk { get; }

        /// <summary>
        /// Optional notes for Claude Code to use during repair.
        /// Can include: alternative approaches, known issues, migration hints.
        /// </summary>
        public string Notes { get; }

        /// <summary>
        /// Creates a new game dependency annotation.
        /// </summary>
        /// <param name="type">Dependency type: "Inheritance", "Direct", "Patch", "Reflection"</param>
        /// <param name="target">Target API or class being depended upon</param>
        /// <param name="risk">Risk level: "High", "Medium", "Low" (default: "Medium")</param>
        /// <param name="notes">Optional notes for repair guidance</param>
        public GameDependencyAttribute(string type, string target,
                                       string risk = "Medium", string notes = null)
        {
            Type = type;
            Target = target;
            Risk = risk;
            Notes = notes;
        }
    }
}

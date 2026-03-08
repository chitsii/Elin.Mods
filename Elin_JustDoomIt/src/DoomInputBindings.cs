using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elin_JustDoomIt
{
    internal readonly struct DoomResolvedBinding
    {
        public DoomBindingKind Kind { get; }
        public KeyCode KeyCode { get; }
        public int MouseButton { get; }

        private DoomResolvedBinding(DoomBindingKind kind, KeyCode keyCode, int mouseButton)
        {
            Kind = kind;
            KeyCode = keyCode;
            MouseButton = mouseButton;
        }

        public static DoomResolvedBinding ForKey(KeyCode keyCode)
        {
            return new DoomResolvedBinding(DoomBindingKind.Key, keyCode, -1);
        }

        public static DoomResolvedBinding ForMouseButton(int mouseButton)
        {
            return new DoomResolvedBinding(DoomBindingKind.MouseButton, KeyCode.None, mouseButton);
        }

        public static DoomResolvedBinding ForWheelUp()
        {
            return new DoomResolvedBinding(DoomBindingKind.WheelUp, KeyCode.None, -1);
        }

        public static DoomResolvedBinding ForWheelDown()
        {
            return new DoomResolvedBinding(DoomBindingKind.WheelDown, KeyCode.None, -1);
        }
    }

    internal sealed class DoomBindingSet
    {
        private readonly DoomResolvedBinding[] _bindings;

        public DoomBindingSet(IReadOnlyList<DoomResolvedBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0)
            {
                _bindings = Array.Empty<DoomResolvedBinding>();
                return;
            }

            _bindings = new DoomResolvedBinding[bindings.Count];
            for (var i = 0; i < bindings.Count; i++)
            {
                _bindings[i] = bindings[i];
            }
        }

        public bool IsHeld()
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                var binding = _bindings[i];
                if (binding.Kind == DoomBindingKind.Key && Input.GetKey(binding.KeyCode))
                {
                    return true;
                }

                if (binding.Kind == DoomBindingKind.MouseButton && Input.GetMouseButton(binding.MouseButton))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsPressed()
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                var binding = _bindings[i];
                if (binding.Kind == DoomBindingKind.Key && Input.GetKeyDown(binding.KeyCode))
                {
                    return true;
                }

                if (binding.Kind == DoomBindingKind.MouseButton && Input.GetMouseButtonDown(binding.MouseButton))
                {
                    return true;
                }
            }

            return false;
        }

        public int ReadStepDelta()
        {
            var steps = 0;
            var wheel = Input.mouseScrollDelta.y;

            for (var i = 0; i < _bindings.Length; i++)
            {
                var binding = _bindings[i];
                switch (binding.Kind)
                {
                    case DoomBindingKind.Key:
                        if (Input.GetKeyDown(binding.KeyCode))
                        {
                            steps += 1;
                        }
                        break;
                    case DoomBindingKind.MouseButton:
                        if (Input.GetMouseButtonDown(binding.MouseButton))
                        {
                            steps += 1;
                        }
                        break;
                    case DoomBindingKind.WheelUp:
                        if (wheel > 0.01f)
                        {
                            steps += Mathf.CeilToInt(wheel);
                        }
                        break;
                    case DoomBindingKind.WheelDown:
                        if (wheel < -0.01f)
                        {
                            steps += Mathf.CeilToInt(-wheel);
                        }
                        break;
                }
            }

            return steps;
        }
    }

    internal sealed class DoomInputBindings
    {
        public DoomBindingSet MoveForward { get; }
        public DoomBindingSet MoveBackward { get; }
        public DoomBindingSet TurnLeft { get; }
        public DoomBindingSet TurnRight { get; }
        public DoomBindingSet StrafeLeft { get; }
        public DoomBindingSet StrafeRight { get; }
        public DoomBindingSet Fire { get; }
        public DoomBindingSet Use { get; }
        public DoomBindingSet Run { get; }
        public DoomBindingSet Weapon1 { get; }
        public DoomBindingSet Weapon2 { get; }
        public DoomBindingSet Weapon3 { get; }
        public DoomBindingSet Weapon4 { get; }
        public DoomBindingSet Weapon5 { get; }
        public DoomBindingSet Weapon6 { get; }
        public DoomBindingSet Weapon7 { get; }
        public DoomBindingSet NextWeapon { get; }
        public DoomBindingSet PreviousWeapon { get; }

        public DoomInputBindings(
            DoomBindingSet moveForward,
            DoomBindingSet moveBackward,
            DoomBindingSet turnLeft,
            DoomBindingSet turnRight,
            DoomBindingSet strafeLeft,
            DoomBindingSet strafeRight,
            DoomBindingSet fire,
            DoomBindingSet use,
            DoomBindingSet run,
            DoomBindingSet weapon1,
            DoomBindingSet weapon2,
            DoomBindingSet weapon3,
            DoomBindingSet weapon4,
            DoomBindingSet weapon5,
            DoomBindingSet weapon6,
            DoomBindingSet weapon7,
            DoomBindingSet nextWeapon,
            DoomBindingSet previousWeapon)
        {
            MoveForward = moveForward;
            MoveBackward = moveBackward;
            TurnLeft = turnLeft;
            TurnRight = turnRight;
            StrafeLeft = strafeLeft;
            StrafeRight = strafeRight;
            Fire = fire;
            Use = use;
            Run = run;
            Weapon1 = weapon1;
            Weapon2 = weapon2;
            Weapon3 = weapon3;
            Weapon4 = weapon4;
            Weapon5 = weapon5;
            Weapon6 = weapon6;
            Weapon7 = weapon7;
            NextWeapon = nextWeapon;
            PreviousWeapon = previousWeapon;
        }

        public static DoomInputBindings FromConfig(
            string moveForward,
            string moveBackward,
            string turnLeft,
            string turnRight,
            string strafeLeft,
            string strafeRight,
            string fire,
            string use,
            string run,
            string weapon1,
            string weapon2,
            string weapon3,
            string weapon4,
            string weapon5,
            string weapon6,
            string weapon7,
            string nextWeapon,
            string previousWeapon)
        {
            return new DoomInputBindings(
                Resolve(moveForward, "MoveForward", allowWheel: false),
                Resolve(moveBackward, "MoveBackward", allowWheel: false),
                Resolve(turnLeft, "TurnLeft", allowWheel: false),
                Resolve(turnRight, "TurnRight", allowWheel: false),
                Resolve(strafeLeft, "StrafeLeft", allowWheel: false),
                Resolve(strafeRight, "StrafeRight", allowWheel: false),
                Resolve(fire, "Fire", allowWheel: false),
                Resolve(use, "Use", allowWheel: false),
                Resolve(run, "Run", allowWheel: false),
                Resolve(weapon1, "Weapon1", allowWheel: false),
                Resolve(weapon2, "Weapon2", allowWheel: false),
                Resolve(weapon3, "Weapon3", allowWheel: false),
                Resolve(weapon4, "Weapon4", allowWheel: false),
                Resolve(weapon5, "Weapon5", allowWheel: false),
                Resolve(weapon6, "Weapon6", allowWheel: false),
                Resolve(weapon7, "Weapon7", allowWheel: false),
                Resolve(nextWeapon, "NextWeapon", allowWheel: true),
                Resolve(previousWeapon, "PreviousWeapon", allowWheel: true));
        }

        private static DoomBindingSet Resolve(string csv, string actionName, bool allowWheel)
        {
            var tokens = DoomBindingParser.ParseCsv(csv);
            var bindings = new List<DoomResolvedBinding>(tokens.Count);

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token.Kind)
                {
                    case DoomBindingKind.Key:
                        if (Enum.TryParse(token.KeyName, true, out KeyCode keyCode))
                        {
                            bindings.Add(DoomResolvedBinding.ForKey(keyCode));
                        }
                        else
                        {
                            DoomDiagnostics.Warn("[JustDoomIt] Ignoring invalid keybind token for " +
                                actionName + ": " + token.KeyName);
                        }
                        break;
                    case DoomBindingKind.MouseButton:
                        bindings.Add(DoomResolvedBinding.ForMouseButton(token.MouseButton));
                        break;
                    case DoomBindingKind.WheelUp:
                        if (allowWheel)
                        {
                            bindings.Add(DoomResolvedBinding.ForWheelUp());
                        }
                        else
                        {
                            DoomDiagnostics.Warn("[JustDoomIt] Ignoring wheel binding for non-cycle action " +
                                actionName + ": WheelUp");
                        }
                        break;
                    case DoomBindingKind.WheelDown:
                        if (allowWheel)
                        {
                            bindings.Add(DoomResolvedBinding.ForWheelDown());
                        }
                        else
                        {
                            DoomDiagnostics.Warn("[JustDoomIt] Ignoring wheel binding for non-cycle action " +
                                actionName + ": WheelDown");
                        }
                        break;
                }
            }

            return new DoomBindingSet(bindings);
        }
    }
}

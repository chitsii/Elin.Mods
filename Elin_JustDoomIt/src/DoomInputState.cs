using UnityEngine;

namespace Elin_JustDoomIt
{
    public struct DoomInputState
    {
        public bool MoveForward;
        public bool MoveBackward;
        public bool TurnLeft;
        public bool TurnRight;
        public bool StrafeLeft;
        public bool StrafeRight;
        public bool Fire;
        public bool Use;
        public bool Run;
        public bool Weapon1;
        public bool Weapon2;
        public bool Weapon3;
        public bool Weapon4;
        public bool Weapon5;
        public bool Weapon6;
        public bool Weapon7;
        public int WeaponCycleSteps;
        public float MouseDeltaX;

        public static DoomInputState ReadFromUnity()
        {
            var bindings = ModConfig.InputBindings ?? DoomInputBindings.FromConfig(
                DoomInputBindingDefaults.MoveForward,
                DoomInputBindingDefaults.MoveBackward,
                DoomInputBindingDefaults.TurnLeft,
                DoomInputBindingDefaults.TurnRight,
                DoomInputBindingDefaults.StrafeLeft,
                DoomInputBindingDefaults.StrafeRight,
                DoomInputBindingDefaults.Fire,
                DoomInputBindingDefaults.Use,
                DoomInputBindingDefaults.Run,
                DoomInputBindingDefaults.Weapon1,
                DoomInputBindingDefaults.Weapon2,
                DoomInputBindingDefaults.Weapon3,
                DoomInputBindingDefaults.Weapon4,
                DoomInputBindingDefaults.Weapon5,
                DoomInputBindingDefaults.Weapon6,
                DoomInputBindingDefaults.Weapon7,
                DoomInputBindingDefaults.NextWeapon,
                DoomInputBindingDefaults.PreviousWeapon);

            return new DoomInputState
            {
                MoveForward = bindings.MoveForward.IsHeld(),
                MoveBackward = bindings.MoveBackward.IsHeld(),
                TurnLeft = bindings.TurnLeft.IsHeld(),
                TurnRight = bindings.TurnRight.IsHeld(),
                StrafeLeft = bindings.StrafeLeft.IsHeld(),
                StrafeRight = bindings.StrafeRight.IsHeld(),
                Fire = bindings.Fire.IsHeld(),
                Use = bindings.Use.IsHeld(),
                Run = bindings.Run.IsHeld(),
                Weapon1 = bindings.Weapon1.IsPressed(),
                Weapon2 = bindings.Weapon2.IsPressed(),
                Weapon3 = bindings.Weapon3.IsPressed(),
                Weapon4 = bindings.Weapon4.IsPressed(),
                Weapon5 = bindings.Weapon5.IsPressed(),
                Weapon6 = bindings.Weapon6.IsPressed(),
                Weapon7 = bindings.Weapon7.IsPressed(),
                WeaponCycleSteps = bindings.NextWeapon.ReadStepDelta() - bindings.PreviousWeapon.ReadStepDelta(),
                MouseDeltaX = Input.GetAxis("Mouse X")
            };
        }
    }
}


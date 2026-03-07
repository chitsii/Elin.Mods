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
        public bool OpenMenu;
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
            return new DoomInputState
            {
                MoveForward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                MoveBackward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                TurnLeft = Input.GetKey(KeyCode.LeftArrow),
                TurnRight = Input.GetKey(KeyCode.RightArrow),
                StrafeLeft = Input.GetKey(KeyCode.A),
                StrafeRight = Input.GetKey(KeyCode.D),
                Fire = Input.GetMouseButton(0),
                Use = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E),
                Run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                OpenMenu = Input.GetKeyDown(KeyCode.Tab),
                Weapon1 = Input.GetKeyDown(KeyCode.Alpha1),
                Weapon2 = Input.GetKeyDown(KeyCode.Alpha2),
                Weapon3 = Input.GetKeyDown(KeyCode.Alpha3),
                Weapon4 = Input.GetKeyDown(KeyCode.Alpha4),
                Weapon5 = Input.GetKeyDown(KeyCode.Alpha5),
                Weapon6 = Input.GetKeyDown(KeyCode.Alpha6),
                Weapon7 = Input.GetKeyDown(KeyCode.Alpha7),
                WeaponCycleSteps = ReadWheelSteps(),
                MouseDeltaX = Input.GetAxis("Mouse X")
            };
        }

        private static int ReadWheelSteps()
        {
            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
            {
                return 0;
            }

            return wheel > 0f ? Mathf.CeilToInt(wheel) : Mathf.FloorToInt(wheel);
        }
    }
}


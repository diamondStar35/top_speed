using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal struct JoystickStateSnapshot
    {
        public int X;
        public int Y;
        public int Z;
        public int Rx;
        public int Ry;
        public int Rz;
        public int Slider1;
        public int Slider2;
        public bool B1;
        public bool B2;
        public bool B3;
        public bool B4;
        public bool B5;
        public bool B6;
        public bool B7;
        public bool B8;
        public bool B9;
        public bool B10;
        public bool B11;
        public bool B12;
        public bool B13;
        public bool B14;
        public bool B15;
        public bool B16;
        public bool Pov1;
        public bool Pov2;
        public bool Pov3;
        public bool Pov4;
        public bool Pov5;
        public bool Pov6;
        public bool Pov7;
        public bool Pov8;

        public static JoystickStateSnapshot From(SharpDX.DirectInput.JoystickState state)
        {
            var snapshot = new JoystickStateSnapshot
            {
                X = state.X,
                Y = state.Y,
                Z = state.Z,
                Rx = state.RotationX,
                Ry = state.RotationY,
                Rz = state.RotationZ,
                Slider1 = state.Sliders.Length > 0 ? state.Sliders[0] : 0,
                Slider2 = state.Sliders.Length > 1 ? state.Sliders[1] : 0
            };

            if (state.Buttons.Length > 0) snapshot.B1 = state.Buttons[0];
            if (state.Buttons.Length > 1) snapshot.B2 = state.Buttons[1];
            if (state.Buttons.Length > 2) snapshot.B3 = state.Buttons[2];
            if (state.Buttons.Length > 3) snapshot.B4 = state.Buttons[3];
            if (state.Buttons.Length > 4) snapshot.B5 = state.Buttons[4];
            if (state.Buttons.Length > 5) snapshot.B6 = state.Buttons[5];
            if (state.Buttons.Length > 6) snapshot.B7 = state.Buttons[6];
            if (state.Buttons.Length > 7) snapshot.B8 = state.Buttons[7];
            if (state.Buttons.Length > 8) snapshot.B9 = state.Buttons[8];
            if (state.Buttons.Length > 9) snapshot.B10 = state.Buttons[9];
            if (state.Buttons.Length > 10) snapshot.B11 = state.Buttons[10];
            if (state.Buttons.Length > 11) snapshot.B12 = state.Buttons[11];
            if (state.Buttons.Length > 12) snapshot.B13 = state.Buttons[12];
            if (state.Buttons.Length > 13) snapshot.B14 = state.Buttons[13];
            if (state.Buttons.Length > 14) snapshot.B15 = state.Buttons[14];
            if (state.Buttons.Length > 15) snapshot.B16 = state.Buttons[15];

            if (state.PointOfViewControllers.Length > 0)
                SetPov(state.PointOfViewControllers[0], ref snapshot.Pov1, ref snapshot.Pov2, ref snapshot.Pov3, ref snapshot.Pov4);
            if (state.PointOfViewControllers.Length > 1)
                SetPov(state.PointOfViewControllers[1], ref snapshot.Pov5, ref snapshot.Pov6, ref snapshot.Pov7, ref snapshot.Pov8);

            return snapshot;
        }

        private static void SetPov(int value, ref bool up, ref bool right, ref bool down, ref bool left)
        {
            if (value < 0)
            {
                up = right = down = left = false;
                return;
            }

            up = value > 31500 || value < 4500;
            right = value > 4500 && value < 13500;
            down = value > 13500 && value < 22500;
            left = value > 22500 && value < 31500;
        }
    }
}

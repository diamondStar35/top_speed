using System.Drawing;
using System.Windows.Forms;

namespace TopSpeed.Windowing
{
    internal sealed class GameWindow : Form
    {
        public GameWindow()
        {
            Text = "Top Speed";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(640, 360);
            KeyPreview = true;
        }
    }
}

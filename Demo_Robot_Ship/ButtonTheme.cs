using System.Drawing;

namespace Demo_Robot_Ship
{
    internal class ButtonTheme
    {
        public Color Normal { get; private set; }
        public Color Hover { get; private set; }

        public ButtonTheme(Color normal, Color hover)
        {
            Normal = normal;
            Hover = hover;
        }
    }
}

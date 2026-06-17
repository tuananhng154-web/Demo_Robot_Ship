using System.Drawing;

namespace Demo_Robot_Ship
{
    internal class BuildingView
    {
        public string Name { get; private set; }
        public Rectangle GridRect { get; private set; }
        public Point Door { get; private set; }

        public BuildingView(string name, Rectangle gridRect, Point door)
        {
            Name = name;
            GridRect = gridRect;
            Door = door;
        }
    }
}

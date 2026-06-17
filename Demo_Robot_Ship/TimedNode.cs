namespace Demo_Robot_Ship
{
    internal class TimedNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Time { get; set; }
        public int G { get; set; }
        public int H { get; set; }
        public int F { get { return G + H; } }
        public TimedNode Parent { get; set; }

        public TimedNode(int x, int y, int time)
        {
            X = x;
            Y = y;
            Time = time;
        }
    }
}

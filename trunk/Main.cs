using System;
using SFML.Window;
using SFML.Graphics;

namespace SpiritPurger
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the game and run it.
            SpiritPurger.Engine engine = new SpiritPurger.Engine();
            engine.Run();
        }
    }
}

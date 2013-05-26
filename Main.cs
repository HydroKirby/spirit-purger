using System;
using SFML.Window;
using SFML.Graphics;

namespace SpiritPurger
{
    class Program
    {
        static void OnClose(object sender, EventArgs e)
        {
            // Close the window when OnClose event is received.
            RenderWindow window = (RenderWindow) sender;
            window.Close();
        }

        static void Main(string[] args)
        {
            // Create the game and run it.
            SpiritPurger.Engine engine = new SpiritPurger.Engine();
            // Create the main window.
            RenderWindow app = new RenderWindow(new VideoMode(640, 480), "Spirit Purger");
            app.Closed += new EventHandler(OnClose);
            engine.Run(app);
        }
    }
}

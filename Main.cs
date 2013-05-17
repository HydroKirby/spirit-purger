using System;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
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
            TestSFMLDotNet.Engine engine = new TestSFMLDotNet.Engine();
            // Create the main window.
            RenderWindow app = new RenderWindow(new VideoMode(640, 480), "SFML window");
            app.Closed += new EventHandler(OnClose);
            engine.Run(app);
        }
    }
}

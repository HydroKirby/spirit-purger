using System;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace Example
{
    class Program
    {
        static void OnClose(object sender, EventArgs e)
        {
            // Close the window when OnClose event is received.
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }

        static void Main(string[] args)
        {
            // Create the main window.
            RenderWindow app = new RenderWindow(new VideoMode(800, 600), "SFML window");
            app.Closed += new EventHandler(OnClose);
            // Create the game and run it.
            TestSFMLDotNet.Engine engine = new TestSFMLDotNet.Engine();
            engine.Run(app);
        }
    }
}

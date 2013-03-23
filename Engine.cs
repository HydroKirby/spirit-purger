using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
    class Engine
    {
        // High-level stuff.
        protected delegate void PaintHandler(object sender);
        protected TestSFMLDotNet.Engine.PaintHandler paintHandler = null;

        // TODO: Move to Renderer; possibly eradicate
        protected void OnPaint(object sender)
        {
            paintHandler(sender);
        }

        // TODO: Move to Renderer
        /// <summary>
        /// Loads an image and gives a replacement on failure.
        /// </summary>
        /// <param name="filename">Where the image is.</param>
        /// <returns>The loaded image on success or a 1x1 Image otherwise.</returns>
        private Image LoadImage(String filename)
        {
            Image bitmap;
            try
            {
                bitmap = new Image("res/" + filename);
            }
            catch (ArgumentException)
            {
                bitmap = new Image(1, 1);
            }
            return bitmap;
        }

        public void Run(RenderWindow app)
        {
            // Load a sprite to display
            //            Image image = new Image("cute_image.jpg");
            //            Sprite sprite = new Sprite(image);

            // Create a graphical string to display
            Font arial = new Font("arial.ttf");
            Text text = new Text("Hello SFML.Net", arial);

            // Load a music to play
            //            Music music = new Music("nice_music.ogg");
            //            music.Play();

            // Start the game loop
            while (app.IsOpen())
            {
                // Process events
                app.DispatchEvents();

                // Clear screen
                app.Clear();

                // Draw the sprite
                //                app.Draw(sprite);

                // Draw the string
                app.Draw(text);

                // Update the window
                app.Display();
            }
        }
    }
}

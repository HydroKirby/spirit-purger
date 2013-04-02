using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
    /// <summary>
    /// Holds all images in the game.
    /// It should provide sprites for objects.
    /// </summary>
    public class Renderer
    {
        // This variable holds the images shared between all bullets. It's
        //   organized by [color_code][size_index].
        public Texture[][] bulletImages;
        // This goes to the spark that flies out of the player when a
        // bullet passes by the player's hitbox.
        public Texture grazeSparkImage;
        // This goes to the spark the flies out when an enemy is shot.
        public Texture bullseyeSparkImage;
        // hitCircleImage is displayed when the player slows down; it's an identifier.
        public Texture playerImage, hitCircleImage;
        public Texture bg;
        public Texture bossImage;
        public Texture playerBulletImage;

        // Fonts and images generated solely to display text.
        protected Font menuFont;

        public Renderer()
        {
            menuFont = new Font("arial.ttf");
            MakeBulletImages();
        }

        public Font MenuFont
        {
            get { return menuFont; }
        }

        /// <summary>
        /// Loads an image and gives a replacement on failure.
        /// </summary>
        /// <param name="filename">Where the image is.</param>
        /// <returns>The loaded image on success or a 1x1 Texture otherwise.</returns>
        public Texture LoadImage(String filename)
        {
            Texture img;
            try
            {
                img = new Texture("res/" + filename);
            }
            catch (ArgumentException)
            {
                img = new Texture(1, 1);
            }
            return img;
        }

        /// <summary>
        /// Makes all bullet images for the first time.
        /// All images are put into the bulletImages array.
        /// </summary>
        public void MakeBulletImages()
        {
            bulletImages = new Texture[Bullet.RADII.Length][];
            for (int atSize = 0; atSize < Bullet.RADII.Length; atSize++)
            {
                bulletImages[atSize] =
                    new Texture[(int)Bullet.BulletColors.EndColors];
                for (int color = 0; color < bulletImages[atSize].Length; color++)
                {
                    int radius = Bullet.RADII[atSize];
                    bulletImages[atSize][color] = LoadImage("b_" +
                        (radius + radius).ToString() + "x" + (radius + radius).ToString() +
                        Bullet.GetColorByName(color) + ".png");
                }
            }
        }

        public Sprite GetSprite(Texture img)
        {
            return new Sprite(img);
        }
    }

    public class MenuRenderer : Renderer
    {
		protected Color commonTextColor;
        protected Text cursorText;
		protected Text startText;
		protected Text godModeText;
		protected Text funBombText;
		protected Text repulsiveText;
		protected Text exitText;

		/// <summary>
		/// Makes a MenuRenderer and optionally sets the intial selected menu item.
		/// </summary>
		/// <param name="selection">Which menu item is currently focused.</param>
        public MenuRenderer(MainMenu selection=0)
        {
			commonTextColor = Color.Blue;

            cursorText = new Text(">", menuFont, 12);
            startText = new Text("Start", menuFont, 12);
			exitText = new Text("Exit", menuFont, 12);

			// These text images set their own properties when you call their methods.
			SetOptGodMode(false);
			SetOptFunBomb(false);
			SetOptRepulsive(false);

			// Set the remaining menu strings' positions.
			// Note: The app window is 290x290.
			SetSelection(selection);
			startText.Position = new Vector2f(145, 130 + (float)MainMenu.Play * 15.0F);
			exitText.Position = new Vector2f(145, 130 + (float)MainMenu.Exit * 15.0F);
			
			// Set the color of the remaining text images.
			startText.Color = exitText.Color = commonTextColor;
        }

		/// <summary>
		/// Tells the renderer which menu item is focused.
		/// </summary>
		/// <param name="selection">The selected menu item.</param>
		public void SetSelection(MainMenu selection)
		{
			cursorText.Position = new Vector2f(136, 130 + 15 * (int)selection);
			cursorText.Color = commonTextColor;
		}

		public void SetOptGodMode(bool isOn)
		{
			godModeText = new Text("God Mode" + (isOn ? " *" : ""), menuFont, 12);
			godModeText.Position = new Vector2f(145, 130 + (float)MainMenu.GodMode * 15.0F);
			godModeText.Color = commonTextColor;
		}

		public void SetOptFunBomb(bool isOn)
		{
			funBombText = new Text("Fun Bomb" + (isOn ? " *" : ""), menuFont, 12);
			funBombText.Position = new Vector2f(145, 130 + (float)MainMenu.FunBomb * 15.0F);
			funBombText.Color = commonTextColor;
		}

		public void SetOptRepulsive(bool isOn)
		{
			repulsiveText = new Text("Repulsive" + (isOn ? " *" : ""), menuFont, 12);
			repulsiveText.Position = new Vector2f(145, 130 + (float)MainMenu.Repulsive * 15.0F);
			repulsiveText.Color = commonTextColor;
		}

		public void Paint(object sender)
		{
			RenderWindow app = (RenderWindow)sender;
			app.Draw(cursorText);
			app.Draw(startText);
			app.Draw(godModeText);
			app.Draw(funBombText);
			app.Draw(repulsiveText);
			app.Draw(exitText);
		}
    }

	public class GameRenderer : Renderer
	{
		// Text/font variables.
		protected Color commonTextColor;
		protected Text labelBombs;
        protected Text labelLives;
        protected Text labelScore;
		protected Vector2f labelScorePos;
        protected Text labelPaused;
        protected Text labelPausedToEnd;
        protected Text labelPausedToPlay;
		protected bool isPaused;

		public GameRenderer()
		{
			commonTextColor = Color.Black;

			// These functions generate their own texts.
			SetScore(0);

			labelBombs = new Text("☆☆☆", menuFont, 12);
			labelLives = new Text("◎◎", menuFont, 12);
			labelPaused = new Text("Paused", menuFont, 12);
			labelPausedToPlay = new Text("Press Escape to Play", menuFont, 12);
			labelPausedToEnd = new Text("Tap Bomb to End", menuFont, 12);

			labelBombs.Position = new Vector2f(229, 236);
			labelScorePos = new Vector2f(113, 9);
			labelLives.Position = new Vector2f(229, 217);
			labelPaused.Position = new Vector2f(92, 98);
			labelPausedToPlay.Position = new Vector2f(79, 121);
			labelPausedToEnd.Position = new Vector2f(92, 144);

			labelBombs.Color = labelLives.Color = labelPaused.Color =
				labelPausedToEnd.Color = labelPausedToPlay.Color = commonTextColor;
		}

		public bool IsPaused
		{
			get { return isPaused; }
			set { isPaused = value; }
		}

		public void SetScore(int val)
		{
			labelScore = new Text(val.ToString(), menuFont, 12);
			labelScore.Position = labelScorePos;
			labelScore.Color = commonTextColor;
		}

		public void Paint(object sender)
		{
			RenderWindow app = (RenderWindow)sender;
			app.Draw(labelScore);
			app.Draw(labelBombs);
			app.Draw(labelLives);
			app.Draw(labelPaused);
			app.Draw(labelPausedToPlay);
			app.Draw(labelPausedToEnd);
		}
	}
}

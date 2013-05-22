using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
	public class CenterSprite : Sprite
	{
		public CenterSprite(Texture img)
		{
			Texture = img;
			Origin = new Vector2f(Texture.Size.X * 0.5F,
				Texture.Size.Y * 0.5F);
		}

		public void setPosition(Vector2f v)
		{
			Position = new Vector2f(v.X, v.Y);
		}
	}

    /// <summary>
    /// Holds all images in the game.
    /// It should provide sprites for objects.
    /// </summary>
    public class Renderer
    {
		// Fonts and images generated solely to display text.
        protected Font menuFont;
        // The size of the whole application window when not scaled.
        public const uint APP_BASE_WIDTH = 640;
        public const uint APP_BASE_HEIGHT = 480;
		// The size of the gameplay field.
		public const uint FIELD_TOP = 50;
		public const uint FIELD_LEFT = 50;
		public const uint FIELD_WIDTH = 300;
		public const uint FIELD_HEIGHT = 380;
		// Easy access members for other areas of the program.
		private static Vector2u appBaseSize;
		private static Vector2f fieldUpperLeft;
		private static Vector2f fieldSize;

        public Renderer()
        {
            menuFont = new Font("arial.ttf");
			appBaseSize = new Vector2u(APP_BASE_WIDTH, APP_BASE_HEIGHT);
			fieldUpperLeft = new Vector2f(FIELD_LEFT, FIELD_TOP);
			fieldSize = new Vector2f(FIELD_WIDTH, FIELD_HEIGHT);
        }

        public Font MenuFont
        {
            get { return menuFont; }
        }

		public static Vector2u AppBaseSize
		{
			get { return appBaseSize; }
		}

		public static Vector2f FieldUpperLeft
		{
			get { return fieldUpperLeft; }
		}

		public static Vector2f FieldSize
		{
			get { return fieldSize; }
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

        public Sprite GetSprite(Texture img)
        {
            return new Sprite(img);
        }

		public CenterSprite GetCenterSprite(Texture img)
		{
			return new CenterSprite(img);
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
		protected Text scaleText;
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
			SetOptScale(1.0);

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

		public void SetOptScale(double scale)
		{
			scaleText = new Text("Scale: " + scale.ToString(), menuFont, 12);
			scaleText.Position = new Vector2f(145, 130 + (float)MainMenu.Scale * 15.0F);
			scaleText.Color = commonTextColor;
		}

		public void Paint(object sender)
		{
			RenderWindow app = (RenderWindow)sender;
			app.Draw(cursorText);
			app.Draw(startText);
			app.Draw(godModeText);
			app.Draw(funBombText);
			app.Draw(repulsiveText);
			app.Draw(scaleText);
			app.Draw(exitText);
		}
    }

	public class GameRenderer : Renderer
	{
        // Refers to when a boss pattern has completed. It's the time to wait
        // until initiating the next pattern.
        public const int PATTERN_TRANSITION_PAUSE = 80;

		// Text/font variables.
		protected Color commonTextColor;
		protected Text labelBombs;
		protected Vector2f labelBombsPos;
        protected Text labelLives;
		protected Vector2f labelLivesPos;
        protected Text labelScore;
		protected Vector2f labelScorePos;
        protected Text labelPaused;
        protected Text labelPausedToEnd;
        protected Text labelPausedToPlay;
		protected Text labelBullets;
		protected Vector2f labelBulletsPos;
        protected Text labelBombCombo;
        protected Vector2f labelBombComboPos;
        protected Text labelBossHealth;
        protected Vector2f labelBossHealthPos;
        // How long a boss has before terminating a pattern on its own.
        protected Text labelPatternTime;
        protected Vector2f labelPatternTimePos;
        // The result of a finishing a boss pattern.
        protected Text labelPatternResult;
        protected Vector2f labelPatternResultPos;
        protected Text labelGameOver;
		
        protected bool isPaused;
        protected bool isInBossPattern;
        protected bool isBombComboShown;
        protected bool isGameOver;
        protected int _bombComboTimeCountdown;
        protected int timeLeftToShowPatternResult;

		// Game Textures.
		// This variable holds the images shared between all bullets. It's
		//   organized by [color_code][size_index].
		public Texture[][] bulletImages;
		// This goes to the spark that flies out of the player when a
		// bullet passes by the player's hitbox.
		public Texture grazeSparkImage;
		// This goes to the spark the flies out when an enemy is shot.
		public Texture bullseyeSparkImage;
		public Texture bombImage;
		// hitCircleImage is displayed when the player slows down; it's an identifier.
		public Texture playerImage, hitCircleImage;
		public Texture bg;
		// The area drawn outside of the game field.
		public Texture border;
		public Texture bossImage;
		public Texture playerBulletImage;
        
		// Game Sprites. Let the objects have (not own) the sprites so that
		// the objects can request drawing, swapping, and alterations of sprites.
		public Sprite bgSprite;
		public Sprite borderSprite;
		public CenterSprite playerSprite, hitCircleSprite;
        public CenterSprite bossSprite;

		public GameRenderer()
		{
			commonTextColor = Color.Black;

			// Create images.
			MakeBulletImages();
			bg = LoadImage("bg.png");
			bgSprite = GetSprite(bg);
			bgSprite.Origin = new Vector2f(bg.Size.X / 2, bg.Size.Y / 2);
			bgSprite.Position = bgSprite.Origin - FieldUpperLeft;
			border = LoadImage("border.png");
			borderSprite = GetSprite(border);
			playerImage = LoadImage("p_fly.png");
			playerSprite = GetCenterSprite(playerImage);
			// TODO: Deprecate need for player.SetImage(playerImage);
			bossImage = LoadImage("boss_fly.png");
            bossSprite = GetCenterSprite(bossImage);
			playerBulletImage = LoadImage("b_player.png");
			hitCircleImage = LoadImage("hitbox.png");
            hitCircleSprite = GetCenterSprite(hitCircleImage);
			grazeSparkImage = LoadImage("spark_graze.png");
			bullseyeSparkImage = LoadImage("spark_nailed_foe.png");
			bombImage = LoadImage("bomb.png");

            // Set the positions for constantly regenerating labels.
			float rightmost = FIELD_LEFT + FIELD_WIDTH;
            labelScorePos = new Vector2f(rightmost + 50, FIELD_TOP + 10);
            labelLivesPos = new Vector2f(rightmost + 50, FIELD_TOP + 20);
			labelBombsPos = new Vector2f(rightmost + 50, FIELD_TOP + 30);
            labelBulletsPos = new Vector2f(rightmost + 50, FIELD_TOP + 40);
            labelBombComboPos = new Vector2f(FIELD_LEFT + 15F, FIELD_TOP + 35F);
            labelBossHealthPos = new Vector2f(FIELD_LEFT + 30F, FIELD_TOP + 5F);
            labelPatternTimePos = new Vector2f(FIELD_LEFT + 200, FIELD_TOP + 5F);
			labelPatternResultPos = new Vector2f(FIELD_LEFT + FIELD_WIDTH / 6, FIELD_TOP + FIELD_HEIGHT / 3);

			// Create the labels that are constantly regenerated.
			SetScore(0);
			SetBullets(0);
            SetBombs(0);
            SetBombCombo(0, 0);
            SetBossHealth(0);
            SetPatternTime(0);
            SetPatternResult(false, 0);
            timeLeftToShowPatternResult = 0;

            // Create the labels that are only created once.
			labelPaused = new Text("Paused", menuFont, 12);
			labelPausedToPlay = new Text("Press Escape to Play", menuFont, 12);
			labelPausedToEnd = new Text("Tap Bomb to End", menuFont, 12);
            labelGameOver = new Text("Game Over... Press Shoot", menuFont, 16);

            // Set the positions for labels that are made only one time.
			labelPaused.Position = new Vector2f(92, 98);
			labelPausedToPlay.Position = new Vector2f(79, 121);
			labelPausedToEnd.Position = new Vector2f(92, 144);
            labelGameOver.Position = new Vector2f(70, 70);

			labelPaused.Color = labelGameOver.Color =
				labelPausedToEnd.Color = labelPausedToPlay.Color = commonTextColor;
		}

        public int BombComboTimeCountdown
        {
            get { return _bombComboTimeCountdown; }
            set
            {
                _bombComboTimeCountdown = value;
                if (value <= 0)
                    isBombComboShown = false;
            }
        }

		public bool IsPaused
		{
			get { return isPaused; }
			set { isPaused = value; }
		}

        public bool IsGameOver
        {
            get { return isGameOver; }
            set { isGameOver = value; }
        }

		public void SetScore(long val)
		{
			labelScore = new Text("Score: " + val.ToString(), menuFont, 12);
			labelScore.Position = labelScorePos;
			labelScore.Color = commonTextColor;
		}

		public void SetLives(int val)
		{
            // The last life is life 0. When lives = -1, it's game over.
            if (val < 0)
                val = 0;
            else
                isGameOver = false;
			String livesString = "Lives: " + val;
			labelLives = new Text(livesString, menuFont, 12);
			labelLives.Position = labelLivesPos;
			labelLives.Color = commonTextColor;
		}

		public void SetBombs(int val)
		{
			String bombsString = "Bombs: " + val;
			labelBombs = new Text(bombsString, menuFont, 12);
			labelBombs.Position = labelBombsPos;
			labelBombs.Color = commonTextColor;
		}

        public void SetBombCombo(int combo, int score)
        {
            isBombComboShown = combo > 0;
            if (isBombComboShown)
            {
                labelBombCombo = new Text(
                    combo.ToString() + " Bomb Combo! Score + " + score.ToString(),
                    menuFont, 12);
                labelBombCombo.Position = labelBombComboPos;
                labelBombCombo.Color = commonTextColor;
            }
        }

		public void SetBullets(int val)
		{
			labelBullets = new Text("Bullets: " + val.ToString(), menuFont, 12);
            labelBullets.Position = labelBulletsPos;
            labelBullets.Color = commonTextColor;
		}

        public void SetBossHealth(int val)
        {
            labelBossHealth = new Text("Health: " + val.ToString(), menuFont, 16);
            labelBossHealth.Color = commonTextColor;
            labelBossHealth.Position = labelBossHealthPos;
        }

        public void SetPatternTime(int val)
        {
            isInBossPattern = val > 0;
            if (isInBossPattern)
            {
                labelPatternTime = new Text("Time: " + val.ToString(), menuFont, 12);
                labelPatternTime.Position = labelPatternTimePos;
                labelPatternTime.Color = commonTextColor;
            }
        }

        public void SetPatternResult(bool success, long score)
        {
            // Reset the amount of time to show the pattern result.
            timeLeftToShowPatternResult = PATTERN_TRANSITION_PAUSE;
            labelPatternResult = new Text((success ? "Pattern Success! Score + " :
                "Survival Failure... Score + ") + score.ToString(), menuFont, 16);
            labelPatternResult.Color = commonTextColor;
            labelPatternResult.Position = labelPatternResultPos;
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

        public void Update(double dt)
        {
            timeLeftToShowPatternResult--;
            if (timeLeftToShowPatternResult < 0)
                timeLeftToShowPatternResult = 0;
			bgSprite.Rotation += 1;
			if (bgSprite.Rotation >= 360)
				bgSprite.Rotation -= 360;
        }

		public void Paint(object sender)
		{
			RenderWindow app = (RenderWindow)sender;
            if (isGameOver)
                app.Draw(labelGameOver);
			app.Draw(borderSprite);
			app.Draw(labelScore);
			app.Draw(labelBombs);
			app.Draw(labelLives);

			if (isBombComboShown)
				app.Draw(labelBombCombo);

			if (isInBossPattern)
			{
				app.Draw(labelPatternTime);
				app.Draw(labelBossHealth);
			}

			if (timeLeftToShowPatternResult > 0)
				app.Draw(labelPatternResult);

			app.Draw(labelBullets);

			if (isPaused)
			{
				app.Draw(labelPaused);
				app.Draw(labelPausedToPlay);
				app.Draw(labelPausedToEnd);
			}
		}
	}
}

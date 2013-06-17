using System;
using System.Collections;
using System.Collections.Generic;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace SpiritPurger
{
	/// <summary>
	/// Holds all images in the game.
	/// It should provide sprites for objects.
	/// </summary>
	public class Renderer
	{
		// Fonts and images generated solely to display text.
		protected static Font menuFont = null;
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
			if (menuFont == null)
				menuFont = new Font(@"res\ttf-bitstream-vera-1.10\Vera.ttf");
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
		public MenuRenderer(MainMenu selection = 0)
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
		protected Dictionary<string, Texture> textures;
		private readonly string[] PNG_FILENAMES =
		{
			"bg",
			"border",
		};

		// Game Sprites. Let the objects have (not own) the sprites so that
		// the objects can request drawing, swapping, and alterations of sprites.
		public Sprite bgSprite;
		public Sprite borderSprite;
		public Healthbar bossHealthbar;

		// Animation variables.
		public double bgRotSpeed = 1.0;
		public int playerAnimSpeed = 5;
		public int bossAnimSpeed = 5;

		public GameRenderer(ImageManager imageManager)
		{
			commonTextColor = Color.Black;

			// Create images.
			textures = new Dictionary<string, Texture>(StringComparer.Ordinal);
			foreach (string filename in PNG_FILENAMES)
			{
				imageManager.LoadPNG(filename);
				textures.Add(filename, imageManager.GetImage(filename));
			}

			// Instantiate various sprites and other renderables.
			bgSprite = imageManager.GetSprite("bg");
			bgSprite.Origin = new Vector2f(bgSprite.TextureRect.Width / 2,
				bgSprite.TextureRect.Height / 2);
			bgSprite.Position = bgSprite.Origin - FieldUpperLeft;
			borderSprite = imageManager.GetSprite("border");
			bossHealthbar = new Healthbar(imageManager);

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

		public Sprite GetSprite(string key)
		{
			return new Sprite(textures[key]);
		}

		public CenterSprite GetCenterSprite(string key)
		{
			return new CenterSprite(textures[key]);
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
			bossHealthbar.CurrentHealth = val;
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

		public void Update(double dt)
		{
			timeLeftToShowPatternResult--;
			if (timeLeftToShowPatternResult < 0)
				timeLeftToShowPatternResult = 0;
			bgSprite.Rotation += (float)bgRotSpeed;
			if (bgSprite.Rotation >= 360)
				bgSprite.Rotation -= 360;
		}

		public void UpdatePlayer(ref Player p)
		{
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
				//app.Draw(labelBossHealth);
				bossHealthbar.Draw(app);
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

	public class Healthbar
	{
		protected float _maxWidth;
		protected int _maxHealth;
		protected int _currHealth;
		protected RectangleShape rectShape;
		protected Sprite healthbarBorder;

		public float MaxWidth
		{
			set { _maxWidth = value; }
			get { return _maxWidth; }
		}

		public Vector2f Size
		{
			set
			{
				rectShape.Size = new Vector2f(value.X, value.Y);
				if (value.X > MaxWidth)
					MaxWidth = value.X;
			}
			get { return rectShape.Size; }
		}

		public Vector2f Position
		{
			set
			{
				rectShape.Position = new Vector2f(value.X, value.Y);
				healthbarBorder.Position = new Vector2f(
					(float)((rectShape.Position.X + MaxWidth * 0.5) - healthbarBorder.Texture.Size.X * 0.5),
					(float)((rectShape.Position.Y + rectShape.Size.Y * 0.5) - healthbarBorder.Texture.Size.Y * 0.5));
			}
			get { return rectShape.Position; }
		}

		public int MaxHealth
		{
			set
			{
				_maxHealth = value;
				CurrentHealth = value;
			}
			get { return _maxHealth; }
		}

		public int CurrentHealth
		{
			set
			{
				if (_currHealth != value)
				{
					_currHealth = value;
					double percent = (double)CurrentHealth / MaxHealth;
					Size = new Vector2f((float)(MaxWidth * percent), Size.Y);
				}
			}
			get { return _currHealth; }
		}

		public Healthbar(ImageManager imageManager)
		{
			_maxHealth = 0;
			_currHealth = 0;
			rectShape = new RectangleShape();
			rectShape.FillColor = Color.Red;
			imageManager.LoadPNG(ImageManager.HEALTHBAR_BORDER);
			healthbarBorder = imageManager.GetSprite(ImageManager.HEALTHBAR_BORDER);
		}

		public void Draw(RenderWindow app)
		{
			app.Draw(rectShape);
			app.Draw(healthbarBorder);
		}
	}
}

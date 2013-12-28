using System;
using System.Collections;
using System.Collections.Generic;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;
using MENUITEM = SpiritPurger.MenuManager.MENUITEM;
using SUBMENU = SpiritPurger.MenuManager.SUBMENU;
using REACTION = SpiritPurger.MenuManager.REACTION;

namespace SpiritPurger
{
	/// <summary>
	/// An Ellipse for rendering.
	/// Full example provided by SFML's tutorials.
	/// http://www.sfml-dev.org/tutorials/2.1/graphics-shape.php
	/// </summary>
	public class EllipseShape : Shape
	{
		protected Vector2f radius;
		public EllipseShape(Vector2f radius)
		{
			this.radius = radius;
			Update();
		}

		public Vector2f Radius
		{
			get { return radius; }
			set
			{
				radius = value;
				Update();
			}
		}

		public override uint GetPointCount()
		{
			return 30;
		}

		public override Vector2f GetPoint(uint index)
		{
			float angle = (float)(index * 2 * Math.PI / GetPointCount() - Math.PI / 2);
			float x = (float)(Math.Cos(angle) * radius.X);
			float y = (float)(Math.Sin(angle) * radius.Y);

			return new Vector2f(radius.X + x, radius.Y + y);
		}
	}

	/// <summary>
	/// Holds all images in the game.
	/// It should provide sprites for objects.
	/// </summary>
	public abstract class Renderer : Observer
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
		protected MenuManager menuManager;
		protected Sprite bg;
		protected Color commonTextColor;
		protected List<List<Text>> submenuLabels;
		protected List<Text> creditsLabels;
		// Separate the volume labels because they are generated separately.
		protected Text musicVolLabel, soundVolLabel;
		protected const int MENU_FONT_SIZE = 24;
		// The tallest a label could be. Calculated upon construction.
		protected float maxLabelHeight;
		protected EllipseShape focusCircle;
		// From the top of the game screen, how far down the 1st menu item is drawn.
		protected const int BELOW_TITLE = 250;
		// How to position menu items.
		protected enum MENU_ITEM_POSITION
		{
			CENTER, LEFT, RIGHT,
			// Unique positions for the WINDOW_TYPE selections.
			WTYPE_WINDOWED, WTYPE_FULLSCREEN,
			// Unique positions for the numerous WINDOW_SIZE selections.
			WSIZE1, WSIZE1_5, WSIZE2, WSIZE3, WSIZEMAX,
			// Unique positions for the credits screen labels.
			CREDIT_LEFT, CREDIT_RIGHT,
		}

		/// <summary>
		/// Makes a MenuRenderer.
		/// </summary>
		public MenuRenderer(ImageManager imageManager, MenuManager menuManager)
		{
			this.menuManager = menuManager;
			imageManager.LoadPNG(ImageManager.TITLE_BG);
			bg = imageManager.GetSprite(ImageManager.TITLE_BG);
			commonTextColor = Color.Cyan;

			// Create the selection focus halo.
			focusCircle = new EllipseShape(new Vector2f(50, 100));
			focusCircle.FillColor = new Color(255, 255, 0, 250);

			submenuLabels = new List<List<Text>>();
			MENUITEM[] tempMenuItems;
			float maxLabelWidth = 0F;

			// Determine the max label height ahead of time.
			Text tempText = new Text("',.PYFGCRLAOEUIDHTN;QJKXBM1234567890",
				menuFont, MENU_FONT_SIZE);
			maxLabelHeight = tempText.GetLocalBounds().Height;

			// Make all labels for all menus.
			List<Text> labels = new List<Text>();
			Text label;
			for (int i = 0; i < (int)SUBMENU.END_SUBMENUS; i++)
			{
				tempMenuItems = menuManager.GetSubmenuLayout((SUBMENU)i);
				if (tempMenuItems != null && tempMenuItems.Length > 0)
				{
					labels = new List<Text>();
					// Make labels for each menu item.
					for (int j = 0; j < tempMenuItems.Length; j++)
					{
						// For unique cases, interact with them separately.
						switch (tempMenuItems[j])
						{
							case MENUITEM.CREDITS:
								label = MakeTextInstance(tempMenuItems[j], j);
								break;
							case MENUITEM.TUTORIAL:
								label = MakeTextInstance(tempMenuItems[j], j);
								break;
							case MENUITEM.WINDOW_SIZE:
								label = MakeTextInstance(tempMenuItems[j], j);
								labels.Add(label);
								// Each of the labels for the screen size are small,
								// so don't check if they exceed the max label width.
								label = MakeTextInstance(MENUITEM.WINDOW_SIZE_1, j);
								labels.Add(label);
								label = MakeTextInstance(MENUITEM.WINDOW_SIZE_1_5, j);
								labels.Add(label);
								label = MakeTextInstance(MENUITEM.WINDOW_SIZE_2, j);
								labels.Add(label);
								label = MakeTextInstance(MENUITEM.WINDOW_SIZE_3, j);
								labels.Add(label);
								label = MakeTextInstance(MENUITEM.WINDOW_SIZE_MAX, j);
								labels.Add(label);
								break;
							case MENUITEM.WINDOW_TYPE:
								label = MakeTextInstance(tempMenuItems[j], j);
								if (label.GetLocalBounds().Width > maxLabelWidth)
									maxLabelWidth = label.GetLocalBounds().Width;
								labels.Add(label);
								
								label = MakeTextInstance(MENUITEM.WINDOW_TYPE_WINDOWED, j);
								if (label.GetLocalBounds().Width > maxLabelWidth)
									maxLabelWidth = label.GetLocalBounds().Width;
								labels.Add(label);
								
								label = MakeTextInstance(MENUITEM.WINDOW_TYPE_FULLSCREEN, j);
								break;
							default:
								label = MakeTextInstance(tempMenuItems[j], j);
								break;
						}
						if (label.GetLocalBounds().Width > maxLabelWidth)
							maxLabelWidth = label.GetLocalBounds().Width;
						labels.Add(label);
					}
					// Add the new list of labels to the full list of labels.
					submenuLabels.Add(labels);
				}
			}

			// Add the names of all developers as labels.
			creditsLabels = new List<Text>();
			label = MakeTextInstance(MENUITEM.CREDIT_PROGRAMMER, 0);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_LARRY, 0);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_ART, 1);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_LUCY, 1);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_SFX, 2);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_SOUND_JAY, 2);
			creditsLabels.Add(label);
			label = MakeTextInstance(MENUITEM.CREDIT_BITSTREAM_VERA, 3);
			creditsLabels.Add(label);
			
			// Make the volume labels separately. They are generated specially, so they
			// are not part of the full list of labels.
			RefreshMusicVolume();
			RefreshSoundVolume();

			SetSelection(menuManager);
		}

		/// <summary>
		/// Creates the label that shows the music volume.
		/// </summary>
		/// <param name="volume">The volume of the music.</param>
		public void RefreshMusicVolume()
		{
			musicVolLabel = MakeVolumeTextInstance(
				(int)menuManager.newOptions.Settings["bgm volume"], 2);
		}

		/// <summary>
		/// Creates the label that shows the sound effects volume.
		/// </summary>
		/// <param name="volume">The volume of the sound effects.</param>
		public void RefreshSoundVolume()
		{
			soundVolLabel = MakeVolumeTextInstance(
				(int)menuManager.newOptions.Settings["sfx volume"], 3);
		}

		/// <summary>
		/// Tells the renderer which menu item is focused.
		/// The renderer shows focus on the new menu item by moving its form of focus
		/// to the newly selected menu item.
		/// </summary>
		/// <param name="menuManager">The logical backend controlling the menu.</param>
		public void SetSelection(MenuManager menuManager)
		{
			// Determine the size and place of the menu entry where that the halo focuses on.
			float x, y;
			uint width, height;
			Text label = GetLabel(menuManager);
			if (menuManager.CurrentMenu == SUBMENU.OPTIONS && menuManager.SelectedIndex == 2)
			{
				// Currently selecting the Music Volume menu item in the Options menu.
				x = musicVolLabel.Position.X;
				y = musicVolLabel.Position.Y;
				width = (uint)(musicVolLabel.GetLocalBounds().Width);
				height = (uint)(musicVolLabel.GetLocalBounds().Height);
			}
			else if (menuManager.CurrentMenu == SUBMENU.OPTIONS && menuManager.SelectedIndex == 3)
			{
				// Currently selecting the Sound Volume menu item in the Options menu.
				x = soundVolLabel.Position.X;
				y = soundVolLabel.Position.Y;
				width = (uint)(soundVolLabel.GetLocalBounds().Width);
				height = (uint)(soundVolLabel.GetLocalBounds().Height);
			}
			else
			{
				x = label.Position.X;
				y = label.Position.Y;
				width = (uint)(label.GetLocalBounds().Width);
				height = (uint)(label.GetLocalBounds().Height);
			}

			if (!(menuManager.CurrentMenu == SUBMENU.CREDITS))
			{
				// Put the focus halo over the selected menu entry.
				focusCircle.Radius = new Vector2f(width / 2 + 20, height / 2);
				focusCircle.Position = new Vector2f(x - 20, y + 4);
			}
			else
			{
				// Hide the focus halo.
				focusCircle.Radius = new Vector2f(0, 0);
			}
		}

		protected Text GetLabel(MenuManager menuManager)
		{
			SUBMENU submenu = menuManager.CurrentMenu;
			int select = menuManager.SelectedIndex;
			if (submenu == SUBMENU.OPTIONS)
			{
				// There's some complicated hand-tweaking we must do to the selection.
				// Match the menuManager's selection to our list of labels while
				// recalling that some labels are not actually selectable.

				// Refer to MenuManager's constructor for the submenu's items.
				if (menuManager.SelectedIndex == 0)
				{
					// Currently on the Window Size menu item.
					double wsize = (double)menuManager.newOptions.Settings["window size"];
					if (wsize == 1.5) select = 2;
					else if (wsize == 2.0) select = 3;
					else if (wsize == 3.0) select = 4;
					else if (wsize == 0.0) select = 5;
					else select = 1;
				}
				else if (menuManager.SelectedIndex == 1)
				{
					// Currently on the Window Display Type menu item.
					bool fullscreen = ((int)menuManager.newOptions.Settings["fullscreen"]) == 1;
					if (fullscreen) select = 9;
					else select = 8;
				}
				else if (menuManager.SelectedIndex == 2)
				{
					// Currently on the Music Volume menu item.
					select = 10;
				}
				else if (menuManager.SelectedIndex == 3)
				{
					// Currently on the Sound Volume menu item.
					select = 11;
				}
				else
				{
					// Currently on the Return menu item.
					select = 12;
				}
			}
			else if (submenu == SUBMENU.CREDITS)
				select = 0;
			return submenuLabels[(int)submenu][select];
		}

		/// <summary>
		/// Creates a Text object for rendering on the menu screen.
		/// </summary>
		/// <param name="text">The string to render.</param>
		/// <param name="depth">How many rows below the title to render. Increment in one's.</param>
		/// <returns>The new Text object with coloring and positioning set.</returns>
		protected Text MakeTextInstance(String text, int depth,
			MENU_ITEM_POSITION pos=MENU_ITEM_POSITION.CENTER)
		{
			Text ret = new Text(text, menuFont, 24);
			// x is centered by default.
			float x = APP_BASE_WIDTH / 2 - ret.GetLocalBounds().Width / 2;
			// y is merely dropped down by the value of BELOW TITLE.
			float y = BELOW_TITLE + maxLabelHeight * depth;

			if (pos == MENU_ITEM_POSITION.LEFT)
				x = APP_BASE_WIDTH / 4;
			else if (pos == MENU_ITEM_POSITION.RIGHT)
				x = APP_BASE_WIDTH / 4 * 3 - ret.GetLocalBounds().Width;
			else if (pos == MENU_ITEM_POSITION.CREDIT_LEFT)
				x = APP_BASE_WIDTH / 10;
			else if (pos == MENU_ITEM_POSITION.CREDIT_RIGHT)
				x = APP_BASE_WIDTH / 10 * 9 - ret.GetLocalBounds().Width;

			ret.Color = commonTextColor;
			ret.Position = new Vector2f(x, y);

			return ret;
		}

		/// <summary>
		/// Creates a Text object for rendering on the menu screen.
		/// </summary>
		/// <param name="item">The menu item to turn into a string.</param>
		/// <param name="depth">How many rows below the title to render. Increment in one's.</param>
		/// <returns>The new Text object with coloring and positioning set.</returns>
		protected Text MakeTextInstance(MENUITEM item, int depth)
		{
			Text ret;
			switch (item)
			{
				case MENUITEM.ABOUT: ret = MakeTextInstance("ABOUT", depth); break;
				case MENUITEM.START_GAME: ret = MakeTextInstance("PLAY", depth); break;
				case MENUITEM.OPTIONS: ret = MakeTextInstance("OPTIONS", depth); break;
				case MENUITEM.EXIT_MAIN: ret = MakeTextInstance("QUIT", depth); break;
				case MENUITEM.EASY_DIFF: ret = MakeTextInstance("EASY", depth); break;
				case MENUITEM.NORM_DIFF: ret = MakeTextInstance("NORMAL", depth); break;
				case MENUITEM.HARD_DIFF: ret = MakeTextInstance("HARD", depth); break;
				case MENUITEM.EXIT_DIFF: ret = MakeTextInstance("RETURN", depth); break;
				case MENUITEM.WINDOW_SIZE:
					ret = MakeTextInstance("SIZE", depth, MENU_ITEM_POSITION.LEFT); break;
				case MENUITEM.WINDOW_SIZE_1:
					ret = MakeTextInstance("1x", 0, MENU_ITEM_POSITION.RIGHT); break;
				case MENUITEM.WINDOW_SIZE_1_5:
					ret = MakeTextInstance("1.5x", 0, MENU_ITEM_POSITION.RIGHT); break;
				case MENUITEM.WINDOW_SIZE_2:
					ret = MakeTextInstance("2x", 0, MENU_ITEM_POSITION.RIGHT); break;
				case MENUITEM.WINDOW_SIZE_3:
					ret = MakeTextInstance("3x", 0, MENU_ITEM_POSITION.RIGHT); break;
				case MENUITEM.WINDOW_SIZE_MAX:
					ret = MakeTextInstance("MAX", 0, MENU_ITEM_POSITION.RIGHT); break;
				case MENUITEM.WINDOW_TYPE:
					ret = MakeTextInstance("DISPLAY", depth, MENU_ITEM_POSITION.LEFT); break;
				case MENUITEM.WINDOW_TYPE_WINDOWED:
					ret = MakeTextInstance("WINDOWED", 1, MENU_ITEM_POSITION.RIGHT);
					break;
				case MENUITEM.WINDOW_TYPE_FULLSCREEN:
					ret = MakeTextInstance("FULLSCREEN", 1, MENU_ITEM_POSITION.RIGHT);
					break;
				case MENUITEM.MUSIC_VOL:
					ret = MakeTextInstance("MUSIC VOLUME", depth, MENU_ITEM_POSITION.LEFT); break;
				case MENUITEM.SOUND_VOL:
					ret = MakeTextInstance("SOUND VOLUME", depth, MENU_ITEM_POSITION.LEFT); break;
				case MENUITEM.EXIT_OPTIONS:
					ret = MakeTextInstance("RETURN", depth, MENU_ITEM_POSITION.LEFT); break;
				case MENUITEM.TUTORIAL: ret = MakeTextInstance("TUTORIAL", depth); break;
				case MENUITEM.CREDITS: ret = MakeTextInstance("CREDITS", depth); break;
				case MENUITEM.CREDIT_PROGRAMMER:
					ret = MakeTextInstance("PROGRAMMER", depth, MENU_ITEM_POSITION.CREDIT_LEFT);
					break;
				case MENUITEM.CREDIT_LARRY:
					ret = MakeTextInstance("LARRY RESNIK", depth, MENU_ITEM_POSITION.CREDIT_RIGHT);
					break;
				case MENUITEM.CREDIT_ART:
					ret = MakeTextInstance("ARTIST", depth, MENU_ITEM_POSITION.CREDIT_LEFT);
					break;
				case MENUITEM.CREDIT_LUCY:
					ret = MakeTextInstance("LUCY HALLIWELL-SMITH", depth,
						MENU_ITEM_POSITION.CREDIT_RIGHT);
					break;
				case MENUITEM.CREDIT_SFX:
					ret = MakeTextInstance("SOUNDS", depth, MENU_ITEM_POSITION.CREDIT_LEFT);
					break;
				case MENUITEM.CREDIT_SOUND_JAY:
					ret = MakeTextInstance("SOUND JAY", depth, MENU_ITEM_POSITION.CREDIT_RIGHT);
					break;
				case MENUITEM.CREDIT_BITSTREAM_VERA:
					ret = MakeTextInstance("BITSTREAM VERA FONTS", depth, MENU_ITEM_POSITION.CENTER);
					break;
				case MENUITEM.EXIT_ABOUT: ret = MakeTextInstance("RETURN", depth); break;
				case MENUITEM.EXIT_TUTORIAL: ret = MakeTextInstance("RETURN", depth); break;
				case MENUITEM.EXIT_CREDITS: ret = MakeTextInstance("RETURN", depth); break;
				default: ret = MakeTextInstance("", depth); break;
			}
			return ret;
		}

		/// <summary>
		/// Creates a Text object for rendering the volume of music or sound.
		/// </summary>
		/// <param name="volume">The volume of the music/sound.</param>
		/// <param name="depth">How many rows below the title to render. Increment in one's.</param>
		/// <returns>The new Text object with the same text as the volume.</returns>
		protected Text MakeVolumeTextInstance(int volume, int depth)
		{
			return MakeTextInstance(volume.ToString(), depth, MENU_ITEM_POSITION.RIGHT);
		}

		public override void Update()
		{
			// React to whatever the menu manager wants.
			REACTION state = menuManager.State;
			if (state == REACTION.MENU_TO_ABOUT ||
				state == REACTION.MENU_TO_CREDITS ||
				state == REACTION.MENU_TO_DIFF ||
				state == REACTION.MENU_TO_MAIN ||
				state == REACTION.MENU_TO_OPTIONS ||
				state == REACTION.MENU_TO_TUTORIAL)
			{
				//currMenu = (int)menuManager.CurrentMenu;
			}

			// Move the menu selection's focus.
			SetSelection(menuManager);
		}

		public void Paint(object sender)
		{
			RenderWindow app = (RenderWindow)sender;
			app.Draw(bg);
			app.Draw(focusCircle);
			if (menuManager.CurrentMenu == SUBMENU.OPTIONS)
			{
				// Draw the Options menu in a special way.
				// Temporary index for which submenu to draw.
				int i = 0;
				int currMenu = (int)menuManager.CurrentMenu;

				// Label of WINDOW_SIZE
				app.Draw(submenuLabels[currMenu][0]);
				double wsize = (double)menuManager.newOptions.Settings["window size"];
				if (wsize == 1.5) i = 2;
				else if (wsize == 2.0) i = 3;
				else if (wsize == 3.0) i = 4;
				else if (wsize == 0.0) i = 5;
				else i = 1;
				// Draw the specific label for what the current window size is.
				app.Draw(submenuLabels[currMenu][i]);
				
				// Label of WINDOW_TYPE
				app.Draw(submenuLabels[currMenu][7]);
				bool fullscreen = ((int)menuManager.newOptions.Settings["fullscreen"]) == 1;
				if (fullscreen) i = 9;
				else i = 8;
				// Draw the specific label for what the windowing type is.
				app.Draw(submenuLabels[currMenu][i]);

				// Label of MUSIC_VOL
				app.Draw(submenuLabels[currMenu][10]);
				app.Draw(musicVolLabel);
				// Label of SOUND_VOL
				app.Draw(submenuLabels[currMenu][11]);
				app.Draw(soundVolLabel);
				// Label of RETURN
				app.Draw(submenuLabels[currMenu][12]);
			}
			else if (menuManager.CurrentMenu == SUBMENU.CREDITS)
			{
				foreach (Text label in creditsLabels)
				{
					app.Draw(label);
				}
			}
			else
			{
				foreach (Text label in submenuLabels[(int)menuManager.CurrentMenu])
				{
					app.Draw(label);
				}
			}
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
			bgSprite.Position = FieldUpperLeft + FieldSize / 2;
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
			labelPausedToEnd = new Text("Hold Bomb to Return to Title Screen", menuFont, 12);
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

		public override void Update()
		{
			throw new NotImplementedException();
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

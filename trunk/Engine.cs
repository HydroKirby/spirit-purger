using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;
using MENUREACTION = SpiritPurger.MenuManager.REACTION;
using GAMEREACTION = SpiritPurger.GameplayManager.REACTION;

namespace SpiritPurger
{
	/// <summary>
	/// Provides the meaning of a timer and the values of its timings.
	/// Based on the meaning, an appropriate frame count will be retrievable from GetTime.
	/// </summary>
	public abstract class TimerPurpose
	{
		// Override this using the "new" keyword.
		public enum PURPOSE { }

		public int SpecificPurpose { get; set; }

		/// <summary>
		/// Gets the amount of time needed to serve a particular purpose.
		/// </summary>
		/// <returns>The time in frames for the purpose to be done.</returns>
		public abstract int GetTime();
	}

	/// <summary>
	/// A simple time-tracker that goes from a high number to zero.
	/// It is multipurpose, so it can store its purpose as well.
	/// </summary>
	public class DownTimer
	{
		public int Frame { get; set; }
		public TimerPurpose Purpose { get; set; }

		public DownTimer(TimerPurpose purpose)
		{
			Frame = 0;
			Purpose = purpose;
		}

		public bool TimeIsUp()
		{
			return Frame == 0;
		}

		public void Tick()
		{
			Frame -= 1;
			if (Frame < 0)
				Frame = 0;
		}

		/// <summary>
		/// Reset the timer's time based on the timer's purpose.
		/// </summary>
		public void Reset()
		{
			Frame = Purpose.GetTime();
		}
	}

    /// <summary>
    /// IComparer class for sorting integers backwards.
    /// </summary>
    public class ReversedSortInt : IComparer
    {
        public int Compare(object o1, object o2)
        {
            return (int)o2 - (int)o1;
        }
    }

	// Below are the classes for doing the Observer Pattern.
	public abstract class Observer
	{
		public abstract void Update();
	}

	/// <summary>
	/// Does things and informs all of its Observers.
	/// </summary>
	public abstract class Subject
	{
		private List<Observer> observers = new List<Observer>();

		public void Attach(Observer observer)
		{
			observers.Add(observer);
		}

		public void Detach(Observer observer)
		{
			observers.Remove(observer);
		}

		public void Notify()
		{
			foreach (Observer o in observers)
			{
				o.Update();
			}
		}
	}

    class Engine : Observer
    {
        // The ticks to wait after the redraw before updating the game.
        // Division by 1,000 turns the seconds value into the milliseconds
        //   which are used by the Timer class.
        public const double UPDATE_TICKS = 30.0 / 1000.0;

        // High-level stuff.
        protected delegate void PaintHandler(object sender, double ticks);
        protected PaintHandler paintHandler = null;
		public KeyHandler keys = new KeyHandler();

        // Game state variables.
        public enum GameState { MainMenu, GamePlay };
        protected GameState gameState;

        // Rendering variables.
		protected RenderWindow app;
		protected ImageManager imageManager;
		protected MenuRenderer menuRenderer;
		protected GameRenderer gameRenderer;

		// Sound variables.
		protected SoundManager soundManager;
		protected MusicManager musicManager;

        // Current-game variables.
		protected bool isPlaying;
		protected MenuManager menuManager;
		protected GameplayManager gameManager;

        public void Run(RenderWindow window=null)
        {
			Options options = new Options();
			Dictionary<string, object> settings = options.Settings;

            // Apply extra things to the window.
			app = window;
			MakeWindow((int)options.Settings["fullscreen"] == 1);

            // Load all resources.
			soundManager = new SoundManager();
			musicManager = new MusicManager();
			imageManager = new ImageManager();
			menuManager = new MenuManager(options);
            menuRenderer = new MenuRenderer(imageManager, menuManager);
			gameManager = new GameplayManager(imageManager, soundManager, keys, settings);
			gameRenderer = new GameRenderer(imageManager);
			menuManager.Attach(this);
			menuManager.Attach(menuRenderer);
			gameManager.Attach(this);
			gameManager.Attach(gameRenderer);

			// Assign sprites.
			imageManager.LoadPNG(ImageManager.GAME_ICON);
			Texture icon = imageManager.GetImage(ImageManager.GAME_ICON);
			app.SetIcon(icon.Size.X, icon.Size.Y,
				icon.CopyToImage().Pixels);

            // Prepare the game to be run.
			isPlaying = true;
            Reset();
            gameState = GameState.MainMenu;
            paintHandler = new PaintHandler(PaintMenu);
			musicManager.ChangeMusic(MusicManager.MUSIC_LIST.TITLE);

			AssignOptions(options);
			MainLoop();
		}

		private void MakeWindow(bool fullscreen=false)
		{
			if (app != null)
			{
				app.Closed -= OnClose;
				app.KeyPressed -= app_KeyPressed;
				app.KeyReleased -= app_KeyReleased;
				app.Close();
			}

			Styles style = fullscreen ? Styles.Fullscreen : (Styles.Default & ~Styles.Resize);
			app = new RenderWindow(new VideoMode(640, 480), "Spirit Purger", style);
			
			// Add all event handlers.
			app.Closed += OnClose;
			app.KeyPressed += app_KeyPressed;
			app.KeyReleased += app_KeyReleased;
			app.SetKeyRepeatEnabled(false);
		}

		/// <summary>
		/// Takes the read-in options and sets variables to them.
		/// </summary>
		/// <param name="options">The assigned options.</param>
		private void AssignOptions(Options options)
		{
			Dictionary<string, object> settings = options.Settings;
			gameRenderer.bgRotSpeed = (double) settings["bg swirl speed"];
			soundManager.Volume = (int)settings["sfx volume"];
			musicManager.Volume = (int)settings["bgm volume"];
			gameRenderer.bossHealthbar.Size = new Vector2f(
				(int)settings["healthbar width"],
				(int)settings["healthbar height"]);
			gameRenderer.bossHealthbar.Position = new Vector2f(
				(int)settings["healthbar x"] + GameRenderer.FIELD_LEFT,
				(int)settings["healthbar y"] + GameRenderer.FIELD_TOP);
			if ((double)settings["window size"] != 1.0)
				ResizeWindow();
		}

		/// <summary>
		/// Sets the game into a before-main-gameplay state.
		/// Use during the game start and after a Game Over.
		/// </summary>
		protected void Reset()
		{
			gameManager.Reset();
			gameRenderer.bossHealthbar.MaxHealth =
				Boss.fullHealth[gameManager.boss.currentPattern];
			gameRenderer.IsPaused = gameManager.paused;
			gameRenderer.IsGameOver = false;
			gameRenderer.SetPatternTime(0);
            gameRenderer.SetScore(gameManager.score);
			gameRenderer.SetLives(gameManager.lives);
			gameRenderer.SetBombs(gameManager.bombs);
        }

		static void OnClose(object sender, EventArgs e)
		{
			// Close the window when OnClose event is received.
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}

        /// <summary>
        /// The game's infinite loop. In a timely manner it updates data,
        /// forces a repaint, and gives outside programs a chance to funnel
        /// events. While waiting for the next update cycle, the loop gives
        /// sleeps.
        /// </summary>
        protected void MainLoop()
        {
            Timer timer = new Timer();
            double lastUpdate = timer.GetTicks();
            Color clearColor = new Color(250, 250, 250);
            
            // Start the game loop
            while (app.IsOpen() && isPlaying)
            {
                // Begin rendering.
                app.Clear(clearColor);
                paintHandler(app, lastUpdate);
                // Make SFML redraw the window.
                app.Display();

                // Update the game.
                // If it is time for a game update, update all gameplay.
                if ((lastUpdate = timer.GetTicks()) >= UPDATE_TICKS)
                {
                    // DispatchEvents made all key states up-to-date.
                    // Now, increment the time they were held down.
                    if (gameState == GameState.GamePlay)
                        UpdateGame(app, lastUpdate);
                    else if (gameState == GameState.MainMenu)
                        UpdateMenu(app, lastUpdate);
					soundManager.Update();
                    // TODO: Reset is bad logic.
                    // We should subtract the old time so no ticks are lost.
                    timer.Reset();
					keys.Update();
                }

                // Process events.
                // If we are done, exit the loop immediately.
                app.DispatchEvents();
				musicManager.Update();
            }

			if (!app.IsOpen())
				app.Close();
			/*
			 * TODO: Cleanup? This cleanup code was in the old engine.
			 * 
			   for (int i = 0; i < bulletImages.Length; i++)
					if (bulletImages[i] != null)
						for (int j = 0; j < bulletImages[i].Length; j++)
							if (bulletImages[i][j] != null)
								bulletImages[i][j].Dispose();
				if (playerBulletImage != null)
					playerBulletImage.Dispose();
				if (grazeSparkImage != null)
					grazeSparkImage.Dispose();
				if (bullseyeSparkImage != null)
					bullseyeSparkImage.Dispose();
				if (hitCircleImage != null)
					hitCircleImage.Dispose();
				playerBullets.Clear();
				enemyBullets.Clear();
				hitSparks.Clear();
				enemies.Clear();
			 */
        }

        void app_KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
				if (gameState == GameState.GamePlay)
				{
					// Toggle pausing the game.
					gameRenderer.IsPaused = gameManager.paused = !gameManager.paused;
				}
				else if (gameState == GameState.MainMenu)
				{
					// Make the menu focus the last item in the submenu.
					keys.bomb = 1;
				}
            }
            else
            {
                keys.KeyDown(e.Code);
            }
        }

        void app_KeyReleased(object sender, KeyEventArgs e)
        {
            keys.KeyUp(e.Code);
        }

        
        /* --- Menu Logic --- */


        /// <summary>
        /// Updates the top menu.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        protected void UpdateMenu(object sender, double ticks)
        {
			bool moved = false;
			bool acted = false;

			// Only accept one directional key.
			if (keys.up == 1)
			{
				menuManager.OnUpKey();
				moved = true;
			}
			else if (keys.down == 1)
			{
				menuManager.OnDownKey();
				moved = true;
			}
			else if (keys.left == 1)
			{
				menuManager.OnLeftKey();
				moved = true;
			}
			else if (keys.right == 1)
			{
				menuManager.OnRightKey();
				moved = true;
			}

			// Only accept one of either accept or cancel keys.
			if (keys.shoot == 1)
			{
				menuManager.OnSelectKey();
				acted = true;
			}
			else if (keys.bomb == 1)
			{
				menuManager.OnCancelKey();
				acted = true;
			}

			if (moved)
				soundManager.QueueToPlay(SoundManager.SFX.MENU_MOVE);

			if (acted)
				soundManager.QueueToPlay(SoundManager.SFX.MENU_SELECT);

			if (acted || moved)
				menuManager.Notify();
		}


        /* --- Game Logic Code --- */


		protected void ResizeWindow()
		{
			// Note: On Windows, windows that are bigger than the desktop will
			// appear strangely. Do not let the window get too big.
			uint maxWidth = VideoMode.DesktopMode.Width;
			uint maxHeight = VideoMode.DesktopMode.Height;
			double appScale = (double)menuManager.newOptions.Settings["window size"];
			uint newWidth = (uint)(Renderer.APP_BASE_WIDTH * appScale);
			uint newHeight = (uint)(Renderer.APP_BASE_HEIGHT * appScale);
			
			if (appScale == 0.0)
			{
				// Maximize the screen.
				newWidth = maxWidth;
				newHeight = maxHeight;
			}

			// If the window is too big, shrink it.
			if (newWidth > maxWidth)
				newWidth = maxWidth;
			if (newHeight > maxHeight)
				newHeight = maxHeight;

			// Check if the ratio of the window size does not match what the game uses.
			if ((int)((newWidth * 100.0) / newHeight) !=
				(int)((Renderer.APP_BASE_WIDTH * 100.0) / Renderer.APP_BASE_HEIGHT))
			{
				// Shrink the bigger value. The code the from the following link.
				// http://stackoverflow.com/questions/15417135/
				double widthRatio = ((double)Renderer.APP_BASE_WIDTH) / Renderer.APP_BASE_HEIGHT;
				double heightRatio = ((double)Renderer.APP_BASE_HEIGHT) / Renderer.APP_BASE_WIDTH;
				if (newHeight * widthRatio <= newWidth)
					newWidth = (uint)(newHeight * widthRatio);
				else if (newWidth * heightRatio <= newHeight)
					newHeight = (uint)(newWidth * heightRatio);
			}

			// Move the app window within the desktop if it grew outside the bounds.
			bool mustMove = false;
			Vector2i newAppPos = new Vector2i();
			if (newWidth + app.Position.X > maxWidth)
			{
				newAppPos.X = (int)(maxWidth - newWidth);
				mustMove = true;
			}
			if (app.Position.X < 0)
			{
				newAppPos.X = 0;
				mustMove = true;
			}
			if (newHeight + app.Position.Y > maxHeight)
			{
				newAppPos.Y = (int)(maxHeight - newHeight);
				mustMove = true;
			}
			if (app.Position.Y < 0)
			{
				newAppPos.Y = 0;
				mustMove = true;
			}
			if (mustMove)
				app.Position = newAppPos;

			// Finally, tell SFML to do the resizing.
			app.Size = new Vector2u(newWidth, newHeight);
		}

		/// <summary>
		/// Reacts to the state of its Subjects.
		/// </summary>
		public override void Update()
		{
			if (gameState == GameState.MainMenu)
			{
				switch (menuManager.State)
				{
					case MENUREACTION.PLAY_GAME:
						gameState = GameState.GamePlay;
						musicManager.ChangeMusic(MusicManager.MUSIC_LIST.GAME);
						// Switch the delegate to painting the game.
						paintHandler = new PaintHandler(PaintGame);
						break;
					case MENUREACTION.SMALLER_WINDOW:
						ResizeWindow();
						break;
					case MENUREACTION.BIGGER_WINDOW:
						ResizeWindow();
						break;
					case MENUREACTION.TO_FULLSCREEN:
						MakeWindow(true);
						break;
					case MENUREACTION.TO_WINDOWED:
						MakeWindow(false);
						ResizeWindow();
						break;
					case MENUREACTION.LESS_MUSIC_VOL:
						musicManager.Volume =
							(int)menuManager.GetNewOptions().Settings["bgm volume"];
						menuRenderer.RefreshMusicVolume();
						break;
					case MENUREACTION.MORE_MUSIC_VOL:
						musicManager.Volume =
							(int)menuManager.GetNewOptions().Settings["bgm volume"];
						menuRenderer.RefreshMusicVolume();
						break;
					case MENUREACTION.LESS_SOUND_VOL:
						soundManager.Volume =
							(int)menuManager.GetNewOptions().Settings["sfx volume"];
						menuRenderer.RefreshSoundVolume();
						break;
					case MENUREACTION.MORE_SOUND_VOL:
						soundManager.Volume =
							(int)menuManager.GetNewOptions().Settings["sfx volume"];
						menuRenderer.RefreshSoundVolume();
						break;
					case MENUREACTION.MENU_TO_MAIN:
						if (menuManager.SelectedIndex == 1)
						{
							// We came from the Options menu. Save the new options.
							Options opt = menuManager.GetNewOptions();
							opt.WriteConfig();
							AssignOptions(opt);
						}
						break;
					case MENUREACTION.END_GAME:
						isPlaying = false;
						break;
				}
				menuManager.StateHandled();
			}
			else if (gameState == GameState.GamePlay)
			{
				switch (gameManager.State)
				{
					case GAMEREACTION.REFRESH_BOMBS:
						gameRenderer.SetBombs(gameManager.bombs);
						break;
					case GAMEREACTION.BOMB_MADE_COMBO:
						gameRenderer.SetBombCombo(gameManager.bombCombo,
							gameManager.bombComboScore);
						break;
					case GAMEREACTION.BOMB_LIFETIME_DECREMENT:
						gameRenderer.BombComboTimeCountdown =
							gameManager.bombComboTimeCountdown;
						break;
					case GAMEREACTION.BOSS_REFRESH_MAX_HEALTH:
						// Tell the renderer the new health value.
						gameRenderer.bossHealthbar.MaxHealth =
							gameManager.boss.health;
						gameRenderer.SetBossMaxHealth(gameManager.boss.health);
						break;
					case GAMEREACTION.BOSS_TOOK_DAMAGE:
						// Tell the renderer that the boss' health changed.
						gameRenderer.SetBossHealth(gameManager.boss.health);
						gameRenderer.bossHealthbar.CurrentHealth =
							gameManager.boss.health;
						break;
					case GAMEREACTION.BOSS_PATTERN_SUCCESS:
						// Remove the pattern time label from the HUD.
						gameRenderer.SetPatternTime(0);
						// Show the pattern result.
						gameRenderer.SetPatternResult(true, 30000);
						break;
					case GAMEREACTION.BOSS_PATTERN_FAIL:
						// Opposite of BOSS_PATTERN_SUCCESS.
						gameRenderer.SetPatternTime(0);
						gameRenderer.SetPatternResult(false, 5000);
						break;
					case GAMEREACTION.BOSS_PATTERN_TIMEOUT:
						gameRenderer.bossHealthbar.MaxHealth =
							gameManager.boss.health;
						break;
					case GAMEREACTION.BOSS_REFRESH_PATTERN_TIME:
						gameRenderer.SetPatternTime(gameManager.patternTime);
						break;
					case GAMEREACTION.REFRESH_SCORE:
						gameRenderer.SetScore(gameManager.score);
						break;
					case GAMEREACTION.REFRESH_BULLET_COUNT:
						gameRenderer.SetBullets(gameManager.playerBullets.Count +
							gameManager.enemyBullets.Count);
						break;
					case GAMEREACTION.LOST_ALL_LIVES:
						gameRenderer.IsGameOver = gameManager.gameOver;
						break;
					case GAMEREACTION.REFRESH_LIVES:
						gameRenderer.SetLives(gameManager.lives);
						break;
					case GAMEREACTION.RESET_GAME:
						Reset();
						gameState = GameState.MainMenu;
						musicManager.ChangeMusic(MusicManager.MUSIC_LIST.TITLE);
						paintHandler = new PaintHandler(PaintMenu);
						break;
				}
				gameManager.StateHandled();
			}
		}

        /// <summary>
        /// Updates the game.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        public void UpdateGame(object sender, double ticks)
        {
			gameManager.UpdateGame(sender, ticks);
            gameRenderer.Update(ticks);
        }


        /* --- Rendering Code --- */


        /// <summary>
        /// Renders the top menu screen.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        protected void PaintMenu(object sender, double ticks)
        {
            menuRenderer.Paint(sender);
        }

        /// <summary>
        /// Renders the main game.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        protected void PaintGame(object sender, double ticks)
        {
			RenderWindow app = (RenderWindow)sender;

			// Tell the renderer about any new positions.
			app.Draw(gameRenderer.bgSprite);
			gameManager.PaintGame(sender, ticks);

			// Draw the HUD.
			gameRenderer.Paint(sender);
        }
    }
}

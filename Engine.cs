using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;
using REACTION = SpiritPurger.MenuManager.REACTION;

namespace SpiritPurger
{
	public enum MainMenu
	{
		Play, GodMode, FunBomb, Repulsive, Scale, Exit,
		EndChoices
	};

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
        // After dying, this is how long the player automatically moves back
        // up to the playfield.
        public const int REENTRY_FRAMES = 50;
        public const int POST_DEATH_INVINC_FRAMES =
            REENTRY_FRAMES + Player.DEATH_SEQUENCE_FRAMES + 40;
        // Refers to when a boss pattern has completed. It's the time to wait
        // until initiating the next pattern.
        public const int PATTERN_TRANSITION_PAUSE = 80;
        // The time to wait before the boss begins the intro sequence.
        public const int BOSS_PRE_INTRO_FRAMES = 20;
        // The time to wait during the boss' fade-in sequence.
        public const int BOSS_INTRO_FRAMES = 50;
        // The time to show the bomb combo score after a bomb wears off.
        public const int BOMB_COMBO_DISPLAY_FRAMES = 270;

        // High-level stuff.
        protected delegate void PaintHandler(object sender, double ticks);
        protected PaintHandler paintHandler = null;
        public KeyHandler keys = new KeyHandler();
        public Random rand = new Random();
        protected MainMenu menuChoice = (MainMenu)0;
        protected bool godMode = false;
        protected bool funBomb = false;
        protected bool repulsive = false;
        // Patches a stupid bug where the key repeat feature makes going from
        //   the game to the menu a pain. When true, double-tapped selections
        //   in the menu are ignored. Becomes false when another key is pressed
        //   which proves that the player has had time to do other things in
        //   the menu than hold shoot after entering the menu.
        protected bool disallowRapidSelection = true;

        // Boss-related variables.
        public enum BossState { NotArrived, Intro, Active, Killed };
        protected Boss boss;
        protected BossState bossState = BossState.Intro;
        // How long the boss has been doing the intro sequence.
        protected int bossIntroTime = 0;

        // Game state variables.
        public enum GameState { MainMenu, GamePlay };
        protected GameState gameState;
        protected bool gameOver = false;
        // Gives points when true. Becomes false if the players dies or bombs.
        protected bool beatThisPattern = true;
        protected bool paused = false;
        protected Player player;
		// Stores all base types of bullets. Keep in memory.
		protected ArrayList bulletTypes = new ArrayList();
        protected ArrayList enemies = new ArrayList();
        protected ArrayList enemyBullets = new ArrayList();
        protected ArrayList playerBullets = new ArrayList();
        protected ArrayList hitSparks = new ArrayList();

        // Rendering variables.
		protected RenderWindow app;
		protected ImageManager imageManager;
		protected MenuRenderer menuRenderer;
		protected GameRenderer gameRenderer;
        protected double appScale = 1.0;

		// Sound variables.
		protected SoundManager soundManager;
		protected MusicManager musicManager;

        // Current-game variables.
		protected bool isPlaying;
		protected MenuManager menuManager;
		protected BulletCreator bulletCreator;
        // This is the number of seconds left to complete a pattern.
        protected int patternTime = 0;
        // During a timed boss pattern, this is the milliseconds accumulated
        //   between each passing second.
        protected double prevSecondUpdateFraction = 0.0;
        // The amount of frames waited between boss pattern transitions.
        protected int transitionFrames = 0;
        // The number of times a bullet touched the player's hitbox.
        protected int grazeCount = 0;
        // The number of bullets removed by the current bomb.
        protected int bombCombo = 0;
        // The timer for how long the combo has been displayed.
        protected int bombComboTimeCountdown = 0;
        // The accumulated score of the current bomb combo.
        protected int bombComboScore = 0;
        protected Bomb bombBlast;
        protected long score = 0;
        protected short lives = 2;
        protected short bombs = 3;

        public void Run(RenderWindow app)
        {
			Options options = new Options();
			Dictionary<string, object> settings = options.Settings;

            // Apply extra things to the window.
            app.SetKeyRepeatEnabled(false);
            app.KeyPressed += new EventHandler<KeyEventArgs>(app_KeyPressed);
            app.KeyReleased += new EventHandler<KeyEventArgs>(app_KeyReleased);

            // Load all resources.
			this.app = app;
			imageManager = new ImageManager();
			menuManager = new MenuManager();
            menuRenderer = new MenuRenderer(imageManager, menuManager);
			gameRenderer = new GameRenderer(imageManager);
			bulletCreator = new BulletCreator(imageManager);
			soundManager = new SoundManager();
			musicManager = new MusicManager();
			menuManager.Attach(this);
			menuManager.Attach(menuRenderer);

			// Assign sprites.
			player = new Player(
				new AniPlayer(imageManager,
					Animation.GetStyleFromString((string)settings["player animation type"]),
					(int)settings["player animation speed"]),
				new Hitbox(bulletCreator.GetSprite(15), 2, new Vector2f(),
					new Vector2f(), 0.0));
			player.UpdateDisplayPos();
			boss = new Boss(
				new AniBoss(imageManager,
					Animation.GetStyleFromString((string)settings["boss animation type"]),
					(int)settings["boss animation speed"]));
			boss.UpdateDisplayPos();
			bombBlast = new Bomb(bulletCreator.GetSprite(16), 0, new Vector2f(),
				new Vector2f(), 0.0);
			imageManager.LoadPNG(ImageManager.GAME_ICON);
			Texture icon = imageManager.GetImage(ImageManager.GAME_ICON);
			app.SetIcon(icon.Size.X, icon.Size.Y,
				icon.CopyToImage().Pixels);

            // Prepare the game to be run.
			isPlaying = true;
            Reset();
            // Disable the patch because the annoyance is only when going from
            //   the game to the menu - not during startup.
            disallowRapidSelection = false;
            gameState = GameState.MainMenu;
            paintHandler = new PaintHandler(PaintMenu);

			AssignOptions(options);
			MainLoop(app);
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
		}

		/// <summary>
		/// Sets the game into a before-main-gameplay state.
		/// Use during the game start and after a Game Over.
		/// </summary>
		protected void Reset()
        {
            player.Location = new Vector2f(Renderer.FIELD_WIDTH / 2,
				Renderer.FIELD_WIDTH / 4 * 3);
			player.UpdateDisplayPos();
            player.deathCountdown = 0;
			player.reentryCountdown = 0;
            player.invincibleCountdown = 0;
			boss.Location = new Vector2f(Renderer.FIELD_WIDTH / 2,
				Renderer.FIELD_HEIGHT / 4);
			boss.UpdateDisplayPos();
            boss.currentPattern = -1;
            boss.NextPattern();
            patternTime = Boss.patternDuration[boss.currentPattern];
			gameRenderer.bossHealthbar.MaxHealth =
				Boss.fullHealth[boss.currentPattern];
            bossState = BossState.Intro;
            disallowRapidSelection = true;
            playerBullets.Clear();
            enemyBullets.Clear();
            hitSparks.Clear();
            enemies.Clear();
            beatThisPattern = true;
            gameOver = false;
			gameRenderer.IsPaused = paused = false;
            transitionFrames = 0;
            bossIntroTime = 0;
            bombBlast.Kill();
            bombCombo = 0;
            bombComboTimeCountdown = 0;
            bombComboScore = 0;
            grazeCount = 0;
			gameRenderer.SetPatternTime(0);
            gameRenderer.SetScore(score = 0);
			// TODO: Reset gameRenderer and menuRenderer.
			gameRenderer.SetLives(lives = 2);
			gameRenderer.SetBombs(bombs = 3);
        }

        /// <summary>
        /// The game's infinite loop. In a timely manner it updates data,
        /// forces a repaint, and gives outside programs a chance to funnel
        /// events. While waiting for the next update cycle, the loop gives
        /// sleeps.
        /// </summary>
        protected void MainLoop(RenderWindow app)
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
					gameRenderer.IsPaused = paused = !paused;
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


		public void MovePlayer()
		{
			// Act on states that disable player movement first.
			if (player.deathCountdown > 0)
				if (player.deathCountdown == 1)
				{
					lives--;
                    if (lives < 0)
                        gameOver = gameRenderer.IsGameOver = true;
					gameRenderer.SetLives(lives);
					player.reentryCountdown = REENTRY_FRAMES;
					player.Location = new Vector2f(145.0F, 320.0F);
					player.UpdateDisplayPos();
				}
				else
					return;
			if (lives < 0)
				return;
			if (player.reentryCountdown > 0)
			{
				player.Move(0, -Player.LO_SPEED);
				player.UpdateDisplayPos();
				return;
			}

			if (keys.Horizontal() != 0)
			{
				if (keys.left > 0)
				{
					player.Move(keys.slow > 0 || !bombBlast.IsGone() ?
						-Player.LO_SPEED : -Player.HI_SPEED, 0);
					if (player.Location.X - player.HalfSize.X < 0)
						player.Location = new Vector2f(player.HalfSize.X, player.Location.Y);
				}
				else
				{
					player.Move(keys.slow > 0 || !bombBlast.IsGone() ?
						Player.LO_SPEED : Player.HI_SPEED, 0);
					if (player.Location.X + player.HalfSize.X > Renderer.FIELD_WIDTH)
						player.Location = new Vector2f(Renderer.FIELD_WIDTH - player.HalfSize.X,
							player.Location.Y);
				}
				player.UpdateDisplayPos();
			}

			if (keys.Vertical() != 0)
			{
				if (keys.up > 0)
				{
					player.Move(0, keys.slow > 0 || !bombBlast.IsGone() ?
						-Player.LO_SPEED : -Player.HI_SPEED);
					if (player.Location.Y - player.HalfSize.Y < 0)
						player.Location = new Vector2f(player.Location.X, player.HalfSize.Y);
				}
				else
				{
					player.Move(0, keys.slow > 0 || !bombBlast.IsGone() ?
						Player.LO_SPEED : Player.HI_SPEED);
					if (player.Location.Y + player.HalfSize.Y > Renderer.FIELD_HEIGHT)
						player.Location = new Vector2f(player.Location.X,
							Renderer.FIELD_HEIGHT - player.HalfSize.Y);
				}
				player.UpdateDisplayPos();
			}
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
					case REACTION.PLAY_GAME:
						gameState = GameState.GamePlay;
						musicManager.ChangeMusic(MusicManager.MUSIC_LIST.GAME);
						// Switch the delegate to painting the game.
						paintHandler = new PaintHandler(PaintGame);
						break;
					case REACTION.SMALLER_WINDOW:
						if (appScale == 1.0)
							appScale = 2.0;
						else if (appScale == 2.0)
							appScale = 1.0;
						app.Size = new Vector2u( (uint) (Renderer.APP_BASE_WIDTH * appScale),
							(uint) (Renderer.APP_BASE_HEIGHT * appScale) );
						break;
					case REACTION.END_GAME:
						isPlaying = false;
						break;
				}
			}
		}

        /// <summary>
        /// Updates the game.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        public void UpdateGame(object sender, double ticks)
        {
            if (paused)
            {
                if (keys.bomb > 60)
                {
                    // End the game.
                    Reset();
                    gameState = GameState.MainMenu;
					musicManager.ChangeMusic(MusicManager.MUSIC_LIST.TITLE);
                    paintHandler = new PaintHandler(PaintMenu);
                }
                return;
            }

            if (patternTime > 0 && transitionFrames <= 0)
            {
                prevSecondUpdateFraction += ticks;
                if (prevSecondUpdateFraction >= 1.0)
                {
                    // Decrease the amount of time the pattern will be run for.
                    patternTime--;
                    // To prevent timing errors, follow a real clock time.
                    // patternTime is only an integer representation for
                    // easy coding and displaying.
                    prevSecondUpdateFraction -= 1.0;
                    gameRenderer.SetPatternTime(patternTime);
                    if (patternTime == 0)
                    {
                        // Ran out of time trying to beat the pattern.
                        beatThisPattern = false;
						if (!boss.NextPattern())
						{
							bossState = BossState.Killed;
							gameOver = true;
						}
						else
						{
							patternTime = 1 +
								Boss.patternDuration[boss.currentPattern];
							gameRenderer.bossHealthbar.MaxHealth = boss.health;
						}
                    }
                }
            }

            MovePlayer();

			if (godMode && keys.slow == 2 && !bombBlast.IsGone())
			{
				bombBlast.Kill();
				soundManager.QueueToPlay(SoundManager.SFX.BOMB_DEACTIVATED);
			}

            if (gameOver)
            {
				// Press the Shot button to return to the main menu.
                if (keys.shoot == 2)
                {
                    this.Reset();
                    gameState = GameState.MainMenu;
					musicManager.ChangeMusic(MusicManager.MUSIC_LIST.TITLE);
                    paintHandler = new PaintHandler(PaintMenu);
                    return;
                }
            }
            else
            {
				// Press the Shot button to fire bullets.
                if (keys.shoot > 0 && player.deathCountdown <= 0 &&
                    player.reentryCountdown <= 0)
                {
                    if (player.TryShoot())
                    {
						// Fire bullets from the player.
						BulletProp prop = new BulletProp(19, new Vector2f(
							player.Location.X - 4,
							player.Location.Y),
							VectorLogic.AngleToVector(VectorLogic.Radians(270)),
							9.0);
                        Bullet bullet = bulletCreator.MakeBullet(prop);
                        playerBullets.Add(bullet);
						prop.Renew();
                        prop.Location = new Vector2f(player.Location.X +
                            4, prop.Location.Y);
						bullet = bulletCreator.MakeBullet(prop);
                        playerBullets.Add(bullet);
						soundManager.QueueToPlay(SoundManager.SFX.PLAYER_SHOT_BULLET);
                    }
                }

				// Press the Bomb button to fire a bomb.
				if (keys.bomb == 2 && bombBlast.IsGone() && (godMode ||
                    bombs > 0 && player.reentryCountdown <= 0 &&
                    player.deathCountdown <= 0))
                {
					// Fire a bomb.
                    player.invincibleCountdown = Bomb.LIFETIME_ACTIVE;
					bombBlast.Renew(player.Location);
                    bombCombo = 0;
                    bombComboTimeCountdown = BOMB_COMBO_DISPLAY_FRAMES;
                    bombs--;
                    beatThisPattern = false;
					gameRenderer.SetBombs(bombs);
					soundManager.QueueToPlay(SoundManager.SFX.PLAYER_SHOT_BOMB);
                }
            }

            if (bombComboTimeCountdown >= 0)
            {
                bombComboTimeCountdown--;
                gameRenderer.BombComboTimeCountdown = bombComboTimeCountdown;
            }

            UpdateBullets();
            UpdateEnemies();
            player.Update();
            gameRenderer.Update(ticks);
        }

        public void UpdateEnemies()
        {
			List<BulletProp> newBullets;
            foreach (Boss enemy in enemies)
            {
				if (enemy.health <= 0)
				{
					soundManager.QueueToPlay(SoundManager.SFX.FOE_DESTROYED);
					break;
				}

                enemy.Update(out newBullets, player.Location, rand);
				// TODO: Maybe this should be moved outside of the loop.
				if (newBullets.Count > 0)
				{
					soundManager.QueueToPlay(SoundManager.SFX.FOE_SHOT_BULLET);
					foreach (BulletProp b in newBullets)
						enemyBullets.Add(bulletCreator.MakeBullet(b));
					newBullets.Clear();
				}

                if (player.invincibleCountdown <= 0 && !godMode &&
					lives >= 0 && Physics.Touches(player, enemy))
                {
					// The player took damage.
                    player.invincibleCountdown = POST_DEATH_INVINC_FRAMES;
                    player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
                    beatThisPattern = false;
					soundManager.QueueToPlay(SoundManager.SFX.PLAYER_TOOK_DAMAGE);
                }
            }

            if (bossState == BossState.Active)
            {
                if (player.invincibleCountdown <= 0 && !godMode &&
                    lives >= 0 && Physics.Touches(player, boss))
                {
					// The player took damage.
                    player.invincibleCountdown = POST_DEATH_INVINC_FRAMES;
                    player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
                    beatThisPattern = false;
					soundManager.QueueToPlay(SoundManager.SFX.PLAYER_TOOK_DAMAGE);
                }

                if (boss.health > 0)
                {
                    // Increment the time the boss is in its pattern.
					boss.Update(out newBullets, player.Location, rand);
					if (newBullets.Count > 0)
					{
						soundManager.QueueToPlay(SoundManager.SFX.FOE_SHOT_BULLET);
						foreach (BulletProp b in newBullets)
							enemyBullets.Add(bulletCreator.MakeBullet(b));
						newBullets.Clear();
					}
                }
                else
                {
					// The boss lost its health. It might not be dead because it is mid-pattern.
					boss.Update(1);
                    // Increment the boss' time transitioning between patterns.
                    transitionFrames++;
                    if (transitionFrames >= PATTERN_TRANSITION_PAUSE)
                    {
						// Reset the transition frames for the next time.
                        transitionFrames = 0;
                        beatThisPattern = true;
						if (!boss.NextPattern())
						{
							bossState = BossState.Killed;
							gameOver = true;
							soundManager.QueueToPlay(SoundManager.SFX.BOSS_DESTROYED);
						}
						else
						{
							// Activate the next pattern.
							patternTime =
								Boss.patternDuration[boss.currentPattern];
							// Tell the renderer the new health value.
							gameRenderer.bossHealthbar.MaxHealth = boss.health;
							gameRenderer.SetBossHealth(boss.health);
						}
                    }
                }
            }
            else if (bossState == BossState.Intro)
            {
                bossIntroTime++;
				if (bossIntroTime > BOSS_INTRO_FRAMES + BOSS_PRE_INTRO_FRAMES)
				{
					// The boss is no longer in the intro sequence.
					bossState = BossState.Active;
					// Tell the renderer the health of the first pattern.
					gameRenderer.bossHealthbar.MaxHealth = boss.health;
					gameRenderer.SetBossHealth(boss.health);
				}
            }
        }

        public void UpdateBullets()
        {
            ArrayList toRemove = new ArrayList();

            for (int i = 0; i < hitSparks.Count; i++)
            {
                Bullet spark = (Bullet)hitSparks[i];
                spark.Update();
                if (spark.IsGone())
                    toRemove.Add(i);
            }
            // Sort the list from biggest to smallest index.
            // Logically this is unnecessary, but I'd rather be safe.
            if (toRemove.Count > 1)
                toRemove.Sort(new ReversedSortInt());
            // Remove each "dead" bullet sequentially from back to front.
            // The separate, parallel toRemove list is used because the list
            // with elements being removed shrinks during deletion and the
            // wrong stuff gets removed since the indexes would change.
            foreach (int i in toRemove)
                hitSparks.RemoveAt(i);
            // Clear the removal list so that it doesn't affect the next batch
            // of bullets that will need removal.
            toRemove.Clear();

			// Do bomb logic. Affects other bullets in the bomb blast radius.
            if (!bombBlast.IsGone())
            {
                bool bombMadeCombo = true;
                bombBlast.Update();
                bool finalFrame = bombBlast.IsGone();
                for (int i = 0; i < enemyBullets.Count; i++)
                {
                    if (Physics.Touches(bombBlast, (Bullet)enemyBullets[i]))
                    {
						// Make the bullet gravitate towards the bomb's center.
                        bombMadeCombo = true;
                        if (!funBomb && finalFrame)
                        {
                            // Just remove everything in the blast radius.
                            toRemove.Add(i);
                            bombCombo++;
                            score += 5;
                            bombComboScore += 5;
                            continue;
                        }

                        Bullet enemyBullet = (Bullet)enemyBullets[i];
                        if (!funBomb && Math.Abs(VectorLogic.GetDistance(
                            enemyBullet.location, bombBlast.location)) <=
                            enemyBullet.Speed + 0.1)
                        {
                            // It's within the center of the bomb.
                            toRemove.Add(i);
                            bombCombo++;
                            score += 5;
                            bombComboScore += 5;
							soundManager.QueueToPlay(SoundManager.SFX.BOMB_ATE_BULLET);
                            continue;
                        }

                        // Get the angles to compare.
                        double towardBombAngle = VectorLogic.GetDirection(
                            bombBlast.location, enemyBullet.location);
                        double currentAngle = VectorLogic.GetAngle(enemyBullet.Direction);

                        // If the angle is far (more than 90 degrees), then
                        //   decrease the speed.
						if (Math.Abs(towardBombAngle - currentAngle) >=
							Math.PI / 2.0)
						{
							enemyBullet.Speed -= 0.5;
							if (enemyBullet.Speed <= 0.0)
							{
								// This speed change would give a negative
								// speed. Instead, change the angle.
								enemyBullet.Speed =
									Math.Max(Math.Abs(enemyBullet.Speed), 0.1);
								currentAngle = towardBombAngle;
							}
						}
						else
						{
							// The angle is small, so increase speed.
							enemyBullet.Speed = Math.Min(4.0,
								enemyBullet.Speed + 0.5);
						}

                        // Alter the bullet's current trrajectory by a
                        //   maximum of 0.1 radians.
                        enemyBullet.Direction = VectorLogic.AngleToVector(
                            currentAngle + Math.Max(-0.1, Math.Min(0.1,
                            towardBombAngle - currentAngle)));
                    }
                }

                if (bombMadeCombo)
                    gameRenderer.SetBombCombo(bombCombo, bombComboScore);

                if (finalFrame)
                {
                    // Disable the bomb.
                    bombBlast.Kill();
                    // Give the player a little leeway after the blast.
                    player.invincibleCountdown = 20;
                }

                gameRenderer.SetScore(score);
                if (toRemove.Count > 1)
                    toRemove.Sort(new ReversedSortInt());
                foreach (int i in toRemove)
                    enemyBullets.RemoveAt(i);
                toRemove.Clear();
            }

			// Do enemy bullet logic. They can cause the player to graze or die.
            for (int i = 0; i < enemyBullets.Count; i++)
            {
                Bullet bullet = (Bullet)enemyBullets[i];
                bullet.Update();
                if (bullet.IsOutside(Renderer.FieldSize, 30))
                    // Build a list of "dead" bullets.
                    toRemove.Add(i);
				else if (player.invincibleCountdown <= 0 && lives >= 0 &&
						 Math.Abs(player.Location.Y - bullet.location.Y) <=
						 player.Size.X + bullet.Radius)
				{
					// The bullet is close to the player. Check for collison.
					if (Physics.Touches(player, bullet))
					{
						if (!bullet.grazed)
						{
							// The player grazed the bullet.
							grazeCount++;
							soundManager.QueueToPlay(SoundManager.SFX.PLAYER_GRAZE);
							if (repulsive)
							{
								// Make the bullet point directly away from
								// the player.
								bullet.Direction =
									VectorLogic.GetDirectionVector(
									bullet.location, player.Location);
							}
							else
							{
								// Show feedback of a graze with a hitspark.
								// The size index does not matter.
								Bullet h = bulletCreator.MakeBullet(new BulletProp(17,
									new Vector2f(bullet.location.X, bullet.location.Y),
									VectorLogic.GetDirectionVector(
										bullet.location, player.Location),
									5.0));
								h.Lifetime = Bullet.LIFETIME_PARTICLE;
								hitSparks.Add(h);
								score += 50;
							}
							bullet.grazed = true;
						}

						if (!godMode && !repulsive &&
							Physics.Touches(bullet, player.hitbox))
						{
							// The player was hit by a bullet.
							player.invincibleCountdown =
								POST_DEATH_INVINC_FRAMES;
							player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
							beatThisPattern = false;
							soundManager.QueueToPlay(SoundManager.SFX.PLAYER_TOOK_DAMAGE);
						}
					}
				}
            }
            if (toRemove.Count > 1)
                toRemove.Sort(new ReversedSortInt());
            foreach (int i in toRemove)
                enemyBullets.RemoveAt(i);
            toRemove.Clear();

			// Do player bullet logic. It can hit enemies and bosses.
            // TODO: Cram both bullets together.
            bool hitBoss = false;
            for (int i = 0; i < playerBullets.Count; i++)
            {
                Bullet bullet = (Bullet)playerBullets[i];
                bullet.Update();
                if (bullet.IsOutside(Renderer.FieldSize, 0))
                    toRemove.Add(i);
                else
                {
                    bool scoreUp = false;
					// See if bullets hit any enemies.
					foreach (Boss enemy in enemies)
					{
						if (Physics.Touches(enemy, bullet))
						{
							enemy.health -= 2;
							toRemove.Add(i);
							// Make 2 hitsparks to show that the enemy was hit.
							for (int k = 0; k < 2; k++)
							{
								// Makes a hitspark shoot downwards at an angle
								// between 210 and 330 degrees.
								Bullet h = bulletCreator.MakeBullet(new BulletProp(18,
									new Vector2f(bullet.location.X,
										boss.Location.Y + boss.Size.Y),
									VectorLogic.AngleToVector(VectorLogic.Radians(
										60.0 + rand.NextDouble() * 60.0)),
									2.5));
								h.Lifetime = Bullet.LIFETIME_PARTICLE;
								hitSparks.Add(h);
							}
							soundManager.QueueToPlay(SoundManager.SFX.FOE_TOOK_DAMAGE);
							score += 20;
							scoreUp = true;
						}
					}
					// See if bullets hit the boss.
                    if (bossState == BossState.Active && Physics.Touches(boss, bullet))
                    {
                        hitBoss = true;
						if (boss.health >= 75)
							soundManager.QueueToPlay(SoundManager.SFX.FOE_TOOK_DAMAGE);
						else
							soundManager.QueueToPlay(SoundManager.SFX.FOE_TOOK_DAMAGED_WEAKENED);
                        if (boss.DealDamage(2))
                        {
                            // The boss has no more health.
                            enemyBullets.Clear();
                            long addScore = beatThisPattern ? 30000 : 5000;
                            score += addScore;
                            // Remove the pattern time label from the HUD.
                            gameRenderer.SetPatternTime(0);
                            // Show the pattern result.
                            gameRenderer.SetPatternResult(beatThisPattern, addScore);
							soundManager.QueueToPlay(beatThisPattern ?
								SoundManager.SFX.FANFARE_PATTERN_SUCCESS :
								SoundManager.SFX.FANFARE_PATTERN_FAILURE);
                        }
                        toRemove.Add(i);
                        // Make 2 hitsparks to show that the enemy was hit.
                        for (int k = 0; k < 2; k++)
                        {
                            Bullet h = bulletCreator.MakeBullet(new BulletProp(18,
                                new Vector2f(bullet.location.X,
                                    bullet.location.Y),
                                VectorLogic.AngleToVector(VectorLogic.Radians(
                                    60.0 + rand.NextDouble() * 60.0)), 2.5));
							h.Lifetime = Bullet.LIFETIME_PARTICLE;
                            hitSparks.Add(h);
                        }
                        score += 20;
                        scoreUp = true;
                    }
                    if (scoreUp)
                        gameRenderer.SetScore(score);
                }
            }

			if (hitBoss)
			{
				// Tell the renderer that the boss' health changed.
				gameRenderer.SetBossHealth(boss.health);
				gameRenderer.bossHealthbar.CurrentHealth = boss.health;
			}

            if (toRemove.Count > 1)
                toRemove.Sort(new ReversedSortInt());
            foreach (int i in toRemove)
                playerBullets.RemoveAt(i);
            gameRenderer.SetBullets(playerBullets.Count + enemyBullets.Count);
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
			if (!bombBlast.IsGone())
			{
				app.Draw(bombBlast.Sprite);
			}
			// Draw the player, boss, and enemies.
			if (lives >= 0)
				player.Draw(app);
            if (bossState == BossState.Active)
                boss.Draw(app);
            else if (bossState == BossState.Intro &&
                     bossIntroTime > BOSS_PRE_INTRO_FRAMES)
            {
                // The boss is invisible unless it has waited more than
                // BOSS_PRE_INTRO_FRAMES at which it then fades-in gradually.
                // The visibility is the proprtion of time waited compared
                // to BOSS_INTRO_FRAMES.
                /*
                boss.Draw(e.Graphics,
                    (int)((bossIntroTime - BOSS_PRE_INTRO_FRAMES) /
                    (float)BOSS_INTRO_FRAMES * 255.0F));
                 */
                // TODO: Temporary code is below. Swap for the above.
				boss.Draw(app);
            }

			// Draw the bullets and hitsparks.
			foreach (Bullet spark in hitSparks)
                app.Draw(spark.Sprite);
            foreach (Bullet bullet in playerBullets)
                app.Draw(bullet.Sprite);
            foreach (Bullet bullet in enemyBullets)
                app.Draw(bullet.Sprite);

			if (keys.slow > 0)
				app.Draw(player.hitBoxSprite);

			// Draw the HUD.
			gameRenderer.Paint(sender);
        }
    }
}

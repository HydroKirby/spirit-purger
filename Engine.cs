using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

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

    class Engine
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
        // Replaces the color index for hitsparks.
        public const int GRAZE_SPARK_INDEX = 0;
        public const int BULLSEYE_SPARK_INDEX = 1;
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
        protected Enemy boss = new Enemy();
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
        protected Player player = new Player();
		// Stores all base types of bullets. Keep in memory.
		protected ArrayList bulletTypes = new ArrayList();
        protected ArrayList enemies = new ArrayList();
        protected ArrayList enemyBullets = new ArrayList();
        protected ArrayList playerBullets = new ArrayList();
        protected ArrayList hitSparks = new ArrayList();

        // Rendering variables.
        protected Renderer renderer;
		protected MenuRenderer menuRenderer;
		protected GameRenderer gameRenderer;
        protected double appScale = 1.0;

        // Current-game variables.
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
            // Apply extra things to the window.
            app.SetKeyRepeatEnabled(false);
            app.KeyPressed += new EventHandler<KeyEventArgs>(app_KeyPressed);
            app.KeyReleased += new EventHandler<KeyEventArgs>(app_KeyReleased);

            // Load all resources.
			renderer = new Renderer();
            menuRenderer = new MenuRenderer();
			gameRenderer = new GameRenderer();

			// Assign sprites.
			player.SetImage(gameRenderer.GetCenterSprite("p_fly"));
			player.SetHitboxSprite(gameRenderer.GetCenterSprite("hitbox"));
			player.UpdateDisplayPos();
			boss.SetImage(gameRenderer.GetCenterSprite("boss_fly"));
			boss.UpdateDisplayPos();
			bombBlast = new Bomb(new Vector2f(0, 0));
			bombBlast.Sprite = gameRenderer.GetCenterSprite("bomb");

            // Prepare the game to be run.
            Reset();
            // Disable the patch because the annoyance is only when going from
            //   the game to the menu - not during startup.
            disallowRapidSelection = false;
            gameState = GameState.MainMenu;
            paintHandler = new PaintHandler(PaintMenu);

			if (ReadOptions())
				MainLoop(app);
			else
			{
				// Produce default options and write that to the config.
			}
        }

		protected bool ReadOptions()
		{
			return true;
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
            patternTime = Enemy.patternDuration[boss.currentPattern];
            bossState = BossState.Intro;
            disallowRapidSelection = true;
            playerBullets.Clear();
            enemyBullets.Clear();
            hitSparks.Clear();
            enemies.Clear();
            beatThisPattern = true;
            gameOver = false;
            paused = false;
            transitionFrames = 0;
            bossIntroTime = 0;
            bombBlast.Kill();
            bombCombo = 0;
            bombComboTimeCountdown = 0;
            bombComboScore = 0;
            grazeCount = 0;
            lives = 2;
            bombs = 3;
            score = 0;
			// TODO: Reset gameRenderer and menuRenderer.
			gameRenderer.SetLives(lives);
			gameRenderer.SetBombs(bombs);
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
            while (app.IsOpen())
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
                    keys.Update();
                    if (gameState == GameState.GamePlay)
                        UpdateGame(app, lastUpdate);
                    else if (gameState == GameState.MainMenu)
                        UpdateMenu(app, lastUpdate);
                    // TODO: Reset is bad logic.
                    // We should subtract the old time so no ticks are lost.
                    timer.Reset();
                }

                // Process events.
                // If we are done, exit the loop immediately.
                app.DispatchEvents();
            }
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
                Window app = (Window)sender;
                app.Close();
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
            // The comparisons to 2 accounts for how KEY_UP and KEY_DOWN change
            // the key hold-times to 1, but an immediately following Update()
            // makes those 1s into 2s.

            // The logic looks backwards for up/down, but it works.
            if (keys.up == 2)
            {
                menuChoice = menuChoice <= 0 ? MainMenu.EndChoices - 1 :
                    menuChoice - 1;
				menuRenderer.SetSelection(menuChoice);
                disallowRapidSelection = false;
            }
            if (keys.down == 2)
            {
                menuChoice = menuChoice > MainMenu.EndChoices - 2 ? 0 :
                    menuChoice + 1;
				menuRenderer.SetSelection(menuChoice);
                disallowRapidSelection = false;
            }
            // Disallow rapid selection while patch flag is on.
            // But allow non-rapid selection even if patch flag is on.
            // Rapid selection flag patch is disabled when any action is done
            //   within the menu.
            if (keys.shoot == 2)
            {
                if (disallowRapidSelection && keys.TappedShoot)
                    return;
                switch ((MainMenu)menuChoice)
                {
                    case MainMenu.Play:
                        gameState = GameState.GamePlay;
                        // Switch the delegate to painting the game.
                        paintHandler = new PaintHandler(PaintGame);
                        break;
                    case MainMenu.GodMode:
                        godMode = !godMode;
						menuRenderer.SetOptGodMode(godMode);
                        break;
                    case MainMenu.FunBomb:
                        funBomb = !funBomb;
						menuRenderer.SetOptFunBomb(funBomb);
                        break;
                    case MainMenu.Repulsive:
                        repulsive = !repulsive;
						menuRenderer.SetOptRepulsive(repulsive);
                        break;
					case MainMenu.Scale:
						if (appScale == 1.0)
							appScale = 2.0;
						else if (appScale == 2.0)
							appScale = 1.0;
						RenderWindow app = (RenderWindow)sender;
						app.Size = new Vector2u( (uint) (Renderer.APP_BASE_WIDTH * appScale),
							(uint) (Renderer.APP_BASE_HEIGHT * appScale) );
						menuRenderer.SetOptScale(appScale);
						break;
                    case MainMenu.Exit:
                        ((RenderWindow)sender).Close();
                        break;
                }
                disallowRapidSelection = false;
            }
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
        /// Updates the game.
        /// </summary>
        /// <param name="sender">The caller of this method.</param>
        /// <param name="ticks">The ticks since the last call to this.</param>
        public void UpdateGame(object sender, double ticks)
        {
            if (paused)
            {
                if (keys.TappedBomb)
                {
                    // End the game.
                    Reset();
                    gameState = GameState.MainMenu;
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
                            patternTime = 1 +
                                Enemy.patternDuration[boss.currentPattern];
                    }
                }
            }

            MovePlayer();

			if (godMode && keys.slow == 2 && !bombBlast.IsGone())
                bombBlast.Kill();
            if (gameOver)
            {
                if (keys.shoot == 2)
                {
                    this.Reset();
                    gameState = GameState.MainMenu;
                    paintHandler = new PaintHandler(PaintMenu);
                    return;
                }
            }
            else
            {
                if (keys.shoot > 0 && player.deathCountdown <= 0 &&
                    player.reentryCountdown <= 0)
                {
                    if (player.TryShoot())
                    {
                        // The color (index) doesn't matter.
                        Bullet bullet = new Bullet(4, new Vector2f(
                            player.Location.X - 4,
                            player.Location.Y),
                            VectorLogic.AngleToVector(VectorLogic.Radians(270)),
                            9.0);
                        bullet.Sprite = gameRenderer.GetCenterSprite(
                            "b_player");
                        playerBullets.Add(bullet);
                        bullet = new Bullet(bullet);
                        bullet.location.X = player.Location.X +
                            4;
                        bullet.Sprite = gameRenderer.GetCenterSprite(
                            "b_player");
                        playerBullets.Add(bullet);
                    }
                }
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
            foreach (Enemy enemy in enemies)
            {
                if (enemy.health <= 0)
                    // TODO: Make enemies go pop.
                    break;
                enemy.Update(ref enemyBullets, gameRenderer, player.Location, rand);
                if (player.invincibleCountdown <= 0 && !godMode &&
					lives >= 0 && Physics.Touches(player, enemy))
                {
                    player.invincibleCountdown = POST_DEATH_INVINC_FRAMES;
                    player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
                    beatThisPattern = false;
                }
            }

            if (bossState == BossState.Active)
            {
                if (player.invincibleCountdown <= 0 && !godMode &&
                    lives >= 0 && Physics.Touches(player, boss))
                {
                    player.invincibleCountdown = POST_DEATH_INVINC_FRAMES;
                    player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
                    beatThisPattern = false;
                }

                if (boss.health > 0)
                {
                    // Increment the time the boss is in its pattern.
                    boss.Update(ref enemyBullets, gameRenderer, player.Location, rand);
                }
                else
                {
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
						}
						else
						{
							// Activate the next pattern.
							patternTime =
								Enemy.patternDuration[boss.currentPattern];
							// Tell the renderer the new health value.
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
					// Tell the renderer the new health value.
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

            if (!bombBlast.IsGone())
            {
                bool bombMadeCombo = true;
                bombBlast.Update();
                bool finalFrame = bombBlast.IsGone();
                for (int i = 0; i < enemyBullets.Count; i++)
                {
                    if (Physics.Touches(bombBlast, (Bullet)enemyBullets[i]))
                    {
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
                            // The angle is small, so increase speed.
                            enemyBullet.Speed = Math.Min(4.0,
                                enemyBullet.Speed + 0.5);

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
                    // It is kind of close to the player. Check for collison.
                    if (Physics.Touches(player, bullet))
                    {
                        if (!bullet.grazed)
                        {
                            grazeCount++;
                            if (repulsive)
                                // Make the bullet point directly away from
                                // the player.
                                bullet.Direction =
                                    VectorLogic.GetDirectionVector(
                                    bullet.location, player.Location);
                            else
                            {
                                // Show feedback of a graze with a hitspark.
                                // The size index does not matter.
                                Bullet h = new Bullet(GRAZE_SPARK_INDEX,
                                    new Vector2f(bullet.location.X, bullet.location.Y),
                                    VectorLogic.GetDirectionVector(
                                        bullet.location, player.Location),
                                    5.0);
                                h.Sprite = gameRenderer.GetCenterSprite(
                                    "spark_graze");
								h.Lifetime = Bullet.LIFETIME_PARTICLE;
                                hitSparks.Add(h);
                                score += 50;
                            }
                            bullet.grazed = true;
                        }
                        if (!godMode && !repulsive &&
                            Physics.Touches(bullet, player.hitbox))
                        {
                            player.invincibleCountdown =
                                POST_DEATH_INVINC_FRAMES;
                            player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
                            beatThisPattern = false;
                        }
                    }
            }
            if (toRemove.Count > 1)
                toRemove.Sort(new ReversedSortInt());
            foreach (int i in toRemove)
                enemyBullets.RemoveAt(i);
            toRemove.Clear();

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
                    foreach (Enemy enemy in enemies)
                        if (Physics.Touches(enemy, bullet))
                        {
                            enemy.health -= 2;
                            toRemove.Add(i);
                            // Make 2 hitsparks to show that the enemy was hit.
							for (int k = 0; k < 2; k++)
							{
								// Makes a hitspark shoot downwards at an angle
								// between 210 and 330 degrees.
								Bullet h = new Bullet(BULLSEYE_SPARK_INDEX,
									new Vector2f(bullet.location.X,
										boss.Location.Y + boss.Size.Y),
									VectorLogic.AngleToVector(VectorLogic.Radians(
										60.0 + rand.NextDouble() * 60.0)),
									2.5);
								h.Sprite = gameRenderer.GetCenterSprite(
									"spark_nailed_foe");
								h.Lifetime = Bullet.LIFETIME_PARTICLE;
								hitSparks.Add(h);
							}
                            score += 20;
                            scoreUp = true;
                        }
                    if (bossState == BossState.Active && Physics.Touches(boss, bullet))
                    {
                        hitBoss = true;
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
                        }
                        toRemove.Add(i);
                        // Make 2 hitsparks to show that the enemy was hit.
                        for (int k = 0; k < 2; k++)
                        {
                            Bullet h = new Bullet(BULLSEYE_SPARK_INDEX,
                                new Vector2f(bullet.location.X,
                                    boss.Location.Y + boss.Size.Y),
                                VectorLogic.AngleToVector(VectorLogic.Radians(
                                    60.0 + rand.NextDouble() * 60.0)), 2.5);
                            h.Sprite = gameRenderer.GetCenterSprite(
								"spark_nailed_foe");
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
                // Tell the renderer that the boss' health changed.
                gameRenderer.SetBossHealth(boss.health);

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
				app.Draw(player.sprite);
            if (bossState == BossState.Active)
                app.Draw(boss.sprite);
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
				app.Draw(boss.sprite);
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

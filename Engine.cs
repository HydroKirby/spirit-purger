using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Audio;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
	public enum MainMenu
	{
		Play, GodMode, FunBomb, Repulsive, Exit,
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
//        public const int POST_DEATH_INVINC_FRAMES =
//            REENTRY_FRAMES + Player.DEATH_SEQUENCE_FRAMES + 40;
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
        protected ArrayList enemies = new ArrayList();
        protected ArrayList enemyBullets = new ArrayList();
        protected ArrayList playerBullets = new ArrayList();
        protected ArrayList hitSparks = new ArrayList();

        // Rendering variables.
        protected Renderer renderer;
		protected MenuRenderer menuRenderer;
        protected Vector2u appSize;

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
        protected double bombComboScore = 0;
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
            appSize = app.Size;
            renderer = new Renderer();
            renderer.bg = renderer.LoadImage("bg.png");
            renderer.playerImage = renderer.LoadImage("p_fly.png");
            // TODO: Deprecate need for player.SetImage(playerImage);
            renderer.bossImage = renderer.LoadImage("boss_fly.png");
            renderer.playerBulletImage = renderer.LoadImage("b_player.png");
            renderer.hitCircleImage = renderer.LoadImage("hitbox.png");
            renderer.grazeSparkImage = renderer.LoadImage("spark_graze.png");
            renderer.bullseyeSparkImage = renderer.LoadImage("spark_nailed_foe.png");
			menuRenderer = new MenuRenderer();

            // Prepare the game to be run.
            Reset();
            // Disable the patch because the annoyance is only when going from
            //   the game to the menu - not during startup.
            disallowRapidSelection = false;
            gameState = GameState.MainMenu;
            paintHandler = new PaintHandler(PaintMenu);

            MainLoop(app);
        }

        /// <summary>
        /// Sets the game into a before-main-gameplay state.
        /// Use during the game start and after a Game Over.
        /// </summary>
        protected void Reset()
        {
            player.location = new Vector2f(appSize.X / 2,
                appSize.Y / 4 * 3);
            player.deathCountdown = 0;
            player.reentryCountdown = 0;
            player.invincibleCountdown = 0;
            boss.location = new Vector2f(appSize.X / 2,
                appSize.Y / 4);
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
            bombBlast = null;
            bombCombo = 0;
            bombComboTimeCountdown = 0;
            bombComboScore = 0;
            grazeCount = 0;
            lives = 2;
            bombs = 3;
            score = 0;
            /*
            this.labelBombs.Visible = false;
            this.labelBombs.Text = "☆☆☆";
            this.labelLives.Visible = false;
            this.labelLives.Text = "◎◎";
            this.labelScore.Visible = false;
            this.labelScore.Text = "0";
            this.labelPaused.Visible = false;
            this.labelPausedToEnd.Visible = false;
            this.labelPausedToPlay.Visible = false;
             */
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
                    case MainMenu.Exit:
                        ((RenderWindow)sender).Close();
                        break;
                }
                disallowRapidSelection = false;
            }
        }


        /* --- Game Logic Code --- */
        
        
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

            /*
            if (patternTime > 0 && transitionFrames <= 0)
            {
                prevSecondUpdateFraction += ticks;
                if (prevSecondUpdateFraction >= 1.0)
                {
                    patternTime--;
                    prevSecondUpdateFraction -= 1.0;
                    if (patternTime == 0)
                    {
                        beatThisPattern = false;
                        if (!boss.NextPattern())
                        {
                            bossState = BossState.Killed;
                            gameOver = true;
                        }
                        else
                            patternTime =
                                Enemy.patternDuration[boss.currentPattern];
                    }
                }
            }

            MovePlayer();

            if (godMode && keys.slow == 2 && bombBlast != null)
                bombBlast = null;
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
                        Bullet bullet = new Bullet(0, 1, new Vector2D(
                            player.location.X - Bullet.RADII[1],
                            player.DrawLocation.Y),
                            new Vector2D(Vector2D.DegreesToRadians(270)),
                            9.0);
                        playerBullets.Add(bullet);
                        bullet = new Bullet(bullet);
                        bullet.location.X = player.location.X +
                            Bullet.RADII[1];
                        playerBullets.Add(bullet);
                    }
                }
                if (keys.bomb == 2 && bombBlast == null && (godMode ||
                    bombs > 0 && player.reentryCountdown <= 0 &&
                    player.deathCountdown <= 0))
                {
                    player.invincibleCountdown = Bomb.ACTIVE_FRAMES;
                    bombBlast = new Bomb(player.location);
                    bombCombo = 0;
                    bombComboTimeCountdown = BOMB_COMBO_DISPLAY_FRAMES;
                    bombs--;
                    beatThisPattern = false;
                    String leftoverBombs = "";
                    for (int i = 0; i < bombs; i++)
                        leftoverBombs += "☆";
                    this.labelBombs.Text = leftoverBombs;
                }
            }

            if (bombComboTimeCountdown >= 0)
                bombComboTimeCountdown--;

            UpdateBullets();
            UpdateEnemies();
            player.Update();
             */
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
            /*
            e.Graphics.DrawImage(bg, 0, 0);
            // Draw the player, boss, and enemies.
            if (lives >= 0)
                player.Draw(e.Graphics);
            if (bossState == BossState.Active)
                boss.Draw(e.Graphics);
            else if (bossState == BossState.Intro &&
                     bossIntroTime > BOSS_PRE_INTRO_FRAMES)
                // The boss is invisible unless it has waited more than
                // BOSS_PRE_INTRO_FRAMES at which it then fades-in gradually.
                // The visibility is the proprtion of time waited compared
                // to BOSS_INTRO_FRAMES.
                boss.Draw(e.Graphics,
                    (int)((bossIntroTime - BOSS_PRE_INTRO_FRAMES) /
                    (float)BOSS_INTRO_FRAMES * 255.0F));

            // Draw the bullets and hitsparks.
            foreach (Bullet spark in hitSparks)
                if (spark.colorIndex == GRAZE_SPARK_INDEX)
                    e.Graphics.DrawImage(grazeSparkImage, spark.DrawLocation);
                else
                    e.Graphics.DrawImage(bullseyeSparkImage,
                                         spark.DrawLocation);
            foreach (Bullet bullet in playerBullets)
                e.Graphics.DrawImage(playerBulletImage, bullet.DrawLocation);
            foreach (Bullet bullet in enemyBullets)
                e.Graphics.DrawImage(
                    bulletImages[bullet.SizeIndex][bullet.colorIndex],
                    bullet.DrawLocation);

            if (bombBlast != null)
                bombBlast.Draw(e.Graphics);

            if (keys.slow > 0)
                e.Graphics.DrawImage(hitCircleImage,
                    (int)((player.location.X - hitCircleImage.Width * 0.5)),
                    (int)((player.location.Y - hitCircleImage.Height * 0.5)));

            // Draw the HUD.
            SolidBrush solidBrush = new SolidBrush(Color.Black);
            // TODO: Fade-out when close-by.
            e.Graphics.DrawString("Bullets:" +
                (playerBullets.Count + enemyBullets.Count).ToString(),
                Font, solidBrush, 0, this.ClientRectangle.Bottom - 14);
            e.Graphics.DrawString("Grazed: " + grazeCount.ToString(), Font,
                solidBrush, 0, this.ClientRectangle.Bottom - 25);
            solidBrush.Dispose();

            // Draw the boss' health bar.
            if (!gameOver)
            {
                solidBrush = new SolidBrush(Color.Red);
                e.Graphics.FillRectangle(solidBrush, 10, 28,
                    (float)boss.health /
                    Enemy.fullHealth[boss.currentPattern] *
                    (this.ClientRectangle.Width - 20), 13);
                solidBrush.Dispose();
            }

            // Draw the boss pattern time.
            if (patternTime > 0)
            {
                solidBrush = new SolidBrush(Color.Red);
                e.Graphics.DrawString(patternTime.ToString(), Font,
                    solidBrush, 265.0F, 1.0F);
                solidBrush.Dispose();
            }

            // Draw the bomb combo.
            if (bombCombo > 0 && bombComboTimeCountdown >= 0)
            {
                solidBrush = new SolidBrush(Color.Firebrick);
                e.Graphics.DrawString(String.Format("{0} Combo!", bombCombo),
                    Font, solidBrush, 15.0F, 45.0F);
                e.Graphics.DrawString(String.Format("{0}", bombComboScore),
                    Font, solidBrush, 19.0F, 57.0F);
                solidBrush.Dispose();
            }

            // Show the end-pattern result.
            if (transitionFrames > 0 && !gameOver)
            {
                Font bigFont = new Font("Courier", 16, FontStyle.Bold);
                solidBrush = new SolidBrush(Color.RoyalBlue);
                e.Graphics.DrawString(beatThisPattern ? "Pattern Success!" :
                    "Survival Failure...", bigFont, solidBrush, 60.0F, 50.0F);
                e.Graphics.DrawString(beatThisPattern ? "30,000" : "5,000",
                    bigFont, solidBrush, 100.0F, 70.0F);
                solidBrush.Dispose();
            }

            // Show Game Over Result.
            if (gameOver)
            {
                Font bigFont = new Font("Courier", 16, FontStyle.Bold);
                solidBrush = new SolidBrush(Color.Crimson);
                e.Graphics.DrawString("Game Over", bigFont, solidBrush,
                    90.0F, 70.0F);
                e.Graphics.DrawString("Please Press Shoot", Font, solidBrush,
                    110.0F, 95.0F);
                solidBrush.Dispose();
            }
             */
        }
    }
}

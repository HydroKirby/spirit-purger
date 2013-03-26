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
        protected delegate void PaintHandler(object sender);
        protected PaintHandler paintHandler = null;
        public KeyHandler keys = new KeyHandler();
        public Random rand = new Random();
        public enum MainMenu
        {
            Play, GodMode, FunBomb, Repulsive, Exit,
            EndChoices
        };
        protected int menuChoice = 0;
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
//        protected Enemy boss = new Enemy();
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
//        protected Player player = new Player();
        protected ArrayList enemies = new ArrayList();
        protected ArrayList enemyBullets = new ArrayList();
        protected ArrayList playerBullets = new ArrayList();
        protected ArrayList hitSparks = new ArrayList();

        // Game images.
        // TODO: Move to resource handling class?
        // This variable holds the images shared between all bullets. It's
        //   organized by [color_code][size_index].
        protected Image[][] bulletImages;
        protected Image playerBulletImage;
        // This goes to the spark that flies out of the player when a
        // bullet passes by the player's hitbox.
        protected Image grazeSparkImage;
        // This goes to the spark the flies out when an enemy is shot.
        protected Image bullseyeSparkImage;
        // This is displayed when the player slows down; it's an identifier.
        protected Image hitCircleImage;
        protected Image bg;

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
//        protected Bomb bombBlast;
        protected long score = 0;
        protected short lives = 2;
        protected short bombs = 3;

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
            // Load all resources.
            bg = LoadImage("bg.png");
            // Load a sprite to display
            //            Image image = new Image("cute_image.jpg");
            //            Sprite sprite = new Sprite(image);

            // Create a graphical string to display
            Font arial = new Font("arial.ttf");
            Text text = new Text("Hello SFML.Net", arial);

            // Prepare the game to be run.
            Reset();
            // Disable the patch because the annoyance is only when going from
            //   the game to the menu - not during startup.
            disallowRapidSelection = false;
            MakeBulletImages();
            gameState = GameState.MainMenu;
            paintHandler = new PaintHandler(PaintMenu);

            MainLoop(app);
        }

        /// <summary>
        /// Makes all bullet images for the first time.
        /// All images are put into the bulletImages array.
        /// </summary>
        protected void MakeBulletImages()
        {
            bulletImages = new Image[Bullet.RADII.Length][];
            for (int atSize = 0; atSize < Bullet.RADII.Length; atSize++)
            {
                bulletImages[atSize] =
                    new Image[(int)Bullet.BulletColors.EndColors];
                for (int color = 0; color < bulletImages[atSize].Length; color++)
                {
                    int radius = Bullet.RADII[atSize];
                    bulletImages[atSize][color] = LoadImage("b_" +
                        (radius + radius).ToString() + "x" + (radius + radius).ToString() +
                        Bullet.GetColorByName(color) + ".png");
                }
            }
        }

        /// <summary>
        /// Sets the game into a before-main-gameplay state.
        /// Use during the game start and after a Game Over.
        /// </summary>
        protected void Reset()
        {
        }

        /// <summary>
        /// The game's infinite loop. In a timely manner it updates data,
        /// forces a repaint, and gives outside programs a chance to funnel
        /// events. While waiting for the next update cycle, the loop gives
        /// sleeps.
        /// </summary>
        protected void MainLoop(RenderWindow app)
        {
            /*
            Timer timer = new Timer();
            double lastUpdate = timer.GetTicks();
            while (app.IsOpen())
            {
                keys.Update();
                if (gameState == GameState.GamePlay)
                {
                    this.UpdateGame(lastUpdate);
                    this.Invalidate();
                }
                else if (gameState == GameState.MainMenu)
                    this.UpdateMenu();

                Application.DoEvents();
                while ((lastUpdate = timer.GetTicks()) <= UPDATE_TICKS)
                    Thread.Sleep(1);
                timer.Reset();
            }
             */
            Timer timer = new Timer();
            double lastUpdate = timer.GetTicks();
            app.SetKeyRepeatEnabled(false);
            app.KeyPressed += new EventHandler<KeyEventArgs>(app_KeyPressed);
            app.KeyReleased += new EventHandler<KeyEventArgs>(app_KeyReleased);
            Font font = new Font("arial.ttf");
            Text text = new Text("test", font);
            // Start the game loop
//            double temp1 = 0;
            while (app.IsOpen())
            {
                // Process events
                app.DispatchEvents();

                // If it is time for a game update, update all gameplay.
                if ((lastUpdate = timer.GetTicks()) >= UPDATE_TICKS)
                {
                    // DispatchEvents made all key states up-to-date.
                    // Now, increment the time they were held down.
                    keys.Update();
//                    temp1 = Math.Max(temp1, keys.left);
                    text.DisplayedString = keys.left.ToString();
//                    text.DisplayedString = temp1.ToString();
                    timer.Reset();
                }

                // Begin rendering.
                // Clear screen
                app.Clear();

                // Draw the sprite
                //app.Draw(sprite);

                // Draw the string
                app.Draw(text);

                // Update the window
                app.Display();
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

        protected void PaintMenu(object sender)
        {
            // Note: The app window is 290x290.
            /*
            float y = 130.0F + 15.0F * menuChoice;
            PointF[] cursorPts = {
				new PointF(136.0F, y),
				new PointF(142.0F, y + 4.0F),
				new PointF(136.0F, y + 8.0F)
			};

            // Draw the menu items.
            SolidBrush solidBrush = new SolidBrush(Color.Blue);
            // The position of the menu items is based on their enum value.
            e.Graphics.DrawString("Start", Font, solidBrush, 145.0F,
                130.0F + (int)MainMenu.Play * 15.0F);
            e.Graphics.DrawString("God Mode" + (godMode ? " *" : ""),
                Font, solidBrush, 145.0F,
                130.0F + (int)MainMenu.GodMode * 15.0F);
            e.Graphics.DrawString("Fun Bomb" + (funBomb ? " *" : ""), Font,
                solidBrush, 145.0F, 130.0F + (int)MainMenu.FunBomb * 15.0F);
            e.Graphics.DrawString("Repulsive" + (repulsive ? " *" : ""),
                Font, solidBrush, 145.0F,
                130.0F + (int)MainMenu.Repulsive * 15.0F);
            e.Graphics.DrawString("Exit", Font, solidBrush, 145.0F,
                130.0F + (int)MainMenu.Exit * 15.0F);
            solidBrush.Dispose();

            // Draw the cursor.
            solidBrush = new SolidBrush(Color.Red);
            e.Graphics.FillPolygon(solidBrush, cursorPts);
            solidBrush.Dispose();
             */
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;

namespace SpiritPurger
{
	/// <summary>
	/// Makes the meaning of a timer be related to the Player's inputs.
	/// </summary>
	public class PlayerDuty : TimerDuty
	{
		public new enum DUTY
		{
			NONE,
            // The revival flash is appearing. The player can't act and is not shown.
            REVIVAL_FLASH_SHOWING,
            // The revival flash is disappearing. The player can't act, but is shown.
            REVIVAL_FLASH_LEAVING,
            // Invinciblity time after revival.
            INVINCIBLE,
		}

        private DUTY _duty;
        public override void SetDuty(object duty) { _duty = (DUTY)duty; }
        public override object GetDuty() { return _duty; }

		public PlayerDuty() { }

		public override double GetTime()
		{
			// Interpret Purpose as the local variant of DUTY in this class.
            return GetTime((int)GetDuty());
		}

        public static new double GetTime(int purpose)
        {
            switch ((DUTY)purpose)
            {
                case DUTY.REVIVAL_FLASH_SHOWING: return 0.3;
                case DUTY.REVIVAL_FLASH_LEAVING: return 0.3;
                case DUTY.INVINCIBLE: return 2.0;
                default: return 0;
            }
        }
	}

	/// <summary>
	/// Makes the meaning of a timer be related to the gameplay.
	/// </summary>
	public class GameDuty : TimerDuty
	{
		public new enum DUTY
		{
			NONE,
			FADE_IN_FROM_MENU,
			FADE_OUT_TO_MENU,
			// During pausing, hold down the Bomb button to go
			// back to the menu.
			PAUSE_BOMB_HELD,
			// Upon completing the game, how long the player can do
			// nothing except move around. (No shooting). Meant to
			// pause so that the final score can be shown.
			WAIT_AFTER_BEAT_GAME,
		}

        private DUTY _duty;
        public override void SetDuty(object duty) { _duty = (DUTY)duty; }
        public override object GetDuty() { return _duty; }

		public GameDuty() { }

		public override double GetTime()
		{
			// Interpret Purpose as the local variant of DUTY in this class.
            return GetTime((int)GetDuty());
		}

        public static new double GetTime(int purpose)
        {
            switch ((DUTY)purpose)
            {
                case DUTY.FADE_IN_FROM_MENU: return 0.4;
                case DUTY.FADE_OUT_TO_MENU: return 0.4;
                case DUTY.PAUSE_BOMB_HELD: return 1.0;
                case DUTY.WAIT_AFTER_BEAT_GAME: return 4.0;
                default: return 0;
            }
        }
	}

	public class GameplayManager : Subject
	{
		// After dying, this is how long the player automatically moves back
		// up to the playfield.
		public const int POST_DEATH_INVINC_FRAMES =
			50 + Player.DEATH_SEQUENCE_FRAMES + 40;
		// Refers to when a boss pattern has completed. It's the time to wait
		// until initiating the next pattern.
		public const int PATTERN_TRANSITION_PAUSE = 80;
		// The time to wait before the boss begins the intro sequence.
		public const int BOSS_PRE_INTRO_FRAMES = 20;
		// The time to wait during the boss' fade-in sequence.
		public const int BOSS_INTRO_FRAMES = 50;
		// The time to show the bomb combo score after a bomb wears off.
		public const int BOMB_COMBO_DISPLAY_FRAMES = 270;

		// How to react to updates in the gameplay.
		// These REACTIONs are informed back to the Engine so that the
		// Engine can handle the complicated operations.
		public enum REACTION
		{
			// No particular action to take.
			NONE,
			// Fade into the gameplay from the menu screen. Disallow inputs.
			FADE_FROM_MENU,
			// Fade out of the gameplay to the menu screen. Disallow inputs.
			FADE_TO_MENU,
			FADE_COMPLETE,
			// Tell the renderer to refresh something.
			REFRESH_SCORE, REFRESH_BULLET_COUNT, REFRESH_LIVES, REFRESH_BOMBS,
            // Tell the renderer that the player's invincibility status has changed.
            PLAYER_GOT_INV, PLAYER_LOST_INV,
			// Bomb-related reactions.
			BOMB_MADE_COMBO, BOMB_LIFETIME_DECREMENT,
			// Boss-related reactions.
			BOSS_TOOK_DAMAGE, BOSS_REFRESH_MAX_HEALTH, BOSS_PATTERN_SUCCESS, BOSS_PATTERN_FAIL,
				BOSS_REFRESH_PATTERN_TIME, BOSS_PATTERN_TIMEOUT,
			// Game-clearing-related reactions.
			LOST_ALL_LIVES, COMPLETED_GAME, RESET_GAME,
			// End of list of REACTIONS.
			END_REACTIONS
		}

		private REACTION _state;
		public REACTION State { get { return _state; } }

		// High level governing variables.
		// soundManager and keys are references to instances in Engine.
		protected SoundManager soundManager;
		public KeyHandler keys;
		public Random rand = new Random();
		public bool godMode = false;
		public bool funBomb = false;
		public bool repulsive = false;
		// A general timer that effects any inputs given from the player.
		// For example, the time when the player cannot act while respawning.
		public DownTimer PlayerTimer
		{
			get;
			protected set;
		}
		// A general timer that controls the entire game.
		public DownTimer GameTimer
		{
			get;
			protected set;
		}

		// Game state variables.
		public bool gameOver = false;
		// Gives points when true. Becomes false if the players dies or bombs.
		public bool beatThisPattern = true;
		private bool _paused;
		public bool Paused
		{
			get { return _paused; }
			set
			{
				if (!GameTimer.TimeIsUp() && (GameTimer.SamePurpose(
					GameDuty.DUTY.FADE_OUT_TO_MENU) ||
					GameTimer.SamePurpose(
					GameDuty.DUTY.FADE_IN_FROM_MENU)))
				{
					// Input is not accepted right now,
					// so changing Paused in not possible.
					return;
				}
				_paused = value;
				if (_paused)
					GameTimer.Repurporse(
						(int)GameDuty.DUTY.PAUSE_BOMB_HELD);
				else
					GameTimer.Repurporse(
						(int)GameDuty.DUTY.NONE);
			}
		}
		public Player player;
		// Stores all base types of bullets. Keep in memory.
		protected ArrayList bulletTypes = new ArrayList();
		protected ArrayList enemies = new ArrayList();
		public ArrayList enemyBullets = new ArrayList();
		public ArrayList playerBullets = new ArrayList();
		public ArrayList hitSparks = new ArrayList();
        public RevivalFlash revivalFlash;
        // This is a temporary hack until BulletManager can allow someone
        // to access bullet type IDs and BulletManager could hold the
        // sprite itself.
        protected Texture revivalFlashImage;
        public CenterSprite revivalFlashSprite;

		// Boss-related variables.
		public enum BossState { NotArrived, Intro, Active, Killed };
		public Boss boss;
		protected BossState bossState = BossState.Intro;
		// How long the boss has been doing the intro sequence.
		public int bossIntroTime;

		// Current-game variables.
		protected BulletCreator bulletCreator;
		// This is the number of seconds left to complete a pattern.
		public int patternTime;
		// During a timed boss pattern, this is the milliseconds accumulated
		//   between each passing second.
		public double prevSecondUpdateFraction;
		// The amount of frames waited between boss pattern transitions.
		public int transitionFrames;
		// The number of times a bullet touched the player's hitbox.
		public int grazeCount;
		// The number of bullets removed by the current bomb.
		public int bombCombo;
		// The timer for how long the combo has been displayed.
		public int bombComboTimeCountdown;
		// The accumulated score of the current bomb combo.
		public int bombComboScore;
		public Bomb bombBlast;
        public long Score
        {
            get;
            protected set;
        }
        public short Lives
        {
            get;
            protected set;
        }
        public short Bombs
        {
            get;
            protected set;
        }
        public bool IsInFocusedMovement
        {
            get;
            protected set;
        }

		public GameplayManager(ImageManager imageManager, SoundManager sndManager,
			KeyHandler keyHandler, Dictionary<string, object> settings)
		{
			soundManager = sndManager;
			keys = keyHandler;
			bulletCreator = new BulletCreator(imageManager);
			PlayerTimer = new DownTimer(new PlayerDuty());
			GameTimer = new DownTimer(new GameDuty());

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
            imageManager.LoadPNG("reentry");
            revivalFlashImage = imageManager.GetImage("reentry");
            revivalFlashSprite = new CenterSprite(revivalFlashImage);
            revivalFlash = new RevivalFlash(revivalFlashSprite,
                new Vector2f(player.Location.X, player.Location.Y));
		}

		private void ChangeState(REACTION newState)
		{
			_state = newState;
			Notify();
		}

		/// <summary>
		/// Tells GameplayManager that something has handled this manager's REACTION request.
		/// </summary>
		public void StateHandled()
		{
			_state = REACTION.NONE;
		}

		public void Reset()
		{
			player.UpdateDisplayPos();
			player.deathCountdown = 0;
			player.invincibleCountdown = 0;
			boss.Location = new Vector2f(Renderer.FIELD_WIDTH / 2,
				Renderer.FIELD_HEIGHT / 4);
			boss.UpdateDisplayPos();
			boss.currentPattern = -1;
			boss.NextPattern();
			patternTime = Boss.patternDuration[boss.currentPattern];
			bossState = BossState.Intro;
			playerBullets.Clear();
			enemyBullets.Clear();
			hitSparks.Clear();
			enemies.Clear();
			beatThisPattern = true;
			gameOver = false;
			Paused = false;
			prevSecondUpdateFraction = 0.0;
			transitionFrames = 0;
			bossIntroTime = 0;
			bombBlast.Kill();
			bombCombo = 0;
			bombComboTimeCountdown = 0;
			bombComboScore = 0;
			grazeCount = 0;
			Score = 0;
			Lives = 2;
			Bombs = 3;
            IsInFocusedMovement = false;
            StartRevival();
			GameTimer.Repurporse((int)GameDuty.DUTY.NONE);
		}

		/// <summary>
		/// Makes the game fade into existence.
		/// </summary>
		public void StartGame()
		{
			ChangeState(REACTION.FADE_FROM_MENU);
			GameTimer.Repurporse((int)GameDuty.DUTY.FADE_IN_FROM_MENU);
		}

		public void MovePlayer()
		{
			// Act on states that disable player movement first.
            IsInFocusedMovement = false;
            if (player.deathCountdown > 0)
            {
                if (player.deathCountdown == 1)
                {
                    Lives--;
                    if (Lives < 0)
                    {
                        gameOver = true;
                        ChangeState(REACTION.LOST_ALL_LIVES);
                    }
                    else
                    {
                        ChangeState(REACTION.REFRESH_LIVES);
                        StartRevival();
                    }
                }
                else
                    return;
            }
            if (Lives < 0)
				return;
            if (PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_SHOWING))
            {
                // The revival flash is coming into existence.
                // The player is not yet shown.
                if (PlayerTimer.TimeIsUp())
                {
                    PlayerTimer.Repurporse(
                        (int)PlayerDuty.DUTY.REVIVAL_FLASH_LEAVING);
                    // Show the player as invincible.
                    ChangeState(REACTION.PLAYER_GOT_INV);
                }
                else
                {
                    // Animate the revival flash.
                    float scale = (float)(LerpLogic.SlowAccel(PlayerTimer.Frame,
                        PlayerDuty.GetTime(
                        (int)PlayerDuty.DUTY.REVIVAL_FLASH_SHOWING),
                        0, 1));
                    revivalFlashSprite.Scale = new Vector2f(scale, scale);
                    return;
                }
            }
            else if (PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_LEAVING))
            {
                // 
                if (PlayerTimer.TimeIsUp())
                {
                    PlayerTimer.Repurporse(
                        (int)PlayerDuty.DUTY.INVINCIBLE);
                }
                else
                {
                    // Animate the revival flash.
                    float scale = (float)(1.0 - LerpLogic.SlowDecel(PlayerTimer.Frame,
                        PlayerDuty.GetTime(
                        (int)PlayerDuty.DUTY.REVIVAL_FLASH_LEAVING),
                        0, 1));
                    revivalFlashSprite.Scale = new Vector2f(scale, scale);
                    return;
                }
            }
            else if (PlayerTimer.SamePurpose(PlayerDuty.DUTY.INVINCIBLE) &&
                PlayerTimer.TimeIsUp())
            {
                PlayerTimer.Repurporse((int)PlayerDuty.DUTY.NONE);
                ChangeState(REACTION.PLAYER_LOST_INV);
            }

            IsInFocusedMovement = keys.slow > 0;
			if (keys.Horizontal() != 0)
			{
				if (keys.left > 0)
				{
                    player.Move(IsInFocusedMovement || !bombBlast.IsGone() ?
						-Player.LO_SPEED : -Player.HI_SPEED, 0);
					if (player.Location.X - player.HalfSize.X < 0)
						player.Location = new Vector2f(player.HalfSize.X, player.Location.Y);
				}
				else
				{
                    player.Move(IsInFocusedMovement || !bombBlast.IsGone() ?
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
                    player.Move(0, IsInFocusedMovement || !bombBlast.IsGone() ?
						-Player.LO_SPEED : -Player.HI_SPEED);
					if (player.Location.Y - player.HalfSize.Y < 0)
						player.Location = new Vector2f(player.Location.X, player.HalfSize.Y);
				}
				else
				{
                    player.Move(0, IsInFocusedMovement || !bombBlast.IsGone() ?
						Player.LO_SPEED : Player.HI_SPEED);
					if (player.Location.Y + player.HalfSize.Y > Renderer.FIELD_HEIGHT)
						player.Location = new Vector2f(player.Location.X,
							Renderer.FIELD_HEIGHT - player.HalfSize.Y);
				}
				player.UpdateDisplayPos();
			}
		}

        /// <summary>
        /// Begins the sequence of events that causes the revival of the player.
        /// </summary>
        protected void StartRevival()
        {
            PlayerTimer.Repurporse((int)PlayerDuty.
                DUTY.REVIVAL_FLASH_SHOWING);
            // Recall that the player's position is already offsetted
            // by the gameplay frame.
            player.Location = new Vector2f(Renderer.FIELD_WIDTH / 2,
                Renderer.FIELD_HEIGHT / 6 * 5);
            player.UpdateDisplayPos();

            // Create the revival effect's "bullet".
            revivalFlash.location = player.Location;
            // Set the revival flash to be the same position as the player,
            // but it must be offsetted to be within the gameplay frame.
            revivalFlashSprite.setPosition(new Vector2f(
                player.Location.X + Renderer.FIELD_LEFT,
                player.Location.Y + Renderer.FIELD_TOP));
            revivalFlashSprite.Scale = new Vector2f(0, 0);
            // Make sure the revival flash is not removed before the event ends.
            // Add a little time before the bullet itself is killed.
            revivalFlash.Lifetime = (uint) (PlayerTimer.Frame + 1.0);
        }

		public void UpdateEnemies()
		{
			List<BulletProp> newBullets;

			// Update all enemies.
			foreach (Boss enemy in enemies)
			{
				if (enemy.health <= 0)
				{
					soundManager.QueueToPlay(SoundManager.SFX.FOE_DESTROYED);
					break;
				}

				enemy.Update(out newBullets, player.Location, rand);
				// newBullets was filled in, so make bullets instances.
				if (newBullets.Count > 0)
				{
					soundManager.QueueToPlay(SoundManager.SFX.FOE_SHOT_BULLET);
					foreach (BulletProp b in newBullets)
						enemyBullets.Add(bulletCreator.MakeBullet(b));
					newBullets.Clear();
				}

				if (player.invincibleCountdown <= 0 && !godMode &&
					Lives >= 0 && Physics.Touches(player, enemy))
				{
					// The player took damage.
					player.invincibleCountdown = GameplayManager.POST_DEATH_INVINC_FRAMES;
					player.deathCountdown = Player.DEATH_SEQUENCE_FRAMES;
					beatThisPattern = false;
					soundManager.QueueToPlay(SoundManager.SFX.PLAYER_TOOK_DAMAGE);
				}
			}
		}

		protected void UpdateBoss()
		{
			List<BulletProp> newBullets;

			if (bossState == BossState.Active)
			{
				if (player.invincibleCountdown <= 0 && !godMode &&
					Lives >= 0 && Physics.Touches(player, boss))
				{
					// The player took damage.
					player.invincibleCountdown = GameplayManager.POST_DEATH_INVINC_FRAMES;
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
					if (transitionFrames >= GameplayManager.PATTERN_TRANSITION_PAUSE)
					{
						// Reset the transition frames for the next time.
						transitionFrames = 0;
						beatThisPattern = true;
						if (!boss.NextPattern())
						{
							bossState = BossState.Killed;
							gameOver = true;
							soundManager.QueueToPlay(SoundManager.SFX.BOSS_DESTROYED);
							ChangeState(REACTION.COMPLETED_GAME);
							GameTimer.Purpose.SetDuty(GameDuty.
								DUTY.WAIT_AFTER_BEAT_GAME);
						}
						else
						{
							// Activate the next pattern.
							patternTime =
								Boss.patternDuration[boss.currentPattern];
							ChangeState(REACTION.BOSS_REFRESH_MAX_HEALTH);
						}
					}
				}
			}
			else if (bossState == BossState.Intro)
			{
				bossIntroTime++;
				if (bossIntroTime > GameplayManager.BOSS_INTRO_FRAMES +
					GameplayManager.BOSS_PRE_INTRO_FRAMES)
				{
					// The boss is no longer in the intro sequence.
					bossState = BossState.Active;
					ChangeState(REACTION.BOSS_REFRESH_MAX_HEALTH);
				}
			}
		}

		protected void BossPatternEnded(bool success)
		{
			enemyBullets.Clear();
			long addScore = success ? 30000 : 5000;
			Score += addScore;
			if (success)
			{
				ChangeState(REACTION.BOSS_PATTERN_SUCCESS);
				soundManager.QueueToPlay(SoundManager.SFX.FANFARE_PATTERN_SUCCESS);
			}
			else
			{
				ChangeState(REACTION.BOSS_PATTERN_FAIL);
				soundManager.QueueToPlay(SoundManager.SFX.FANFARE_PATTERN_FAILURE);
			}
			ChangeState(REACTION.BOSS_REFRESH_MAX_HEALTH);
		}

		protected void UpdateBomb(ArrayList toRemove)
		{
			if (bombBlast.IsGone())
				return;

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
						Score += 5;
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
						Score += 5;
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
				ChangeState(REACTION.BOMB_MADE_COMBO);

			if (finalFrame)
			{
				// Disable the bomb.
				bombBlast.Kill();
				// Give the player a little leeway after the blast.
				player.invincibleCountdown = 20;
			}

			ChangeState(REACTION.REFRESH_SCORE);
			if (toRemove.Count > 1)
				toRemove.Sort(new ReversedSortInt());
			foreach (int i in toRemove)
				enemyBullets.RemoveAt(i);
			toRemove.Clear();
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
			UpdateBomb(toRemove);

			// Do enemy bullet logic. They can cause the player to graze or die.
			for (int i = 0; i < enemyBullets.Count; i++)
			{
				Bullet bullet = (Bullet)enemyBullets[i];
				bullet.Update();
				if (bullet.IsOutside(Renderer.FieldSize, 30))
					// Build a list of "dead" bullets.
					toRemove.Add(i);
				else if (player.invincibleCountdown <= 0 && Lives >= 0 &&
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
								Score += 50;
							}
							bullet.grazed = true;
						}

						if (!godMode && !repulsive &&
							Physics.Touches(bullet, player.hitbox))
						{
							// The player was hit by a bullet.
							player.invincibleCountdown =
								GameplayManager.POST_DEATH_INVINC_FRAMES;
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
							Score += 20;
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
							BossPatternEnded(beatThisPattern);
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
						Score += 20;
						scoreUp = true;
					}
					if (scoreUp)
						ChangeState(REACTION.REFRESH_SCORE);
				}
			}

			if (hitBoss)
				ChangeState(REACTION.BOSS_TOOK_DAMAGE);

			if (toRemove.Count > 1)
				toRemove.Sort(new ReversedSortInt());
			foreach (int i in toRemove)
				playerBullets.RemoveAt(i);
			ChangeState(REACTION.REFRESH_BULLET_COUNT);
		}

		protected void ShootPlayerBullet()
		{
			if (keys.shoot > 0 && player.deathCountdown <= 0 &&
                (PlayerTimer.TimeIsUp() || !(PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_LEAVING) ||
                PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_SHOWING))
                ))
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
		}

		protected void ShootPlayerBomb()
		{
			if (keys.bomb == 2 && bombBlast.IsGone() && (godMode ||
                Bombs > 0 && (PlayerTimer.TimeIsUp() || !(PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_LEAVING) ||
                PlayerTimer.SamePurpose(
                PlayerDuty.DUTY.REVIVAL_FLASH_SHOWING))
                ) &&
                player.deathCountdown <= 0))
			{
				// Fire a bomb.
				player.invincibleCountdown = Bomb.LIFETIME_ACTIVE;
				bombBlast.Renew(player.Location);
				bombCombo = 0;
				bombComboTimeCountdown = GameplayManager.BOMB_COMBO_DISPLAY_FRAMES;
				Bombs--;
				beatThisPattern = false;
				ChangeState(REACTION.REFRESH_BOMBS);
				soundManager.QueueToPlay(SoundManager.SFX.PLAYER_SHOT_BOMB);
			}
		}

		protected void UpdateBossPatternTime(double ticks)
		{
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
					ChangeState(REACTION.BOSS_REFRESH_PATTERN_TIME);
					if (patternTime == 0)
					{
						// Ran out of time trying to beat the pattern.
						BossPatternEnded(false);
						beatThisPattern = true;
						if (!boss.NextPattern())
						{
							bossState = BossState.Killed;
							gameOver = true;
							GameTimer.Repurporse(
								(int)GameDuty.DUTY.WAIT_AFTER_BEAT_GAME);
						}
						else
						{
							patternTime = 1 +
								Boss.patternDuration[boss.currentPattern];
							ChangeState(REACTION.BOSS_PATTERN_TIMEOUT);
						}
					}
				}
			}
		}

		public void NextFrame(object sender, double ticks)
		{
            // The GameTimer affects the entire game, so tick it first.
			GameTimer.Tick(ticks);
			if (GameTimer.SamePurpose(
				GameDuty.DUTY.FADE_IN_FROM_MENU))
			{
				if (!GameTimer.TimeIsUp())
					return;
				GameTimer.Repurporse((int)GameDuty.DUTY.NONE);
				ChangeState(REACTION.FADE_COMPLETE);
			}
			else if (GameTimer.SamePurpose(
				GameDuty.DUTY.FADE_OUT_TO_MENU))
			{
				if (!GameTimer.TimeIsUp())
					return;
				GameTimer.Repurporse((int)GameDuty.DUTY.NONE);
				ChangeState(REACTION.FADE_COMPLETE);
				ChangeState(REACTION.RESET_GAME);
			}
			else if (GameTimer.SamePurpose(
				GameDuty.DUTY.PAUSE_BOMB_HELD))
			{
				if (!GameTimer.TimeIsUp())
				{
					if (keys.bomb == 0)
					{
						GameTimer.Reset();
					}
					return;
				}
				// The Bomb button was held down in the
				// Pause screen for a long time, so the player
				// has requested to go to the main menu.
				ChangeState(REACTION.FADE_TO_MENU);
				GameTimer.Repurporse(
					(int)GameDuty.DUTY.FADE_OUT_TO_MENU);
			}

            // Tick all gameplay related timers now.
            PlayerTimer.Tick(ticks);

			if (Paused)
			{
				if (keys.bomb == 0)
				{
					GameTimer.Reset();
				}
				return;
			}

			// All potential interruptions were processed,
			// so continue on to update the gameplay elements.
			UpdateBossPatternTime(ticks);
			MovePlayer();

			if (gameOver)
			{
				// Press the Shot button to return to the main menu.
				if (keys.shoot == 2)
				{
					ChangeState(REACTION.FADE_FROM_MENU);
					GameTimer.Repurporse((int)GameDuty.DUTY.FADE_OUT_TO_MENU);
					return;
				}
			}
			else
			{
				// Press the Shot button to fire bullets.
				ShootPlayerBullet();
				// Press the Bomb button to fire a bomb.
				ShootPlayerBomb();
			}

			if (bombComboTimeCountdown >= 0)
			{
				bombComboTimeCountdown--;
				ChangeState(REACTION.BOMB_LIFETIME_DECREMENT);
			}

			UpdateBullets();
			UpdateEnemies();
			UpdateBoss();
			player.Update();
		}
	}
}

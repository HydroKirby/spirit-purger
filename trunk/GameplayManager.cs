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
	public class PlayerTimerPurpose : TimerPurpose
	{
		public new enum PURPOSE
		{
			NONE,
			// Upon completing the game, how long the player can do
			// nothing except move around. (No shooting). Meant to
			// pause so that the final score can be shown.
			WAIT_AFTER_BEAT_GAME,
		}

		public PlayerTimerPurpose() { }

		public override int GetTime()
		{
			// Interpret Purpose as the local variant of PURPOSE in this class.
			switch ((PURPOSE)SpecificPurpose)
			{
				case PURPOSE.WAIT_AFTER_BEAT_GAME: return 0;
				default: return 0;
			}
		}
	}

	public class GameplayManager : Subject
	{
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
		// Upon completing the game, how long until replacing the functionality
		// of the shoot button. Pressing the shoot button causes the game to reset.
		public const int COMPLETED_GAME_TIL_CAN_EXIT_FRAMES = 400;

		// How to react to updates in the gameplay.
		// These REACTIONs are informed back to the Engine so that the
		// Engine can handle the complicated operations.
		public enum REACTION
		{
			// No particular action to take.
			NONE,
			// Tell the renderer to refresh something.
			REFRESH_SCORE, REFRESH_BULLET_COUNT, REFRESH_LIVES, REFRESH_BOMBS,
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
		protected DownTimer timerPlayer;

		// Game state variables.
		public bool gameOver = false;
		// Gives points when true. Becomes false if the players dies or bombs.
		public bool beatThisPattern = true;
		public bool paused = false;
		public Player player;
		// Stores all base types of bullets. Keep in memory.
		protected ArrayList bulletTypes = new ArrayList();
		protected ArrayList enemies = new ArrayList();
		public ArrayList enemyBullets = new ArrayList();
		public ArrayList playerBullets = new ArrayList();
		protected ArrayList hitSparks = new ArrayList();

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
		public long score;
		public short lives;
		public short bombs;

		public GameplayManager(ImageManager imageManager, SoundManager sndManager,
			KeyHandler keyHandler, Dictionary<string, object> settings)
		{
			soundManager = sndManager;
			keys = keyHandler;
			bulletCreator = new BulletCreator(imageManager);
			timerPlayer = new DownTimer(new PlayerTimerPurpose());

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
			bossState = BossState.Intro;
			playerBullets.Clear();
			enemyBullets.Clear();
			hitSparks.Clear();
			enemies.Clear();
			beatThisPattern = true;
			gameOver = false;
			paused = false;
			prevSecondUpdateFraction = 0.0;
			transitionFrames = 0;
			bossIntroTime = 0;
			bombBlast.Kill();
			bombCombo = 0;
			bombComboTimeCountdown = 0;
			bombComboScore = 0;
			grazeCount = 0;
			score = 0;
			lives = 2;
			bombs = 3;
			timerPlayer.Purpose.SpecificPurpose = (int)PlayerTimerPurpose.PURPOSE.NONE;
			timerPlayer.Reset();
		}

		public void MovePlayer()
		{
			// Act on states that disable player movement first.
			if (player.deathCountdown > 0)
				if (player.deathCountdown == 1)
				{
					lives--;
					if (lives < 0)
					{
						gameOver = true;
						ChangeState(REACTION.LOST_ALL_LIVES);
					}
					ChangeState(REACTION.REFRESH_LIVES);
					player.reentryCountdown = GameplayManager.REENTRY_FRAMES;
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
					lives >= 0 && Physics.Touches(player, enemy))
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
					lives >= 0 && Physics.Touches(player, boss))
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
							timerPlayer.Purpose.SpecificPurpose = (int)PlayerTimerPurpose.
								PURPOSE.WAIT_AFTER_BEAT_GAME;
						}
						else
						{
							// Activate the next pattern.
							patternTime =
								Boss.patternDuration[boss.currentPattern];
							ChangeState(REACTION.BOSS_PATTERN_SUCCESS);
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
			score += addScore;
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
						score += 20;
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
		}

		protected void ShootPlayerBomb()
		{
			if (keys.bomb == 2 && bombBlast.IsGone() && (godMode ||
					bombs > 0 && player.reentryCountdown <= 0 &&
					player.deathCountdown <= 0))
			{
				// Fire a bomb.
				player.invincibleCountdown = Bomb.LIFETIME_ACTIVE;
				bombBlast.Renew(player.Location);
				bombCombo = 0;
				bombComboTimeCountdown = GameplayManager.BOMB_COMBO_DISPLAY_FRAMES;
				bombs--;
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

		public void UpdateGame(object sender, double ticks)
		{
			if (paused)
			{
				if (keys.bomb > 60)
					ChangeState(REACTION.RESET_GAME);
				return;
			}

			timerPlayer.Tick();
			UpdateBossPatternTime(ticks);
			MovePlayer();

			if (gameOver)
			{
				// Press the Shot button to return to the main menu.
				if (keys.shoot == 2)
				{
					ChangeState(REACTION.RESET_GAME);
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

		public void PaintGame(object sender, double ticks)
		{
			RenderWindow app = (RenderWindow)sender;

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
					 bossIntroTime > GameplayManager.BOSS_PRE_INTRO_FRAMES)
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
		}
	}
}

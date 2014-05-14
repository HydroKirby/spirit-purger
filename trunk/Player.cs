/*
 * Larry Resnik          Pgm 9-1           Game (Shmup)
 *
 * Created by SharpDevelop.
 * SharpDevelop Version : 3.0.0.3800
 * .NET Version         : 2.0.50727.4200
 *
 * Date: 11/18/2009
 * Time: 1:18 PM
 *
 * Mainly adapted from http://www.bobpowell.net/manipulate_graphics.htm
 */


using System;
using System.Collections;
using System.Collections.Generic;
using SFML;
using SFML.Window;
using SFML.Graphics;

namespace SpiritPurger {
	public class Entity {
		public Color color = Color.Red;
		protected Vector2f location;
		private Vector2u size;
		// Stored to bypass calculating it all the time.
		private Vector2u halfSize;

		public Vector2u Size {
			get { return size; }
			set {
				size = value;
				halfSize.X = size.X / 2;
				halfSize.Y = size.Y / 2;
			}
		}
		public Vector2u HalfSize {
			get { return halfSize; }
			set {
				halfSize = value;
				size.X = halfSize.X + halfSize.X;
				size.Y = halfSize.Y + halfSize.Y;
			}
		}

		public Vector2f Location
		{
			get { return location; }
			set { location = new Vector2f(value.X, value.Y); }
		}

		public Entity() {
			Location = new Vector2f();
			Size = new Vector2u(1, 1);
		}

		public Entity(Vector2f location) {
			Location = new Vector2f(location.X, location.Y);
			Size = new Vector2u(1, 1);
		}

		public Entity(Vector2f location, Vector2u size) {
			Location = new Vector2f(location.X, location.Y);
			Size = size;
		}

		public Entity(Entity square) {
			Location = new Vector2f(square.Location.X, square.Location.Y);
			Size = square.size;
			color = square.color;
		}

		public virtual void Move(float x, float y)
		{
			location.X += x;
			location.Y += y;
		}
	}

	public class Player : Entity {
		public enum STATE {MOVE_FORWARD, MOVE_LEFT, MOVE_RIGHT, DEAD, REVIVING};
		public const float HI_SPEED = 4.0F;
		public const float LO_SPEED = 2.0F;
		// This is how long the player waits after being hit until being sent
		// outside of the screen.
		public const int DEATH_SEQUENCE_FRAMES = 30;
		// Frames until being able to fire again; Based on rapid fire.
		public const int fireRate = 2;
		protected int timeSinceLastFire = fireRate + 1;
		// This relates to the smaller, circular hitbox at the center.
		// It is not related to the player's width or height.
		public Hitbox hitbox;
		protected uint radius = 2;
		// How many frames to be waited after being hit. It stays 0 when not in
		// use and counts down from DEATH_SEQUENCE_FRAMES when initiated.
		// The player can't move during the death sequence. The death sequence
		// transitions immediately into the reentry sequence.
		public int deathCountdown = 0;
		public int invincibleCountdown = 0;
		protected AniPlayer animation;
		public CenterSprite hitBoxSprite;
		protected STATE _state;

		public int Radius {
			get { return hitbox.Radius; }
		}

		public STATE State
		{
			get { return _state; }
		}

		public new Vector2f Location
		{
			get { return location; }
			set
			{
				location = new Vector2f(value.X, value.Y);
				hitbox.location = location;
			}
		}

		public Player(AniPlayer anim, Hitbox hitbox)
		{
			_state = STATE.REVIVING;
			animation = anim;
			// Do not use the base class' constructor because hitbox
			// must be initialized to set Player's Location.
			this.hitbox = hitbox;
			hitBoxSprite = hitbox.Sprite;
			Location = new Vector2f(0, 0);
			Size = new Vector2u(20, 20);
		}

        public void Reset()
        {
            animation.Reset();
        }

		public void SetHitboxSprite(CenterSprite s)
		{
			s.setPosition(Location + Renderer.FieldUpperLeft);
			hitBoxSprite = s;
		}

		public override void Move(float x, float y)
		{
			location.X += x;
			location.Y += y;
			hitbox.location = location;
		}

		public void UpdateDisplayPos()
		{
			animation.Update(0, Location + Renderer.FieldUpperLeft);
			hitBoxSprite.setPosition(Location + Renderer.FieldUpperLeft);
		}

		/// <summary>
		/// Checks if a shot can be fired based on the pause imposed by the
		/// streamPause. If a shot can be fired, the full time of the
		/// streamPause must be waited through before another shot can be
		/// fired.
		/// </summary>
		/// <returns>If a shot should be fired or if it is not possible.</returns>
		public bool TryShoot() {
			if (timeSinceLastFire > fireRate) {
				timeSinceLastFire = 0;
				return true;
			}
			return false;
		}

		public void Update(double ticks=1.0) {
            if (invincibleCountdown <= 0 && deathCountdown <= 0)
                animation.Update((int)ticks);
			timeSinceLastFire++;
			if (invincibleCountdown > 0)
				invincibleCountdown--;
			if (deathCountdown > 0)
				deathCountdown--;
		}

		public void Draw(RenderWindow app)
		{
			animation.Draw(app);
		}

        /*
		public override void Draw(Graphics g) {
            g.DrawImage(sprite, DrawLocation);
            return;

			SolidBrush solidBrush;
			if (deathCountdown != 0) {
				// Half of the time it takes to do the death sequence.
				double halfFrames = DEATH_SEQUENCE_FRAMES / 2.0;
				// The color goes from normal to black. Beyond the halfway
				// point, the alpha goes from fully visible to invisible.
				if (deathCountdown > halfFrames)
					solidBrush = new SolidBrush(Color.FromArgb(
						255, 0, 0, (int) ((deathCountdown - halfFrames) /
						                  halfFrames * 255)));
				else
					// Be pitch black. Go from visible to invisible.
					solidBrush = new SolidBrush(Color.FromArgb(
						(int) ((double) deathCountdown / halfFrames * 255),
						0, 0, 0));
			} else if (invincibleCountdown != 0) {
				// Fade in and out of green a bit. This formula circulates
				// every 10 frames.

				// This is the ones-digit of invincibleCountdown.
				int framePortion = (int) (invincibleCountdown % 10);
				int val;
				if (framePortion <= 5)
					// Fade-out gradually.
					val = 255 - framePortion * 30;
				else
					// Fade-in gradually. num + max - 2 * num is a formula that
					// makes smaller output as num gets bigger.
					val = 255 - (framePortion + 10 - 2 * framePortion) * 30;
				solidBrush = new SolidBrush(Color.FromArgb(
					val, 0, 255 - val, 255));
			} else
				solidBrush = new SolidBrush(Color.Blue);
			g.FillRectangle(solidBrush, new Rectangle(DrawLocation, Size));
			solidBrush.Dispose();
		}
         */
	}

    public class Boss : Entity
    {
        public enum EntityState { NotArrived, Intro, Active, Killed };
        public enum MoveStyle { NoMove, Accel, Decel, Constant }
        protected int updateCount = 0;
        // Ideally, I imagine the next 4 parameters would be in a single
        // script or structure because they each relate to a pattern.
        // This is how much health the boss has during a pattern.
		public static int[] fullHealth = { 400, 500, 350, 350 };
        // This is where the boss begins firing from. If the boss is not in
        // this point when the pattern begins, the boss rushes to there.
        // If the point is null, then the boss keeps its position.
        public static Vector2f[] startPoints = {
			new Vector2f(145.0F, 72.5F),
			new Vector2f(145.0F, 120.0F),
			new Vector2f(145.0F, 72.5F),
			new Vector2f(),
		};
        // This is how long the boss waits before initiating a pattern's
        // attack. During this time, the assigned startPoints may be reached.
        protected static int[] startupFrames = { 10, 40, 40, 40 };
        // This is how long a pattern lasts in seconds.
        public static int[] patternDuration = { 80, 80, 90, 100 };
        public int health;
        public int currentPattern = 0;
        // This true when updateCount reaches the related startupFrames value.
        protected bool startedPattern = false;
        // The variables are extra data for patterns.
        // vari means integer variable.
        protected int vari1 = 0;
        protected int vari2 = 0;
        // vard means double/floating point variable.
        protected double vard1 = 0.0;
        protected double vard2 = 0.0;
        // varv means Vector2d variable.
        protected Vector2f varv1 = new Vector2f();
        protected Vector2f varv2 = new Vector2f();
        // When the enemy moves around, this is the final destination.
        // When aimFor is assigned, direction will be assigned.
        protected Vector2f aimFor;
        protected Vector2f direction = new Vector2f(0, 1);
        // The speed, dx, and dy are only used for MoveStyle.Constant.
        // Setting the Speed accessor alters dx and dy.
        public double speed = 3.0;
        protected double dx = 0.0;
        protected double dy = 1.0;
        // Refers to moving towards aimFor.
        protected MoveStyle moveStyle = MoveStyle.NoMove;
		protected AniBoss animation;
        protected EntityState _state;

        public Boss(AniBoss anim)
            : base(new Vector2f(), new Vector2u(35, 25))
        {
            animation = anim;
            Reset(Location);
        }

        public EntityState State
        {
            get { return _state; }
            protected set { _state = value; }
        }

        public double Speed
        {
            get { return speed; }
            set
            {
                speed = value;
                this.RefreshVelocity();
            }
        }

        public Vector2f Direction
        {
            get { return direction; }
            set
            {
                direction = value;
                VectorLogic.Normalize(direction);
                this.RefreshVelocity();
            }
        }

        public AniBoss Animate
        {
            get { return animation; }
            private set { animation = value; }
        }

        public void Reset(Vector2f newPos)
        {
            Location = newPos;
            UpdateDisplayPos();
            animation.Reset();
            State = EntityState.Intro;
            currentPattern = -1;
            NextPattern();
            health = fullHealth[0];
        }

        public void DoneWithIntro()
        {
            if (State == EntityState.Intro)
                State = EntityState.Active;
        }

        /// <summary>
        /// Resets dx and dy so that the overall velocity is correct in
        /// accordance with the direction vector and the speed.
        /// </summary>
        public void RefreshVelocity()
        {
            dx = direction.X * speed;
            dy = direction.Y * speed;
        }

        protected void PointAt(Vector2f pt)
        {
            direction = VectorLogic.GetDirectionVector(pt, Location);
            RefreshVelocity();
        }

        /// <summary>
        /// Resets the variables and progresses to the next pattern.
        /// Used when the previous pattern has been completed.
        /// </summary>
        /// <returns>False if there are no more patterns.</returns>
        public bool NextPattern()
        {
            vard1 = vard2 = vari1 = vari2 = 0;
            varv1 = new Vector2f();
            varv2 = new Vector2f();
            updateCount = 0;
            startedPattern = false;
            currentPattern++;
            if (currentPattern >= fullHealth.Length)
            {
                // No more patterns are available.
                State = EntityState.Killed;
                return false;
            }
            health = fullHealth[currentPattern];
            moveStyle = MoveStyle.NoMove;
            if (startPoints[currentPattern].X != 0 && startPoints[currentPattern].Y != 0)
                if (Math.Abs(VectorLogic.GetDistance(
                    startPoints[currentPattern], Location)) > 0.5)
                {
                    // We're far from the desired point. Move there.
                    SetDestination(startPoints[currentPattern],
                        MoveStyle.Constant);
                    Speed = 3.0;
                }
            return true;
        }

        /// <summary>
        /// Requests that this enemy moves to the new position at Point pt.
        /// If MoveStyle.Constant is desired, setting Speed before or after
        /// calling this will be optional but will affect the movement.
        /// If MoveStyle.NoMove is passed, MoveStyle.Constant will be used.
        /// </summary>
        /// <param name="pt">The new desired location.</param>
        /// <param name="moveStyle">How to progress towards Point pt.</param>
        public void SetDestination(Vector2f pt, MoveStyle moveStyle)
        {
            if (this.moveStyle != MoveStyle.NoMove)
                return;
            aimFor = pt;
            PointAt(pt);
            if (moveStyle == MoveStyle.Accel || moveStyle == MoveStyle.Decel)
                // TODO: Accel and Decel MoveStyles.
                throw new NotImplementedException();
            this.moveStyle = moveStyle != MoveStyle.NoMove ? moveStyle :
                MoveStyle.Constant;
        }

        /// <summary>
        /// Progresses towards aimFor based on moveStyle. When very close to
        /// the destination, moveStyle is set to MoveStyle.NoMove.
        /// </summary>
        /// <returns>Whether or not the destination was reached.</returns>
        protected bool MoveTowardsDestination()
        {
            if (moveStyle == MoveStyle.Constant)
            {
				Location += new Vector2f((float)dx, (float)dy);
            }

            if (Math.Abs(VectorLogic.GetDistance(Location, aimFor)) <= 4.0)
            {
                moveStyle = MoveStyle.NoMove;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Inflicts damage on the boss. Returns true if the attack felled the
        /// boss, but not if it is an excess operation.
        /// </summary>
        /// <param name="damage">The value to remove from health.</param>
        /// <returns>If the damage dealt was the final blow.</returns>
        public bool DealDamage(int damage)
        {
            if (health <= 0)
                return false;
            health = Math.Max(health - damage, 0);
            return health <= 0;
        }

        /// <summary>
        /// Creates a slow helix. To make the pattern less stale and to punish
        /// campers, the pattern occasionally fires a line of bullets towards
        /// the player. The line has a half-dozen pairs of adjacent bullets
        /// to punish reckless running around.
        /// </summary>
		protected bool UpdatePtrn0(out List<BulletProp> newBullets,
            Vector2f playerPos, Random rand)
        {
			newBullets = new List<BulletProp>();
			BulletProp prop;
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount % 2 == 0)
            {
                // Makes 4 bullets that arc from each end of the x-axis. They
                //   gradually move to the other side before restarting their
                //   trajectory. This statement is hit 45 times.
                //   45 updates * 2 is 90 degrees.
                double baseDegree = VectorLogic.Radians(updateCount * 2);
                // Moves clockwise from 0 to 180 degrees.
				prop = new BulletProp(3, Location,
					VectorLogic.AngleToVector(baseDegree), 1.5);
				newBullets.Add(prop);

                // Moves clockwise from 180 to 0 degrees.
				prop = new BulletProp(prop,  VectorLogic.AngleToVector(baseDegree + Math.PI));
				newBullets.Add(prop);

                // Moves counter-clockwise from 180 to 0 degrees.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(baseDegree +
					Math.PI - 2 * baseDegree));
				newBullets.Add(prop);

                // Moves CCW from 0 to 180 degrees.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(baseDegree +
					Math.PI * 2 - 2 * baseDegree));
				newBullets.Add(prop);
            }
            if ((updateCount >= 0 && updateCount <= 6) ||
                (updateCount >= 30 && updateCount <= 36) ||
                (updateCount >= 60 && updateCount <= 66))
            {
                if (updateCount % 20 == 0)
                    // Store the angle towards the player in vard1.
                    vard1 = VectorLogic.GetDirection(playerPos, Location);
                // Make a line of bullets aimed at the player.
				prop = new BulletProp(6, Location,
					VectorLogic.AngleToVector(vard1), 1.7);
				newBullets.Add(prop);
                if (updateCount % 10 == 3)
                {
                    for (double angle = 0.07; angle < 0.13; angle += 0.01)
                    {
                        // Make 2 bullets aimed at the player's sides.
						prop = new BulletProp(5, Location,
							VectorLogic.AngleToVector(vard1 + Math.PI * angle), 2.0);
						newBullets.Add(prop);
						prop = new BulletProp(5, Location,
							VectorLogic.AngleToVector(vard1 - Math.PI * angle), 2.0);
						newBullets.Add(prop);
                    }
                }
            }
            if (updateCount >= 91)
                updateCount = 1;
            return movedToDest;
        }

        /// <summary>
        /// Stays in place and fires everywhere randomly. Occasionally shoots
        /// a line of large bullets aimed at the player.
        /// </summary>
		protected bool UpdatePtrn1(out List<BulletProp> newBullets,
            Vector2f playerPos, Random rand)
        {
			newBullets = new List<BulletProp>();
			BulletProp prop;
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount >= 30)
            {
                // Creates a straight line of big bullets aimed at the player.
                // Makes as many bullets in a row until the updateCount is
                // refreshed to 1.
                if (updateCount == 30)
                    varv1 = VectorLogic.GetDirectionVector(playerPos, Location);
				prop = new BulletProp(14, Location, varv1, 3.0);
				newBullets.Add(prop);
            }
            if (updateCount % 5.0 == 0.0)
            {
                // Fires 5 bullets off in random directions.
                for (int i = 0; i < 6; i++)
                {
					newBullets.Add(new BulletProp(
						(int)rand.Next(5, 10), Location,
						VectorLogic.AngleToVector(VectorLogic.Radians(rand.Next(360))),
						1.0 + rand.NextDouble() * 1.5));
                }
            }
            if (updateCount >= 40)
                updateCount = 1;
            return movedToDest;
        }

        /// <summary>
        /// Moves back and forth, staying 80 pixels from the screen borders.
        /// Fires a ring of bullets. Fires an offset spread towards the
        /// player's safety zone. A twisting cross of bullets is made
        /// continuously.
        /// </summary>
		protected bool UpdatePtrn2(out List<BulletProp> newBullets,
            Vector2f playerPos, Random rand)
        {
			newBullets = new List<BulletProp>();
			BulletProp prop;
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (moveStyle == MoveStyle.NoMove && vari1 == 0)
            {
                // Start moving to the left.
                Vector2f dest = new Vector2f(80.0F, Location.Y);
                Speed = 2.0;
                SetDestination(dest, MoveStyle.Constant);
                // vari1 = 1 means it is moving left.
                vari1 = 1;
            }
            else if (moveStyle == MoveStyle.NoMove && vari1 == 1)
            {
                // Start moving right.
                Vector2f dest = new Vector2f(210.0F, Location.Y);
                SetDestination(dest, MoveStyle.Constant);
                // vari1 = 0 means it is moving right.
                vari1 = 0;
            }

			if (updateCount < 40 && updateCount % 4 == 0)
			{
				// Shoots 4 bullets in a cross. The shot direction
				// progressively moves from clockwise.
				// Note: 90 is 1/4th of a full 360 degrees. 9 is 90/10.
				//   This area will be executed about 10 times.
				double baseRadian = VectorLogic.Radians(
					updateCount / 4.0 * 9.0);
				prop = new BulletProp(6, Location,
					VectorLogic.AngleToVector(baseRadian), 2.0);
				newBullets.Add(prop);
				// Add 90 degrees.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(baseRadian -
					Math.PI / 2.0));
				newBullets.Add(prop);
				// Add 180 degrees.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(baseRadian - Math.PI));
				newBullets.Add(prop);
				// Add 270 degrees.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(baseRadian +
					Math.PI / 2.0));
				newBullets.Add(prop);
			}
			else if (updateCount == 40)
			{
				double towardsPlayer = VectorLogic.GetDirection(playerPos,
															 Location);
				// Make a ring of 10 bullets. One bullet is aimed at the player.
				for (int i = 0; i < 11; i++)
				{
					Vector2f v = VectorLogic.AngleToVector(towardsPlayer +
						VectorLogic.Radians(i / 10.0 * 360));
					newBullets.Add(new BulletProp(13, Location, v, 3.0));
				}
			}
			else if (updateCount >= 45 && updateCount % 4 == 0)
			{
				double towardsPlayer = VectorLogic.GetDirection(
					playerPos, Location);
				// Make 2 bullets aimed adjacent to the player. They both
				// miss by 30 degrees on each side.
				double offsetedAngle = towardsPlayer +
					VectorLogic.Radians(15);
				Vector2f offsetedVector = VectorLogic.AngleToVector(offsetedAngle);
				prop = new BulletProp(14, Location, offsetedVector, 3.0);
				newBullets.Add(prop);

				offsetedAngle = towardsPlayer -
					VectorLogic.Radians(15);
				offsetedVector = VectorLogic.AngleToVector(offsetedAngle);
				prop.Renew();
				prop.Direction = offsetedVector;
				prop.Speed = 3.0;
				newBullets.Add(prop);
			}
			else if (updateCount >= 60)
				updateCount = 1;
            return movedToDest;
        }

        /// <summary>
        /// Moves towards the player in short steps. Will periodically fire a
        /// composite string of bullets of different sizes at specified
        /// angles. Upon stopping, a string of bullets is aimed at the player.
        /// While idle, a cross of slow, tiny bullets are fired continuously.
        /// </summary>
		protected bool UpdatePtrn3(out List<BulletProp> newBullets,
            Vector2f playerPos, Random rand)
        {
			newBullets = new List<BulletProp>();
			BulletProp prop;
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount == 2)
            {
                if (!Location.Equals(playerPos))
                    SetDestination(playerPos, MoveStyle.Constant);
                Speed = 2.0;
            }

            // In this pattern, vari1 states the direction to shoot the
            //   bullets. Diagonally aimed bullets are shot on the opposite
            //   end as well.
            if (updateCount % 15 == 0)
            {
                // Out of 45 frames, this area gets hit thrice per cycle.
                double angle;
                switch (vari1)
                {
                    case 0: // Aimed at the lower-right.
                        angle = Math.PI * 0.25; break;
                    case 1: // Aimed at the lower-left.
                        angle = Math.PI * 0.75; break;
                    case 2:
                    default: // Aimed at the player.
                        angle = VectorLogic.GetDirection(playerPos, Location);
                        break;
                }
				for (int i = 1; i <= 3; i++)
				{
					// vari1 types 0 and 1 have colorIndex 0.
					// vari1 type 2 has a colorIndex of 1.
					prop = new BulletProp((int)Math.Max(vari1 - 1, 0) + 5,
						Location, VectorLogic.AngleToVector(angle), i + 0.5);
					newBullets.Add(prop);
					// Make a small trailing bullet.
					prop = new BulletProp((int)Math.Max(vari1 - 1, 0),
						Location, VectorLogic.AngleToVector(angle), i + 1);
					newBullets.Add(prop);
				}
                if (vari1 < 2)
                    // It's aimed diagonally.
                    // Shoot along the opposite diagonal.
                    for (int i = 2; i < 5; i++)
                    {
						prop = new BulletProp(5, Location,
							VectorLogic.AngleToVector(angle + Math.PI), i);
						newBullets.Add(prop);
						prop = new BulletProp(0, Location,
							VectorLogic.AngleToVector(angle + Math.PI), i - 0.5);
						newBullets.Add(prop);
					}
                vari1++;
                if (vari1 >= 3)
                    vari1 = 0;
            }
            if (updateCount == 30)
                moveStyle = MoveStyle.NoMove;
            if (moveStyle == MoveStyle.NoMove)
            {
                // Shoot a cross of bullets - something to tease the player
                //   into grazing.
				prop = new BulletProp(4, Location,
					VectorLogic.AngleToVector(0), 1.5);
				newBullets.Add(prop);
				// Aimed down.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(Math.PI / 2.0));
				newBullets.Add(prop);
				// Aimed right.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(Math.PI));
				newBullets.Add(prop);
				// Aimed up.
				prop = new BulletProp(prop, VectorLogic.AngleToVector(Math.PI * 1.5));
				newBullets.Add(prop);
            }

            if (updateCount >= 51)
                updateCount = 1;
            return movedToDest;
        }

		public bool Update(out List<BulletProp> newBullets,
            Vector2f playerPos, Random rand)
        {
			newBullets = null;
            bool finishedMoving = false;
            if (startedPattern)
            {
                if (currentPattern == 0)
					finishedMoving = UpdatePtrn0(out newBullets, playerPos, rand);
                else if (currentPattern == 1)
					finishedMoving = UpdatePtrn1(out newBullets, playerPos, rand);
                else if (currentPattern == 2)
					finishedMoving = UpdatePtrn2(out newBullets, playerPos, rand);
                else if (currentPattern == 3)
					finishedMoving = UpdatePtrn3(out newBullets, playerPos, rand);
            }
            else
            {
				newBullets = new List<BulletProp>();
				if (moveStyle != MoveStyle.NoMove)
				{
					finishedMoving = MoveTowardsDestination();
				}
                if (updateCount > startupFrames[currentPattern])
                {
                    startedPattern = true;
                    updateCount = -1;
                }
            }
			UpdateDisplayPos();
			animation.Update(1);
            updateCount++;
            return finishedMoving;
        }

		public void Update(int elapsed)
		{
            if (State == EntityState.Active)
                animation.Update(elapsed);
            else if (State == EntityState.Intro)
                animation.Update(0);
            UpdateDisplayPos();
		}

		public void UpdateDisplayPos()
		{
			animation.Update(0, Location + Renderer.FieldUpperLeft);
		}

		public void Draw(RenderWindow app)
		{
			animation.Draw(app);
		}
	}
}

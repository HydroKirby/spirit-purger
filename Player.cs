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
using SFML;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet {
	public class Square {
		public Color color = Color.Red;
		public Vector2f location;
		private Vector2i size;
		// Stored to bypass calculating it all the time.
		private Vector2i halfSize;

		public Vector2i Size {
			get { return size; }
			set {
				size = value;
				halfSize.X = size.X / 2;
				halfSize.Y = size.Y / 2;
			}
		}
		public Vector2i HalfSize {
			get { return halfSize; }
			set {
				halfSize = value;
				size.X = halfSize.X + halfSize.X;
				size.Y = halfSize.Y + halfSize.Y;
			}
		}
		public Point DrawLocation {
			get { return new Point((int) location.X - halfSize.X,
			                       (int) location.Y - halfSize.Y); }
		}

		public Square() {
			location = new Vector2f();
			Size = new Vector2i(1, 1);
		}

		public Square(Vector2f location) {
			this.location = new Vector2f(location.X, location.Y);
			Size = new Vector2i(1, 1);
		}

		public Square(Vector2f location, Vector2i size) {
			this.location = new Vector2f(location.X, location.Y);
			this.Size = size;
		}

		public Square(Square square) {
			location = new Vector2f(square.location.X, square.location.Y);
			Size = square.size;
			color = square.color;
		}

        /*
		public bool HitTest(Point pt) {
			return new Rectangle(location.AsPoint(), size).Contains(pt);
		}
         */

		public bool HitTest(Vector2i rSize, Vector2f loc) {
			return location.X <= loc.X + rSize.X &&
				location.X + size.X >= loc.X &&
				location.Y <= loc.Y + rSize.Y &&
				location.Y + size.Y >= loc.Y;
		}

		public bool HitTest(Square square) {
			return location.X <= square.location.X + square.Size.X &&
				location.X + size.X >= square.location.X &&
				location.Y <= square.location.Y + square.Size.Y &&
				location.Y + size.Y >= square.location.Y;
		}

		public bool HitTest(Bullet circle) {
			// Get the center of the circle relative to the center of this.
			Vector2f rectCenter =
				new Vector2f(DrawLocation.X + size.X / 2,
				             DrawLocation.Y + size.Y / 2);
			Vector2f circleCenterRelRect = circle.location - rectCenter;

			// Get the point on the surface of the square that's closest to
			// the bullet.
			Vector2f rectPoint = new Vector2f();
			// Check circle against rect on the x-axis alone. If the circle
			// is to the left of the rect, then the left edge is closest.
			// Vice versa is true as well. When the circle is between the
			// rect's edges, the circle's distance from the rect is 0.
			if (circleCenterRelRect.X < -size.X / 2.0)
				rectPoint.X = -size.X / 2;
			else if (circleCenterRelRect.X > size.X / 2.0)
				rectPoint.X = size.X / 2;
			else
				rectPoint.X = circleCenterRelRect.X;

			// Do the same check for the y-axis.
			if (circleCenterRelRect.Y < -size.Y / 2.0)
				rectPoint.Y = -size.Y / 2;
			else if (circleCenterRelRect.Y > size.Y / 2.0)
				rectPoint.Y = size.Y / 2;
			else
				rectPoint.Y = circleCenterRelRect.Y;

			// See if the distance from the closest point on the rect to the
			// circle is less than the radius.
			Vector2f dist = circleCenterRelRect - rectPoint;
			return dist.X * dist.X + dist.Y * dist.Y <
				circle.Radius * circle.Radius;
		}

        /*
		public virtual void Draw(Graphics g) {
			SolidBrush solidBrush = new SolidBrush(color);
			g.FillRectangle(solidBrush, new Rectangle(DrawLocation, size));
			solidBrush.Dispose();
		}

		public void Draw(Graphics g, int alpha) {
			SolidBrush solidBrush = new SolidBrush(
				Color.FromArgb(alpha, color.R, color.G, color.B));
			g.FillRectangle(solidBrush, new Rectangle(DrawLocation, size));
			solidBrush.Dispose();
		}
         */
	}

	public class Player : Square {
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
		protected int radius = 2;
		// How many frames to be waited after being hit. It stays 0 when not in
		// use and counts down from DEATH_SEQUENCE_FRAMES when initiated.
		// The player can't move during the death sequence. The death sequence
		// transitions immediately into the reentry sequence.
		public int deathCountdown = 0;
		public int invincibleCountdown = 0;
		// How many frames to wait while the player comes back up from the
		// bottom of the screen after dying. The player can't move during
		// the reentry sequence.
		public int reentryCountdown = 0;
		public CenterSprite sprite;
		public CenterSprite hitBoxSprite;

		public int Radius {
			get { return radius; }
		}

		public Point DrawHitCircleLocation {
			get { return new Point((int) location.X - radius,
			                       (int) location.Y - radius); }
		}

		public Player() : base(new Vector2f(), new Vector2i(20, 20)) {}

		public void SetImage(CenterSprite s) {
            s.setPosition(location + Renderer.FieldUpperLeft);
			sprite = s;
		}

		public void SetHitboxSprite(CenterSprite s)
		{
			s.setPosition(location + Renderer.FieldUpperLeft);
			hitBoxSprite = s;
		}

		public void UpdateDisplayPos()
		{
			sprite.setPosition(location + Renderer.FieldUpperLeft);
			hitBoxSprite.setPosition(location + Renderer.FieldUpperLeft);
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

		public void Update() {
			timeSinceLastFire++;
			if (invincibleCountdown > 0)
				invincibleCountdown--;
			if (deathCountdown > 0)
				deathCountdown--;
			if (reentryCountdown > 0)
				reentryCountdown--;
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

    public class Enemy : Square
    {
        public enum MoveStyle { NoMove, Accel, Decel, Constant }
        protected int updateCount = 0;
        // Ideally, I imagine the next 4 parameters would be in a single
        // script or structure because they each relate to a pattern.
        // This is how much health the boss has during a pattern.
        //public static int[] fullHealth = { 400, 500, 350, 350 };
		public static int[] fullHealth = { 400, 500, 350, 350 };
        // This is where the boss begins firing from. If the boss is not in
        // this point when the pattern begins, the boss rushes to there.
        // If the point is null, then the boss keeps its position.
        // Note: The app window is 290x290.
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
        public CenterSprite sprite;

        public Enemy()
            : base(new Vector2f(), new Vector2i(25, 25))
        {
            health = fullHealth[0];
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
                Vector2D.Normalize(direction);
                this.RefreshVelocity();
            }
        }

        public void SetImage(CenterSprite b) { sprite = b; }

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
            direction = Vector2D.GetDirectionVector(pt, location);
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
                // No more patterns are available.
                return false;
            health = fullHealth[currentPattern];
            moveStyle = MoveStyle.NoMove;
            if (startPoints[currentPattern].X != 0 && startPoints[currentPattern].Y != 0)
                if (Math.Abs(Vector2D.GetDistance(
                    startPoints[currentPattern], location)) > 0.5)
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
                location.X += (float)dx;
                location.Y += (float)dy;
            }

            if (Math.Abs(Vector2D.GetDistance(location, aimFor)) <= 4.0)
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
        protected bool UpdatePtrn0(ref ArrayList bullets, GameRenderer renderer,
            Vector2f playerPos, Random rand)
        {
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount % 2 == 0)
            {
                // Makes 4 bullets that arc from each end of the x-axis. They
                //   gradually move to the other side before restarting their
                //   trajectory. This statement is hit 45 times.
                //   45 updates * 2 is 90 degrees.
                double baseDegree = Vector2D.DegreesToRadians(updateCount * 2);
                // Moves clockwise from 0 to 180 degrees.
                Bullet b = new Bullet(3, 0, location,
                    Vector2D.VectorFromAngle(baseDegree), 1.5);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);

                // Moves clockwise from 180 to 0 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseDegree +
                    Math.PI));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);

                // Moves counter-clockwise from 180 to 0 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseDegree +
                    Math.PI - 2 * baseDegree));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);

                // Moves CCW from 0 to 180 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseDegree +
                    Math.PI * 2 - 2 * baseDegree));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
            }
            if ((updateCount >= 0 && updateCount <= 6) ||
                (updateCount >= 30 && updateCount <= 36) ||
                (updateCount >= 60 && updateCount <= 66))
            {
                if (updateCount % 20 == 0)
                    // Store the angle towards the player in vard1.
                    vard1 = Vector2D.GetDirection(playerPos, location);
                // Make a line of bullets aimed at the player.
                Bullet b = new Bullet((int)Bullet.BulletColors.Orange, 1,
                    location, Vector2D.VectorFromAngle(vard1), 1.7);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                if (updateCount % 10 == 3)
                {
                    for (double angle = 0.07; angle < 0.13; angle += 0.01)
                    {
                        // Make 2 bullets aimed at the player's sides.
                        b = new Bullet(
                            (int)Bullet.BulletColors.Red, 1, location,
                            Vector2D.VectorFromAngle(vard1 + Math.PI * angle), 2.0);
                        b.Sprite = renderer.GetCenterSprite(
                            renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                        bullets.Add(b);
                        b = new Bullet(
                            (int)Bullet.BulletColors.Red, 1, location,
                            Vector2D.VectorFromAngle(vard1 - Math.PI * angle), 2.0);
                        b.Sprite = renderer.GetCenterSprite(
                            renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                        bullets.Add(b);
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
        protected bool UpdatePtrn1(ref ArrayList bullets, GameRenderer renderer,
            Vector2f playerPos, Random rand)
        {
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount >= 30)
            {
                // Creates a straight line of big bullets aimed at the player.
                // Makes as many bullets in a row until the updateCount is
                // refreshed to 1.
                if (updateCount == 30)
                    varv1 = Vector2D.GetDirectionVector(playerPos, location);
                Bullet b = new Bullet(
                    (int)Bullet.BulletColors.EndColors - 1, 2, location,
                    varv1, 3.0);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
            }
            if (updateCount % 5.0 == 0.0)
            {
                // Fires 5 bullets off in random directions.
                for (int i = 0; i < 6; i++)
                {
                    Bullet b = new Bullet(
                        rand.Next((int)Bullet.BulletColors.EndColors - 2),
                        1, location);
                    b.Speed = 1.0 + rand.NextDouble() * 1.5;
                    b.Direction = Vector2D.VectorFromAngle(
                        Vector2D.DegreesToRadians(rand.Next(360)));
                    b.RefreshVelocity();
                    b.Sprite = renderer.GetCenterSprite(
                        renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                    bullets.Add(b);
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
        protected bool UpdatePtrn2(ref ArrayList bullets, GameRenderer renderer,
            Vector2f playerPos, Random rand)
        {
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (moveStyle == MoveStyle.NoMove && vari1 == 0)
            {
                // Start moving to the left.
                Vector2f dest = new Vector2f(80.0F, location.Y);
                Speed = 2.0;
                SetDestination(dest, MoveStyle.Constant);
                // vari1 = 1 means it is moving left.
                vari1 = 1;
            }
            else if (moveStyle == MoveStyle.NoMove && vari1 == 1)
            {
                // Start moving right.
                Vector2f dest = new Vector2f(210.0F, location.Y);
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
                double baseRadian = Vector2D.DegreesToRadians(
                    updateCount / 4.0 * 9.0);
                Bullet b = new Bullet((int)Bullet.BulletColors.Orange, 1,
                    location, Vector2D.VectorFromAngle(baseRadian), 2.0);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Add 90 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseRadian -
                    Math.PI / 2.0));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Add 180 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseRadian - Math.PI));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Add 270 degrees.
                b = new Bullet(b, Vector2D.VectorFromAngle(baseRadian +
                    Math.PI / 2.0));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
            }
            else if (updateCount == 40)
            {
                double towardsPlayer = Vector2D.GetDirection(playerPos,
                                                             location);
                // Make a ring of 10 bullets. One bullet is aimed at the player.
                for (int i = 0; i < 11; i++)
                {
					Vector2f v = Vector2D.VectorFromAngle(towardsPlayer + Vector2D.DegreesToRadians(i / 10.0 * 360));
                    Bullet b = new Bullet(
                        (int)Bullet.BulletColors.Green, 2, location,
                        v,
                        3.0F);
                    b.Sprite = renderer.GetCenterSprite(
                        renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                    bullets.Add(b);
                }
            }
            else if (updateCount >= 45 && updateCount % 4 == 0)
            {
                double towardsPlayer = Vector2D.GetDirection(
					playerPos, location);
				// Make 2 bullets aimed adjacent to the player. They both
                // miss by 30 degrees on each side.
				double offsetedAngle = towardsPlayer +
					Vector2D.DegreesToRadians(15);
				Vector2f offsetedVector = Vector2D.VectorFromAngle(offsetedAngle);
                Bullet b = new Bullet(
                    (int)Bullet.BulletColors.Violet, 1, location,
                    offsetedVector, 3.0);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);

				offsetedAngle = towardsPlayer -
					Vector2D.DegreesToRadians(15);
				offsetedVector = Vector2D.VectorFromAngle(offsetedAngle);
                b = new Bullet(b, offsetedVector);
				b.Speed = 3.0;
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
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
        protected bool UpdatePtrn3(ref ArrayList bullets, GameRenderer renderer,
            Vector2f playerPos, Random rand)
        {
            bool movedToDest = false;
            if (moveStyle != MoveStyle.NoMove)
                movedToDest = MoveTowardsDestination();
            if (updateCount == 2)
            {
                if (!location.Equals(playerPos))
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
                        angle = Vector2D.GetDirection(playerPos, location);
                        break;
                }
                for (int i = 1; i <= 3; i++)
                {
                    // vari1 types 0 and 1 have colorIndex 0.
                    // vari1 type 2 has a colorIndex of 1.
                    Bullet b = new Bullet(Math.Max(vari1 - 1, 0), 1,
                        location, Vector2D.VectorFromAngle(angle), i + 0.5);
                    b.Sprite = renderer.GetCenterSprite(
                        renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                    bullets.Add(b);
                    // Make a small trailing bullet.
                    b = new Bullet(Math.Max(vari1 - 1, 0), 0,
                        location, Vector2D.VectorFromAngle(angle), i + 1);
                    b.Sprite = renderer.GetCenterSprite(
                        renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                    bullets.Add(b);
                }
                if (vari1 < 2)
                    // It's aimed diagonally.
                    // Shoot along the opposite diagonal.
                    for (int i = 2; i < 5; i++)
                    {
                        Bullet b = new Bullet(0, 1, location,
                            Vector2D.VectorFromAngle(angle + Math.PI), i);
                        b.Sprite = renderer.GetCenterSprite(
                            renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                        bullets.Add(b);
                        b = new Bullet(0, 0, location,
                            Vector2D.VectorFromAngle(angle + Math.PI), i - 0.5);
                        b.Sprite = renderer.GetCenterSprite(
                            renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                        bullets.Add(b);
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
                Bullet b = new Bullet((int)Bullet.BulletColors.Violet, 0,
                    location, Vector2D.VectorFromAngle(0), 1.5);
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Aimed down.
                b = new Bullet(b, Vector2D.VectorFromAngle(Math.PI / 2.0));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Aimed right.
                b = new Bullet(b, Vector2D.VectorFromAngle(Math.PI));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
                // Aimed up.
                b = new Bullet(b, Vector2D.VectorFromAngle(Math.PI * 1.5));
                b.Sprite = renderer.GetCenterSprite(
                    renderer.bulletImages[b.SizeIndex][b.colorIndex]);
                bullets.Add(b);
            }

            if (updateCount >= 51)
                updateCount = 1;
            return movedToDest;
        }

        public bool Update(ref ArrayList bullets, GameRenderer renderer,
            Vector2f playerPos, Random rand)
        {
            bool finishedMoving = false;
            if (startedPattern)
            {
                if (currentPattern == 0)
                    finishedMoving = UpdatePtrn0(ref bullets, renderer, playerPos, rand);
                else if (currentPattern == 1)
                    finishedMoving = UpdatePtrn1(ref bullets, renderer, playerPos, rand);
                else if (currentPattern == 2)
                    finishedMoving = UpdatePtrn2(ref bullets, renderer, playerPos, rand);
                else if (currentPattern == 3)
                    finishedMoving = UpdatePtrn3(ref bullets, renderer, playerPos, rand);
            }
            else
            {
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
            updateCount++;
            return finishedMoving;
        }

		public void UpdateDisplayPos()
		{
			sprite.setPosition(location + Renderer.FieldUpperLeft);
		}

        public void Draw()
        {
        }
    }
}

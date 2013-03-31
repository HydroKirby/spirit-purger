/*
 * Larry Resnik          Pgm 9-1           Game (Shmup)
 * 
 * Created by SharpDevelop.
 * SharpDevelop Version : 3.0.0.3800
 * .NET Version         : 2.0.50727.4200
 * 
 * Date: 12/10/2009
 * Time: 6:46 PM
 */

using System;
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
	public class Bullet {
		public static int[] RADII = {2, 4, 8};
		public enum BulletColors {Red, Orange, Green, Blue, Violet, EndColors};
		// The index offset in the array of pre-generated images with which
		// this bullet's graphics will correspond to.
		public int colorIndex = 0;
		protected int sizeIndex = 0;
		protected int radius = 2;
		public Vector2f location;
		protected Vector2f direction;
		protected double speed = 1.0;
		// dx and dy are the result of the direction vector magnified by the
		// speed. They're altered when the Speed accessor is assigned.
		protected float dx = 0.0F;
		protected float dy = 1.0F;
		// Refers to if the bullet touched the player's hitbox,
		// but not the player's hitcircle.
		public bool grazed = false;
		public int lifetime = 0;
		
		public int SizeIndex {
			get { return sizeIndex; }
			set {
				if (value >= 0 && value <= RADII.Length) {
					sizeIndex = value;
					Radius = RADII[value];
				} else if (radius == 0)
					// It was not set before.
					Radius = RADII[0];
			}
		}
		
		public int Radius {
			get { return radius; }
			set {
				if (value >= 0)
					radius = value;
			}
		}
		
		public int Diameter {
			get { return radius + radius; }
		}
		
		public Vector2f Direction {
			get { return direction; }
			set {
				direction = value;
                Vector2D.Normalize(direction);
				this.RefreshVelocity();
			}
		}
		
		public Point DrawLocation {
			get { return new Point((int) location.X - Radius,
			                       (int) location.Y - Radius); }
		}
		
		public double Speed {
			get { return speed; }
			set {
				speed = value;
				this.RefreshVelocity();
			}
		}
		
		public Bullet() {
			location = new Vector2f();
			direction = new Vector2f(dx, dy);
		}
		
		public Bullet(int colorIndex, int sizeIndex) {
			this.colorIndex = colorIndex;
			this.sizeIndex = sizeIndex;
			location = new Vector2f();
			Direction = new Vector2f(dx, dy);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location) {
			this.colorIndex = colorIndex;
			this.sizeIndex = sizeIndex;
			this.location = new Vector2f(location.X, location.Y);
			Direction = new Vector2f(dx, dy);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location,
		              Vector2f direction) {
			this.colorIndex = colorIndex;
			this.sizeIndex = sizeIndex;
			this.location = new Vector2f(location.X, location.Y);
			Direction = new Vector2f(direction.X, direction.Y);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location,
		              Vector2f direction, double speed) {
			this.colorIndex = colorIndex;
			this.sizeIndex = sizeIndex;
            this.location = new Vector2f(location.X, location.Y);
            Direction = new Vector2f(direction.X, direction.Y);
			Radius = RADII[sizeIndex];
			Speed = speed;
		}
		
		public Bullet(Bullet bullet) {
			colorIndex = bullet.colorIndex;
			sizeIndex = bullet.sizeIndex;
			location = new Vector2f(bullet.location.X, bullet.location.Y);
			// Don't use the accessor Size - refresh dx/dy once with the
			// assignment to Direction.
			speed = bullet.speed;
			Direction = new Vector2f(bullet.direction.X, bullet.direction.Y);
			Radius = bullet.radius;
		}
		
		/// <summary>
		/// Makes a copy of the passed Bullet, but uses the passed direction
		/// vector instead of the passed Bullet's direction vector.
		/// </summary>
		/// <param name="bullet">The Bullet to mirror.</param>
		/// <param name="direction">The new direction vector.</param>
		public Bullet(Bullet bullet, Vector2f direction) {
			colorIndex = bullet.colorIndex;
			sizeIndex = bullet.sizeIndex;
			location = new Vector2f(bullet.location.X, bullet.location.Y);
			// Don't use the accessor Size - refresh dx/dy once with the
			// assignment to Direction.
			speed = bullet.speed;
			Direction = new Vector2f(direction.X, direction.Y);
			Radius = bullet.radius;
		}
		
		/// <summary>
		/// Resets dx and dy so that the overall velocity is correct in
		/// accordance with the direction vector and the speed.
		/// </summary>
		public void RefreshVelocity() {
			dx = (float)(direction.X * speed);
            dy = (float)(direction.Y * speed);
		}
		
		/// <summary>
		/// See if the Bullet is outside of the Rectangle.
		/// </summary>
		/// <param name="rect">The Rectangle that should contain this Bullet.</param>
		/// <param name="lenience">The number of extra pixels the Bullet may be
		/// outside of the Rectangle. When positive, it extends the Bullet's
		/// range of life. When negative, it shortens the Bullet's lifespan.</param>
		/// <returns></returns>
		public bool isOutside(Rectangle rect, int lenience) {
			return location.X - radius + lenience <= rect.Left ||
				location.X + radius - lenience >= rect.Right ||
				location.Y - radius + lenience <= rect.Top ||
				location.Y + radius - lenience >= rect.Bottom;
		}
		
		/// <summary>
		/// Hit test for a bullet-against-bullet.
		/// </summary>
		/// <param name="bullet">The other bullet to compare to.</param>
		/// <returns>Whether or not the bullets have collided.</returns>
		public bool HitTest(Bullet bullet) {
			return HitTest(bullet.location, bullet.Radius);
		}
		
		/// <summary>
		/// HitTest for a non-classed circle.
		/// </summary>
		/// <param name="pt">The center of the passed circle.</param>
		/// <param name="radius">The radius of the passed circle.</param>
		/// <returns>Whether or not the circles have collided.</returns>
		public bool HitTest(Vector2f pt, int radius) {
			int a = (this.radius + radius) * (this.radius + radius);
			double dx = location.X - pt.X;
			double dy = location.Y - pt.Y;
			return a > (dx * dx) + (dy * dy);
		}
		
		/// <summary>
		/// Returns a color based on the index in the pre-generated bullet
		/// images array.
		/// </summary>
		/// <param name="colorIndex">The array index relating to bullet color.</param>
		/// <returns>The Color value of the bullet.</returns>
		public static Color GetColorByIndex(int colorIndex) {
			switch ((BulletColors) colorIndex) {
				case BulletColors.Blue: return Color.Blue;
				case BulletColors.Green: return Color.Green;
				case BulletColors.Orange: return Color.Cyan;
				case BulletColors.Red: return Color.Red;
				case BulletColors.Violet: return Color.Magenta;
				default: return Color.Black;
			}
		}

        /// <summary>
        /// Returns a color's name as a string.
        /// </summary>
        /// <param name="colorIndex">The array index relating to bullet color.</param>
        /// <returns>The name of the color.</returns>
        public static String GetColorByName(int colorIndex)
        {
            switch ((BulletColors)colorIndex)
            {
                case BulletColors.Blue: return "blue";
                case BulletColors.Green: return "green";
                case BulletColors.Orange: return "orange";
                case BulletColors.Red: return "red";
                case BulletColors.Violet: return "violet";
                default: return "black";
            }
        }
		
		public virtual void Update() {
			location.X += dx;
			location.Y += dy;
			lifetime++;
		}
	}
	
	public class Bomb : Bullet {
		// The time when the bomb comes from the player and inflates.
		public const int GROW_FRAMES = 35;
		// The time the bomb lasts at full power.
		public const int ACTIVE_FRAMES = GROW_FRAMES + 170;
		// At full size, this is the bomb's radius.
		public const int FULL_RADIUS = 50;
		
		public Bomb(Vector2f pt) : base(0, 0, pt) {}
		
		public override void Update() {
			lifetime++;
			if (lifetime <= GROW_FRAMES) {
				location.Y -= 2;
				Radius = (int) ((double) lifetime / GROW_FRAMES * FULL_RADIUS);
			}
		}
		
        /*
		public void Draw(Graphics g) {
			// Fade from yellow to green a tiny bit during full expansion.
			// This formula circulates every 10 frames. It changes yellow color
			// to green in increments of 2 between the values 230 and 210.
			
			int val = 230;
			if (lifetime > GROW_FRAMES) {
				// This is the ones-digit of lifetime.
				int framePortion = (int) (lifetime % 10);
				if (framePortion <= 5)
					// Fade-out gradually.
					val -= framePortion * 2;
				else
					// Fade-in gradually. num + max - 2 * num is a formula that
					// makes smaller output as num gets bigger.
					val -= (framePortion + 2 - 2 * framePortion) * 2;
			}
			SolidBrush solidBrush;
			solidBrush = new SolidBrush(Color.FromArgb(150, val, 230, 0));
			g.FillEllipse(solidBrush, (int) location.X - radius,
				(int) location.Y - radius, radius + radius, radius + radius);
			solidBrush.Dispose();
		}
         */

        public void Draw()
        {
        }
	}
}

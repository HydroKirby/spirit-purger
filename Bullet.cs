/*
 * Larry Resnik
 * 
 * Since: 12/10/2009
 */

using System;
using System.Collections;
using SFML.Window;
using SFML.Graphics;

namespace SpiritPurger
{
	public class BulletCreator
	{
		// Sprite sheets of images shared between all bullets.
		protected ArrayList bulletImages;
		// Stores distinct bullets based on radius, sprite, etc.
		protected ArrayList bulletTypes;

		public BulletCreator(Renderer renderer)
		{
			bulletImages = new ArrayList();
			bulletTypes = new ArrayList();
			LoadBulletImages(renderer);
		}

		/// <summary>
		/// Makes all bullet images for the first time.
		/// All images are put into the bulletImages array.
		/// </summary>
		private void LoadBulletImages(Renderer renderer)
		{
			string[] filenames = { "b_4.png", "b_8.png", "b_16.png" };
			for (int i = 0; i < filenames.Length; ++i)
			{
				Texture spriteSheetImage = renderer.LoadImage(filenames[i]);
				bulletImages.Add(spriteSheetImage);
				// The images are vertically aligned.
				// There should be 5 images per sheet.
				int numSubImages = (int)(spriteSheetImage.Size.Y / spriteSheetImage.Size.X);
				for (int j = 0; j < numSubImages; ++j)
				{
					// Width is the same as height for a square sub-image,
					// so use the width as a height multiplier.
					bulletTypes.Add(new BulletType(
						// radius is half the width
						(int)spriteSheetImage.Size.X / 2,
						// image index is the last element in the array.
						bulletImages.Count - 1,
						// The subrect is x=0, y=offset_from_top, width, height(=width)
						new IntRect(0, (int)spriteSheetImage.Size.X * j,
							(int)spriteSheetImage.Size.X, (int)spriteSheetImage.Size.X)));
				}
			}
		}

		public CenterSprite MakeBulletSprite(int index)
		{
			BulletType type = (BulletType)bulletTypes[index];
			return new CenterSprite((Texture)bulletImages[type.ImageIndex], type.SubRect);
		}

		public CenterSprite MakeBulletSprite(int sizeIndex, int colorIndex)
		{
			// This is a temporary function. Remove when colorIndex and sizeIndex
			// are removed from the codebase entirely.
			int index = sizeIndex * 5 + colorIndex;
			return MakeBulletSprite(index);
		}

		protected Bullet MakeBullet(BulletType type)
		{
			int radius = type.Radius;
			Bullet bullet = new Bullet(radius);
			bullet.Sprite = new CenterSprite(
				(Texture)bulletImages[type.ImageIndex], type.SubRect);
			return bullet;
		}

		public Bullet MakeBullet(int index)
		{
			BulletType type = (BulletType)bulletTypes[index];
			Bullet bullet = MakeBullet(type);
			return bullet;
		}

		public Bullet MakeBullet(int index, Vector2f loc)
		{
			BulletType type = (BulletType)bulletTypes[index];
			Bullet bullet = MakeBullet(type);
			bullet.location = loc;
			return bullet;
		}

		public Bullet MakeBullet(int index, Vector2f loc, Vector2f dir)
		{
			BulletType type = (BulletType)bulletTypes[index];
			Bullet bullet = MakeBullet(type);
			bullet.location = loc;
			bullet.Direction = dir;
			return bullet;
		}

		public Bullet MakeBullet(int index, Vector2f loc, Vector2f dir, double speed)
		{
			BulletType type = (BulletType)bulletTypes[index];
			Bullet bullet = MakeBullet(type);
			bullet.location = loc;
			bullet.Direction = dir;
			bullet.Speed = speed;
			return bullet;
		}

		public Bullet MakeBullet(Bullet b)
		{
			Bullet bullet = new Bullet(b);
			BulletType type = (BulletType)bulletTypes[b.typeID];
			bullet.Sprite = new CenterSprite(
				(Texture)bulletImages[type.ImageIndex], type.SubRect);
			return bullet;
		}

		public Bullet MakeBullet(Bullet b, Vector2f dir)
		{
			Bullet bullet = MakeBullet(b);
			bullet.Direction = dir;
			return bullet;
		}
	}

	public class BulletType
	{
		protected int radius;
		// Index of the texture in the array of bullet sprite sheets.
		protected int imageIndex;
		// The sprite to take from the sprite sheet.
		protected IntRect subRect;
		protected bool rotates;

		public BulletType(int radius, int imgIndex, IntRect rect)
		{
			imageIndex = imgIndex;
			subRect = rect;
			this.radius = radius;
			rotates = false;
		}

		public int Radius { get { return radius; } }
		public int ImageIndex { get { return imageIndex; } }
		public IntRect SubRect { get { return subRect; } }
		public bool Rotates
		{
			get { return rotates; }
			set { rotates = value; }
		}
	}

	public class Bullet {
		public const uint LIFETIME_DEFAULT = 4000;
		public const uint LIFETIME_PARTICLE = 5;
		public enum BulletColors {Red, Orange, Green, Blue, Violet, EndColors};
		// The program will load the different types of bullets and
		// assign them typeIDs to differentiate the base types.
		public int typeID = 0;
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
		protected uint lifetime = LIFETIME_DEFAULT;
        protected CenterSprite sprite;

        public CenterSprite Sprite
        {
            get { return sprite; }
            set
			{
				sprite = value;
				UpdateDisplayPos();
			}
        }
		
		public int Radius {
			get { return radius; }
			set { radius = value; }
		}
		
		public int Diameter {
			get { return radius + radius; }
		}
		
		public Vector2f Direction {
			get { return direction; }
			set {
				direction = value;
                VectorLogic.Normalize(direction);
				this.RefreshVelocity();
			}
		}
		
		public double Speed {
			get { return speed; }
			set {
				speed = value;
				this.RefreshVelocity();
			}
		}

		public uint Lifetime
		{
			get { return lifetime; }
			set
			{
				lifetime = value;
			}
		}
		
		public Bullet() {
			location = new Vector2f();
			Direction = new Vector2f(dx, dy);
		}

		public Bullet(int radius)
		{
			Radius = radius;
			location = new Vector2f();
			Direction = new Vector2f(dx, dy);
		}

		public Bullet(int radius, Vector2f loc)
		{
			Radius = radius;
			location = new Vector2f(loc.X, loc.Y);
			Direction = new Vector2f(dx, dy);
		}

		public Bullet(int radius, Vector2f loc, Vector2f dir)
		{
			Radius = radius;
			location = new Vector2f(loc.X, loc.Y);
			Direction = new Vector2f(dir.X, dir.Y);
		}

		public Bullet(int radius, Vector2f loc, Vector2f dir, double speed)
		{
			Radius = radius;
			location = new Vector2f(loc.X, loc.Y);
			Direction = new Vector2f(dir.X, dir.Y);
			Speed = speed;
		}
		
		/*
		public Bullet(int colorIndex, int sizeIndex) {
			this.typeID = colorIndex;
			location = new Vector2f();
			Direction = new Vector2f(dx, dy);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location) {
			this.typeID = colorIndex;
			this.sizeIndex = sizeIndex;
			this.location = new Vector2f(location.X, location.Y);
			Direction = new Vector2f(dx, dy);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location,
		              Vector2f direction) {
			this.typeID = colorIndex;
			this.sizeIndex = sizeIndex;
			this.location = new Vector2f(location.X, location.Y);
			Direction = new Vector2f(direction.X, direction.Y);
			Radius = RADII[sizeIndex];
		}
		
		public Bullet(int colorIndex, int sizeIndex, Vector2f location,
		              Vector2f direction, double speed) {
			this.typeID = colorIndex;
			this.sizeIndex = sizeIndex;
            this.location = new Vector2f(location.X, location.Y);
            Direction = new Vector2f(direction.X, direction.Y);
			Radius = RADII[sizeIndex];
			Speed = speed;
		}
		 * */
		
		public Bullet(Bullet bullet) {
			typeID = bullet.typeID;
			location = new Vector2f(bullet.location.X, bullet.location.Y);
			// Don't use the accessor Size - refresh dx/dy once with the
			// assignment to Direction.
			speed = bullet.speed;
			Direction = new Vector2f(bullet.direction.X, bullet.direction.Y);
			Radius = bullet.radius;
		}

        ~Bullet()
        {
            if (sprite != null)
                sprite.Dispose();
        }
		
		/// <summary>
		/// Makes a copy of the passed Bullet, but uses the passed direction
		/// vector instead of the passed Bullet's direction vector.
		/// </summary>
		/// <param name="bullet">The Bullet to mirror.</param>
		/// <param name="direction">The new direction vector.</param>
		public Bullet(Bullet bullet, Vector2f direction) {
			typeID = bullet.typeID;
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
		/// Check if the Bullet is outside of the game field.
		/// </summary>
		/// <param name="extremity">The size of the game field.</param>
		/// <param name="lenience">How far beyond the game field the bullet can exist.</param>
		/// <returns>True if this bullet has left the game field.</returns>
		public bool IsOutside(Vector2f extremity, int lenience)
		{
			return location.X - radius + lenience <= 0 ||
				location.X + radius - lenience >= extremity.X ||
				location.Y - radius + lenience <= 0 ||
				location.Y + radius - lenience >= extremity.Y;
		}

		/// <summary>
		/// Checks if the bullet has no more life time.
		/// </summary>
		/// <returns>True if lifetime is zero.</returns>
		public bool IsGone() { return lifetime <= 0; }

		/// <summary>
		/// Simply sets the bullet's lifetime to zero.
		/// </summary>
		public void Kill() { lifetime = 0; }
		
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

		public void UpdateDisplayPos()
		{
			if (sprite != null)
			{
				sprite.setPosition(location + Renderer.FieldUpperLeft);
			}
		}
		
		public virtual void Update() {
			location.X += dx;
			location.Y += dy;
			lifetime = lifetime > 0 ? lifetime - 1 : 0;
			UpdateDisplayPos();
		}
	}

	public class Hitbox : Bullet
	{
		public Hitbox()
		{
			radius = 2;
		}
	}

	public class Bomb : Bullet {
		// The time the bomb lasts at full power.
		public const int LIFETIME_ACTIVE = 220;
		// The time spent inflating the bomb radius.
		public const int LIFETIME_GROWING = 30;
		// At full size, this is the bomb's radius.
		public const int FULL_RADIUS = 100;
		private static Vector2f SCALE_ONE = new Vector2f(1, 1);
		
		public Bomb(Vector2f pt) : base(0, pt) {
			lifetime = LIFETIME_ACTIVE;
			dx = 0.0F;
			dy = -2.0F;
		}
		
		public override void Update() {
			base.Update();
			if (lifetime > LIFETIME_ACTIVE - LIFETIME_GROWING)
			{
				float scale = (float) (LIFETIME_ACTIVE - lifetime) / LIFETIME_GROWING;
				sprite.Scale = SCALE_ONE * scale;
				Radius = (int)(scale * FULL_RADIUS);
			}
			else if (lifetime == LIFETIME_ACTIVE - LIFETIME_GROWING)
			{
				dy = 0.0F;
				sprite.Scale = SCALE_ONE;
				Radius = FULL_RADIUS;
			}
			UpdateDisplayPos();
		}

		/// <summary>
		/// Restores the bomb into activity.
		/// </summary>
		/// <param name="pos">Where to spawn the bomb.</param>
		public void Renew(Vector2f pos)
		{
			location = new Vector2f(pos.X, pos.Y);
			lifetime = LIFETIME_ACTIVE;
			dy = -2.0F;
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
	}
}

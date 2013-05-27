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

		public Bullet MakeBullet(BulletProp b)
		{
			BulletType type = (BulletType)bulletTypes[b.typeID];
			return new Bullet( new CenterSprite(
				(Texture)bulletImages[type.ImageIndex], type.SubRect),
				type.Radius, b.Location,
				b.Direction, b.Speed);
		}
	}

	/// <summary>
	/// Defines how a bullet behaves and appears.
	/// Rendering details like its sprite and sound effects are stored.
	/// Behavior like its radius and trajectory pattern are stored.
	/// </summary>
	public class BulletType
	{
		protected int radius;
		// Index of the texture in the array of bullet sprite sheets.
		protected int imageIndex;
		// The sprite to take from the sprite sheet.
		protected IntRect subRect;
		protected bool rotates;
		protected int sfxIndex;

		public BulletType(int radius, int imgIndex, IntRect rect)
		{
			imageIndex = imgIndex;
			subRect = rect;
			this.radius = radius;
			rotates = false;
			sfxIndex = 0;
		}

		public int Radius { get { return radius; } }
		public int ImageIndex { get { return imageIndex; } }
		public int SFXIndex { get { return sfxIndex; } }
		public IntRect SubRect { get { return subRect; } }
		public bool Rotates
		{
			get { return rotates; }
			set { rotates = value; }
		}
	}

	/// <summary>
	/// Properties that provide a template for making a bullet instance.
	/// Stores the type of bullet it will become along with the bullet's
	/// instantiation variables.
	/// </summary>
	public class BulletProp
	{
		public int typeID = 0;
		protected double _speed = 0.0;
		protected Vector2f _location;
		protected Vector2f _direction;

		public double Speed
		{
			get { return _speed; }
			set { _speed = value; }
		}

		public Vector2f Location
		{
			get { return _location; }
			set { _location = new Vector2f(value.X, value.Y); }
		}

		public Vector2f Direction
		{
			get { return _direction; }
			set { _direction = new Vector2f(value.X, value.Y); }
		}

		public BulletProp()
		{
			_location = new Vector2f();
			_direction = new Vector2f();
		}

		public BulletProp(int type) : this()
		{
			typeID = type;
		}

		public BulletProp(int type, Vector2f loc)
		{
			typeID = type;
			Location = loc;
			_direction = new Vector2f();
		}

		public BulletProp(int type, Vector2f loc, Vector2f dir)
		{
			typeID = type;
			Location = loc;
			Direction = dir;
		}

		public BulletProp(int type, Vector2f loc, Vector2f dir, double speed)
			: this(type, loc, dir)
		{
			this._speed = speed;
		}
		
		public BulletProp(Bullet b)
			: this(b.typeID, b.location, b.Direction, b.Speed)
		{ }

		public BulletProp(Bullet b, Vector2f direction)
			: this(b.typeID, b.location, direction, b.Speed)
		{ }

		public BulletProp(BulletProp b)
			: this(b.typeID, b.Location, b.Direction, b.Speed)
		{ }

		public BulletProp(BulletProp b, Vector2f direction)
			: this(b.typeID, b.Location, direction, b.Speed)
		{ }

		/// <summary>
		/// Duplicates the instance's own instances to avoid shared memory.
		/// </summary>
		public void Renew()
		{
			Location = Location;
			Direction = Direction;
		}
	}

	public class Bullet {
		public const uint LIFETIME_DEFAULT = 4000;
		public const uint LIFETIME_PARTICLE = 5;
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
			set { lifetime = value; }
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
		
		public Bullet(Bullet bullet) {
			typeID = bullet.typeID;
			location = new Vector2f(bullet.location.X, bullet.location.Y);
			// Don't use the accessor Size - refresh dx/dy once with the
			// assignment to Direction.
			speed = bullet.speed;
			Direction = new Vector2f(bullet.direction.X, bullet.direction.Y);
			Radius = bullet.radius;
		}

		// Note: This is the master constructor which is the only one to use.
		public Bullet(CenterSprite sprite, int radius, Vector2f loc,
			Vector2f dir, double speed)
		{
			this.sprite = sprite;
			Radius = radius;
			location = new Vector2f(loc.X, loc.Y);
			Direction = new Vector2f(dir.X, dir.Y);
			Speed = speed;
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

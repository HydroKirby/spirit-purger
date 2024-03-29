﻿using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;

namespace SpiritPurger
{
	public class ImageManager
	{
		protected Dictionary<string, Texture> _images;
		// For consistency, all(?) image names are accessible here.
		// They should be loadable with LoadPNG.
		// Game resource images.
		public const String GAME_ICON = "icon";
		// Title Screen images.
		public const String TITLE_BG = "title";
		// In-game images.
		public const String BULLET_4 = "b_4";
		public const String BULLET_8 = "b_8";
		public const String BULLET_16 = "b_16";
		public const String BULLET_BOMB = "bomb";
		public const String BULLET_PLAYER = "b_player";
		public const String BG = "bg";
		public const String SPARK_GRAZE = "spark_graze";
		public const String SPARK_HIT = "spark_nailed_foe";
		public const String BOSS_FORWARD = "boss_forward";
		public const String BOSS_LEFT = "boss_left";
		public const String BOSS_RIGHT = "boss_right";
		public const String PLAYER_FORWARD = "p_forward";
		public const String PLAYER_LEFT = "p_left";
		public const String PLAYER_RIGHT = "p_right";
        public const String PLAYER_DYING = "p_dying";
		public const String HEALTHBAR_BORDER = "healthbar";
        public const String BOSS_DYING = "boss_dying";

		public ImageManager()
		{
			_images = new Dictionary<string, Texture>(StringComparer.Ordinal);
		}

		/// <summary>
		/// Loads an image and gives a replacement on failure.
		/// </summary>
		/// <param name="filename">Where the image is.</param>
		/// <returns>The loaded image on success or a 1x1 Texture otherwise.</returns>
		public bool LoadPNG(String name)
		{
			bool success = true;
			Texture img;
			try
			{
				img = new Texture("res/img/" + name + ".png");
			}
			catch (ArgumentException)
			{
				img = new Texture(1, 1);
				success = false;
			}
			_images[name] = img;
			return success;
		}

		public Texture GetImage(String name)
		{
			return _images[name];
		}

		public Sprite GetSprite(String name)
		{
			return new Sprite(_images[name]);
		}

		public Sprite GetSprite(String name, IntRect subrect)
		{
			return new Sprite(_images[name], subrect);
		}

		public CenterSprite GetCenterSprite(String name)
		{
			return new CenterSprite(_images[name]);
		}

		public CenterSprite GetCenterSprite(String name, IntRect subrect)
		{
			return new CenterSprite(_images[name], subrect);
		}

		/// <summary>
		/// Turns an evenly divided vertical sprite sheet into separate sprites.
		/// </summary>
		/// <param name="name">The image filename.</param>
		/// <returns>A List of each unique sprite.</returns>
		public List<Sprite> GetSpriteSheet(String name)
		{
			Texture img = _images[name];
			int numSubImages = (int)(img.Size.Y / img.Size.X);
			List<Sprite> sprites = new List<Sprite>(numSubImages);

			for (int i = 0; i < numSubImages; i += 1)
			{
				// The subrect is x=0, y=offset_from_top, width, height(=width)
				sprites.Add(new CenterSprite(img,
					new IntRect(0, (int)img.Size.X * i,
						(int)img.Size.X, (int)img.Size.X)) );
			}

			return sprites;
		}
	}

	/// <summary>
	/// A sprite whose origin is at the image's center.
	/// Used to keep track of which sprites are centered or not.
	/// </summary>
	public class CenterSprite : Sprite
	{
		public CenterSprite(Texture img)
		{
			Texture = img;
			Origin = new Vector2f(Texture.Size.X * 0.5F,
				Texture.Size.Y * 0.5F);
		}

		public CenterSprite(Texture img, IntRect subRect)
		{
			Texture = img;
			TextureRect = subRect;
			Origin = new Vector2f(subRect.Width * 0.5F,
				subRect.Height * 0.5F);
		}

		public void setPosition(Vector2f v)
		{
			Position = new Vector2f(v.X, v.Y);
		}
	}

	/// <summary>
	/// Stores sprites to create an animation.
	/// </summary>
	public class Animation
	{
		public enum ANIM_STYLE { LOOP, PINGPONG, UNANIMATED, END_STYLES }
		protected ANIM_STYLE _style;
		protected String _imageName;
		protected List<Sprite> _sprites;
		// How fast the sprites transition.
		protected int _anim_speed;
		// Frames count DOWN from the animation speed to zero.
		protected int _frame;
		// Index of the sprite being used from the list of sprites.
		protected int _currentSpriteIdx;
		// When using PINGPONG, go backwards through sprites.
		protected bool _spriteTransitionBackwards;

		public ANIM_STYLE Style
		{
			get { return _style; }
			set { _style = value; }
		}

		public List<Sprite> Sprites
		{
			get { return _sprites; }
		}

		public int AniSpeed
		{
			get { return _anim_speed; }
			set { _anim_speed = value; }
		}

		public int Frame
		{
			get { return _frame; }
		}

        public Sprite CurrentSprite
        {
            get { return _sprites[_currentSpriteIdx]; }
        }

		public Animation(ImageManager imgMan, String filename, ANIM_STYLE style, int speed)
		{
			_spriteTransitionBackwards = false;
			_imageName = filename;
			AniSpeed = speed;
			Style = style;
			_frame = speed;
			imgMan.LoadPNG(filename);
			_sprites = imgMan.GetSpriteSheet(filename);
			if (_sprites.Count == 1)
				Style = ANIM_STYLE.UNANIMATED;
            Reset();
		}

        public void Reset()
        {
            _currentSpriteIdx = 0;
            _frame = 0;
        }

		public static ANIM_STYLE GetStyleFromString(String str)
		{
			if (str.CompareTo("pingpong") == 0)
				return ANIM_STYLE.PINGPONG;
			else if (str.CompareTo("loop") == 0)
				return ANIM_STYLE.LOOP;
			else
				return ANIM_STYLE.UNANIMATED;
		}

		/// <summary>
		/// Updates the animation by incrementing the frame.
		/// </summary>
		/// <param name="elapsed">How much time has passed since the last draw.</param>
		public void Update(int elapsed)
		{
			_frame -= elapsed;
			if (Frame <= 0)
			{
				// Save the old position to give to the new sprite.
				Vector2f oldPos = _sprites[_currentSpriteIdx].Position;
				if (Style == ANIM_STYLE.LOOP)
				{
					_currentSpriteIdx += 1;
					if (_currentSpriteIdx >= Sprites.Count)
						_currentSpriteIdx = 0;
				}
				else if (Style == ANIM_STYLE.PINGPONG)
				{
					if (_spriteTransitionBackwards)
					{
						_currentSpriteIdx -= 1;
						if (_currentSpriteIdx < 0)
						{
							_currentSpriteIdx = 1;
							_spriteTransitionBackwards = false;
						}
					}
					else
					{
						_currentSpriteIdx += 1;
						if (_currentSpriteIdx >= Sprites.Count)
						{
							_currentSpriteIdx -= 2;
							_spriteTransitionBackwards = true;
						}
					}
				}
				// Give the old position to the new sprite.
                CurrentSprite.Position =
                    new Vector2f(oldPos.X, oldPos.Y);
				_frame = _anim_speed;
			}
		}

		/// <summary>
		/// Updates the animation by incrementing the frame.
		/// </summary>
		/// <param name="pos">The new position of the object.</param>
		/// <param name="elapsed">How much time has passed since the last draw.</param>
		public void Update(int elapsed, Vector2f pos)
		{
			Update(elapsed);
			CurrentSprite.Position = new Vector2f(pos.X, pos.Y);
		}

		public void Draw(RenderWindow app)
		{
			app.Draw(CurrentSprite);
		}
	}

	public class AniPlayer
	{
		public enum ANI_STATE { FORWARD, LEFT, RIGHT, DYING, };
		protected ANI_STATE _state;
		protected static Animation _forwardAni = null;
		protected static Animation _leftAni = null;
		protected static Animation _rightAni = null;
        protected static Animation _dyingAni = null;

		public ANI_STATE State
		{
			get { return _state; }
			set
			{
				_state = value;
				SetCurrentAnimation();
                CurrentAnimation.Reset();
			}
		}

        public Animation CurrentAnimation
        {
            get;
            protected set;
        }

		public AniPlayer(ImageManager imgMan, Animation.ANIM_STYLE style, int anim_speed)
		{
			if (_forwardAni == null)
				_forwardAni = new Animation(imgMan, ImageManager.PLAYER_FORWARD,
					style, anim_speed);
            if (_dyingAni == null)
                _dyingAni = new Animation(imgMan, ImageManager.PLAYER_DYING,
                    style, anim_speed);
			State = ANI_STATE.FORWARD;
		}

		protected void SetCurrentAnimation()
		{
            switch (State)
            {
                case ANI_STATE.FORWARD:
                    CurrentAnimation = _forwardAni;
                    break;
                case ANI_STATE.LEFT:
                    CurrentAnimation = _leftAni;
                    break;
                case ANI_STATE.RIGHT:
                    CurrentAnimation = _rightAni;
                    break;
                case ANI_STATE.DYING:
                    CurrentAnimation = _dyingAni;
                    break;
                default:
                    CurrentAnimation = _forwardAni;
                    break;
            }
		}

        public void Reset()
        {
            CurrentAnimation.Reset();
        }

		public void Update(int elapsed)
		{
            CurrentAnimation.Update(elapsed);
		}

		public void Update(int elapsed, Vector2f pos)
		{
            CurrentAnimation.Update(elapsed, pos);
		}

		public void Draw(RenderWindow app)
		{
            CurrentAnimation.Draw(app);
		}
	}

	public class AniBoss
	{
		public enum ANI_STATE { FORWARD, LEFT, RIGHT, DYING, };
		protected ANI_STATE _state;
		protected static Animation _forwardAni = null;
		protected static Animation _leftAni = null;
		protected static Animation _rightAni = null;
        protected static Animation _dyingAni = null;

		public ANI_STATE State
		{
			get { return _state; }
			set
			{
                _state = value;
                SetCurrentAnimation();
                CurrentAnimation.Reset();
			}
		}

        public Animation CurrentAnimation
        {
            get;
            protected set;
        }

		public AniBoss(ImageManager imgMan, Animation.ANIM_STYLE style, int anim_speed)
		{
			if (_forwardAni == null)
				_forwardAni = new Animation(imgMan, ImageManager.BOSS_FORWARD,
					style, anim_speed);
            if (_dyingAni == null)
                _dyingAni = new Animation(imgMan, ImageManager.BOSS_DYING,
                    style, anim_speed);
			State = ANI_STATE.FORWARD;
		}

        public Sprite GetSprite()
        {
            return CurrentAnimation.CurrentSprite;
        }

        protected void SetCurrentAnimation()
        {
            switch (State)
            {
                case ANI_STATE.FORWARD:
                    CurrentAnimation = _forwardAni;
                    break;
                case ANI_STATE.DYING:
                    CurrentAnimation = _dyingAni;
                    break;
                default:
                    CurrentAnimation = _forwardAni;
                    break;
            }
        }

        public void Reset()
        {
            CurrentAnimation.Reset();
        }

        public void Update(int elapsed)
        {
            CurrentAnimation.Update(elapsed);
        }

        public void Update(int elapsed, Vector2f pos)
        {
            CurrentAnimation.Update(elapsed, pos);
        }

        public void Draw(RenderWindow app)
        {
            CurrentAnimation.Draw(app);
        }
	}
}

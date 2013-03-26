/*
 * Larry Resnik          Pgm 9-1           Game (Shmup)
 * 
 * Created by SharpDevelop.
 * SharpDevelop Version : 3.0.0.3800
 * .NET Version         : 2.0.50727.4200
 * 
 * Date: 11/20/2009
 * Time: 6:17 PM
 */

using System;
using SFML.Window;

namespace TestSFMLDotNet {
	/// <summary>
	/// Maintains how long specific keys have been held down.
	/// </summary>
	public class KeyHandler {
		// After this time, don't register a press as a double-tap.
		public static int DOUBLE_TAP_LENIENCE = 10;
		
        /*
         * These are the old key mappings before I used SFML.
		public const int K_ESCAPE = 27;
		public const int K_PERIOD = 190;
		public const int K_COMMA = 188;
		public const int K_SLASH = 191;
		public const int K_L = 76;
		public const int K_LEFT = 37;
		public const int K_RIGHT = 39;
		public const int K_UP = 38;
		public const int K_DOWN = 40;
		public const int K_LSHIFT = 16;
		public const int K_Z = 90;
		public const int K_X = 88;
         */
        public const Keyboard.Key K_ESCAPE = Keyboard.Key.Escape;
        public const Keyboard.Key K_PERIOD = Keyboard.Key.Period;
        public const Keyboard.Key K_COMMA = Keyboard.Key.Comma;
        public const Keyboard.Key K_SLASH = Keyboard.Key.Slash;
        public const Keyboard.Key K_L = Keyboard.Key.L;
        public const Keyboard.Key K_LEFT = Keyboard.Key.Left;
        public const Keyboard.Key K_RIGHT = Keyboard.Key.Right;
        public const Keyboard.Key K_UP = Keyboard.Key.Up;
        public const Keyboard.Key K_DOWN = Keyboard.Key.Down;
        public const Keyboard.Key K_LSHIFT = Keyboard.Key.LShift;
        public const Keyboard.Key K_Z = Keyboard.Key.Z;
        public const Keyboard.Key K_X = Keyboard.Key.X;
		
        // These are integers because they are also hold-down times.
		public int left = 0;
		public int right = 0;
		public int up = 0;
		public int down = 0;
		public int shoot = 0;
		public int slow = 0;
		public int bomb = 0;
		
		// The update counts before the previous time the corresponding button
		//   was pressed. Used to register double-tapping.
		public int prevLeft = 0;
		public int prevRight = 0;
		public int prevUp = 0;
		public int prevDown = 0;
		public int prevShoot = 0;
		public int prevSlow = 0;
		public int prevBomb = 0;
		
		public bool TappedLeft {
			get {  return left > 0 && prevLeft - left < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedRight {
			get {  return right > 0 && prevRight-right < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedUp {
			get {  return up > 0 && prevUp - up < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedDown {
			get {  return down > 0 && prevDown - down < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedShoot {
			get {  return shoot > 0 && prevShoot-shoot < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedSlow {
			get {  return slow > 0 && prevSlow - slow < DOUBLE_TAP_LENIENCE; }
		}
		public bool TappedBomb {
			get {  return bomb > 0 && prevBomb - bomb < DOUBLE_TAP_LENIENCE; }
		}
		
		public KeyHandler() {}
		
		public void KeyDown(Keyboard.Key key) {
			if (key == K_DOWN || key == K_PERIOD)
				down = 1;
			else if (key == K_LEFT || key == K_COMMA)
				left = 1;
			else if (key == K_RIGHT || key == K_SLASH)
				right = 1;
			else if (key == K_UP || key == K_L)
				up = 1;
			else if (key == K_LSHIFT)
				slow = 1;
			else if (key == K_Z)
				shoot = 1;
			else if (key == K_X)
				bomb = 1;
		}
		
		public void KeyUp(Keyboard.Key key) {
			if (key == K_DOWN || key == K_PERIOD) {
				down = 0;
				prevDown = 1;
			} else if (key == K_LEFT || key == K_COMMA) {
				left = 0;
				prevLeft = 1;
			} else if (key == K_RIGHT || key == K_SLASH) {
				right = 0;
				prevRight = 1;
			} else if (key == K_UP || key == K_L) {
				up = 0;
				prevUp = 1;
			} else if (key == K_LSHIFT) {
				slow = 0;
				prevSlow = 1;
			} else if (key == K_Z) {
				shoot = 0;
				prevShoot = 1;
			} else if (key == K_X) {
				bomb = 0;
				prevBomb = 1;
			}
		}
		
		/// <summary>
		/// Returns The overall direction headed based on whether left, right,
		/// or both keys are held. When both are held, the result is neutral.
		/// </summary>
		/// <returns>1, 0, or -1 for right, stationary, and left respectively.</returns>
		public int Horizontal() {
			return left > 0 ? (right > 0 ? 0 : -1) : (right > 0 ? 1 : 0);
		}
		
		/// <summary>
		/// Returns The overall direction headed based on whether up, down,
		/// or both keys are held. When both are held, the result is neutral.
		/// </summary>
		/// <returns>1, 0, or -1 for down, stationary, and up respectively.</returns>
		public int Vertical() {
			return up > 0 ? (down > 0 ? 0 : -1) : (down > 0 ? 1 : 0);
		}

        /// <summary>
        /// Gives an int's incremented value if it's non-zero.
        /// </summary>
        /// <param name="val">The int to increment.</param>
        /// <returns>The potentially incremented int.</returns>
        protected int IncrementIfOn(int val)
        {
            int ret = val;
            if (val > 0 && val < Int32.MaxValue)
                ret += 1;
            // If val = 0, then ret = 0
            // If val = MaxInt, then ret = MaxInt
            return ret;
        }
		
		/// <summary>
		/// Increments every held key's hold time. In the case of overflow, the
		/// key hold time becomes 1.
		/// </summary>
		public void Update() {
			up = IncrementIfOn(up);
            down = IncrementIfOn(down);
            left = IncrementIfOn(left);
            right = IncrementIfOn(right);
            shoot = IncrementIfOn(shoot);
            slow = IncrementIfOn(slow);
            bomb = IncrementIfOn(bomb);
            prevUp = IncrementIfOn(prevUp);
            prevDown = IncrementIfOn(prevDown);
            prevLeft = IncrementIfOn(prevLeft);
            prevRight = IncrementIfOn(prevRight);
            prevShoot = IncrementIfOn(prevShoot);
            prevSlow = IncrementIfOn(prevSlow);
            prevBomb = IncrementIfOn(prevBomb);
		}
	}
}

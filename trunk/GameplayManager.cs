using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
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

		// How to react to updates in the gamepaly.
		// These REACTIONs are informed back to the Engine so that the
		// Engine can handle the complicated operations.
		public enum REACTION
		{
			// No particular action to take.
			NONE,
			// End of list of REACTIONS.
			END_REACTIONS
		}

		private REACTION _state;
		public REACTION State { get { return _state; } }

		public GameplayManager()
		{
		}

		/// <summary>
		/// Tells GameplayManager that something has handled this manager's REACTION request.
		/// </summary>
		public void StateHandled()
		{
			_state = REACTION.NONE;
		}
	}
}

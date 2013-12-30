using System;
using System.Collections.Generic;
using SFML.Audio;

namespace SpiritPurger
{
	/// <summary>
	/// Loads and plays sounds and music.
	/// 
	/// The SoundManager loads all sounds upon construction.
	/// To play a sound, call QueueToPlay() using the class' SFX enum.
	/// For example: managerInstance.QueueToPlay(SoundManager.SFX.UNASSIGNED);
	/// When it is time to update the game, call Update() to play sound effects.
	/// </summary>
	public class SoundManager
	{
		public enum SFX
		{
			// Use the first entry for unimplemented SFX.
			UNASSIGNED,
			
			// Menu SFX
			MENU_MOVE,
			MENU_SELECT,

			// Game SFX
			PLAYER_GRAZE,
			PLAYER_SHOT_BULLET,
			PLAYER_BULLET_GAVE_DAMAGE,
			PLAYER_TOOK_DAMAGE,
			PLAYER_SHOT_BOMB,
			BOMB_ACTIVE,
			BOMB_DEACTIVATED,
			BOMB_ATE_BULLET,
			FOE_SHOT_BULLET,
			FOE_TOOK_DAMAGE,
			FOE_TOOK_DAMAGED_WEAKENED,
			FOE_DESTROYED,
			BOSS_DESTROYED,
			FANFARE_PATTERN_SUCCESS,
			FANFARE_PATTERN_FAILURE,

			// Use the last entry to count the number of enums.
			END_SFX
		}
		protected List<SoundBuffer> _sfx;
		protected List<SFX> _queuedSFX;
		protected List<Sound> _playingSounds;
		public const int MAX_SIMULT_SFX = 126;
		protected int _currSFX = 0;
		protected int volume = 100;

		public int Volume
		{
			get { return volume; }
			set
			{
				volume = value;
				foreach (Sound sfx in _playingSounds)
				{
					sfx.Volume = volume;
				}
			}
		}

		public SoundManager()
		{
			AssignAllSFX(out _sfx);
			_queuedSFX = new List<SFX>(MAX_SIMULT_SFX);
			_playingSounds = new List<Sound>(MAX_SIMULT_SFX);
			for (int i = 0; i < MAX_SIMULT_SFX; ++i)
			{
				_playingSounds.Add(new Sound());
			}
		}

		/// <summary>
		/// Gets the filename of the sound in relation to a sound effect.
		/// If a new sound effect file was added as a resource to the game,
		/// it should be added into this function.
		/// </summary>
		/// <param name="action">The action that causes a sound effect.</param>
		/// <returns>A filename for an SFX or silent sound's filename for unassigned SFX.</returns>
		protected String GetSFXFilename(SFX action)
		{
			String ret = "";
			if (action == SFX.MENU_MOVE)
				ret = "button-31.wav";
			else if (action == SFX.MENU_SELECT)
				ret = "button-31.wav";
			else if (action == SFX.FOE_TOOK_DAMAGE)
				ret = "hit_foe.wav";
			else if (action == SFX.FOE_TOOK_DAMAGED_WEAKENED)
				ret = "hit_foe_weak.wav";
			else if (action == SFX.PLAYER_GRAZE)
				ret = "button-15.wav";
			else
				ret = "silence.wav";
			return ret;
		}

		/// <summary>
		/// Loads all SFX into SoundBuffers just once.
		/// </summary>
		/// <param name="sfxList">A Dictionary relating SFX filenames to SFX SoundBuffers.</param>
		/// <returns>True if all loads succeeded. False otherwise.</returns>
		private bool LoadAllSFX(out Dictionary<string, SoundBuffer> sfxFileInstances)
		{
			bool success = true;
			
			sfxFileInstances = new Dictionary<string, SoundBuffer>(StringComparer.Ordinal);
			for (int i = 0; i < (int)SFX.END_SFX; ++i)
			{
				String action = GetSFXFilename((SFX)i);
				if (action.Length > 0 && !sfxFileInstances.ContainsKey(action))
				{
					try
					{
						sfxFileInstances[action] = new SoundBuffer("res/se/" + action);
					}
					catch (SFML.LoadingFailedException)
					{
						// TODO: Log an error of the bad filename.
						success = false;
						break;
					}
				}
			}

			return success;
		}

		/// <summary>
		/// Assigns actions to SoundBuffers.
		/// Care is taken to not load the same SoundBuffer twice.
		/// That means multiple actions can be set to the same SoundBuffer.
		/// </summary>
		/// <param name="sfx">A dictionary of actions set to SFX SoundBuffers.</param>
		/// <returns>True if all loads succeeded. False otherwise.</returns>
		private bool AssignAllSFX(out List<SoundBuffer> sfx)
		{
			Dictionary<string, SoundBuffer> sfxFileInstances;
			bool success = LoadAllSFX(out sfxFileInstances);

			sfx = new List<SoundBuffer>((int) SFX.END_SFX);
			if (success)
			{
				for (int i = 0; i < (int)SFX.END_SFX; ++i)
				{
					sfx.Add(sfxFileInstances[GetSFXFilename((SFX)i)]);
				}
			}
			else
			{
				// Load silent sounds into all of the SFX.
				// TODO: Create error report.
				SoundBuffer silent = sfxFileInstances[GetSFXFilename(SFX.UNASSIGNED)];
				for (int i = 0; i < (int)SFX.END_SFX; ++i)
				{
					sfx.Add(silent);
				}
			}

			return success;
		}

		protected void Play(SFX action)
		{
			_playingSounds[_currSFX].Stop();
			_playingSounds[_currSFX].SoundBuffer = _sfx[(int)action];
			_playingSounds[_currSFX].Play();
			++_currSFX;
			if (_currSFX >= MAX_SIMULT_SFX)
				_currSFX = 0;
		}

		/// <summary>
		/// Adds a sound to play for the next update.
		/// </summary>
		/// <param name="action">The action associated with a sound.</param>
		public void QueueToPlay(SFX action)
		{
			if (!_queuedSFX.Contains(action))
				_queuedSFX.Add(action);
		}

		/// <summary>
		/// Plays all queued sounds.
		/// </summary>
		public void Update()
		{
			// Only inform the player that the boss is weakened.
			if (_queuedSFX.Contains(SFX.FOE_TOOK_DAMAGE) && _queuedSFX.Contains(SFX.FOE_TOOK_DAMAGED_WEAKENED))
				_queuedSFX.Remove(SFX.FOE_TOOK_DAMAGE);

			foreach (SFX action in _queuedSFX)
			{
				Play(action);
			}
			_queuedSFX.Clear();
		}
	}
}

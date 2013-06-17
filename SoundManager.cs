using System;
using System.Collections.Generic;
using SFML.Audio;

namespace SpiritPurger
{
	class SoundManager
	{
		public enum SFX
		{
			MENU_MOVE,
			MENU_SELECT,
			HIT_FOE,
			HIT_FOE_WEAKENED,
			GRAZE,
			END_SFX
		}
		protected List<SoundBuffer> _sfx;
		protected List<SFX> _queuedSFX;
		protected List<Sound> _playingSounds;
		public const int MAX_SIMULT_SFX = 126;
		protected int _currSFX = 0;
		protected int volumeSFX = 100;
		protected int volumeMusic = 100;

		public int VolumeSFX
		{
			get { return volumeSFX; }
			set
			{
				volumeSFX = value;
				foreach (Sound sfx in _playingSounds)
				{
					sfx.Volume = volumeSFX;
				}
			}
		}

		public int VolumeMusic
		{
			get { return volumeMusic; }
			set
			{
				volumeMusic = value;
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

		protected String GetSFXFilename(SFX action)
		{
			String ret = "";
			if (action == SFX.MENU_MOVE)
				ret = "button-31.wav";
			else if (action == SFX.MENU_SELECT)
				ret = "button-31.wav";
			else if (action == SFX.HIT_FOE)
				ret = "hit_foe.wav";
			else if (action == SFX.HIT_FOE_WEAKENED)
				ret = "hit_foe_weak.wav";
			else if (action == SFX.GRAZE)
				ret = "button-15.wav";
			return ret;
		}

		/// <summary>
		/// Loads all SFX into SoundBuffers just once.
		/// </summary>
		/// <param name="sfxList">A Dictionary relating SFX filenames to SFX SoundBuffers.</param>
		/// <returns>True if all loads succeeded. False otherwise.</returns>
		private bool LoadAllSFX(out Dictionary<string, SoundBuffer> sfxFileInstances)
		{
			bool success = true;;
			
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
			// TODO: Load mute sounds otherwise.

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
			if (_queuedSFX.Contains(SFX.HIT_FOE) && _queuedSFX.Contains(SFX.HIT_FOE_WEAKENED))
				_queuedSFX.Remove(SFX.HIT_FOE);

			foreach (SFX action in _queuedSFX)
			{
				Play(action);
			}
			_queuedSFX.Clear();
		}
	}
}

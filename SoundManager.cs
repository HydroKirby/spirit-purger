using System;
using System.Collections.Generic;
using SFML.Audio;

namespace SpiritPurger
{
	class SoundManager
	{
		protected Dictionary<string, SoundBuffer> _sfx;
		protected List<String> _queuedSFX;
		protected List<Sound> _playingSounds;
		public const int MAX_SIMULT_SFX = 126;
		protected int _currSFX = 0;

		public SoundManager()
		{
			AssignAllSFX(out _sfx);
			_queuedSFX = new List<string>(MAX_SIMULT_SFX);
			_playingSounds = new List<Sound>(MAX_SIMULT_SFX);
			for (int i = 0; i < MAX_SIMULT_SFX; ++i)
			{
				_playingSounds.Add(new Sound());
			}
		}

		/// <summary>
		/// Creates a dictionary relating game actions to SFX.
		/// </summary>
		/// <returns>A dictionary relating actions to SFX filenames.</returns>
		private void CreateSFXList(out Dictionary<string, string> sfxActions)
		{
			sfxActions = new Dictionary<string, string>(StringComparer.Ordinal);
			sfxActions["menu move"] = "button-31.wav";
			sfxActions["menu select"] = "button-31.wav";
			sfxActions["hit foe"] = "hit_foe.wav";
			sfxActions["hit foe weak"] = "hit_foe_weak.wav";
			sfxActions["graze"] = "button-15.wav";
		}

		/// <summary>
		/// Loads all SFX into SoundBuffers just once.
		/// </summary>
		/// <param name="sfxList">A Dictionary relating SFX filenames to SFX SoundBuffers.</param>
		/// <returns>True if all loads succeeded. False otherwise.</returns>
		private bool LoadAllSFX(out Dictionary<string, string> sfxActions,
			out Dictionary<string, SoundBuffer> sfxFileInstances)
		{
			bool success = true;;
			CreateSFXList(out sfxActions);

			sfxFileInstances = new Dictionary<string, SoundBuffer>(StringComparer.Ordinal);
			foreach (KeyValuePair<string, string> kvp in sfxActions)
			{
				if (kvp.Value.Length > 0 && !sfxFileInstances.ContainsKey(kvp.Value))
				{
					try
					{
						sfxFileInstances[kvp.Value] = new SoundBuffer("res/se/" + kvp.Value);
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
		private bool AssignAllSFX(out Dictionary<string, SoundBuffer> sfx)
		{
			Dictionary<string, string> sfxActions;
			Dictionary<string, SoundBuffer> sfxFileInstances;
			bool success = LoadAllSFX(out sfxActions, out sfxFileInstances);

			sfx = new Dictionary<string, SoundBuffer>(StringComparer.Ordinal);
			if (success)
			{
				foreach (KeyValuePair<string, string> kvp in sfxActions)
				{
					sfx[kvp.Key] = sfxFileInstances[kvp.Value];
				}
			}
			// TODO: Load mute sounds otherwise.

			return success;
		}

		protected void Play(String action)
		{
			_playingSounds[_currSFX].Stop();
			_playingSounds[_currSFX].SoundBuffer = _sfx[action];
			_playingSounds[_currSFX].Play();
			++_currSFX;
			if (_currSFX >= MAX_SIMULT_SFX)
				_currSFX = 0;
		}

		/// <summary>
		/// Adds a sound to play for the next update.
		/// </summary>
		/// <param name="action">The action associated with a sound.</param>
		public void QueueToPlay(String action)
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
			if (_queuedSFX.Contains("hit foe") && _queuedSFX.Contains("hit foe weak"))
				_queuedSFX.Remove("hit foe");

			foreach (String action in _queuedSFX)
			{
				Play(action);
			}
			_queuedSFX.Clear();
		}
	}
}

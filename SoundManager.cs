using System;
using System.Collections.Generic;
using SFML.Audio;

namespace SpiritPurger
{
	class SoundManager
	{
		protected Dictionary<string, SoundBuffer> sfx;

		// A list of all(?) sound effects in the game.
		public const String SE_MENU_MOVE = "button-31.wav";
		public const String SE_MENU_SELECT = "button-31.wav";

		public SoundManager()
		{
			AssignAllSFX(out sfx);
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
				if (!sfxFileInstances.ContainsKey(kvp.Value))
				{
					sfxFileInstances[kvp.Value] = new SoundBuffer("res/se/" + kvp.Value);
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
			foreach (KeyValuePair<string, string> kvp in sfxActions)
			{
				sfx[kvp.Key] = sfxFileInstances[kvp.Value];
			}

			return success;
		}
	}
}

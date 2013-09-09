using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Audio;

namespace SpiritPurger
{
	class MusicManager
	{
		// All playable music selectable in the game.
		public enum MUSIC_LIST
		{
			// Use the first entry for unimplemented music.
			UNASSIGNED,

			SILENT,
			TITLE,
			GAME,
			GAME_WON,
			GAME_LOST,

			// Use to count the number of music entries if needed.
			END_LIST
		}
		// Says what part of the music the MusicManager is currently playing.
		public enum PLAYING
		{
			OFF,
			OPENING,
			LOOPING
		}
		protected PLAYING playing;
		// The chosen music to play.
		protected MUSIC_LIST choiceNowPlaying;
		// The literal music files that are playing.
		protected Music nowPlayingOpening;
		protected Music nowPlayingLoop;
		protected bool hasOpening;

		public float Volume
		{
			get { return nowPlayingOpening.Volume; }
			set
			{
				if (value > 100 || value < 0)
					return;
				nowPlayingOpening.Volume = value;
			}
		}

		public MUSIC_LIST NowPlaying
		{
			get { return choiceNowPlaying; }
			set
			{
				if (value == choiceNowPlaying)
					// They chose to play the same music that's playing now.
					return;

				String[] filenames = GetMusicFilenames(value);
				bool opening = filenames[0].CompareTo("") == 0;
				if (!opening && filenames[1].CompareTo("") == 0)
				{
					// There is no music to load.
				}
				else
				{
					// Load the music selected.
					choiceNowPlaying = value;
					if (nowPlayingOpening != null)
					{
						nowPlayingOpening.Stop();
						nowPlayingOpening.Dispose();
					}
					if (nowPlayingLoop != null)
					{
						nowPlayingLoop.Stop();
						nowPlayingLoop.Dispose();
					}
					if (hasOpening)
					{
						nowPlayingOpening = new Music("res/music/" + filenames[0]);
					}
					nowPlayingLoop = new Music("res/music/" + filenames[1]);
				}
			}
		}

		public MusicManager()
		{
			playing = PLAYING.OFF;
			hasOpening = false;
			NowPlaying = MUSIC_LIST.SILENT;
			nowPlayingOpening.Loop = false;
			nowPlayingLoop.Loop = true;
		}

		/// <summary>
		/// Gets the filenames of a music's opening and loop.
		/// If a new music file was added as a resource to the game,
		/// it should be added into this function.
		/// </summary>
		/// <param name="music">The music to play.</param>
		/// <returns>A filename for music's opening (subscript 0) and loop (subscript1),
		/// or a blank string for unassigned music. A blank opening means there is no opening.</returns>
		protected String[] GetMusicFilenames(MUSIC_LIST music)
		{
			String[] ret = new String[2];
			ret[0] = "";
			ret[1] = "";

			if (music == MUSIC_LIST.SILENT)
			{
				hasOpening = true;
				ret[0] = "silence.ogg";
				ret[1] = "silence.ogg";
			}

			return ret;
		}

		/// <summary>
		/// Used to make a paused/stopped song play again.
		/// If the music is still playing, nothing happens.
		/// </summary>
		public void Play()
		{
			if (playing == PLAYING.OFF)
			{
				if (hasOpening)
				{
					playing = PLAYING.OPENING;
					nowPlayingOpening.Play();
				}
				else
				{
					playing = PLAYING.LOOPING;
					nowPlayingLoop.Play();
				}
			}
		}

		/// <summary>
		/// Used to select a new music to play, and then immediately play it.
		/// </summary>
		/// <param name="newMusic">The new music to play.</param>
		public void Play(MUSIC_LIST newMusic)
		{
			NowPlaying = newMusic;
			Play();
		}

		public void Stop()
		{
			playing = PLAYING.OFF;
			if (nowPlayingOpening != null)
				nowPlayingOpening.Stop();
			if (nowPlayingLoop != null)
				nowPlayingLoop.Stop();
		}

		public void Replay()
		{
			Stop();
			Play();
		}
	}
}

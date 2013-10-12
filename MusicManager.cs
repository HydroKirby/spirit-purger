using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Audio;

namespace SpiritPurger
{
	/// <summary>
	/// Stores when a music file should loop and to where.
	/// </summary>
	class LoopPointMusic
	{
		private String _filename;
		private bool _hasLoop;
		private TimeSpan _loopBeginPoint;
		private TimeSpan _loopEndPoint;

		public String Filename
		{
			get { return _filename; }
		}

		public bool Loops
		{
			get { return _hasLoop; }
		}

		public TimeSpan LoopBegin
		{
			get { return _loopBeginPoint; }
		}

		public TimeSpan LoopEnd
		{
			get { return _loopEndPoint; }
		}

		public LoopPointMusic(String filename, TimeSpan startLoop, TimeSpan endLoop)
		{
			_filename = filename;
			_hasLoop = true;
			_loopBeginPoint = startLoop;
			_loopEndPoint = endLoop;
		}

		public LoopPointMusic(String filename)
		{
			_filename = filename;
			_hasLoop = false;
			_loopBeginPoint = TimeSpan.FromMilliseconds(0);
			_loopEndPoint = TimeSpan.FromMilliseconds(1);
		}
	}

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
		// The chosen music to play.
		protected MUSIC_LIST choiceNowPlaying;
		// The literal music files that are playing.
		protected Music music;
		protected Dictionary<MUSIC_LIST, LoopPointMusic> loopPointMusics;
		protected LoopPointMusic currentMusic;

		public float Volume
		{
			get { return music.Volume; }
			set
			{
				if (value > 100 || value < 0)
					return;
				music.Volume = value;
			}
		}

		public SoundStatus Status
		{
			get { return music.Status; }
		}

		public MUSIC_LIST NowPlaying
		{
			get { return choiceNowPlaying; }
			set
			{
				if (value == choiceNowPlaying)
					// They chose to play the same music that's playing now.
					return;

				String filename = GetMusicFilename(value);
				if (filename.CompareTo("") == 0)
				{
					// There is no music to load.
					// What was requested was an invalid music selection.
				}
				else
				{
					// Load the music selected.
					float vol = 100.0F;
					if (music != null)
						vol = music.Volume;
					choiceNowPlaying = value;
					currentMusic = loopPointMusics[value];
					if (music != null)
					{
						music.Stop();
						music.Dispose();
					}
					music = new Music("res/music/" + filename);
					music.Volume = vol;
					music.Play();
					music.Loop = true;
				}
			}
		}

		public MusicManager()
		{
			PopulateMusicList();
			NowPlaying = MUSIC_LIST.SILENT;
		}

		private void PopulateMusicList()
		{
			loopPointMusics = new Dictionary<MUSIC_LIST, LoopPointMusic>();
			LoopPointMusic silence = new LoopPointMusic(GetMusicFilename(MUSIC_LIST.SILENT));
			loopPointMusics.Add(MUSIC_LIST.UNASSIGNED, silence);
			loopPointMusics.Add(MUSIC_LIST.SILENT, silence);
			loopPointMusics.Add(MUSIC_LIST.TITLE, silence);
			loopPointMusics.Add(MUSIC_LIST.GAME_WON, silence);
			loopPointMusics.Add(MUSIC_LIST.GAME_LOST, silence);
			loopPointMusics.Add(MUSIC_LIST.GAME, new LoopPointMusic(GetMusicFilename(MUSIC_LIST.GAME),
				TimeSpan.FromSeconds(3.18), TimeSpan.FromSeconds(33.8)));
		}

		/// <summary>
		/// Gets the filenames of a music's opening and loop.
		/// If a new music file was added as a resource to the game,
		/// it should be added into this function.
		/// </summary>
		/// <param name="music">The music to play.</param>
		/// <returns>A filename for music, or a blank filename for unassigned music.</returns>
		protected String GetMusicFilename(MUSIC_LIST music)
		{
			String ret;

			switch (music)
			{
				case MUSIC_LIST.SILENT:
				case MUSIC_LIST.TITLE:
					ret = "silence.ogg"; break;
				case MUSIC_LIST.GAME:
					ret = "athletic.ogg"; break;
				default:
					ret = ""; break;
			}

			return ret;
		}

		public void ChangeMusic(MUSIC_LIST choice)
		{
			NowPlaying = choice;
		}

		/// <summary>
		/// Used to make a paused/stopped song play again.
		/// If the music is still playing, nothing happens.
		/// </summary>
		public void Play()
		{
			if (Status != SoundStatus.Playing)
			{
				music.Play();
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
			if (music != null)
				music.Stop();
		}

		public void Replay()
		{
			Stop();
			Play();
		}

		public void Update()
		{
			// Loop the music if needed.
			if (music.PlayingOffset > currentMusic.LoopEnd)
			{
				music.PlayingOffset = currentMusic.LoopBegin +
					(music.PlayingOffset - currentMusic.LoopEnd);
			}
		}
	}
}

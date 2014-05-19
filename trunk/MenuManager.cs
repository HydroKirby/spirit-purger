using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
	/// <summary>
	/// Makes the meaning of a timer be related to the Player's inputs.
	/// </summary>
	public class MenuDuty : TimerDuty
	{
		public new enum DUTY
		{
			NONE,
			FADE_IN,
			FADE_OUT_TO_GAMEPLAY,
			FADE_OUT_TO_EXIT,
		}

        private DUTY _duty;
        public override void SetDuty(object duty) { _duty = (DUTY)duty; }
        public override object GetDuty() { return _duty; }

		public MenuDuty() { }

		public override double GetTime()
		{
			// Interpret Purpose as the local variant of DUTY in this class.
			switch ((DUTY)GetDuty())
			{
				case DUTY.FADE_IN: return 0.4;
				case DUTY.FADE_OUT_TO_GAMEPLAY: return 0.4;
				case DUTY.FADE_OUT_TO_EXIT: return 0.4;
				default: return 0;
			}
		}
	}

	public class MenuManager : Subject
	{
		// A list of the menus that can be traversed in the menu screen.
		public enum SUBMENU
		{
			MAIN, DIFFICULTY, OPTIONS, ABOUT, TUTORIAL, CREDITS, END_SUBMENUS
		}

		// Every selectable menu item from every submenu.
		public enum MENUITEM
		{
			// The IDX enums allow coders to determine where each submenu's
			// items begin. Each submenu also ends with an enum named EXIT.
			IDX_MENU_STARTGAME, START_GAME, OPTIONS, ABOUT, EXIT_MAIN,
			IDX_MENU_DIFFMENU, EASY_DIFF, NORM_DIFF, HARD_DIFF, EXIT_DIFF,
			IDX_MENU_OPTIONS, WINDOW_SIZE, WINDOW_TYPE, MUSIC_VOL, SOUND_VOL, EXIT_OPTIONS,
				// Selections for the window size. Part of WINDOW_SIZE.
				WINDOW_SIZE_1, WINDOW_SIZE_1_5, WINDOW_SIZE_2, WINDOW_SIZE_3, WINDOW_SIZE_MAX,
				// Selections for the window type. Part of WINDOW_TYPE.
				WINDOW_TYPE_WINDOWED, WINDOW_TYPE_FULLSCREEN,
			IDX_MENU_ABOUT, TUTORIAL, CREDITS, EXIT_ABOUT,
            IDX_MENU_TUTORIAL, EXIT_TUTORIAL, TUTORIAL_MOVE, TUTORIAL_SLOW,
                TUTORIAL_SHOOT, TUTORIAL_BOMB, TUTORIAL_PAUSE,
			IDX_MENU_CREDITS, EXIT_CREDITS, CREDIT_PROGRAMMER, CREDIT_LARRY,
				CREDIT_ART, CREDIT_LUCY, CREDIT_SFX, CREDIT_INSPIRATION, CREDIT_ZUN,
                CREDIT_SOUND_JAY, CREDIT_BITSTREAM_VERA,
                CREDIT_MUSIC, CREDIT_CAREY,
			// End of list of MENUITEMs.
			END_MENU_ITEMS
		}

		// How to react to certain menu items being interracted with.
		// These REACTIONs are informed back to the Engine so that the
		// Engine can handle the complicated operations.
		public enum REACTION
		{
			// No particular action to take.
			NONE,
			// Fade into the menu and don't allow player input. Fade in from black.
			FADE_IN,
			// Fade out from the menu to the game. Fade out to black.
			FADE_OUT_TO_GAMEPLAY, FADE_OUT_TO_EXIT,
			FADE_COMPLETED,
			// Moved in the menu or selected a menu item.
			MENU_SELECTION_MOVED, MENU_ITEM_SELECTED,
			// Start the game.
			PLAY_GAME,
			// Options
			BIGGER_WINDOW, SMALLER_WINDOW, TO_WINDOWED, TO_FULLSCREEN,
			MORE_MUSIC_VOL, LESS_MUSIC_VOL, MORE_SOUND_VOL, LESS_SOUND_VOL,
			// Menu Transitions. All of them are hard-coded.
			MENU_TO_MAIN, MENU_TO_DIFF, MENU_TO_OPTIONS, MENU_TO_ABOUT, MENU_TO_TUTORIAL,
			MENU_TO_CREDITS,
			// Generic menu transition. It just tells the renderer to move the focus halo.
			MENU_TRANSITION_MADE,
			// Close the game.
			END_GAME,
			// End of list of REACTIONS.
			END_REACTIONS
		}

		// Relates the name of a submenu to all of its menu items.
		protected Dictionary<SUBMENU, MENUITEM[]> menuLayout;
		protected SUBMENU currentMenu;
		// Which index is selected from 0 to "the number of the menu items in submenu".
		protected int selectedItem;
		// How the Engine should react to MenuManager when it feels like updating.
		protected REACTION state;
		// The game's new options as chosen within the Options submenu.
		public Options newOptions;
		// A general timer for any animations and such.
		public DownTimer MenuTimer
		{
			get;
			protected set;
		}

		public int SelectedIndex
		{
			get { return selectedItem; }
		}

		public MENUITEM SelectedMenuItem
		{
			get
			{
				MENUITEM item = (MENUITEM)selectedItem;
				// Offset the menu item by its index in the submenus.
				switch (currentMenu)
				{
					case SUBMENU.MAIN: item += (int)MENUITEM.IDX_MENU_STARTGAME; break;
					case SUBMENU.OPTIONS: item += (int)MENUITEM.IDX_MENU_OPTIONS; break;
					case SUBMENU.DIFFICULTY: item += (int)MENUITEM.IDX_MENU_DIFFMENU; break;
					case SUBMENU.ABOUT: item += (int)MENUITEM.IDX_MENU_ABOUT; break;
					case SUBMENU.TUTORIAL: item += (int)MENUITEM.IDX_MENU_TUTORIAL; break;
					case SUBMENU.CREDITS: item += (int)MENUITEM.IDX_MENU_CREDITS; break;
				}
				item += 1;
				MENUITEM unused = (MENUITEM)item;
				return item;
			}
		}

		public SUBMENU CurrentMenu
		{
			get { return currentMenu; }
		}

		/// <summary>
		/// Gets the intent of the manager based on the selected menu item.
		/// </summary>
		public REACTION State
		{
			get { return state; }
		}

		public MenuManager(Options options)
		{
			currentMenu = SUBMENU.MAIN;
			selectedItem = 0;
			state = REACTION.NONE;
			newOptions = new Options(options);
			MenuTimer = new DownTimer(new MenuDuty());

			// Allocate memory for the layout of the menu and submenus.
			// The number of menu items is hard coded for speed of development.
			menuLayout = new Dictionary<SUBMENU,MENUITEM[]>();
			menuLayout.Add(SUBMENU.MAIN, new MENUITEM[4]);
			menuLayout.Add(SUBMENU.DIFFICULTY, new MENUITEM[4]);
			menuLayout.Add(SUBMENU.OPTIONS, new MENUITEM[5]);
			menuLayout.Add(SUBMENU.ABOUT, new MENUITEM[3]);
			menuLayout.Add(SUBMENU.TUTORIAL, new MENUITEM[1]);
			menuLayout.Add(SUBMENU.CREDITS, new MENUITEM[1]);

			// Set each submenu's items in order.
			menuLayout[SUBMENU.MAIN][0] = MENUITEM.START_GAME;
			menuLayout[SUBMENU.MAIN][1] = MENUITEM.OPTIONS;
			menuLayout[SUBMENU.MAIN][2] = MENUITEM.ABOUT;
			menuLayout[SUBMENU.MAIN][3] = MENUITEM.EXIT_MAIN;
			menuLayout[SUBMENU.DIFFICULTY][0] = MENUITEM.EASY_DIFF;
			menuLayout[SUBMENU.DIFFICULTY][1] = MENUITEM.NORM_DIFF;
			menuLayout[SUBMENU.DIFFICULTY][2] = MENUITEM.HARD_DIFF;
			menuLayout[SUBMENU.DIFFICULTY][3] = MENUITEM.EXIT_DIFF;
			menuLayout[SUBMENU.OPTIONS][0] = MENUITEM.WINDOW_SIZE;
			menuLayout[SUBMENU.OPTIONS][1] = MENUITEM.WINDOW_TYPE;
			menuLayout[SUBMENU.OPTIONS][2] = MENUITEM.MUSIC_VOL;
			menuLayout[SUBMENU.OPTIONS][3] = MENUITEM.SOUND_VOL;
			menuLayout[SUBMENU.OPTIONS][4] = MENUITEM.EXIT_OPTIONS;
			menuLayout[SUBMENU.ABOUT][0] = MENUITEM.TUTORIAL;
			menuLayout[SUBMENU.ABOUT][1] = MENUITEM.CREDITS;
			menuLayout[SUBMENU.ABOUT][2] = MENUITEM.EXIT_ABOUT;
			menuLayout[SUBMENU.TUTORIAL][0] = MENUITEM.EXIT_TUTORIAL;
			menuLayout[SUBMENU.CREDITS][0] = MENUITEM.EXIT_CREDITS;
		}

		/// <summary>
		/// Makes the game fade into existence.
		/// </summary>
		public void StartMenu()
		{
			ChangeState(REACTION.FADE_IN);
			MenuTimer.Repurporse((int)MenuDuty.DUTY.FADE_IN);
		}

		public MENUITEM[] GetSubmenuLayout(SUBMENU submenu)
		{
			MENUITEM[] val;
			if (!menuLayout.TryGetValue(submenu, out val))
				val = null;
			return val;
		}

		/// <summary>
		/// Moves the selection up or down and loops on the menu if needed.
		/// </summary>
		/// <param name="current">What value is currently selected.</param>
		/// <param name="direction">A value of 1 or -1 for where the user is going.</param>
		/// <param name="max">The maximum value that "current" could be.</param>
		/// <returns>The new value to set "current" to. Will be 0, "max", or any value inbetween.</returns>
		private int LoopSelection(int current, int direction, int max)
		{
			current += direction;
			if (current >= max)
				current = 0;
			else if (current < 0)
				current = max - 1;
			return current;
		}

		protected void ChangeState(REACTION newState)
		{
			if (newState != state)
			{
				state = newState;
				// Tell all Observers that the state changed.
				// The Subjects can check the Status property of this class.
				Notify();
			}
		}

		/// <summary>
		/// Tells MenuManager that something has handled this manager's REACTION request.
		/// </summary>
		public void StateHandled()
		{
			state = REACTION.NONE;
		}

		public void OnDownKey()
		{
			selectedItem = LoopSelection(selectedItem, 1,
				menuLayout[currentMenu].Length);
			switch (selectedItem)
			{
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnUpKey()
		{
			selectedItem = LoopSelection(selectedItem, -1,
				menuLayout[currentMenu].Length);
			switch (selectedItem)
			{
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnLeftKey()
		{
			MENUITEM item = SelectedMenuItem;
			int vol;
			switch (item)
			{
				case MENUITEM.WINDOW_SIZE:
					double wsize = (double)newOptions.Settings["window size"];
					if (wsize == 1.0) wsize = 0.0;
					else if (wsize == 1.5) wsize = 1.0;
					else if (wsize == 2.0) wsize = 1.5;
					else if (wsize == 3.0) wsize = 2.0;
					else wsize = 3.0; // wsize == 0.0 here
					newOptions.Settings["window size"] = wsize;
					ChangeState(REACTION.SMALLER_WINDOW);
					break;
				case MENUITEM.WINDOW_TYPE:
					bool fullscreen = (int)newOptions.Settings["fullscreen"] == 1;
					// Toggle the fullscreen setting.
					fullscreen = !fullscreen;
					newOptions.Settings["fullscreen"] = fullscreen ? 1 : 0;
					if (fullscreen)
						ChangeState(REACTION.TO_FULLSCREEN);
					else
						ChangeState(REACTION.TO_WINDOWED);
					break;
				case MENUITEM.MUSIC_VOL:
					vol = (int)newOptions.Settings["bgm volume"];
					vol -= 10;
					if (vol < 0)
						vol = 0;
					newOptions.Settings["bgm volume"] = vol;
					ChangeState(REACTION.LESS_MUSIC_VOL);
					break;
				case MENUITEM.SOUND_VOL:
					vol = (int)newOptions.Settings["sfx volume"];
					vol -= 10;
					if (vol < 0)
						vol = 0;
					newOptions.Settings["sfx volume"] = vol;
					ChangeState(REACTION.LESS_SOUND_VOL);
					break;
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnRightKey()
		{
			MENUITEM item = SelectedMenuItem;
			int vol;
			switch (item)
			{
				case MENUITEM.WINDOW_SIZE:
					double wsize = (double)newOptions.Settings["window size"];
					if (wsize == 1.0) wsize = 1.5;
					else if (wsize == 1.5) wsize = 2.0;
					else if (wsize == 2.0) wsize = 3.0;
					else if (wsize == 3.0) wsize = 0.0;
					else wsize = 1.0; // wsize == 0.0 here
					newOptions.Settings["window size"] = wsize;
					ChangeState(REACTION.BIGGER_WINDOW);
					break;
				case MENUITEM.WINDOW_TYPE:
					// Toggle fullscreen/windowed display.
					// The functionality is the same as if you pressed the Left Arrow key.
					OnLeftKey();
					break;
				case MENUITEM.MUSIC_VOL:
					vol = (int)newOptions.Settings["bgm volume"];
					vol += 10;
					if (vol > 100)
						vol = 100;
					newOptions.Settings["bgm volume"] = vol;
					ChangeState(REACTION.MORE_MUSIC_VOL);
					break;
				case MENUITEM.SOUND_VOL:
					vol = (int)newOptions.Settings["sfx volume"];
					vol += 10;
					if (vol > 100)
						vol = 100;
					newOptions.Settings["sfx volume"] = vol;
					ChangeState(REACTION.MORE_SOUND_VOL);
					break;
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnSelectKey()
		{
			MENUITEM item = SelectedMenuItem;

			// Create a reaction based on the selected menu item.
			switch (item)
			{
				// Top Menu submenu
				case MENUITEM.START_GAME:
					ChangeState(REACTION.FADE_OUT_TO_GAMEPLAY);
					MenuTimer.Repurporse((int)MenuDuty.DUTY.FADE_OUT_TO_GAMEPLAY);
					break;
				case MENUITEM.OPTIONS:
					currentMenu = SUBMENU.OPTIONS;
					selectedItem = 0;
					ChangeState(REACTION.MENU_TO_OPTIONS);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;
				case MENUITEM.ABOUT:
					currentMenu = SUBMENU.ABOUT;
					selectedItem = 0;
					ChangeState(REACTION.MENU_TO_ABOUT);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;
				case MENUITEM.EXIT_MAIN: ChangeState(REACTION.END_GAME); break;

				// Options submenu
				case MENUITEM.EXIT_OPTIONS:
					// Save newly set options in the engine.
					currentMenu = SUBMENU.MAIN;
					selectedItem = 1;
					ChangeState(REACTION.MENU_TO_MAIN);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;

				// About submenu
				case MENUITEM.EXIT_ABOUT:
					currentMenu = SUBMENU.MAIN;
					selectedItem = 2;
					ChangeState(REACTION.MENU_TO_MAIN);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;
				case MENUITEM.TUTORIAL:
					currentMenu = SUBMENU.TUTORIAL;
					selectedItem = 0;
					ChangeState(REACTION.MENU_TO_TUTORIAL);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;
				case MENUITEM.CREDITS:
					currentMenu = SUBMENU.CREDITS;
					selectedItem = 0;
					ChangeState(REACTION.MENU_TO_CREDITS);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;

				// Tutorial submenu
				case MENUITEM.EXIT_TUTORIAL:
					currentMenu = SUBMENU.ABOUT;
					selectedItem = 0;
					ChangeState(REACTION.MENU_TO_ABOUT);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;

				// Credits submenu
				case MENUITEM.EXIT_CREDITS:
					currentMenu = SUBMENU.ABOUT;
					selectedItem = 1;
					ChangeState(REACTION.MENU_TO_ABOUT);
					ChangeState(REACTION.MENU_TRANSITION_MADE);
					break;

				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnCancelKey()
		{
			switch (selectedItem)
			{
				default:
					// Go to the "Return" button on the current submenu.
					switch (currentMenu)
					{
						case SUBMENU.ABOUT: selectedItem = 2; break;
						case SUBMENU.CREDITS: selectedItem = 0; break;
						case SUBMENU.DIFFICULTY: selectedItem = 3; break;
						case SUBMENU.MAIN: selectedItem = 3; break;
						case SUBMENU.OPTIONS: selectedItem = 4; break;
						case SUBMENU.TUTORIAL: selectedItem = 0; break;
					}
					break;
			}
		}

		public Options GetNewOptions()
		{
			return newOptions;
		}

		/// <summary>
		/// Updates the menu by one rendering frame.
		/// </summary>
		/// <param name="sender">The caller of this method.</param>
		/// <param name="ticks">The ticks since the last call to this.</param>
		public void NextFrame(object sender, double ticks, KeyHandler keys)
		{
			MenuTimer.Tick(ticks);
			if (MenuTimer.SamePurpose(MenuDuty.DUTY.FADE_IN))
			{
				if (MenuTimer.TimeIsUp())
				{
					// We have faded into the menu.
					ChangeState(REACTION.FADE_COMPLETED);
					MenuTimer.Repurporse((int)MenuDuty.DUTY.NONE);
                    // Do not allow a button press during this transition.
                    return;
				}
				else
					return;
			}
			else if (MenuTimer.SamePurpose(MenuDuty.DUTY.FADE_OUT_TO_GAMEPLAY))
			{
				if (MenuTimer.TimeIsUp())
				{
					// We have faded into the gameplay from the menu.
					ChangeState(REACTION.FADE_COMPLETED);
					MenuTimer.Repurporse((int)MenuDuty.DUTY.NONE);
					ChangeState(REACTION.PLAY_GAME);
                    // Do not allow a button press during this transition.
                    return;
				}
				else
					return;
			}

			bool moved = false;
			bool acted = false;

			// Only accept one directional key.
			if (keys.up == 1)
			{
				OnUpKey();
				moved = true;
			}
			else if (keys.down == 1)
			{
				OnDownKey();
				moved = true;
			}
			else if (keys.left == 1)
			{
				OnLeftKey();
				moved = true;
			}
			else if (keys.right == 1)
			{
				OnRightKey();
				moved = true;
			}

			// Only accept one of either accept or cancel keys.
			if (keys.shoot == 1)
			{
				OnSelectKey();
				acted = true;
			}
			else if (keys.bomb == 1)
			{
				OnCancelKey();
				moved = true;
				acted = true;
			}

			if (moved)
				ChangeState(REACTION.MENU_SELECTION_MOVED);

			if (acted)
				ChangeState(REACTION.MENU_ITEM_SELECTED);
		}
	}
}

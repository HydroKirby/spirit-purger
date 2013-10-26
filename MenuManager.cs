using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
	public class MenuManager : Subject
	{
		public enum SUBMENU
		{
			MAIN, DIFFICULTY, OPTIONS, ABOUT, TUTORIAL, CREDITS, END_SUBMENUS
		}

		public enum MENUITEM
		{
			START_GAME, OPTIONS, ABOUT, EXIT_MAIN,
			EASY_DIFF, NORM_DIFF, HARD_DIFF, EXIT_DIFF,
			WINDOW_SIZE, WINDOWED, MUSIC_VOL, SOUND_VOL, EXIT_OPTIONS,
			TUTORIAL, CREDITS, EXIT_ABOUT,
			EXIT_TUTORIAL,
			EXIT_CREDITS,
			END_MENU_ITEMS
		}

		// How to react to certain menu items being interracted with.
		// These REACTIONs are informed back to the Engine so that the
		// Engine can handle the complicated operations.
		public enum REACTION
		{
			NONE, // No particular action to take.
			GENERIC, // Let the Engine take care of the reaction.
			PLAY_GAME,
			BIGGER_WINDOW, SMALLER_WINDOW, TO_WINDOWED, TO_FULLSCREEN,
			MORE_MUSIC_VOL, LESS_MUSIC_VOL, MORE_SOUND_VOL, LESS_SOUND_VOL,
			END_GAME,
			END_REACTIONS
		}

		protected Dictionary<SUBMENU, MENUITEM[]> menuLayout;
		protected SUBMENU currentMenu;
		protected int selectedItem;
		protected REACTION state;

		public MENUITEM Selected
		{
			get { return (MENUITEM)selectedItem; }
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

		public MenuManager()
		{
			currentMenu = SUBMENU.MAIN;
			selectedItem = 0;
			state = REACTION.NONE;

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
			menuLayout[SUBMENU.OPTIONS][1] = MENUITEM.WINDOWED;
			menuLayout[SUBMENU.OPTIONS][2] = MENUITEM.MUSIC_VOL;
			menuLayout[SUBMENU.OPTIONS][3] = MENUITEM.SOUND_VOL;
			menuLayout[SUBMENU.OPTIONS][4] = MENUITEM.EXIT_OPTIONS;
			menuLayout[SUBMENU.ABOUT][0] = MENUITEM.TUTORIAL;
			menuLayout[SUBMENU.ABOUT][1] = MENUITEM.CREDITS;
			menuLayout[SUBMENU.ABOUT][2] = MENUITEM.EXIT_ABOUT;
			menuLayout[SUBMENU.TUTORIAL][0] = MENUITEM.EXIT_TUTORIAL;
			menuLayout[SUBMENU.CREDITS][0] = MENUITEM.EXIT_CREDITS;
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
			if (current > max)
				current = 0;
			else if (current < 0)
				current = max;
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

		public void OnDownKey()
		{
			selectedItem = LoopSelection(selectedItem, -1,
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
			switch (selectedItem)
			{
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnRightKey()
		{
			switch (selectedItem)
			{
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnSelectKey()
		{
			switch ((MENUITEM) selectedItem)
			{
				case MENUITEM.START_GAME: ChangeState(REACTION.PLAY_GAME); break;
				case MENUITEM.EXIT_MAIN: ChangeState(REACTION.END_GAME); break;
				default: ChangeState(REACTION.NONE); break;
			}
		}

		public void OnCancelKey()
		{
			switch (selectedItem)
			{
				default: ChangeState(REACTION.NONE); break;
			}
		}

		/// <summary>
		/// Updates the menu.
		/// </summary>
		/// <param name="sender">The caller of this method.</param>
		/// <param name="ticks">The ticks since the last call to this.</param>
		protected void Update(object sender, double ticks)
		{
		}
	}
}

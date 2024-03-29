﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
	public class Options// : IEquatable<Options>
	{
		public const String CONFIG_FILE = "config.cfg";
		protected Dictionary<string, object> settings;

		public Dictionary<string, object> Settings
		{
			get { return settings; }
		}

		public Options()
		{
			SetDefaults(out settings);
			Dictionary<string, string> tempOptions;
			if (ReadConfigFile(out tempOptions))
			{
				if (!TranslateOptions(tempOptions))
				{
					WriteConfig();
				}
			}
			else
			{
				WriteConfig();
			}
		}

		public Options(Options copy)
		{
			settings = new Dictionary<string, object>(copy.settings);
		}

		/*
		// Overriding Equals member method, which will call the IEquatable implementation
		// if appropriate.
		public override bool Equals(object obj)
		{
			var other = obj as Options;
			if (other == null)
				return false;
			return Equals(other);
		}

		// IEquatable's Equal method.
		public bool Equals(Options other)
		{
			if (other == null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return false;
			// Check if each Options instance has the same "settings".
		}
		*/

		/// <summary>
		/// Returns the version number of config files this Options class reads.
		/// </summary>
		/// <returns>The version number.</returns>
		public double GetOptionsVersion()
		{
			return 1.05;
		}

		protected void SetDefaults(out Dictionary<string, object> options)
		{
			options = new Dictionary<string, object>(StringComparer.Ordinal);
			options["version"] = GetOptionsVersion();
			// ver 1.00
			// sfx volume, bgm volume changed from double to int at ver 1.0.
			options["sfx volume"] = 100;
			options["bgm volume"] = 100;
			options["player animation type"] = "pingpong";
			options["boss animation type"] = "pingpong";
			options["player animation speed"] = 4;
			options["boss animation speed"] = 5;
			options["bg swirl speed"] = 0.3;
			// ver 1.01
			options["healthbar width"] = 250;
			options["healthbar height"] = 10;
			options["healthbar x"] = 20;
			options["healthbar y"] = 10;
			// ver 1.02
            // In version 1.05, these are hidden when writing a new config file.
			options["god mode"] = 0;
			options["fun bomb"] = 0;
			options["repulsive"] = 0;
			// ver 1.03
			// Valid options are supposed to be 1.0, 1.5, 2.0, 3.0, 0.0 (max).
			options["window size"] = 1.0;
			// Valid options are 0 (windowed) and 1 (fullscreen).
			options["fullscreen"] = 0;
			// ver 1.04
			options["title bgm loop start"] = 3.18;
			options["title bgm loop end"] = 33.8;
			options["game bgm loop start"] = 0.0;
			options["game bgm loop end"] = 99999.9;
			// ver 1.05
		}

		/// <summary>
		/// Reads the config file, if it exists, as string:string pairs of settings.
		/// </summary>
		/// <param name="tempOptions">A dictionary of read-in key-value pairs.</param>
		/// <returns>True if the file existed and was read without errors.</returns>
		protected bool ReadConfigFile(out Dictionary<string, string> tempOptions)
		{
			tempOptions = new Dictionary<string, string>(StringComparer.Ordinal);
			bool good = true;
			string[] lines = {};
			try
			{
				lines = System.IO.File.ReadAllLines(CONFIG_FILE);
			}
			catch
			{
				good = false;
			}

			Char[] sep = {'='};
			if (good)
			{
				foreach (string s in lines)
				{
					string line = s.Trim();
					if (line.StartsWith("#") || line == "")
						continue;
					else
					{
						// Split the key and value from the '=' character.
						string[] keyValPair = line.Split(sep, 120);
						if (keyValPair.Count() != 2)
						{
							good = false;
							break;
						}
						tempOptions[keyValPair[0]] = keyValPair[1];
					}
				}
			}
			return good;
		}

		/// <summary>
		/// Writes the configuration settings to a file.
		/// The written config file always has the latest version number on it.
		/// </summary>
		/// <returns>True. Always. This should probably be fixed.</returns>
		public bool WriteConfig()
		{
			// Always write the latest version of the config file format.
			settings["version"] = GetOptionsVersion();
			// Create the full string to write to the file in one go.
			String output = "";
			output += "# This is a comment. It is not parsed by the game.\n";
			output += "# Valid animation styles are 'pingpong' and 'replay'.\n";
			foreach (KeyValuePair<string, object> kvp in settings)
			{
				string val;
				if (kvp.Value is double)
					val = String.Format("{0:F2}", kvp.Value);
				else
					val = kvp.Value.ToString();
                // Only output the key-value pair if it is not a debug feature.
                if (kvp.Key != "god mode" && kvp.Key != "fun bomb" &&
                    kvp.Key != "repulsive")
                    output += String.Join("", kvp.Key, "=", val, "\n");
			}
			using (System.IO.StreamWriter file = new System.IO.StreamWriter(CONFIG_FILE))
			{
				file.Write(output);
			}
			return true;
		}

		/// <summary>
		/// Turns stringly typed values into ints, doubles, or strings.
		/// On failure, the default options are regenerated.
		/// </summary>
		/// <param name="tempOptions">The assignments read-in from the config file.</param>
		/// <returns>True if all values could be converted correctly.</returns>
		protected bool TranslateOptions(Dictionary<string, string> tempOptions)
		{
			bool success = true;
			foreach (KeyValuePair<string, string> kvp in tempOptions)
			{
				// For future and backwards compatibility, ignore unknown options.
				if (settings.ContainsKey(kvp.Key))
				{
					// Get the data type of the value and try to re-parse
					// the assigned data to that.
					System.Type type = settings[kvp.Key].GetType();
					if (settings[kvp.Key] is int)
					{
						int result;
						if (!int.TryParse(kvp.Value, out result))
						{
							success = false;
							break;
						}
						else
							settings[kvp.Key] = result;
					}
					else if (settings[kvp.Key] is double)
					{
						double result;
						if (!double.TryParse(kvp.Value, out result))
						{
							success = false;
							break;
						}
						else
							settings[kvp.Key] = result;
					}
					else
					{
						// Keep the data type as a string.
						settings[kvp.Key] = kvp.Value;
					}
				}
				if (!success)
					SetDefaults(out settings);
			}
			return success;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
	class Options
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
					WriteDefaultConfig();
				}
			}
			else
			{
				WriteDefaultConfig();
			}
		}

		protected void SetDefaults(out Dictionary<string, object> options)
		{
			options = new Dictionary<string, object>(StringComparer.Ordinal);
			options["version"] = 1.0;
			options["sfx volume"] = 100;
			options["bgm volume"] = 100;
			options["player animation type"] = "pingpong";
			options["boss animation type"] = "loop";
			options["player animation speed"] = 4;
			options["boss animation speed"] = 5;
			options["bg swirl speed"] = 0.3;
		}

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

		protected bool WriteDefaultConfig()
		{
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

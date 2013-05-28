using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpiritPurger
{
	/// <summary>
	/// Stores a stringly-typed value.
	/// </summary>
	abstract class OptionVal
	{
		public enum DATA_TYPE {INT, DOUBLE, STRING};
		protected DATA_TYPE dataType;
		protected string data;

		public DATA_TYPE Type { get { return dataType; } }

		public string Data { get { return data; } }

		public OptionVal(DATA_TYPE type, string dat)
		{
			dataType = type;
			data = dat;
		}
	}

	class OptionValInt : OptionVal
	{
		public OptionValInt(string dat)
			: base(DATA_TYPE.INT, dat)
		{ }
	}

	class OptionValDouble : OptionVal
	{
		public OptionValDouble(string dat)
			: base(DATA_TYPE.DOUBLE, dat)
		{ }
	}

	class OptionValString : OptionVal
	{
		public OptionValString(string dat)
			: base(DATA_TYPE.STRING, dat)
		{ }
	}

	class Options
	{
		public const String CONFIG_FILE = "config.cfg";
		protected Dictionary<string, OptionVal> options;

		public Options()
		{
			SetDefaults(out options);
			Dictionary<string, string> tempOptions;
			if (ReadConfigFile(out tempOptions))
				TranslateOptions(tempOptions);
			else
				WriteDefaultConfig();
		}

		protected void SetDefaults(out Dictionary<string, OptionVal> options)
		{
			options = new Dictionary<string, OptionVal>();
			options["sfx volume"] = new OptionValDouble("1.0");
			options["bgm volume"] = new OptionValDouble("1.0");
			options["player animation type"] = new OptionValString("pingpong");
			options["boss animation type"] = new OptionValString("replay");
			options["player animation speed"] = new OptionValInt("4");
			options["boss animation speed"] = new OptionValInt("5");
		}

		protected bool ReadConfigFile(out Dictionary<string,string> tempOptions)
		{
			tempOptions = new Dictionary<string,string>();
			bool good = true;
			string[] lines = {};
			try
			{
				lines = System.IO.File.ReadAllLines(CONFIG_FILE);
			}
			catch
			{
				WriteDefaultConfig();
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
			return false;
		}

		protected void TranslateOptions(Dictionary<string, string> tempOptions)
		{
			foreach (KeyValuePair<string, string> kvp in tempOptions)
			{
			}
		}
	}
}

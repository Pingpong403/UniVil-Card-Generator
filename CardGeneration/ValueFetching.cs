namespace UniVil_Card_Generator.CardGeneration
{
	/// <summary>
	/// Static methods used to get values from files within the project.
	/// </summary>
	public static class ValueFetching
	{
		/// <summary>
		/// Gets the value of a given config setting.
		/// </summary>
		/// <param name="configFile">the config file to search, without the "-config.txt" part</param>
		/// <param name="key">the key whose value you seek</param>
		/// <returns>the value from the given key</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static string GetConfigValue(string configFile, string key)
		{
			string path = Structuring.GetFullPath(Path.Combine("config", configFile + "-config.txt"));
			if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					// Split line into key, value pair
					string[] pair = line.Split(":");

					// If key matches given key, return the value
					if (string.Equals(pair[0], key))
					{
						return pair[1];
					}

					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			
			return "";
		}

		/// <summary>
		/// Retrieves all keys and values from the given config file.
		/// </summary>
		/// <param name="configFile">the config file to search, not including the "-config.txt"</param>
		/// <returns>all of the keys and values from the given config file</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static Dictionary<string, string> GetAllConfigValues(string configFile)
		{
			string path = Structuring.GetFullPath(Path.Combine("config", configFile + "-config.txt"));
			if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

			Dictionary<string, string> pairs = [];
			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					// Split line into key, value pair
					string[] pair = line.Split(":");

					// Assign to dictionary
					pairs[pair[0]] = pair[1];

					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			
			return pairs;
		}

		/// <summary>
		/// Gets the value of a given setting.
		/// </summary>
		/// <param name="settingsFile">the file to search for the setting in</param>
		/// <param name="setting">the setting whose value you seek</param>
		/// <returns>the value from the given setting</returns>
		public static string GetSettingsValue(string settingsFile, string setting)
		{
			string path = Structuring.GetFullPath(Path.Combine("Card Data", "-Settings", settingsFile + "Settings.txt"));
			if (!File.Exists(path))
            {
                return "";
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					if (line != "")
					{
					if (line[0] != '#')
						{
							// Split line into key, value pair
							string[] pair = line.Split(":");

							// If right side is set, return true
							if (pair[0] == setting && pair[1] != "")
							{
								return pair[1];
							}
						}
					}

					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			
			return "";
		}

		/// <summary>
		/// Retrieves all keys and values from the given settings file.
		/// </summary>
		/// <param name="settingsFile">the settings file to search, not including the "Settings.txt"</param>
		/// <returns>all of the keys and values from the given settings file</returns>
		public static Dictionary<string, string> GetAllSettings(string settingsFile)
		{
			Dictionary<string, string> settings = [];
			string path = Structuring.GetFullPath(Path.Combine("Card Data", "-Settings", settingsFile + "Settings.txt"));
			if (!File.Exists(path))
            {
				string dirPath = Structuring.GetFullPath(Path.Combine("Card Data", "-Settings"));
				if (!Directory.Exists(dirPath))
				{
					Directory.CreateDirectory(dirPath);
				}
                else
				{
					Console.WriteLine("Missing " + settingsFile + "Settings.txt!");
				}
				return settings;
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					if (line != "" && line[0] != '#')
					{
						// Split line into key, value pair
						string[] pair = line.Split(":");

						// Assign to dictionary
						settings[pair[0]] = pair[1];
					}
					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
			return settings;
		}

		/// <summary>
		/// Retrieves every keyword and variation in Keywords.txt.
		/// </summary>
		/// <returns>every keyword and variation</returns>
		public static HashSet<string> GetKeywords()
		{
			HashSet<string> keywords = [];

			foreach (string line in GetTextFilesLines("Keywords"))
			{
				// Split line individual keywords
				string[] variations = line.Split("|");

				// Add each to hashset
				foreach (string variation in variations) keywords.Add(variation);
			}

			return keywords;
		}

		/// <summary>
		/// Maps the keyword colors to each variation.
		/// </summary>
		/// <returns>a Dictionary containing each keyword and variation, including their colors</returns>
		public static Dictionary<string, string> GetColorMapping()
		{
			Dictionary<string, string> baseKeywordColors = [];
			Dictionary<string, string> keywordsAndColors = [];

			// Populate dictionary to hold each singular keyword and its corresponding color
			foreach (string line in GetTextFilesLines("Colors"))
			{
				string[] lineSplit = line.Split("|");
				// If color exists in -TextFiles\Colors.txt, use that color.
				// If not, use color found in config\color-config.txt
				if (lineSplit.Length == 1)
				{
					// Search through color-config.txt for correct color
					string searchParam = lineSplit[0].ToLower() + "Color";
					baseKeywordColors[lineSplit[0]] = ValueFetching.GetConfigValue("color", searchParam);
				}
				else
				{
					// First part is the base keyword, second part is its color
					baseKeywordColors[lineSplit[0]] = lineSplit[1];
				}
			}

			// Link each keyword variant to its singular form and therefore its correct color
			foreach (string line in GetTextFilesLines("Keywords"))
			{
				string[] lineSplit = line.Split("|");
				foreach (string variant in lineSplit)
				{
					if (variant != "")
					{
						if (!baseKeywordColors.TryGetValue(lineSplit[0], out string? value))
						{
							keywordsAndColors[variant] = ValueFetching.GetConfigValue("color", "fontColor");
						}
						else
						{
							keywordsAndColors[variant] = value == "" ? ValueFetching.GetConfigValue("color", "fontColor") : value;
						}
					}
				}
			}

			return keywordsAndColors;
		}

		/// <summary>
		/// Retrieves each non-comment line from the given -TextFiles file.
		/// </summary>
		/// <param name="file">the whose lines you seek, without the extension</param>
		/// <returns>each line the generator will not ignore</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static List<string> GetTextFilesLines(string file)
		{
			List<string> lines = [];
			string path = Structuring.GetFullPath(Path.Combine("Card Data", "-TextFiles", file + ".txt"));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Text file not found: {path}");
            }

			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(path);

				// Read the first line of text
				line = sr.ReadLine();

				// Continue to read until you reach end of file
				while (line != null)
				{
					// Skip empty lines
					if (line != "")
					{
						// '#' denotes a comment line
						if (line[0] != '#')
						{
							// Add the line to the list
							lines.Add(line);
						}
					}
					// Read the next line
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}

			return lines;
		}

		/// <summary>
		/// Finds the x and y centers of the given element in layout-config.txt.
		/// </summary>
		/// <param name="element">the element whose position you seek</param>
		/// <returns>a Point containing the center x and center y of the given element</returns>
		public static Point GetElementPos(string element)
		{
			int elementCenterX = int.Parse(GetConfigValue("layout", element + "CenterX"));
			int elementCenterY = int.Parse(GetConfigValue("layout", element + "CenterY"));
			return new Point(elementCenterX, elementCenterY);
		}
	}
}
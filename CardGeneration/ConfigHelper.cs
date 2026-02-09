using System;
using System.IO;

namespace ToonVil_Card_Generator.CardGeneration
{
	public static class ConfigHelper
	{
		/// <summary>
		/// Get the value of a config element.
		/// </summary>
		/// <param name="configFile">The file to be searched, not including the "-config.txt".</param>
		/// <param name="key">The key whose value you seek.</param>
		/// <returns>The value, if any, corresponding to the key in the given file.</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static string GetConfigValue(string configFile, string key)
		{
			string path = PathHelper.GetFullPath(Path.Combine("config", configFile + "-config.txt"));
			if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

			string line;
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
		/// Gets every key, value pair in a given config file.
		/// </summary>
		/// <param name="configFile">The file to be searched, not including the "-config.txt".</param>
		/// <returns>All key, value pairs found in the file.</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static Dictionary<string, string> GetAllValues(string configFile)
		{
			string path = PathHelper.GetFullPath(Path.Combine("config", configFile + "-config.txt"));
			if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Config file not found: {path}");
            }

			Dictionary<string, string> pairs = [];
			string line;
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
	}
}
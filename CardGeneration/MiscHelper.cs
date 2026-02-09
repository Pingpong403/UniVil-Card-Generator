using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ToonVil_Card_Generator.CardGeneration
{
	public static class MiscHelper
	{
		/// <summary>
		/// Run this check before the generator is run to ensure the executable is in the right spot
		/// within the project, and all the necessary directories exist.
		/// </summary>
		/// <returns>whether or not the proper file structure exists</returns>
		public static bool CheckStructure()
		{
			// 1, 2, 4, 8, 16, 32 - higher number, more extreme
			int infractions = 0;
			if (!Directory.Exists(PathHelper.GetFullPath("config\\"))) infractions += 32;
			if (!Directory.Exists(PathHelper.GetFullPath("assets\\"))) infractions += 16;
			if (!Directory.Exists(PathHelper.GetFullPath("fonts\\"))) infractions += 8;
			if (!Directory.Exists(PathHelper.GetFullPath("Card Data\\"))) infractions += 7;
			else
			{
				if (!Directory.Exists(PathHelper.GetFullPath(Path.Combine("Card Data", "-TextFiles\\")))) infractions += 4;
				if (!Directory.Exists(PathHelper.GetFullPath(Path.Combine("Card Data", "-Layout\\")))) infractions += 2;
				if (!Directory.Exists(PathHelper.GetFullPath(Path.Combine("Card Data", "-Images\\")))) infractions += 1;
			}
			switch (infractions)
			{
				case 63:
					Console.WriteLine("Executable is not in project root. Please relocate to ToonVil-Card-Generator folder.");
					return false;
				case int n when n < 63 && n >= 8:
					Console.WriteLine("Missing one or more vital configuration folders. Please redownload or relocate missing folders to the ToonVil-Card-Generator folder.");
					return false;
				case 7:
					Console.WriteLine("Missing Card Data folder. Please redownload or relocate the folder to the ToonVil-Card-Generator folder.");
					return false;
				case int n when n < 7 && n >= 1:
					Console.WriteLine("Missing one or more vital Card Data folders. Please ensure -TextFiles, -Layout, and -Images are placed in Card Data.");
					return false;
				case 0:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Gets all the lines in a given -TextFiles sub-directory.
		/// </summary>
		/// <param name="file">The file to be searched, not including the extension.</param>
		/// <returns>Each non-comment line found in the file.</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static List<string> GetTextFilesLines(string file)
		{
			List<string> lines = [];
			string path = PathHelper.GetFullPath(Path.Combine("Card Data", "-TextFiles", file + ".txt"));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Text file not found: {path}");
            }

			string line;
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

		public static Point GetElementPos(string element)
		{
			int elementCenterX = int.Parse(ConfigHelper.GetConfigValue("layout", element + "CenterX"));
			int elementCenterY = int.Parse(ConfigHelper.GetConfigValue("layout", element + "CenterY"));
			return new Point(elementCenterX, elementCenterY);
		}

		/// <summary>
		/// Looks in -Layout for the given deck element.
		/// </summary>
		/// <param name="deck">the deck name</param>
		/// <param name="element">the element name</param>
		/// <returns>whether or not the given deck element exists</returns>
		public static bool ElementExists(string deck, string element)
		{
			string relativePath = Path.Combine("Card Data", "-Layout", deck + element + ".png");
			if (!File.Exists(PathHelper.GetFullPath(relativePath)))
			{
				relativePath = deck + element + ".jpg";
				if (!File.Exists(PathHelper.GetFullPath(relativePath)))
				{
					relativePath = deck + element + ".jpeg";
					return File.Exists(PathHelper.GetFullPath(relativePath));
				}
				return true;
			}
			return true;
		}

		/// <summary>
		/// Returns the extension belonging to this asset name.
		/// </summary>
		/// <param name="assetName">asset name to find the extension of</param>
		/// <returns>the extension, whether .png, .jpg, or .jpeg, of the asset found, or ""</returns>
		public static string FindExtension(string dir, string fileName)
		{
			string pathNoExt = Path.Combine(dir, fileName);
			string ext = ".png";
			string relativePath = pathNoExt + ext;
			if (!File.Exists(PathHelper.GetFullPath(relativePath)))
			{
				ext = ".jpg";
				relativePath = pathNoExt + ext;
				if (!File.Exists(PathHelper.GetFullPath(relativePath)))
				{
					ext = ".jpeg";
					relativePath = pathNoExt + ext;
					if (!File.Exists(PathHelper.GetFullPath(relativePath)))
					{
						return "";
					}
				}
			}
			return ext;
		}

		/// <summary>
		/// Helper method to capitalize just the first letter in a string.
		/// </summary>
		/// <param name="input">String to be capitalized</param>
		/// <returns>The capitalized string</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <meta>Original code from https://stackoverflow.com/a/4405876</meta>
		public static string Capitalize(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
        };

		/// <summary>
		/// Punctuation hugs the ends of words.
		/// </summary>
		/// <param name="text">Text to be compared to list of punctuation.</param>
		/// <returns>Whether or not given text is punctuation.</returns>
		public static bool IsPunctuation(string text)
		{
			if (".?!,;:/-".Contains(text))
			{
				return true;
			}
			return false;
		}
	}
}
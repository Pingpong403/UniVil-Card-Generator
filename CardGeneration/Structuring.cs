using System;
using UniVil_Card_Generator.CardGeneration;

namespace UniVil_Card_Generator
{
	/// <summary>
	/// Static methods used to check and enforce proper file structure.
	/// </summary>
	public static class Structuring
	{
		/// <summary>
		/// Scan each needed file and determine if the executable is in the right spot.
		/// </summary>
		/// <returns>whether or not the program can be run properly based on the existing structure</returns>
		public static bool CheckStructure()
		{
			// 1, 2, 4, 8, 16, 32 - higher number, more extreme
			int infractions = 0;
			if (!Directory.Exists(GetFullPath("config\\"))) infractions += 32;
			if (!Directory.Exists(GetFullPath("assets\\"))) infractions += 16;
			if (!Directory.Exists(GetFullPath("fonts\\"))) infractions += 8;
			if (!Directory.Exists(GetFullPath("Card Data\\"))) infractions += 7;
			else
			{
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-TextFiles\\")))) infractions += 4;
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-Layout\\")))) infractions += 2;
				if (!Directory.Exists(GetFullPath(Path.Combine("Card Data", "-Images\\")))) infractions += 1;
			}
			switch (infractions)
			{
				case 63:
					Console.WriteLine("Executable is not in project root. Please relocate to UniVil-Card-Generator folder.");
					return false;
				case int n when n < 63 && n >= 8:
					Console.WriteLine("Missing one or more vital configuration folders. Please redownload or relocate missing folders to the UniVil-Card-Generator folder.");
					return false;
				case 7:
					Console.WriteLine("Missing Card Data folder. Please redownload or relocate the folder to the UniVil-Card-Generator folder.");
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
		/// Turns a relative path into a fully context-aware path.
		/// </summary>
		/// <param name="relativePath">the path of the file or directory relative to the project root</param>
		/// <returns>the path to the given file or directory from the computer's root</returns>
		public static string GetFullPath(string relativePath)
		{
			var baseDir = AppContext.BaseDirectory;
			var fullPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", relativePath));
			if (!Directory.Exists(Path.GetDirectoryName(fullPath)) && !File.Exists(fullPath))
			{
				fullPath = Path.GetFullPath(Path.Combine(baseDir, relativePath));
			}
			
			return fullPath;
		}

		/// <summary>
		/// Checks if the given layout element exists in -Layout.
		/// </summary>
		/// <param name="deck">the deck the element belongs to</param>
		/// <param name="element">the name of the element</param>
		/// <returns>whether or not the requested layout element exists in the proper spot</returns>
		public static bool ElementExists(string deck, string element)
		{
			string relativePath = Path.Combine("Card Data", "-Layout", deck + element + ".png");
			if (!File.Exists(GetFullPath(relativePath)))
			{
				relativePath = deck + element + ".jpg";
				if (!File.Exists(GetFullPath(relativePath)))
				{
					relativePath = deck + element + ".jpeg";
					return File.Exists(GetFullPath(relativePath));
				}
				return true;
			}
			return true;
		}

		/// <summary>
		/// Checks if the given asset file exists in assets.
		/// </summary>
		/// <param name="assetCode">the name of the asset, including the asset symbol</param>
		/// <returns>whether or not the given asset exists in the proper spot</returns>
		public static bool AssetExists(string assetCode)
		{
			string assetName;
			assetName = TextManipulation.GetAssetName(assetCode);
			if (assetName == "") return false;
			if (TextManipulation.GainPowerAmount(assetName) != "")
			{
				assetName = "GainPower";
			}
			string pathNoExt = Path.Combine("assets", assetName);
			string relativePath = pathNoExt + FindExtension("assets", assetName);
			return File.Exists(GetFullPath(relativePath));
		}

		/// <summary>
		/// Finds which extension the given file uses.
		/// </summary>
		/// <param name="dir">the directory to check for the file in</param>
		/// <param name="fileName">the file whose extension to fetch</param>
		/// <returns>the extension of the given file within the given directory</returns>
		public static string FindExtension(string dir, string fileName)
		{
			string pathNoExt = Path.Combine(dir, fileName);
			string ext = ".png";
			string relativePath = pathNoExt + ext;
			if (!File.Exists(GetFullPath(relativePath)))
			{
				ext = ".jpg";
				relativePath = pathNoExt + ext;
				if (!File.Exists(GetFullPath(relativePath)))
				{
					ext = ".jpeg";
					relativePath = pathNoExt + ext;
					if (!File.Exists(GetFullPath(relativePath)))
					{
						return "";
					}
				}
			}
			return ext;
		}

		/// <summary>
		/// Checks if the given image exists in -Images.
		/// </summary>
		/// <param name="title">the name of the card</param>
		/// <returns>whether or not the given image exists in the proper spot</returns>
		public static bool ImageExists(string title)
		{
			string pathNoExt = Path.Combine("Card Data", "-Images", title);
			string relativePath = pathNoExt + FindExtension(Path.Combine("Card Data", "-Images"), title);
			return File.Exists(GetFullPath(relativePath));
		}
	}
}
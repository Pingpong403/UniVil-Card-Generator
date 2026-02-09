using System;

namespace ToonVil_Card_Generator
{
	public static class PathHelper
	{
		/// <summary>
		/// Gets the actual full path to whatever relative path you want.
		/// </summary>
		/// <param name="relativePath">The path relative from the parent directory.</param>
		/// <returns>The full path from the project base directory.</returns>
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
	}
}
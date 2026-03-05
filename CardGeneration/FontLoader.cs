using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace UniVil_Card_Generator.CardGeneration
{
    /// <summary>
    /// Static methods used to convert font files into C# Font objects.
    /// </summary>
    public static class FontLoader
    {
        // Cache alt font so it remains valid for the app lifetime
        private static PrivateFontCollection altFont = new();

        /// <summary>
        /// Loads the alt font into the cache.
        /// </summary>
        /// <exception cref="FileNotFoundException"></exception>
        public static void LoadAltFont()
        {
            string altFontFile = ValueFetching.GetConfigValue("text", "altFont");
            string altFontPath = Structuring.GetFullPath(Path.Combine("fonts", altFontFile));

            if (!File.Exists(altFontPath))
            {
                throw new FileNotFoundException("No alt font specified. Please check text-config.txt or add an alternate font.");
            }
            var collection = new PrivateFontCollection();
            collection.AddFontFile(altFontPath);
            altFont = collection;
        }

        /// <summary>
        /// Converts the given parameters into a C# Font.
        /// </summary>
        /// <param name="fontFileName">the full name of the font file to load</param>
        /// <param name="size">the size, in pixels, of the font</param>
        /// <param name="style">the FontStyle of the font to load</param>
        /// <param name="unit">the unit of size the font is to be measured in</param>
        /// <returns>the matching Font object</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Font GetFont(string fontFileName, float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Pixel)
        {
            var relativePath = Path.Combine("fonts", fontFileName);
            var fontPath = Structuring.GetFullPath(relativePath);
            if (!File.Exists(fontPath))
            {
                var altFontFamily = altFont.Families[0];
                return new Font(altFontFamily, size, style, unit);
            }

            var collection = new PrivateFontCollection();
            collection.AddFontFile(fontPath);

            var families = collection.Families;
            if (families.Length == 0)
            {
                throw new InvalidOperationException($"No font families loaded from: {fontPath}");
            }

            var family = families[0];
            return new Font(family, size, style, unit);
        }
    }
}

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Reflection.Metadata;
using System.Diagnostics.Tracing;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Data.SqlTypes;

namespace UniVil_Card_Generator.CardGeneration
{
	public static class PrepareText
	{
		/// <summary>
		/// Creates an image in TextIntermediary that contains the title.
		/// </summary>
		/// <param name="text">title text</param>
		/// <param name="font">font to draw the title in</param>
		/// <param name="textColor">color to draw the title in</param>
		/// <param name="maxWidth">maximum width the title is allowed to take up</param>
		/// <param name="maxHeight">maximum height the title is allowed to take up</param>
		public static void DrawTitle(string text, Font font, Color textColor, int maxWidth, int maxHeight)
		{
			// Setup variables
			float granularity = float.Parse(ValueFetching.GetConfigValue("text", "titleFontDecreaseGranularity"));
			float lineSpacingFactor = float.Parse(ValueFetching.GetConfigValue("text", "titleLineSpacingFactor"));

			// Remove duplicate designation
			if (text[^1] == ')' && text[^3] == '(')
			{
				if (text[^4] == ' ') text = text[0..^4];
				else text = text[0..^3];
			}

			// For titles, capitalize the text
			text = text.ToUpper();

			// Set the textformatflags for center alignment and no trimming
			TextFormatFlags tf = TextFormatFlags.VerticalCenter|
				TextFormatFlags.HorizontalCenter|
				TextFormatFlags.NoPadding;

			// Create a new image of the maximum size for our graphics
			Image img = new Bitmap(maxWidth, maxHeight);
			Graphics drawing = Graphics.FromImage(img);

			// Use high quality everything
			drawing.CompositingQuality = CompositingQuality.HighQuality;
			drawing.InterpolationMode = InterpolationMode.HighQualityBilinear;
			drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
			drawing.SmoothingMode = SmoothingMode.HighQuality;
			drawing.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			
			// Paint a transparent background
			drawing.Clear(Color.Transparent);

			// Get all the words
			List<CardWord> words;

			// Find proper font size given line number
			float lineHeight;
			SizeF textSize;
			do
			{
				words = GetCardWords(text, textColor, font, null);
				lineHeight = TextRenderer.MeasureText(text, font, new Size(10000, 10000), tf).Height * lineSpacingFactor;
				textSize = MeasureWordByWord(words, tf, 10000, lineHeight, lineSpacingFactor);
				if (textSize.Height > maxHeight) font = new Font(font.Name, font.Size - granularity, font.Style, font.Unit);
			} while (textSize.Height > maxHeight);

			// Find the proper squish ratio for given title
			float textFullWidth = TextRenderer.MeasureText(text, font, new Size(1000, 1000), tf).Width;
			if (textFullWidth > maxWidth)
			{
				float horizontalSquish = maxWidth / textFullWidth;
				drawing.ScaleTransform(horizontalSquish, 1.0F);
				maxWidth = (int)textFullWidth;
			}

			// Set up variables
			float startY = (maxHeight - textSize.Height) / 2;

			// Draw title word by word
			DrawWordByWord(words, drawing, tf, maxWidth, lineHeight, maxWidth / 2, startY, lineSpacingFactor);

			drawing.Save();
			drawing.Dispose();

			// Ensure output directory exists and save per-element PNG
			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, "Title.png");
			img.Save(outpath, ImageFormat.Png);
			img.Dispose();
		}

		/// <summary>
		/// Uses the given text to create an image containing a card ability. Outputs to TextIntermediary.
		/// </summary>
		/// <param name="ability">the text to draw in the Ability area</param>
		/// <param name="activateAbility">the text to draw in the Activate Ability area</param>
		/// <param name="activateCost">the text to draw in the Activate Cost area</param>
		/// <param name="gainsAction">the text to draw in the Gains Action area</param>
		/// <param name="font">the font with which to draw the text</param>
		/// <param name="textColor">the base color of the text</param>
		/// <param name="maxWidth">how much horizontal space the ability can take up</param>
		/// <param name="maxHeight">how much vertical space the ability can take up</param>
		/// <param name="keywordsAndColors">a mapping of every possible keyword and the colors they will be drawn in</param>
		public static void DrawAbility(string ability, string activateAbility, string activateCost, string gainsAction, Font font, Color textColor, int maxWidth, int maxHeight, Dictionary<string, string> keywordsAndColors)
		{
			// Set up variables we'll potentially need
			float granularity = float.Parse(ValueFetching.GetConfigValue("text", "fontDecreaseGranularity"));
			float paddingLines = float.Parse(ValueFetching.GetConfigValue("text", "abilityPaddingLines"));
			float minFontSize = float.Parse(ValueFetching.GetConfigValue("text", "abilityMinFontSize"));
			float actionSymbolLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			float lineSpacing = float.Parse(ValueFetching.GetConfigValue("text", "lineSpacingFactor"));
			int abilityBottomPadding = int.Parse(ValueFetching.GetConfigValue("card", "abilityBottomPadding"));
			int sideAAMaxW = int.Parse(ValueFetching.GetConfigValue("card", "sideActivateAbilityMaxWidth"));
			int sideAACenterX = int.Parse(ValueFetching.GetConfigValue("card", "sideActivateAbilityCenterX"));
			bool useAltAssets = ValueFetching.GetSettingsValue("Card", "useAlternateAssets") == "true";

			// Set the textformatflags for center alignment and no trimming
			TextFormatFlags tf = TextFormatFlags.VerticalCenter|
				TextFormatFlags.HorizontalCenter|
				TextFormatFlags.NoPadding;

			// Create a new image of the maximum size for our graphics
			Image img = new Bitmap(maxWidth, maxHeight + abilityBottomPadding);
			Graphics g = Graphics.FromImage(img);

			// Use high quality everything
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			
			// Paint a transparent background
			g.Clear(Color.Transparent);

			// We need this for symbol resizing
			float originalLineHeight = TextRenderer.MeasureText("Tq", font, new Size(1000, 1000), tf).Height * lineSpacing;

			// Naively find the maximum allowable font size by measuring everything
			float lineHeight;
			float abilityHeight;
			float activateAbilityHeight;
			bool activateAbilityTextTaller = true;
			float gainsActionHeight;
			float paddingHeight;
			float textHeight;
			do
			{
				// Combine every given ability into one metric
				lineHeight = TextRenderer.MeasureText("TjJ", font, new Size(1000, 1000), tf).Height * lineSpacing;
				abilityHeight = ability == "" ? 0 : MeasureWordByWord(GetCardWords(ability, textColor, font, keywordsAndColors), tf, maxWidth, lineHeight, lineSpacing).Height;
				activateAbilityHeight = 0;
				if (activateAbility != "" || activateCost != "")
				{
					if (ability == "" || activateCost != "") // If there is no ability or there is an activate cost, measure normally
					{
						activateAbilityHeight += actionSymbolLines * lineHeight + MeasureWordByWord(GetCardWords(activateAbility, textColor, font, keywordsAndColors), tf, maxWidth, lineHeight, lineSpacing).Height;
					}
					else
					{
						float aaTextHeight = MeasureWordByWord(GetCardWords(activateAbility, textColor, font, keywordsAndColors), tf, sideAAMaxW, lineHeight, lineSpacing).Height;
						float aaSymbolHeight = actionSymbolLines * lineHeight;
						activateAbilityTextTaller = aaTextHeight > aaSymbolHeight;
						activateAbilityHeight += Math.Max(aaSymbolHeight, aaTextHeight);
					}
				}
				gainsActionHeight = gainsAction == "" ? 0 : MeasureWordByWord(GetCardWords(gainsAction, textColor, font, keywordsAndColors), tf, maxWidth, lineHeight, lineSpacing).Height;
				int numPadding = (abilityHeight > 0 ? 1 : 0) + (activateAbilityHeight > 0 ? 1 : 0) + (gainsActionHeight > 0 ? 1 : 0) - 1;
				if (numPadding < 0) numPadding = 0;
				paddingHeight = numPadding * lineHeight * paddingLines;
				textHeight = abilityHeight + activateAbilityHeight + gainsActionHeight + paddingHeight;

				if (textHeight > maxHeight - (activateAbility != "" && ability == "" ? textHeight * 0.1 : 0)) font = new Font(font.Name, font.Size - granularity, font.Style, font.Unit);
			} while (textHeight > maxHeight);

			// Check if font size went below minimum and notify the user
			if (font.Size < minFontSize)
			{
				Console.WriteLine($"The following card's Ability went below the minimum {minFontSize}px:");
			}

			string regularVariant = ValueFetching.GetConfigValue("text", "abilityFont");
			string boldVariant = regularVariant[0..^4] + "Bold" + regularVariant[^4..^0];
			string boldPath = Structuring.GetFullPath(Path.Combine("fonts", boldVariant));
			Font abilityCostFont = File.Exists(boldPath) ?
				FontLoader.GetFont(boldPath, font.Size, FontStyle.Regular) :
				new(font, FontStyle.Bold);
			List<CardWord> colon = GetCardWords("$:$", textColor, font, keywordsAndColors);

			// For resizing symbols with more complexity
			float lineHeightRatio = lineHeight / originalLineHeight;

			// Drawing variables
			float currentY = (maxHeight - textHeight) / 2;
			List<CardWord> words;

			// Draw the ability first
			if (ability != "")
			{
				words = GetCardWords(ability, textColor, font, keywordsAndColors);
				currentY = DrawWordByWord(words, g, tf, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
				currentY += lineHeight * paddingLines;
			}

			// Then draw the activate symbol, cost, and ability
			if (activateAbility != "" || activateCost != "")
			{
				// Symbol
				string assetName = "Activate";
				if (useAltAssets) assetName += ValueFetching.GetConfigValue("asset", "alternateDesignation");
				string activateSymbolPath = Structuring.GetFullPath(Path.Combine("assets", assetName + Structuring.FindExtension("assets", assetName)));
				Image activateSymbol = Image.FromFile(activateSymbolPath);
				float resizing = actionSymbolLines * lineHeight / activateSymbol.Height;
				float symbolCenterX = maxWidth / 2;
				if (ability == "" || activateCost != "") // If there is no ability or there is an activate cost, draw normally
				{
					int colonCenterX = int.Parse(ValueFetching.GetConfigValue("card", "colonCenterX"));
					int colonPadding = int.Parse(ValueFetching.GetConfigValue("card", "colonPadding"));
					bool drawColon = activateCost != "" && activateCost[0..4] == "Pay " && (activateCost[^6..^0] == " Power" || activateCost[^7..^0] == " Power.");
					float symbolW = activateSymbol.Width * resizing;
					if (activateCost != "")
					{
						symbolCenterX = colonCenterX - colonPadding - symbolW / 2;
					}
					if (ability == "" && activateAbility != "")
					{
						currentY = lineHeight * 0.1F;
					}
					DrawSymbol(activateSymbol, g, textColor, symbolCenterX, currentY + actionSymbolLines * lineHeight / 2, resizing);
					
					// Cost, if any
					if (activateCost != "")
					{
						float costLeftX = colonCenterX + colonPadding;
						float activateCostWidth = maxWidth / 2;
						float activateCostHeight = MeasureWordByWord(GetCardWords(activateCost, textColor, font, keywordsAndColors), tf, activateCostWidth, lineHeight, lineSpacing).Height;
						float activateCostY = currentY + (actionSymbolLines * lineHeight - activateCostHeight) / 2; // maximum of 3 lines for clarity
						if (drawColon)
						{
							Font acFont = new Font(font, FontStyle.Bold);
							float costCenterX = costLeftX + TextRenderer.MeasureText(activateCost, acFont, new Size(1000, 1000), tf).Width / 2;
							DrawWordByWord(colon, g, tf, maxWidth, lineHeight, colonCenterX, activateCostY, lineSpacing);
							words = GetCardWords(activateCost, textColor, acFont, keywordsAndColors);
							DrawWordByWord(words, g, tf, activateCostWidth, lineHeight, costCenterX, activateCostY, lineSpacing);
						}
						else
						{
							float costCenterX = costLeftX + TextRenderer.MeasureText(activateCost, font, new Size(1000, 1000), tf).Width / 2;
							words = GetCardWords(activateCost, textColor, font, keywordsAndColors);
							DrawWordByWord(words, g, tf, activateCostWidth, lineHeight, maxWidth / 2 + costCenterX, activateCostY, lineSpacing);
						}
					}
					currentY += actionSymbolLines * lineHeight;

					// Ability, if any
					if (activateAbility != "")
					{
						words = GetCardWords(activateAbility, textColor, font, keywordsAndColors);
						currentY = DrawWordByWord(words, g, tf, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
					}
				}
				else // Otherwise, the activate ability is to the right of the symbol
				{
					// Symbol
					symbolCenterX = sideAACenterX - sideAAMaxW / 2 - 100 - activateSymbol.Width * resizing / 2;
					DrawSymbol(activateSymbol, g, textColor, symbolCenterX, currentY + activateAbilityHeight / 2, resizing);
					
					// Activate ability
					words = GetCardWords(activateAbility, textColor, font, keywordsAndColors);
					float drawY = currentY;
					if (!activateAbilityTextTaller)
					{
						drawY += (activateAbilityHeight - MeasureWordByWord(words, tf, sideAAMaxW, lineHeight, lineSpacing).Height) / 2;
					}
					DrawWordByWord(words, g, tf, sideAAMaxW, lineHeight, maxWidth - sideAAMaxW / 2 - 30, drawY, lineSpacing);
					currentY += activateAbilityHeight;
				}
				currentY += lineHeight * paddingLines;
			}

			// Finally, draw the gained action
			if (gainsAction != "")
			{
				words = GetCardWords(gainsAction, textColor, font, keywordsAndColors);
				DrawWordByWord(words, g, tf, maxWidth, lineHeight, maxWidth / 2, currentY, lineSpacing);
			}

			g.Save();

			g.Dispose();

			// Ensure output directory exists and save per-element PNG
			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, "Ability.png");
			img.Save(outpath, ImageFormat.Png);
			img.Dispose();
		}

		/// <summary>
		/// Uses the given text to create an image containing a card type. Outputs to TextIntermediary.
		/// </summary>
		/// <param name="text">the text to convert into a Type image</param>
		/// <param name="font">the font with which to draw the text</param>
		/// <param name="textColor">the base color of the text</param>
		/// <param name="maxWidth">how much horizontal space the text can take up</param>
		/// <param name="maxHeight">how much vertical space the text can take up</param>
		/// <param name="keywordsAndColors">a mapping of every possible keyword and the colors they will be drawn in</param>
		public static void DrawType(string text, Font font, Color textColor, int maxWidth, int maxHeight, Dictionary<string, string> keywordsAndColors)
		{
			// Set the textformatflags for center alignment and no trimming
			TextFormatFlags tf = TextFormatFlags.VerticalCenter|
				TextFormatFlags.HorizontalCenter|
				TextFormatFlags.NoPadding;

			// Create a new image of the maximum size for our graphics
			Image img = new Bitmap(maxWidth, maxHeight);
			Graphics g = Graphics.FromImage(img);

			// Use high quality everything
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			
			// Paint a transparent background
			g.Clear(Color.Transparent);

			// One line maximum
			int textHeight = TextRenderer.MeasureText(text, font, new Size(1000, 1000), tf).Height;

			// Find the proper squish ratio for given title
			float textFullWidth = TextRenderer.MeasureText(text, font, new Size(1000, 1000), tf).Width;
			if (textFullWidth > maxWidth)
			{
				float horizontalSquish = maxWidth / textFullWidth;
				g.ScaleTransform(horizontalSquish, 1.0F);
				maxWidth = (int)textFullWidth;
			}

			// Get all the words
			List<CardWord> words = GetCardWords(text, textColor, font, keywordsAndColors, true);

			// Set up variables
			float startY = (maxHeight - textHeight) / 2;

			// Draw type word by word
			DrawWordByWord(words, g, tf, maxWidth, textHeight, maxWidth / 2, startY, 1.0F);

			g.Save();
			g.Dispose();

			// Ensure output directory exists and save per-element PNG
			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, "Type.png");
			img.Save(outpath, ImageFormat.Png);
			img.Dispose();
		}

		/// <summary>
		/// Uses the given text to create an image containing a card corner element. Outputs to TextIntermediary.
		/// </summary>
		/// <param name="text">the text to convert into a corner element image</param>
		/// <param name="font">the font with which to draw the text</param>
		/// <param name="textColor">the base color of the text</param>
		/// <param name="element">which element this is being made for</param>
		/// <param name="maxWidth">how much horizontal space the text can take up</param>
		/// <param name="maxHeight">how much vertical space the text can take up</param>
		public static void DrawCornerElement(string text, Font font, Color textColor, string element, int maxWidth, int maxHeight)
		{
			// Set the textformatflags for center alignment and no trimming
			TextFormatFlags tf = TextFormatFlags.VerticalCenter|
				TextFormatFlags.HorizontalCenter|
				TextFormatFlags.NoPadding;

			// Create a new image of the maximum size for our graphics
			Image img = new Bitmap(maxWidth, maxHeight);
			Graphics g = Graphics.FromImage(img);

			// Use high quality everything
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.InterpolationMode = InterpolationMode.HighQualityBilinear;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			
			// Paint a transparent background
			g.Clear(Color.Transparent);

			// One line maximum
			int textHeight = TextRenderer.MeasureText(text, font, new Size(1000, 1000), tf).Height;

			// Find the proper squish ratio for given corner element
			int textFullWidth = TextRenderer.MeasureText(text, font, new Size(1000, 1000), tf).Width;
			if (textFullWidth > maxWidth)
			{
				float horizontalSquish = (float)maxWidth / textFullWidth;
				g.ScaleTransform(horizontalSquish, 1.0F);
				maxWidth = textFullWidth;
			}

			// Get all the words
			List<CardWord> words = GetCardWords(text, textColor, font, null);

			// Set up variables
			int startY = (maxHeight - textHeight) / 2;

			// Draw corner element word by word
			DrawWordByWord(words, g, tf, maxWidth, textHeight, maxWidth / 2, startY, 1.0F);

			g.Save();
			g.Dispose();

			// Ensure output directory exists and save per-element PNG
			var relativeOutDir = Path.Combine("temp", "TextIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, element + ".png");
			img.Save(outpath, ImageFormat.Png);
			img.Dispose();
		}

		/// <summary>
		/// Converts a single string of text into separate CardWord objects with the proper font, FontStyle, and color.
		/// </summary>
		/// <param name="text">the text to convert into CardWord objects</param>
		/// <param name="defaultColor">the base color of the text</param>
		/// <param name="defaultFont">the base font regular words will use</param>
		/// <param name="keywordData">a mapping of each possible color and the colors they use</param>
		/// <param name="isType">indicates whether these words are part of the Type element</param>
		/// <returns>a list of separate words with the proper formatting</returns>
		public static List<CardWord> GetCardWords(string text, Color defaultColor, Font defaultFont, Dictionary<string, string>? keywordData, bool isType = false)
		{
			bool typeIsCaps = ValueFetching.GetConfigValue("text", "typeIsCaps") == "true";
			bool typeIsBold = ValueFetching.GetConfigValue("text", "typeIsBold") == "true";
			bool typeInAbilityIsBold = ValueFetching.GetConfigValue("text", "typeInAbilityIsBold") == "true";
			
			char italicSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "italicCharacter"));
			char boldSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "boldCharacter"));
			char escapeSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "escapeCharacter"));
			char newlineSymbol = Convert.ToChar(ValueFetching.GetConfigValue("text", "newlineCharacter"));

			Font italicFont = new(defaultFont, FontStyle.Italic);

			Font boldFont;
			string regularVariant = ValueFetching.GetConfigValue("text", isType ? "typeFont" : "abilityFont");
			string boldVariant = regularVariant[0..^4] + "Bold" + regularVariant[^4..^0];
			string boldPath = Structuring.GetFullPath(Path.Combine("fonts", boldVariant));
			boldFont = File.Exists(boldPath) ?
				FontLoader.GetFont(boldPath, defaultFont.Size, FontStyle.Bold) :
				new(defaultFont, FontStyle.Bold);

			Font boldItalicFont = new(boldFont, FontStyle.Bold | FontStyle.Italic);

			List<CardWord> cardWords = [];

			bool italicsOpen = false;
			bool boldOpen = false;
			bool escapeNext = false;
			bool ignoreFormatting = false;
			string builtWord = "";
			foreach (char letter in text)
			{
				if (letter == ' ')
				{
					// End of word
					if (builtWord != "")
					{ // keyword, type, typeBold, typeInAbilityBold, bolding
						// keyword && (type && typeBold || !type && typeInAbilityBold) XR bolding
						bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
						bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
						bool boldWord = isKeyword && masterBold && !boldOpen ||
									   !(isKeyword && masterBold) && boldOpen;
						CardWord word = new(
							builtWord,
							isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
							boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
						);
						word.SetType(isType);
						if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
						cardWords.Add(word);
					}
					cardWords.Add(new CardWord(" ", defaultColor, defaultFont));
					builtWord = "";
					ignoreFormatting = false;
				}
				else if (escapeNext)
				{
					// If the next was a symbol, then we escape it
					if (letter == italicSymbol ||
						letter == boldSymbol   ||
						letter == escapeSymbol ||
						letter == newlineSymbol
						)
					{
						cardWords.Add(new CardWord(Convert.ToString(letter), defaultColor, defaultFont));
					}

					// Otherwise, this means the next word should not be formatted
					else
					{
						if (builtWord == "") ignoreFormatting = true;
						builtWord += letter;
					}
					escapeNext = false;
				}
				else
				{
					if (letter == italicSymbol)
					{
						italicsOpen = !italicsOpen;
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
							bool boldWord = isKeyword && masterBold && !boldOpen ||
										  !(isKeyword && masterBold) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
								boldWord && !ignoreFormatting ? (!italicsOpen ? boldItalicFont : boldFont) : !italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
					}
					else if (letter == boldSymbol)
					{
						boldOpen = !boldOpen;
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
							bool boldWord = isKeyword && masterBold && !boldOpen ||
										  !(isKeyword && masterBold) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
					}
					else if (letter == escapeSymbol)
					{
						escapeNext = true;
					}
					else if (letter == newlineSymbol || letter == '\n')
					{
						if (builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
							bool boldWord = isKeyword && masterBold && !boldOpen ||
										  !(isKeyword && masterBold) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							builtWord = "";
						}
						cardWords.Add(new CardWord("\n", defaultColor, defaultFont));
						ignoreFormatting = false;
					}
					else
					{
						if (TextManipulation.IsPunctuation(Convert.ToString(letter)) && builtWord != "")
						{
							bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
							bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
							bool boldWord = isKeyword && masterBold && !boldOpen ||
										  !(isKeyword && masterBold) && boldOpen;
							CardWord word = new(
								builtWord,
								isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
								boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
							);
							word.SetType(isType);
							if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
							cardWords.Add(word);
							cardWords.Add(new CardWord(Convert.ToString(letter), defaultColor, defaultFont));
							builtWord = "";
							ignoreFormatting = false;
						}
						// Most generic case - add a letter to builtWord
						else
						{
							builtWord += letter;
						}
					}
				}
			}
			if (builtWord != "")
			{
				bool isKeyword = keywordData != null && keywordData.TryGetValue(builtWord, out string? value);
				bool masterBold = isType && typeIsBold || !isType && typeInAbilityIsBold;
				bool boldWord = isKeyword && masterBold && !boldOpen ||
							  !(isKeyword && masterBold) && boldOpen;
				CardWord word = new(
					builtWord,
					isKeyword && !ignoreFormatting ? Color.FromArgb(Convert.ToInt32("ff" + keywordData[builtWord], 16)) : defaultColor,
					boldWord && !ignoreFormatting ? (italicsOpen ? boldItalicFont : boldFont) : italicsOpen ? italicFont : defaultFont
				);
				word.SetType(isType);
				if (isType && typeIsCaps) word.SetText(word.GetText().ToUpper());
				cardWords.Add(word);
			}

			return cardWords;
		}

		/// <summary>
		/// Iterates through each word to determine the longest line and vertical space they will take up.
		/// </summary>
		/// <param name="words">the words to measure</param>
		/// <param name="tf">which TextFormatFlags to use to measure the text</param>
		/// <param name="maxWidth">the maximum width each line can take up</param>
		/// <param name="lineHeight">how far down to move the register when a line ends</param>
		/// <param name="lineSpacing">the ratio that determines how much line height to use</param>
		/// <returns>the maximum line width and total height of the text</returns>
		public static SizeF MeasureWordByWord(List<CardWord> words, TextFormatFlags tf, float maxWidth, float lineHeight, float lineSpacing)
		{
			float lineBreakLines = float.Parse(ValueFetching.GetConfigValue("text", "lineBreakLines"));
			float actionSymbolLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			float dividingLineLines = float.Parse(ValueFetching.GetConfigValue("asset", "dividingLineLines"));

			float longestLine = 0;
			float textHeight = 0;
			float lineWidth = 0;
			int consecutiveLineBreakCount = 0;
			bool space = false;
			float spaceWidth = 0;
			foreach (CardWord word in words)
			{
				// Keywords: add the amount of vertical space they take up
				if (Structuring.AssetExists(word.GetText()))
				{
					consecutiveLineBreakCount = 0;
					if (lineWidth > 0) // Check if some words have already been added to line
					{
						textHeight += lineHeight;
						if (lineWidth > longestLine) longestLine = lineWidth;
						lineWidth = 0;
						space = false;
					}
					if (word.GetText() == "DividingLine_") textHeight += dividingLineLines * lineHeight;
					else textHeight += actionSymbolLines * lineHeight;
				}
				// Spaces: set flag
				else if (word.GetText() == " ")
				{
					consecutiveLineBreakCount = 0;
					space = true;
					spaceWidth = word.GetSizeF((int)maxWidth, tf).Width;
				}
				// Newlines: add a line
				else if (word.GetText() == "\n")
				{
					consecutiveLineBreakCount++;
					switch (consecutiveLineBreakCount % 3)
					{
						case 1:
							textHeight += lineHeight;
							break;
						case 2:
							textHeight += lineHeight * lineBreakLines;
							break;
						case 0:
							textHeight += lineHeight * (1.0F - lineBreakLines);
							break;
						default:
							break;
					}
					if (lineWidth > longestLine) longestLine = lineWidth;
					lineWidth = 0.001F; // Completely ignore the text that was already built up
				}
				// Generic case: add word's width (+ space), check if over
				else
				{
					consecutiveLineBreakCount = 0;
					float wordWidth = word.GetSizeF((int)maxWidth, tf).Width + (space ? spaceWidth : 0);
					lineWidth += wordWidth;
					if (lineWidth > maxWidth)
					{
						textHeight += lineHeight;
						if (lineWidth > longestLine) longestLine = lineWidth - wordWidth;
						lineWidth = word.GetSizeF((int)maxWidth, tf).Width;
					}
					space = false;
				}
			}
			if (lineWidth > 0)
			{
				textHeight += lineHeight;
				if (lineWidth > longestLine) longestLine = lineWidth;
			}

			// Add the end of the last line that was culled
			textHeight += (1 - lineSpacing) * lineHeight * lineSpacing;
			return new SizeF(longestLine, textHeight);
		}

		/// <summary>
		/// Draws the given words one by one.
		/// </summary>
		/// <param name="words">the list of words to draw</param>
		/// <param name="g">the Graphics object to draw the words with</param>
		/// <param name="tf">which TextFormatFlags to use to draw the text</param>
		/// <param name="maxWidth">the maximum width each line can take up</param>
		/// <param name="lineHeight">how far down to move the register when a line ends</param>
		/// <param name="centerX">the horizontal center to draw the text around</param>
		/// <param name="startY">the y-position the first line should be drawn at</param>
		/// <param name="lineSpacing">the ratio that determines how much line height to use</param>
		/// <returns>the ending y-position</returns>
		private static float DrawWordByWord(List<CardWord> words, Graphics g, TextFormatFlags tf, float maxWidth, float lineHeight, float centerX, float startY, float lineSpacing)
		{
			// Set up variables we'll potentially need
			float lineBreakLines = float.Parse(ValueFetching.GetConfigValue("text", "lineBreakLines"));
			float dlLines = float.Parse(ValueFetching.GetConfigValue("asset", "dividingLineLines"));
			float asLines = float.Parse(ValueFetching.GetConfigValue("asset", "actionSymbolLines"));
			int horizontalPadding = int.Parse(ValueFetching.GetConfigValue("text", "wordHorizontalPadding"));
			bool useAltAssets = ValueFetching.GetSettingsValue("Card", "useAlternateAssets") == "true";
			Color color = ColorTranslator.FromHtml("#" + ValueFetching.GetConfigValue("color", "fontColor"));

			// Draw text word by word
			float currentY = startY;
			int iCheck = 0;
			int iDraw = 0;
			float lineLength;
			bool skipLine;
			bool countLineBreaks;
			bool drawAsset = false;
			bool endOfText = false;
			while (!endOfText)
			{
				// First, find the length of this line
				lineLength = 0;
				skipLine = false;
				countLineBreaks = false;
				try
				{
					bool endOfLine = false;
					float currentWordWidth;
					bool space = false;
					float spaceWidth = 0;
					while (!endOfLine)
					{
						// Measure each word
						currentWordWidth = words[iCheck].GetSizeF(maxWidth, tf).Width;

						if (words[iCheck].GetText() == " ")
						{
							if (lineLength == 0)
							{
								space = true;
								spaceWidth = 0;
								iDraw++;
							}
							else
							{
								space = true;
								spaceWidth = words[iCheck].GetSizeF(maxWidth, tf).Width;
							}
							iCheck++;
						}
						else if (Structuring.AssetExists(words[iCheck].GetText()))
						{
							endOfLine = true;
							if (lineLength == 0) skipLine = true;
							drawAsset = true;
						}
						else if (words[iCheck].GetText() == "\n")
						{
							endOfLine = true;
							countLineBreaks = true;
							iCheck++;
						}
						else if (lineLength + currentWordWidth + (space ? spaceWidth : 0) > maxWidth)
						{
							endOfLine = true;
						}
						else
						{
							lineLength += currentWordWidth + (space ? spaceWidth : 0);
							space = false;
							iCheck++;
						}
					}
				}
				catch (Exception ex)            
				{                
					if (ex is IndexOutOfRangeException || ex is ArgumentOutOfRangeException)
					{
						endOfText = true;
					}
					else
						throw;
				}
				// Draw each word in the line
				float currentX = centerX - lineLength / 2;
				for (int i = iDraw; i < iCheck; i++)
				{
					CardWord word = words[i];
					if (word.GetText() != "\n")
					{
						float wordWidth = (word.GetText() != " " || currentX > centerX - lineLength / 2) ? word.GetSizeF((int)maxWidth, tf).Width : 0;
						float wordHeight = word.GetSizeF((int)maxWidth, tf).Height;
						Bitmap textB = new((int)wordWidth + horizontalPadding, (int)wordHeight);
						Graphics textG = Graphics.FromImage(textB);
						textG.CompositingQuality = CompositingQuality.HighQuality;
						textG.InterpolationMode = InterpolationMode.HighQualityBilinear;
						textG.PixelOffsetMode = PixelOffsetMode.HighQuality;
						textG.SmoothingMode = SmoothingMode.HighQuality;
						textG.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
						Color bgColor = ColorTranslator.FromHtml("#7f8f01");
						textG.Clear(bgColor);
						TextRenderer.DrawText(textG, word.GetText(), word.GetTextFont(), new Rectangle(0, (int)((lineHeight - wordHeight) * lineSpacing / 2), (int)wordWidth + horizontalPadding, (int)wordHeight), word.GetTextColor(), bgColor, tf);
						ImageManipulation.Mask(textB, word.GetTextColor(), bgColor);
						g.DrawImage(textB, new Point((int)currentX - horizontalPadding / 2, (int)currentY));
						currentX += wordWidth;
						iDraw++;
					}
				}
				// Move the register down
				if (!skipLine)
				{
					if (countLineBreaks)
					{
						int consecutiveLineBreakCount = 0;
						while (words[iDraw].GetText() == "\n")
						{
							consecutiveLineBreakCount++;
							switch (consecutiveLineBreakCount % 3)
							{
								case 1:
									currentY += lineHeight;
									break;
								case 2:
									currentY += lineHeight * lineBreakLines;
									break;
								case 0:
									currentY += lineHeight * (1.0F - lineBreakLines);
									break;
								default:
									break;
							}
							iDraw++;
						}
						iCheck = iDraw;
					}
					else currentY += lineHeight;
				}
				// Draw the asset that is up to draw
				if (drawAsset)
				{
					string assetName = TextManipulation.GetAssetName(words[iDraw].GetText());
					string gainPowerAmt = TextManipulation.GainPowerAmount(assetName);
					if (gainPowerAmt != "")
					{
						assetName = "GainPower";
					}
					if (useAltAssets) assetName += ValueFetching.GetConfigValue("asset", "alternateDesignation");
					string gainsSymbolPath = Structuring.GetFullPath(Path.Combine("assets", assetName + Structuring.FindExtension("assets", assetName)));
					Image asset = Image.FromFile(gainsSymbolPath);
					float resizing = string.Equals(assetName, "DividingLine") || string.Equals(assetName, "DividingLine" + ValueFetching.GetConfigValue("asset", "alternateDesignation")) ? 1.0F : asLines * lineHeight / asset.Height;
					float yOffset = string.Equals(assetName, "DividingLine") || string.Equals(assetName, "DividingLine" + ValueFetching.GetConfigValue("asset", "alternateDesignation")) ? dlLines * lineHeight / 2 : asLines * lineHeight / 2;
					DrawSymbol(asset, g, color, maxWidth / 2, currentY + yOffset, resizing);

					// If this was a Gain Power action, draw the amount to be gained
					if (gainPowerAmt != "")
					{
						Font gainPowerFont = FontLoader.GetFont(
							ValueFetching.GetConfigValue("text", "elementFont"),
							float.Parse(ValueFetching.GetConfigValue("text", "costFontSize")) * resizing
						);
						Point gainPowerPos = new(
							(int)maxWidth,
							(int)(currentY + asLines * lineHeight / 2)
						);
						TextRenderer.DrawText(g, gainPowerAmt, gainPowerFont, gainPowerPos, color, tf);
					}
					currentY += lineHeight * (string.Equals(assetName, "DividingLine") ? dlLines : asLines);
					iCheck++;
					iDraw++;
					drawAsset = false;
				}
			}
			return currentY;
		}

		/// <summary>
		/// Draws the given square symbol at the given coordinates.
		/// </summary>
		/// <param name="symbol">the symbol to draw</param>
		/// <param name="g">the Graphics object this will use to draw</param>
		/// <param name="centerX">the x-coordinate of the center of the symbol</param>
		/// <param name="centerY">the y-coordinate of the center of the symbol</param>
		/// <param name="resizing">optional resizing factor</param>
		private static void DrawSymbol(Image symbol, Graphics g, Color color, float centerX, float centerY, float resizing = 1.0F)
		{
			Bitmap b = new(symbol, new Size((int)(symbol.Width * resizing), (int)(symbol.Height * resizing)));
			ImageManipulation.ColorSymbol(b, color);
			float x = centerX - resizing * symbol.Width / 2;
			float y = centerY - resizing * symbol.Height / 2;
			g.DrawImage(b, new PointF(x, y));
		}

		/// <summary>
		/// Represents a word to be placed on a card.
		/// </summary>
		public class CardWord
		{
			private string text;
			private Color textColor;
			private Font textFont;
			private bool isType = false;

			public CardWord()
			{
				text = "";
				textColor = Color.Black;
				textFont = new Font(FontLoader.GetFont("roboto.ttf", 1), new FontStyle());
			}

			public CardWord(string text)
			{
				this.text = text;
				textColor = Color.Black;
				textFont = new Font(FontLoader.GetFont("roboto.ttf", 1), new FontStyle());
			}

			public CardWord(string text, Color textColor, Font textFont)
			{
				this.text = text;
				this.textColor = textColor;
				this.textFont = textFont;
			}

			public CardWord(CardWord other)
			{
				text = other.GetText();
				textColor = other.GetTextColor();
				textFont = other.GetTextFont();
			}

			public string GetText() { return text; }
			public Color GetTextColor() { return textColor; }
			public Font GetTextFont() { return textFont; }
			public bool IsType() { return isType; }

			public void SetText(string text) { this.text = text; }
			public void SetTextBrush(Color textColor) { this.textColor = textColor; }
			public void SetTextFont(Font textFont) { this.textFont = textFont; }
			public void SetType(bool isType) { this.isType = isType; }

			public SizeF GetSizeF(float maxWidth, TextFormatFlags tf)
			{
				if (text == " ")
				{
					return TextRenderer.MeasureText(text, textFont, new Size((int)maxWidth, 1000), tf) * float.Parse(ValueFetching.GetConfigValue("text", "spaceShrink"));
				}
				return TextRenderer.MeasureText(text, textFont, new Size((int)maxWidth, 1000), tf);
			}
		}
	}
}
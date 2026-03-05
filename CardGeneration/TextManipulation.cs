namespace UniVil_Card_Generator.CardGeneration
{
	/// <summary>
	/// Static methods used to manipulate text.
	/// </summary>
	public static class TextManipulation
	{
		/// <summary>
		/// Strips a title of any functional characters.
		/// </summary>
		/// <param name="title">the text that contains functional characters to be removed</param>
		/// <returns>the title, ready to be output as the card name</returns>
		public static string CleanTitle(string title)
		{
			string cleanTitle = "";

			bool alreadyAddedSpace = false;
			foreach (char letter in title)
			{
				// For spaces, only add one between words
				if (letter == ' ')
				{
					if (!alreadyAddedSpace)
					{
						cleanTitle += ' ';
						alreadyAddedSpace = true;
					}
				}

				// For newline characters, remove them and add a space
				else if (letter == '%' || letter == '\n')
				{
					if (!alreadyAddedSpace)
					{
						cleanTitle += ' ';
						alreadyAddedSpace = true;
					}
				}

				// For everything else, only add the letter
				else
				{
					cleanTitle += letter;
					alreadyAddedSpace = false;
				}
			}
			while (IsPunctuation(char.ToString(cleanTitle[^1])))
			{
				cleanTitle = cleanTitle[0..^1];
			}
			return cleanTitle;
		}

		/// <summary>
		/// Capitalizes just the first letter of the given text.
		/// </summary>
		/// <param name="input">the text to capitalize</param>
		/// <returns>the given text, but with the first letter capitalized</returns>
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
		/// Tells whether the given string is a word cap.
		/// </summary>
		/// <param name="text">the text to check</param>
		/// <returns>whether or not the given text caps words</returns>
		public static bool IsPunctuation(string text)
		{
			if (".?!,;:/-".Contains(text))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Strips the given asset code of the asset symbol.
		/// </summary>
		/// <param name="assetCode">the asset code to convert</param>
		/// <returns>just the name of the asset given</returns>
		public static string GetAssetName(string assetCode)
		{
			if (assetCode == "") return "";
			string assetCharacter = ValueFetching.GetConfigValue("text", "assetCharacter");
			if (assetCode[^1].ToString() != assetCharacter) return "";
			char[] letters = new char[assetCode.Length - 1];
			for (int i = 0; i < assetCode.Length - 1; i++)
			{
				letters[i] = assetCode[i];
			}

			return new string(letters);
		}

		/// <summary>
		/// Gets the amount of power this GainPower symbol gives.
		/// </summary>
		/// <param name="gainsActionCode">the action symbol code to check</param>
		/// <returns>either the amount of power the symbol gives, or nothing if it's not a proper GainPower symbol</returns>
		public static string GainPowerAmount(string gainsActionCode)
		{
			char[] letters = gainsActionCode.ToCharArray();
			if (letters.Length >= 10)
			{
				// GainXPower
				string gain = new(letters[0..4]);
				string power = new(letters[(letters.Length - 5)..letters.Length]);
				if (Equals(gain, "Gain") && Equals(power, "Power"))
				{
					return new string(letters[4..(letters.Length - 5)]);
				}
			}
			return "";
		}

		/// <summary>
		/// Checks if the given string is in the correct format for a hexcode color, including the "#".
		/// </summary>
		/// <param name="color">the string to check</param>
		/// <returns>whether or not the given string adheres to the required standards</returns>
		public static bool CorrectColorFormat(string color)
		{
			if (color == "") return false; // empty string
			if (color[0] != '#') return false; // needs the leading #
			color = color[1..^0];
			if (color.Length != 6) return false; // hexcode colors use 6 characters
			color = color.ToLower();
			foreach (char letter in color)
			{
				if (letter < 48 || (letter > 57 && letter < 97) || letter > 102) return false; // targetting the values for hexadecimal chars
			}
			return true;
		}
	}
}
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.VisualBasic.Logging;

namespace UniVil_Card_Generator.CardGeneration
{
	public static class Logging
	{
		public static List<string> GetCardsToSkip()
		{
			string allData = "";
			string logFilePath = Structuring.GetFullPath(Path.Combine("logs", "all-data.json"));

			if (!File.Exists(logFilePath)) 
			{
				var logFile = File.Create(logFilePath);
				logFile.Close();
			}
			
			string? line;
			try
			{
				// Pass the file path to the StreamReader constructor
				StreamReader sr = new(logFilePath);

				// Read the first line of text
				line = sr.ReadLine();

				//Continue to read until you reach end of file
				while (line != null)
				{
					allData += line;
					line = sr.ReadLine();
				}
				//close the file
				sr.Close();
			}
			catch(Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}

			try
			{
				bool getAllCards = false;
				ProjectData? deserialized = JsonSerializer.Deserialize<ProjectData>(allData);
				if (deserialized is null) return [];
				foreach (var settingCategory in deserialized.Settings)
				{
					foreach (var setting in settingCategory.Value)
					{
						if (ValueFetching.GetSettingsValue(settingCategory.Key, setting.Key) != setting.Value) getAllCards = true;
					}
				}

				foreach (var configCategory in deserialized.Config)
				{
					foreach (var configLabel in configCategory.Value)
					{
						if (ValueFetching.GetConfigValue(configCategory.Key, configLabel.Key) != configLabel.Value) getAllCards = true;
					}
				}

				foreach (var keyword in deserialized.Keywords)
				{
					Dictionary<string, string> keywordsAndColors = ValueFetching.GetColorMapping();
					if (keywordsAndColors[keyword.Key] != keyword.Value) getAllCards = true;
				}

				// Get current card data
				Dictionary<string, Card> cards = [];
				foreach (string card in ValueFetching.GetTextFilesLines("Cards"))
				{
					string[] cardSplit = card.Split("\t");
					if (cardSplit.Length == 11)
					{
						cards[TextManipulation.CleanTitle(cardSplit[0])] = new(
							cardSplit[0], // title
							cardSplit[1], // cost
							cardSplit[2], // strength
							$"{cardSplit[3]}@{cardSplit[4]}@{cardSplit[5]}@{cardSplit[10]}", // ability@activate ability@active cost@gains action
							cardSplit[6], // type
							cardSplit[7], // top right
							cardSplit[8], // bottom right
							cardSplit[9]  // deck
						)
						{
							hasImage = Structuring.ImageExists(TextManipulation.CleanTitle(cardSplit[0])) ? "true" : "false"
						};
					}
				}

				HashSet<string> cardsInLog = [];
				foreach (var card in deserialized.Cards)
				{
					cardsInLog.Add(card.Key);
					bool includeCard = false;
					string exportsPath = Structuring.GetFullPath(Path.Combine("Card Data", "-Exports"));
					string cardPath = Path.Combine(exportsPath, card.Key + Structuring.FindExtension(exportsPath, card.Key));
					if (File.Exists(cardPath))
					{
						foreach (var field in card.Value)
						{
							if (getAllCards) includeCard = true;
							else
							{
								switch (field.Key)
								{
									case "Title":
										if (cards.TryGetValue(card.Key, out var val0))
											if (val0.Title != field.Value) includeCard = true;
										break;
									case "Cost":
										if (cards.TryGetValue(card.Key, out var val1))
											if (val1.Cost != field.Value) includeCard = true;
										break;
									case "Strength":
										if (cards.TryGetValue(card.Key, out var val2))
											if (val2.Strength != field.Value) includeCard = true;
										break;
									case "Ability":
										if (cards.TryGetValue(card.Key, out var val3))
											if (val3.Ability != field.Value) includeCard = true;
										break;
									case "Type":
										if (cards.TryGetValue(card.Key, out var val4))
											if (val4.Type != field.Value) includeCard = true;
										break;
									case "TopRight":
										if (cards.TryGetValue(card.Key, out var val5))
											if (val5.TopRight != field.Value) includeCard = true;
										break;
									case "BottomRight":
										if (cards.TryGetValue(card.Key, out var val6))
											if (val6.BottomRight != field.Value) includeCard = true;
										break;
									case "Deck":
										if (cards.TryGetValue(card.Key, out var val7))
											if (val7.Deck != field.Value) includeCard = true;
										break;
									case "hasImage":
										if (cards.TryGetValue(card.Key, out var val8))
											if (val8.hasImage != field.Value) includeCard = true;
										break;
									default:
										break;
								}
							}
						}
					}
					else includeCard = true;
					if (includeCard) cards.Remove(card.Key);
				}
				List<string> cardsToSkip = [];
				foreach (var card in cards)
				{
					// If the card is not in the log, then we do not skip it
					if (cardsInLog.Contains(card.Key)) cardsToSkip.Add(card.Key);
				}
				return cardsToSkip;
			}
			catch (JsonException)
			{
				return [];
			}
		}

		/// <summary>
		/// Writes every setting, config value, keyword variation, and card to the log file.
		/// </summary>
		public static void LogThisVersion()
		{
			// First find and delete the current log file
			string logFilePath = Structuring.GetFullPath(Path.Combine("logs", "all-data.json"));
			if (File.Exists(logFilePath)) ClearLogFile();

			// Use StreamWriter to write to the log file
			using StreamWriter sw = File.CreateText(logFilePath);

			sw.WriteLine("{");
			// Settings data
			sw.WriteLine("\"Settings\": {");
			sw.WriteLine(GetSettingsData());
			sw.WriteLine("},");

			// Config data
			sw.WriteLine("\"Config\": {");
			sw.WriteLine(GetConfigData());
			sw.WriteLine("},");

			// Keyword data
			sw.WriteLine("\"Keywords\": ");
			sw.WriteLine(GetKeywordData());
			sw.WriteLine(",");

			// Card data
			sw.WriteLine("\"Cards\": {");
			sw.WriteLine(GetCardData());
			sw.WriteLine("}");
			sw.WriteLine("}");

			sw.Close();
		}

		private static void ClearLogFile()
		{
			string logFilePath = Structuring.GetFullPath(Path.Combine("logs", "all-data.json"));
			string? line;
			try
			{
				File.WriteAllText(logFilePath, string.Empty);
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
			}
		}

		private static string GetSettingsData()
		{
			Dictionary<string, string> cardSettings = ValueFetching.GetAllSettings("Card");
			Dictionary<string, string> dataSettings = ValueFetching.GetAllSettings("Data");
			Dictionary<string, string> textSettings = ValueFetching.GetAllSettings("Text");
			return "\"Card\": " + JsonSerializer.Serialize(cardSettings) + "," + 
				   "\"Data\": " + JsonSerializer.Serialize(dataSettings) + "," + 
				   "\"Text\": " + JsonSerializer.Serialize(textSettings);
		}

		private static string GetConfigData()
		{
			Dictionary<string, string> assetConfig = ValueFetching.GetAllConfigValues("asset");
			Dictionary<string, string> cardConfig = ValueFetching.GetAllConfigValues("card");
			Dictionary<string, string> colorConfig = ValueFetching.GetAllConfigValues("color");
			Dictionary<string, string> layoutConfig = ValueFetching.GetAllConfigValues("layout");
			Dictionary<string, string> settingsConfig = ValueFetching.GetAllConfigValues("settings");
			Dictionary<string, string> textConfig = ValueFetching.GetAllConfigValues("text");
			return "\"asset\": " + JsonSerializer.Serialize(assetConfig) + "," + 
				   "\"card\": " + JsonSerializer.Serialize(cardConfig) + "," + 
				   "\"color\": " + JsonSerializer.Serialize(colorConfig) + "," + 
				   "\"layout\": " + JsonSerializer.Serialize(layoutConfig) + "," + 
				   "\"settings\": " + JsonSerializer.Serialize(settingsConfig) + "," + 
				   "\"text\": " + JsonSerializer.Serialize(textConfig);
		}

		private static string GetKeywordData()
		{
			Dictionary<string, string> keywordsAndColors = ValueFetching.GetColorMapping();
			return JsonSerializer.Serialize(keywordsAndColors);
		}

		private static string GetCardData()
		{
			string allCardData = "";
			List<string> cards = ValueFetching.GetTextFilesLines("Cards");
			foreach (string card in cards)
			{
				string[] cardData = card.Split('\t');
				if (cardData.Length == 11)
				{
					Card cardObject = new(
						cardData[0], // title
						cardData[1], // cost
						cardData[2], // strength
						$"{cardData[3]}@{cardData[4]}@{cardData[5]}@{cardData[10]}", // ability@activate ability@active cost@gains action
						cardData[6], // type
						cardData[7], // top right
						cardData[8], // bottom right
						cardData[9]  // deck
					)
					{
						hasImage = Structuring.ImageExists(TextManipulation.CleanTitle(cardData[0])) ? "true" : "false"
					};

					allCardData += $"\"{TextManipulation.CleanTitle(cardData[0])}\": {JsonSerializer.Serialize(cardObject)},";
				}
			}
			if (allCardData == "") return "";
			return allCardData[0..^1];
		}
	}

	class ProjectData
	{
		public Dictionary<string, Dictionary<string, string>> Settings { get; set; }
		public Dictionary<string, Dictionary<string, string>> Config { get; set; }
		public Dictionary<string, string> Keywords { get; set; }
		public Dictionary<string, Dictionary<string, string>> Cards { get; set; }
	}

	class Card(string title, string cost, string strength, string ability, string type, string topRight, string bottomRight, string deck)
	{
		public string Title { get; } = title;
		public string Cost { get; } = cost;
		public string Strength { get; } = strength;
		public string Ability { get; } = ability;
		public string Type { get; } = type;
		public string TopRight { get; } = topRight;
		public string BottomRight { get; } = bottomRight;
		public string Deck { get; } = deck;
		[JsonInclude]
		public string hasImage = "false";
	}
}
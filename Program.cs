using ToonVil_Card_Generator.CardGeneration;
using static ToonVil_Card_Generator.CardGeneration.PrepareText;
using static ToonVil_Card_Generator.CardGeneration.PrepareImage;
using System.Drawing;
using System.Reflection;

namespace ToonVil_Card_Generator;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ToonVil Card Generator");

        if (!MiscHelper.CheckStructure())
        {
            var test = Console.ReadLine();
            return;
        }
        
        // Setup variables
        Color textColor = ColorTranslator.FromHtml("#" + ConfigHelper.GetConfigValue("color", "fontColor"));
        int titleAreaMaxWidth = SettingsHelper.GetSettingsValue("Card", "titleMaxWidth") == "" ?
            int.Parse(ConfigHelper.GetConfigValue("card", "textAreaMaxWidth")) :
            int.Parse(SettingsHelper.GetSettingsValue("Card", "titleMaxWidth"));
        int titleAreaMaxHeight = int.Parse(ConfigHelper.GetConfigValue("card", "titleAreaMaxHeight"));
        int abilityAreaMaxWidth = int.Parse(ConfigHelper.GetConfigValue("card", "textAreaMaxWidth"));
        int abilityAreaMaxHeight = int.Parse(ConfigHelper.GetConfigValue("card", "abilityAreaMaxHeight"));
        int typeAreaMaxWidth = SettingsHelper.GetSettingsValue("Card", "typeMaxWidth") == "" ?
            int.Parse(ConfigHelper.GetConfigValue("card", "typeAreaMaxWidth")) :
            int.Parse(SettingsHelper.GetSettingsValue("Card", "typeMaxWidth"));
        int typeAreaMaxHeight = int.Parse(ConfigHelper.GetConfigValue("card", "typeAreaMaxHeight"));
        int cornerElementMaxWidth = int.Parse(ConfigHelper.GetConfigValue("card", "cornerElementMaxWidth"));
        int cornerElementMaxHeight = int.Parse(ConfigHelper.GetConfigValue("card", "cornerElementMaxHeight"));

        // Load all the fonts
        string titleFontFile = ConfigHelper.GetConfigValue("text", "titleFont");
        int titleFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "titleFontSize"));
        string abilityFontFile = ConfigHelper.GetConfigValue("text", "abilityFont");
        int abilityFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "abilityFontSize"));
        string elementFontFile = ConfigHelper.GetConfigValue("text", "elementFont");
        int costFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "costFontSize"));
        int strengthFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "strengthFontSize"));
        int topRightFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "topRightFontSize"));
        int bottomRightFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "bottomRightFontSize"));
        string typeFontFile = ConfigHelper.GetConfigValue("text", "typeFont");
        int typeFontSize = int.Parse(ConfigHelper.GetConfigValue("text", "typeFontSize"));
        Font titleFont = FontLoader.GetFont(titleFontFile, titleFontSize);
		Font abilityFont = FontLoader.GetFont(abilityFontFile, abilityFontSize);
        Font typeFont = FontLoader.GetFont(typeFontFile, typeFontSize);
		Font costFont = FontLoader.GetFont(elementFontFile, costFontSize);
        Font strengthFont = FontLoader.GetFont(elementFontFile, strengthFontSize);
        Font topRightFont = FontLoader.GetFont(elementFontFile, topRightFontSize);
        Font bottomRightFont = FontLoader.GetFont(elementFontFile, bottomRightFontSize);

        bool repeat = true;
        while (repeat)
        {
            // Initial cleanup
            CleanIntermediaries();

            repeat = false;

            // Load all the keywords and their colors
            Dictionary<string, string> keywordsAndColors = KeywordHelper.GetColorMapping();

            // Go through each line of Cards.txt and build cards
            List<string> cardData = MiscHelper.GetTextFilesLines("Cards");
            foreach (string card in cardData)
            {
                // Title, Cost, Strength, On Play Ability, Activate Ability, Activate Cost Text, Type, Top Right, Bottom Right, Deck Name, Gains Action Symbol
                string[] cardSplit = card.Split("\t");
                if (cardSplit.Length == 11)
                {
                    string title = cardSplit[0];
                    string cost = cardSplit[1];
                    string strength = cardSplit[2];
                    string ability = cardSplit[3];
                    string activateAbility = cardSplit[4];
                    string activateCost = cardSplit[5];
                    string type = cardSplit[6];
                    string topRight = cardSplit[7];
                    string bottomRight = cardSplit[8];
                    string deck = cardSplit[9];
                    string gainsAction = cardSplit[10];

                    // Skip cards that do not have all the necessary elements
                    if (title != "" && (ability != "" || activateAbility != "" || activateCost != "" || gainsAction != "") && type != "")
                    {
                        DrawTitle(title, titleFont, textColor, titleAreaMaxWidth, titleAreaMaxHeight);
                        DrawAbility(ability, activateAbility, activateCost, gainsAction, abilityFont, textColor, abilityAreaMaxWidth, abilityAreaMaxHeight, keywordsAndColors);
                        DrawType(type, typeFont, textColor, typeAreaMaxWidth, typeAreaMaxHeight, keywordsAndColors);
                        if (cost != "") DrawCornerElement(cost, costFont, textColor, "Cost", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (strength != "") DrawCornerElement(strength, strengthFont, textColor, "Strength", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (topRight != "") DrawCornerElement(topRight, topRightFont, textColor, "TopRight", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (bottomRight != "") DrawCornerElement(bottomRight, bottomRightFont, textColor, "BottomRight", cornerElementMaxWidth, cornerElementMaxHeight);

                        while (MiscHelper.IsPunctuation(char.ToString(title[^1])))
                        {
                            title = title[0..^1];
                        }
                        SizeCardImage(title);
                        CombineImages(title, deck);
                        CleanIntermediaries();
                    }
                }
            }

            // Final cleanup
            CleanIntermediaries();

            Console.WriteLine("Card generation done. Press ENTER to repeat, close this window to exit.");
            string? input = Console.ReadLine();
            repeat = true;
        }
    }
}

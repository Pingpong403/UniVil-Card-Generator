using UniVil_Card_Generator.CardGeneration;
using static UniVil_Card_Generator.CardGeneration.PrepareText;
using static UniVil_Card_Generator.CardGeneration.PrepareImage;
using System.Drawing;
using System.Reflection;

namespace UniVil_Card_Generator;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("UniVil Card Generator");

        if (!Structuring.CheckStructure())
        {
            var test = Console.ReadLine();
            return;
        }
        
        // Setup variables
        string customColor = ValueFetching.GetSettingsValue("Text", "textColor");
        bool useCustomColor = TextManipulation.CorrectColorFormat(customColor);
        Color textColor = useCustomColor ? 
            ColorTranslator.FromHtml(customColor) :
            ColorTranslator.FromHtml("#" + ValueFetching.GetConfigValue("color", "fontColor"));
		Color fateTextColor = ColorTranslator.FromHtml("#000000");
        int titleAreaMaxWidth = ValueFetching.GetSettingsValue("Card", "titleMaxWidth") == "" ?
            int.Parse(ValueFetching.GetConfigValue("card", "textAreaMaxWidth")) :
            int.Parse(ValueFetching.GetSettingsValue("Card", "titleMaxWidth"));
        int titleAreaMaxHeight = int.Parse(ValueFetching.GetConfigValue("card", "titleAreaMaxHeight"));
        int abilityAreaMaxWidth = int.Parse(ValueFetching.GetConfigValue("card", "textAreaMaxWidth"));
        int abilityAreaMaxHeight = int.Parse(ValueFetching.GetConfigValue("card", "abilityAreaMaxHeight"));
        int typeAreaMaxWidth = ValueFetching.GetSettingsValue("Card", "typeMaxWidth") == "" ?
            int.Parse(ValueFetching.GetConfigValue("card", "typeAreaMaxWidth")) :
            int.Parse(ValueFetching.GetSettingsValue("Card", "typeMaxWidth"));
        int typeAreaMaxHeight = int.Parse(ValueFetching.GetConfigValue("card", "typeAreaMaxHeight"));
        int cornerElementMaxWidth = int.Parse(ValueFetching.GetConfigValue("card", "cornerElementMaxWidth"));
        int cornerElementMaxHeight = int.Parse(ValueFetching.GetConfigValue("card", "cornerElementMaxHeight"));

        // Load all the fonts
		FontLoader.LoadAltFont();
        string titleFontFile = ValueFetching.GetConfigValue("text", "titleFont");
        int titleFontSize = int.Parse(ValueFetching.GetConfigValue("text", "titleFontSize"));
        string abilityFontFile = ValueFetching.GetConfigValue("text", "abilityFont");
        int abilityFontSize = int.Parse(ValueFetching.GetConfigValue("text", "abilityFontSize"));
        string elementFontFile = ValueFetching.GetConfigValue("text", "elementFont");
        int costFontSize = int.Parse(ValueFetching.GetConfigValue("text", "costFontSize"));
        int strengthFontSize = int.Parse(ValueFetching.GetConfigValue("text", "strengthFontSize"));
        int topRightFontSize = int.Parse(ValueFetching.GetConfigValue("text", "topRightFontSize"));
        int bottomRightFontSize = int.Parse(ValueFetching.GetConfigValue("text", "bottomRightFontSize"));
        string typeFontFile = ValueFetching.GetConfigValue("text", "typeFont");
        int typeFontSize = int.Parse(ValueFetching.GetConfigValue("text", "typeFontSize"));
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
            // If the user has indicated, get all the cards we need to re-generate
            bool trackChanges = ValueFetching.GetSettingsValue("Data", "trackChanges") == "true";
            List<string> cardsToSkip = [];
            if (trackChanges) cardsToSkip = Logging.GetCardsToSkip();
            else File.Delete(Structuring.GetFullPath(Path.Combine("logs", "all-data.json")));

            // Initial cleanup
            CleanIntermediaries();

            repeat = false;

            // Load all the keywords and their colors
            Dictionary<string, string> keywordsAndColors = ValueFetching.GetColorMapping();

            // Go through each line of Cards.txt and build cards
            List<string> cardData = ValueFetching.GetTextFilesLines("Cards");
			if (cardData.Count == 0) Console.WriteLine("No cards generated.");

            int badCards = 0;
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

                    bool skipCard = trackChanges && cardsToSkip.Contains(TextManipulation.CleanTitle(title));
                    // Skip cards that do not have all the necessary elements
                    if (!skipCard && title != "" && (ability != "" || activateAbility != "" || activateCost != "" || gainsAction != "") && type != "")
                    {
                        DrawTitle(title, titleFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, titleAreaMaxWidth, titleAreaMaxHeight);
                        DrawAbility(ability, activateAbility, activateCost, gainsAction, abilityFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, abilityAreaMaxWidth, abilityAreaMaxHeight, keywordsAndColors);
                        DrawType(type, typeFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, typeAreaMaxWidth, typeAreaMaxHeight, keywordsAndColors);
                        if (cost != "") DrawCornerElement(cost, costFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, "Cost", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (strength != "") DrawCornerElement(strength, strengthFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, "Strength", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (topRight != "") DrawCornerElement(topRight, topRightFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, "TopRight", cornerElementMaxWidth, cornerElementMaxHeight);
                        if (bottomRight != "") DrawCornerElement(bottomRight, bottomRightFont, deck == "Fate" || deck == "MonsterRally" ? fateTextColor : textColor, "BottomRight", cornerElementMaxWidth, cornerElementMaxHeight);

                        SizeCardImage(TextManipulation.CleanTitle(title));
                        CombineImages(TextManipulation.CleanTitle(title), deck);
                        CleanIntermediaries();
                    }
                    else if (title == "" ||
                            (ability == "" && activateAbility == "" && activateCost == "" && gainsAction == "") ||
                             type == "" ) badCards += 1;
                }
                else badCards += 1;
            }
            if (badCards > 0) Console.WriteLine($"{badCards} card{(badCards > 1 ? "s" : "")} skipped due to improper formatting or missing fields.");

            // Final cleanup
            CleanIntermediaries();

            // Record the current state of the data
            if (trackChanges) Logging.LogThisVersion();

            Console.WriteLine("Card generation done. Press ENTER to repeat, close this window to exit.");
            string? input = Console.ReadLine();
            repeat = true;
        }
    }
}
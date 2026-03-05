using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections;

namespace UniVil_Card_Generator.CardGeneration
{
	public static class PrepareImage
	{
		/// <summary>
		/// Produce a properly sized card image, ready to be inserted onto card, from any size
		/// </summary>
		/// <param name="imageName">The name of the image file, not including extension</param>
		/// <meta>Original code from https://www.c-sharpcorner.com/UploadFile/ishbandhu2009/resize-an-image-in-C-Sharp/</meta>
		public static void SizeCardImage(string imageName)
		{
			// Get image into an object
			var dir = Path.Combine("Card Data", "-Images");
			var relativePath = Path.Combine(dir, imageName) + Structuring.FindExtension(dir, imageName);
            var fullPath = Structuring.GetFullPath(relativePath);
			// If no image is found, continue on with black background
			if (!File.Exists(fullPath))
			{
				relativePath = Path.Combine("assets", "black_bg.png");
				fullPath = Structuring.GetFullPath(relativePath);
			}
			bool makeGrayscale = ValueFetching.GetSettingsValue("Card", "convertToGrayscale") == "true";
			using Bitmap img = makeGrayscale ? ImageManipulation.MakeGrayscale((Bitmap)Image.FromFile(fullPath)) : (Bitmap)Image.FromFile(fullPath);

			float targetHeight = float.Parse(ValueFetching.GetConfigValue("card", "imageAreaHeight"), CultureInfo.InvariantCulture);
			float ratio = targetHeight / img.Height;
			int newWidth = Math.Max(1, (int)Math.Round(ratio * img.Width));
			int newHeight = Math.Max(1, (int)Math.Round(ratio * img.Height));

			using Bitmap b = new(newWidth, newHeight);
			using Graphics g = Graphics.FromImage(b);
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.CompositingQuality = CompositingQuality.HighQuality;

			// Draw image with new width and height
			g.DrawImage(img, 0, 0, newWidth, newHeight);

			// Ensure output directory exists and save resized PNG
			var relativeOutDir = Path.Combine("temp", "ImageIntermediary");
            var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, $"{imageName}.png");
			b.Save(outpath, ImageFormat.Png);
		}

		/// <summary>
		/// Puts together all the images in the intermediary directories
		/// and saves the result in -Exports.
		/// </summary>
		/// <param name="cardTitle">The title of the card to be combined</param>
		/// <param name="deck">What deck the card belongs to (Villain, Fate, etc.)</param>
		public static void CombineImages(string cardTitle, string deck)
		{
			string capitalizedDeck = TextManipulation.Capitalize(deck.ToLower());

			var imageIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "ImageIntermediary"));
			var textIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "TextIntermediary"));
			var layoutPath = Structuring.GetFullPath(Path.Combine("Card Data", "-Layout"));
			var assetsPath = Structuring.GetFullPath("assets");

			// All possible elements: image, Title, Ability, Type,
			// Cost, Strength, TopRight, BottomRight
			var imagePath = Path.Combine(imageIntermediaryPath, cardTitle + ".png");
			var altImagePath = Path.Combine(assetsPath, "black_bg.png");
			var titlePath = Path.Combine(textIntermediaryPath, "Title.png");
			var abilityPath = Path.Combine(textIntermediaryPath, "Ability.png");
			var typePath = Path.Combine(textIntermediaryPath, "Type.png");
			var costPath = Path.Combine(textIntermediaryPath, "Cost.png");
			var strengthPath = Path.Combine(textIntermediaryPath, "Strength.png");
			var topRightElementPath = Path.Combine(textIntermediaryPath, "TopRight.png");
			var bottomRightElementPath = Path.Combine(textIntermediaryPath, "BottomRight.png");

			int cardWidth = int.Parse(ValueFetching.GetConfigValue("card", "w"));
			int cardHeight = int.Parse(ValueFetching.GetConfigValue("card", "h"));
			using Bitmap b = new(cardWidth, cardHeight);
			using Graphics g = Graphics.FromImage(b);
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.SmoothingMode = SmoothingMode.HighQuality;
			g.CompositingQuality = CompositingQuality.HighQuality;

			// Only draw any elements if all of the necessary elements are present
			// Necessary elements: Title, Ability, Type
			if (!(File.Exists(titlePath) && File.Exists(abilityPath) && File.Exists(typePath)))
			{
				Console.WriteLine($"Missing Title, Ability, or Type field(s) for {cardTitle}!");
				return;
			}

			// Card image
			bool imageExists = File.Exists(imagePath);
			using (Image cardImg = Image.FromFile(imageExists ? imagePath : altImagePath))
			{
				// get image width
				int imgW = cardImg.Width;
				// set xOffset to appropriate value
				int xOffset = (cardWidth - imgW) / 2;
				g.DrawImage(cardImg, xOffset, 0);
			}
			
			// Deck background
			if (!Structuring.ElementExists(capitalizedDeck, "Deck"))
			{
				Console.WriteLine($"Missing {capitalizedDeck}Deck.png for {cardTitle}.");
				return;
			}
			using (Image bgImg = Image.FromFile(Path.Combine(layoutPath, capitalizedDeck + "Deck.png")))
			{
				g.DrawImage(bgImg, new Rectangle(0, 0, cardWidth, cardHeight));
			}

			// Title
			using (Image titleImg = Image.FromFile(titlePath))
			{
				Point titleCenter = ValueFetching.GetElementPos("title");
				g.DrawImage(titleImg, titleCenter.X - titleImg.Width / 2, titleCenter.Y - titleImg.Height / 2);
			}
			
			// Ability
			using (Image abilityImg = Image.FromFile(abilityPath))
			{
				int abilityCenterX = int.Parse(ValueFetching.GetConfigValue("layout", "abilityCenterX"));
				int abilityTopY = int.Parse(ValueFetching.GetConfigValue("layout", "abilityTopY"));
				int abilityBottomPadding = int.Parse(ValueFetching.GetConfigValue("card", "abilityBottomPadding"));
				g.DrawImage(abilityImg, abilityCenterX - abilityImg.Width / 2, abilityTopY);
			}

			// Type
			using (Image typeImg = Image.FromFile(typePath))
			{
				Point typeCenter = ValueFetching.GetElementPos("type");
				g.DrawImage(typeImg, typeCenter.X - typeImg.Width / 2, typeCenter.Y - typeImg.Height / 2);
			}
			
			// Cost
			if (File.Exists(costPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "Cost"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}Cost.png for {cardTitle}.");
					return;
				}
				g.DrawImage(Image.FromFile(Path.Combine(layoutPath, capitalizedDeck + "Cost.png")), new Rectangle(0, 0, cardWidth, cardHeight));
				using (Image costImg = Image.FromFile(costPath))
				{
					Point costCenter = ValueFetching.GetElementPos("cost");
					g.DrawImage(costImg, costCenter.X - costImg.Width / 2, costCenter.Y - costImg.Height / 2);
				}
			}
			
			// Strength
			if (File.Exists(strengthPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "Strength"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}Strength.png for {cardTitle}.");
					return;
				}
				g.DrawImage(Image.FromFile(Path.Combine(layoutPath, capitalizedDeck + "Strength.png")), new Rectangle(0, 0, cardWidth, cardHeight));
				using (Image strengthImg = Image.FromFile(strengthPath))
				{
					Point strengthCenter = ValueFetching.GetElementPos("strength");
					g.DrawImage(strengthImg, strengthCenter.X - strengthImg.Width / 2, strengthCenter.Y - strengthImg.Height / 2);
				}
			}

			// Top Right Element
			if (File.Exists(topRightElementPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "TopRight"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}TopRight.png for {cardTitle}.");
					return;
				}
				g.DrawImage(Image.FromFile(Path.Combine(layoutPath, capitalizedDeck + "TopRight.png")), new Rectangle(0, 0, cardWidth, cardHeight));
				using (Image topRightImg = Image.FromFile(topRightElementPath))
				{
					Point topRightCenter = ValueFetching.GetElementPos("topRight");
					g.DrawImage(topRightImg, topRightCenter.X - topRightImg.Width / 2, topRightCenter.Y - topRightImg.Height / 2);
				}
			}

			// Bottom Right Element
			if (File.Exists(bottomRightElementPath))
			{
				if (!Structuring.ElementExists(capitalizedDeck, "BottomRight"))
				{
					Console.WriteLine($"Missing {capitalizedDeck}BottomRight.png for {cardTitle}.");
					return;
				}
				g.DrawImage(Image.FromFile(Path.Combine(layoutPath, capitalizedDeck + "BottomRight.png")), new Rectangle(0, 0, cardWidth, cardHeight));
				using (Image bottomRightImg = Image.FromFile(bottomRightElementPath))
				{
					Point bottomRightCenter = ValueFetching.GetElementPos("bottomRight");
					g.DrawImage(bottomRightImg, bottomRightCenter.X - bottomRightImg.Width / 2, bottomRightCenter.Y - bottomRightImg.Height / 2);
				}
			}

			// Ensure output directory exists and save completed card
			bool saveByDeck = ValueFetching.GetSettingsValue("Data", "exportByDeck") == "true";
			var relativeOutDir = Path.Combine("Card Data", "-Exports", saveByDeck ? deck : "");
			var outDir = Structuring.GetFullPath(relativeOutDir);
			Directory.CreateDirectory(outDir);
			var outpath = Path.Combine(outDir, $"{cardTitle}.png");
			b.Save(outpath, ImageFormat.Png);
			Console.WriteLine($"Image saved: {cardTitle}");
		}

		/// <summary>
		/// Deletes all files in each intermediary directory.
		/// </summary>
		public static void CleanIntermediaries()
		{
			var imageIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "ImageIntermediary"));
			var textIntermediaryPath = Structuring.GetFullPath(Path.Combine("temp", "TextIntermediary"));

			var imageIntermediaryDI = new DirectoryInfo(imageIntermediaryPath);
			foreach (FileInfo fi in imageIntermediaryDI.EnumerateFiles())
			{
				if (fi.Extension == ".png") fi.Delete();
			}
			var textIntermediaryDI = new DirectoryInfo(textIntermediaryPath);
			foreach (FileInfo fi in textIntermediaryDI.EnumerateFiles())
			{
				if (fi.Extension == ".png") fi.Delete();
			}
		}
	}
}
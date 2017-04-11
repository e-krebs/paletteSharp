using System;
using System.Drawing;
using System.Linq;
using PaletteSharp;
using PaletteSharp.Helpers;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || !args.Any())
            {
                Console.WriteLine("please provide a path to an image");
                Console.ReadLine();
                return;
            }
            string imagePath = args[0];
            Bitmap bitmap = new Bitmap(Image.FromFile(imagePath));

            Palette palette = Palette.From(bitmap).Generate();
            palette.Generate();

            Console.WriteLine("swatches:");
            PrintSwatch("Dominant", palette.GetDominantSwatch());
            Console.WriteLine();
            PrintSwatch("Vibrant", palette.GetVibrantSwatch());
            PrintSwatch("DarkVibrant", palette.GetDarkVibrantSwatch());
            PrintSwatch("Muted", palette.GetMutedSwatch());
            PrintSwatch("DarkMuted", palette.GetDarkMutedSwatch());
            PrintSwatch("LightVibrant", palette.GetLightVibrantSwatch());
            PrintSwatch("LightMuted", palette.GetLightMutedSwatch());

            Console.ReadLine();
        }

        private static void PrintSwatch(string name, Swatch swatch)
        {
            Color? color = swatch?.GetArgb();
            if (!color.HasValue)
            {
                Console.Write($"{name} has ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("no");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" swatch\n");
            }
            else
            {
                Console.WriteLine($"{name}: {GetRgbHex(color)} - population: {swatch.GetPopulation()}");
                Console.WriteLine($"\ttext colors - title: {GetRgbHex(swatch.GetTitleTextColor())} - body: {GetRgbHex(swatch.GetBodyTextColor())}");
            }
        }

        private static string GetRgbHex(Color? color) => color?.ToHexString(false) ?? string.Empty;
    }
}

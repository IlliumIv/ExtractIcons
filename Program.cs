using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ExtractIcons
{

    class Program
    {
        private static readonly HashSet<string> _atlasFiles = new HashSet<string>();
        private static readonly Regex @catch = new Regex(@"""(.*?)"" ""(.*?)"" (\d*) (\d*) (\d*) (\d*)");
        private static readonly HashSet<string> _texturesPaths = new HashSet<string>();
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory());

                foreach (var file in files)
                {
                    if (File.Exists(file) && file.EndsWith(".txt"))
                    {
                        _atlasFiles.Add(file);
                        continue;
                    }

                    if (File.Exists(file) && new FileInfo(file).Extension == ".dds") _texturesPaths.Add(file);
                }
            }

            foreach (var arg in args)
            {
                if (arg.Length == 2)
                {
                    switch (ConvertStringToEnum(arg[1..]))
                    {
                        case Argument.s:
                            var path = args.Length > Array.IndexOf(args, arg) + 1 ? null : arg;
                            if (path != null && File.Exists(path)) _atlasFiles.Add(path);
                            
                            break;
                        case Argument.h:
                            ShowHelp();
                            
                            return;
                        default:
                            if (File.Exists(arg) && new FileInfo(arg).Extension == ".dds") _texturesPaths.Add(arg);

                            break;
                    }
                }
            }

            if (_texturesPaths.Count > 0) Process_Textures();
        }

        private static void Process_Textures()
        {
            foreach (var _atlas in _atlasFiles)
            {
                var atlasReader = new StreamReader(_atlas, encoding: System.Text.Encoding.Unicode);
                var pathsToFiles = new HashSet<FileInfo>();

                foreach (var filePath in _texturesPaths)
                {
                    pathsToFiles.Add(new FileInfo(filePath));
                }

                string line;

                while ((line = atlasReader.ReadLine()) != null)
                {
                    if (pathsToFiles.Any(x => line.Contains(x.Name)))
                    {
                        CropAndSave(pathsToFiles.First(x => line.Contains(x.Name)), line);
                    }
                }
            }
        }

        private static void CropAndSave(FileInfo pathToFile, string line)
        {
            var matches = @catch.Matches(line).ToHashSet();
            var match = matches.First();

            var textureName = new FileInfo(match.Groups[1].Value).Name;
            // var textureFile = new FileInfo(match.Groups[2].Value).Name;
            var textureX = Int32.Parse(match.Groups[3].Value);
            var textureY = Int32.Parse(match.Groups[4].Value);
            var textureW = Int32.Parse(match.Groups[5].Value) - textureX;
            var textureH = Int32.Parse(match.Groups[6].Value) - textureY;

            var cropRect = new Rectangle(textureX, textureY, textureW, textureH);
            DDSImage img = new DDSImage(pathToFile.FullName);
            img.Save($"{Path.Combine(Directory.GetCurrentDirectory(), $"{textureName}.png")}", cropRect);

            Console.WriteLine($"{pathToFile.FullName}\n{textureName} {textureX} {textureY} {textureW} {textureH}\n");
        }

        private static void ShowHelp()
        {
            Console.WriteLine($"Usage: {nameof(ExtractIcons)} [-s] texture\n");

            foreach (var arg in (Argument[])Enum.GetValues(typeof(Argument)))
            {
                switch (arg)
                {
                    case Argument.s:
                        Console.WriteLine($"  -{ConvertEnumToString(arg)}\tSet path to .txt file with images coordinates. " +
                            $"By default use all .txt files in {Directory.GetCurrentDirectory()}");
                        break;
                    case Argument.h:
                        Console.WriteLine($"  -{ConvertEnumToString(arg)}\tShow this message and exit.");
                        break;
                    default:
                        break;
                }
            }
        }

        public enum Argument
        {
            s,
            h,
        }

        public static string ConvertEnumToString(Argument argument)
        {
            return argument.ToString();
        }

        public static Argument ConvertStringToEnum(string argument)
        {
            return (Argument)Enum.Parse(typeof(Argument), argument.ToLower());
        }
    }
}

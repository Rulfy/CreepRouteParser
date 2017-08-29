// <copyright file="Program.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace CreepRouteParser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    using PlaySharp.Toolkit.Helper;

    using SharpDX;

    public class Program
    {
        private static readonly Regex EndRegex = new Regex(@"-{4}([0-9]+)-{4}");

        private static readonly Regex NameRegex = new Regex(@"\B Name.+ \|(.+)");

        private static readonly Regex NextRegex = new Regex(@"(\[PR#].+)\(type.+");

        private static readonly Regex PositionRegex = new Regex(@"\B Position.+ \|(.+)");

        private static readonly Regex StartRegex = new Regex(@"={4}([0-9]+)={4}");

        // Creep route paths: common\dota 2 beta\game\dota\maps\dota.vpk -> maps/dota/entities/default_ents_vents_c
        private static string[] fileContent;

        private static string path;

        public static void ExtractAndSave(string filename, string routeName)
        {
            var route = ExtractRoute(routeName);

            filename = Path.Combine(path, filename);
            filename = Path.ChangeExtension(filename, "json");

            JsonFactory.ToFile(filename, route);
        }

        public static string ExtractMatch(Regex regex, int index)
        {
            var match = regex.Match(fileContent[index]);
            return match.Groups[match.Groups.Count - 1].Value.Trim();
        }

        public static List<Vector3> ExtractRoute(string name)
        {
            var result = new List<Vector3>();
            FindEntry(result, name);
            return result;
        }

        public static void FindEntry(List<Vector3> result, string name, int lastIndex = 0)
        {
            for (var i = lastIndex; i < fileContent.Length;)
            {
                var startIndex = Match(StartRegex, i);
                if (startIndex == -1)
                {
                    return;
                }

                var endIndex = Match(EndRegex, startIndex);
                if (endIndex == -1)
                {
                    return;
                }

                var nameIndex = Match(NameRegex, startIndex, endIndex);
                if (nameIndex != -1)
                {
                    var matchName = ExtractMatch(NameRegex, nameIndex);
                    if (matchName == name)
                    {
                        var positionIndex = Match(PositionRegex, startIndex, endIndex);
                        if (positionIndex == -1)
                        {
                            return;
                        }

                        var nextIndex = Match(NextRegex, startIndex, endIndex);
                        if (nextIndex == -1)
                        {
                            return;
                        }

                        var pos = ExtractMatch(PositionRegex, positionIndex);
                        result.Add(StringToVector3(pos));

                        // next entry
                        var oldName = name;
                        name = ExtractMatch(NextRegex, nextIndex);
                        Console.WriteLine($"Found {oldName} => {name}");
                        if (oldName == name)
                        {
                            return;
                        }

                        FindEntry(result, name, 0);
                        return;
                    }
                }

                i = endIndex + 1;
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("call with <file>!");
                return;
            }

            // var filename = @"C:\Temp\dotamapent.txt";
            var filename = args[0];

            fileContent = File.ReadAllLines(filename);
            path = Path.GetDirectoryName(filename);

            // radiant routes
            ExtractAndSave("RadiantTopRoute", "[PR#]lane_top_goodguys_melee_spawner");
            ExtractAndSave("RadiantMiddleRoute", "[PR#]lane_mid_goodguys_melee_spawner");
            ExtractAndSave("RadiantBottomRoute", "[PR#]lane_bot_goodguys_melee_spawner");

            // dire routes
            ExtractAndSave("DireTopRoute", "[PR#]lane_top_badguys_melee_spawner");
            ExtractAndSave("DireMiddleRoute", "[PR#]lane_mid_badguys_melee_spawner");
            ExtractAndSave("DireBottomRoute", "[PR#]lane_bot_badguys_melee_spawner");
        }

        public static int Match(Regex regex, int startIndex = 0, int lastIndex = -1)
        {
            if (lastIndex == -1)
            {
                lastIndex = fileContent.Length;
            }

            for (var i = startIndex; i < lastIndex; ++i)
            {
                var line = fileContent[i];
                if (regex.Match(line).Success)
                {
                    return i;
                }
            }

            return -1;
        }

        public static Vector3 StringToVector3(string stringVector)
        {
            var arr = stringVector.Split(' ');
            return new Vector3(
                float.Parse(arr[0], CultureInfo.InvariantCulture),
                float.Parse(arr[1], CultureInfo.InvariantCulture),
                float.Parse(arr[2], CultureInfo.InvariantCulture));
        }
    }
}
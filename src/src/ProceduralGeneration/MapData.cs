using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clawbyrinth.ProceduralGeneration
{
    /// <summary>
    /// Represents map data including grid, trap layer, and metadata
    /// </summary>
    public class MapData
    {
        public string FileName { get; set; }
        public List<List<char>> Grid { get; set; }
        public List<List<char>> TrapLayer { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public (int x, int y)? StartPos { get; set; }
        public (int x, int y)? FinishPos { get; set; }
        public List<string> Directions { get; set; }
        public List<string> StartEntry { get; set; }
        public List<string> FinishExit { get; set; }

        public MapData(string fileName)
        {
            FileName = fileName;
            Grid = new List<List<char>>();
            TrapLayer = new List<List<char>>();
            Directions = new List<string>();
            StartEntry = new List<string>();
            FinishExit = new List<string>();
        }

        /// <summary>
        /// Parse a map file and extract all relevant data
        /// </summary>
        public void ParseFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            // Parse grid dimensions
            foreach (var line in lines)
            {
                if (line.StartsWith("# Grid dimensions:"))
                {
                    var dims = line.Split(':')[1].Trim();
                    var dimensions = dims.Split('x');
                    Width = int.Parse(dimensions[0]);
                    Height = int.Parse(dimensions[1]);
                    break;
                }
            }

            // Find the main grid and trap layer
            bool gridStarted = false;
            bool trapStarted = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check for trap layer section header
                if (trimmedLine == "# Trap Layer")
                {
                    trapStarted = true;
                    gridStarted = false;
                    continue;
                }

                // Parse metadata (can appear anywhere)
                if (trimmedLine.StartsWith("direction :"))
                {
                    var directionPart = trimmedLine.Split(':')[1].Trim();
                    Directions = directionPart.Split(',').Select(d => d.Trim()).ToList();
                    continue;
                }

                if (trimmedLine.StartsWith("start :"))
                {
                    var coords = trimmedLine.Split(':')[1].Trim().Split(',');
                    StartPos = (int.Parse(coords[0]), int.Parse(coords[1]));
                    continue;
                }

                if (trimmedLine.StartsWith("finish :"))
                {
                    var coords = trimmedLine.Split(':')[1].Trim().Split(',');
                    FinishPos = (int.Parse(coords[0]), int.Parse(coords[1]));
                    continue;
                }

                if (trimmedLine.StartsWith("Possible Start Entry :"))
                {
                    var entries = trimmedLine.Split(':')[1].Trim();
                    StartEntry = entries.Split(',').Select(e => e.Trim()).ToList();
                    continue;
                }

                if (trimmedLine.StartsWith("Possible Finish Exit :"))
                {
                    var exits = trimmedLine.Split(':')[1].Trim();
                    FinishExit = exits.Split(',').Select(e => e.Trim()).ToList();
                    continue;
                }

                // Skip other comment lines and empty lines
                if (trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                    continue;

                // Parse grid data (non-comment lines that contain game symbols)
                if (!trapStarted && trimmedLine.Length > 0)
                {
                    Grid.Add(trimmedLine.ToList());
                }
                else if (trapStarted && trimmedLine.Length > 0)
                {
                    TrapLayer.Add(trimmedLine.ToList());
                }
            }
        }
    }
}

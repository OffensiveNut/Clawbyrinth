using System;
using System.Drawing;

namespace Clawbyrinth.Levels
{
    public class Level1 : Level
    {
        private Point startPosition;
        private Point finishPosition;

        public Level1(int windowWidth, int windowHeight)
            : base(windowWidth, windowHeight)
        {
        }

        protected override void GenerateLevel()
        {
            // Initialize all cells empty
            levelData = new int[gridWidth, gridHeight];
            wallTypes = new WallType[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    levelData[x, y] = EMPTY;

            // Blueprint: rows of equal length, ' ' empty, '#' wall, 'S' start, 'F' finish
            string[] blueprint = new[]
            {
                "                    ###",
                "                    #F#",
                "                    # #",
                "                    # #",
                "              ####### #",
                "              #       # ",
                "              #   #####",
                "              #   #    ",
                "              #   ###  ",
                "              #     #  ",
                "              ##### #  ",
                "                  # #  ",
                "                  # #  ",
                "                  # #  ",
                "              ##### ####",
                "              #        #",
                "              #     ## #",
                "              #     ## #",
                "              #     ## #",
                "              #     ## #",
                "              ######## #",
                "        #########    # #",
                "        #       #    # #",
                "        # ##### #    # #",
                "        # #   # #    # #",
                "        # #   # ###### #",
                "        # #   #        #",
                "        # #   ##########",
                "  ####### ##########   #########",
                "  #                #####       #",
                "  #       ########             #",
                "  #       #      ########      #",
                "  #########       #######      #",
                "                  #            #",        
                "                  #            #",
                "                  #            #",
                "                  #   S        #",
                "                  ##############"
            };

            // Parse blueprint
            for (int y = 0; y < blueprint.Length && y < gridHeight; y++)
            {
                string row = blueprint[y];
                for (int x = 0; x < row.Length && x < gridWidth; x++)
                {
                    char c = row[x];
                    switch (c)
                    {
                        case '#':
                            levelData[x, y] = WALL;
                            break;
                        case 'S':
                            // Player starts inside the maze, not on the wall
                            levelData[x, y] = EMPTY;
                            // Find the empty space near 'S' for player to start
                            break;
                        case 'F':
                            finishPosition = new Point(x, y);
                            levelData[x, y] = EMPTY;
                            break;
                        default:
                            levelData[x, y] = EMPTY;
                            break;
                    }
                }
            }

            // Find start position - look for the 'S' area and place player in nearby empty space
            for (int y = 0; y < blueprint.Length && y < gridHeight; y++)
            {
                string row = blueprint[y];
                for (int x = 0; x < row.Length && x < gridWidth; x++)
                {
                    if (row[x] == 'S')
                    {
                        // Look for empty space around the 'S' position
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int checkX = x + dx;
                                int checkY = y + dy;
                                if (checkX >= 0 && checkX < gridWidth && 
                                    checkY >= 0 && checkY < gridHeight &&
                                    levelData[checkX, checkY] == EMPTY)
                                {
                                    startPosition = new Point(checkX, checkY);
                                    goto foundStart;
                                }
                            }
                        }
                    }
                }
            }
            foundStart:

            // Determine wall types for rendering
            DetermineAllWallTypes();
        }

        public override Point GetStartPosition()
        {
            return startPosition;
        }

        public Point GetFinishPosition()
        {
            return finishPosition;
        }
    }
}
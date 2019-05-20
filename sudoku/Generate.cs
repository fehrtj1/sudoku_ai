using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

//Adapted from: https://codereview.stackexchange.com/questions/141763/generating-large-sudoku-grid-in-c

namespace sudoku
{
    class Generate
    {
        // characters used in the grid
        public const string valueList = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ?";

        public static void runPuzzleMaker()
        {
            Console.SetCursorPosition(0, 0);

            // start generating grid
            for (int i = 0; i < 4; i++)
            {
                Thread t = new Thread(() => { new Grid(3); });
                t.Priority = ThreadPriority.Highest;
                t.Start();
            }
        }
    }

    /**
     * Basic building blocks of the grid.
     */
    class Cell : IComparable<Cell>
    {
        // cell coordinates
        public int X;
        public int Y;

        // Containing groups
        public Group Row;
        public Group Column;
        public Group Box;

        // Possible values
        public string PossibleValues = Generate.valueList;

        // Display value
        public char Value = 'x';

        // Use this to randomize sort order
        int I = Grid.Rand.Next();

        /**
         * Constructor
         */
        public Cell(int x, int y, Group row, Group column, Group box, int numValues)
        {
            X = x;
            Y = y;
            Row = row;
            Column = column;
            Box = box;

            // assign to groups
            row.AddCell(this);
            column.AddCell(this);
            box.AddCell(this);

            // init possible values
            PossibleValues = PossibleValues.Substring(0, numValues);
        }

        /**
         * Assign a value to this cell while removing it from possible values of related cells
         */
        public void AssignValue(char value)
        {
            if (PossibleValues.Length > 0 && PossibleValues.IndexOf(value) != -1)
            {
                RemoveValueFromGroups(value);

                Value = value;
            }
        }

        /**
         * Remove a value from possible values
         */
        protected void RemoveValue(char value)
        {
            int index = PossibleValues.IndexOf(value);

            // remove value if exists in possible values
            if (index != -1)
            {
                PossibleValues = PossibleValues.Remove(index, 1);
            }
        }

        /**
         * Remove a value from all related group members
         */
        protected void RemoveValueFromGroups(char value)
        {
            for (int i = 0; i < Row.Cells.Length; i++)
            {
                Row.Cells[i].RemoveValue(value);
                Column.Cells[i].RemoveValue(value);
                Box.Cells[i].RemoveValue(value);
            }
        }

        /**
         * Used to sort cells randomly using their assigned RNG value I
         */
        public int CompareTo(Cell c)
        {
            if (c == null) return 0;

            return I.CompareTo(c.I);
        }

        /**
         * Regenerate the RNG value
         */
        public void ReseedRng()
        {
            I = Grid.Rand.Next();
        }
    }

    /**
     * A class that holds the cells. Can be rows, columns, or boxes
     */
    class Group
    {
        public Cell[] Cells;

        protected int Index = 0;

        /**
         * Constructor
         */
        public Group(int numCells)
        {
            Cells = new Cell[numCells];
        }

        /**
         * Add a cell to the group
         */
        public void AddCell(Cell cell)
        {
            Cells[Index++] = cell;
        }

        /**
         * Get a sorted set of all cells that can potential have the given value
         */
        public SortedSet<Cell> GetCandidates(char value)
        {
            SortedSet<Cell> candidates = new SortedSet<Cell>();

            // Add eligible cells
            foreach (Cell cell in Cells)
            {
                if (cell.Value == 'x' && cell.PossibleValues.Contains(value))
                {
                    candidates.Add(cell);
                }
            }

            return candidates;
        }
    }

    /**
     * Class that represents the sudoku square and all it's parts
     */
    class Grid
    {
        //static Grid Instance;

        string PossibleValues = Generate.valueList;
        public int BoxSideLength;

        public static Random Rand = new Random();

        public Group[] Rows;
        
        public Group[] Columns;
        public Group[] Boxes;
        public Cell[] Cells;

        protected static bool CompletedGrid = false;

        /**
         * Constructor
         */
        public Grid(int boxSideLength)
        {
            int sideLength = boxSideLength * boxSideLength;

            BoxSideLength = boxSideLength;
            PossibleValues = PossibleValues.Substring(0, sideLength);

            Rows = new Group[sideLength];
            Columns = new Group[sideLength];
            Boxes = new Group[sideLength];

            // instantiate the groups
            for (int i = 0; i < sideLength; i++)
            {
                Rows[i] = new Group(sideLength);
                Columns[i] = new Group(sideLength);
                Boxes[i] = new Group(sideLength);
            }

            Cells = new Cell[sideLength * sideLength];

            // instantiate the cells
            for (int y = 0; y < sideLength; y++)
            {
                for (int x = 0; x < sideLength; x++)
                {
                    int boxIndex = (x / boxSideLength) + (y / boxSideLength) * boxSideLength;

                    Cells[x + y * sideLength] = new Cell(x, y, Rows[y], Columns[x], Boxes[boxIndex], sideLength);
                }
            }

            // start building the grid
            // Assign the cell values
            while (!PopulateChar(PossibleValues[0]))
            {
                // reset Rng if complete failure occurs
                foreach (Cell cell in Cells)
                {
                    cell.ReseedRng();
                }
            }

            // first completed grid
            if (!CompletedGrid)
            {
                CompletedGrid = true;
                Draw();
                Console.ReadLine();
            }
        }

        /** 
         * Used to recursively feed values into the AssignValues method
         */
        protected bool PopulateChar(char value)
        {
            //Console.SetCursorPosition(0, 0);
            //Draw();

            // check for completed grid, end processing
            if (CompletedGrid)
            {
                return true;
            }

            return AssignValues(Boxes[0], value);
        }

        /**
         * Used to recursively assign the given value to a cell in each box group
         */
        protected bool AssignValues(Group box, char value)
        {

            var candidates = box.GetCandidates(value);

            if (candidates.Count > 0)
            {

                foreach (Cell cell in candidates)
                {
                    // check for completed grid, end processing
                    if (CompletedGrid)
                    {
                        return true;
                    }

                    // save current state of grid
                    State[] states = new State[Cells.Length];
                    for (int i = 0; i < Cells.Length; i++)
                    {
                        states[i] = new State(Cells[i].Value, Cells[i].PossibleValues);
                    }

                    cell.AssignValue(value);

                    // determine if this cell will cause the next box to error
                    int index = Array.IndexOf(Boxes, box);
                    int gridRowIndex = index / BoxSideLength;
                    int gridColIndex = index % BoxSideLength;

                    bool causesError = false;
                    for (int i = index + 1; i < Boxes.Length; i++)
                    {
                        if (/*i > BoxSideLength * 2 &&*/ gridRowIndex != i / BoxSideLength || gridColIndex != i % BoxSideLength) continue;

                        bool hasFreeCell = false;
                        foreach (Cell testCell in Boxes[i].Cells)
                        {
                            if (testCell.PossibleValues.Contains(value))
                            {
                                hasFreeCell = true;
                                break;
                            }
                        }
                        if (!hasFreeCell)
                        {
                            causesError = true;
                            break;
                        }
                    }

                    // move on to next box if no error
                    if (!causesError)
                    {
                        int nextBoxIndex = index + 1;

                        if (nextBoxIndex == Boxes.Length)
                        {
                            // start assigning next character
                            int indexOfNextChar = PossibleValues.IndexOf(value) + 1;

                            // Check for grid completion
                            if (indexOfNextChar == PossibleValues.Length) return true;

                            // move on to next char
                            if (PopulateChar(PossibleValues[indexOfNextChar])) return true;
                        }
                        else
                        {
                            // recurse through next box
                            if (AssignValues(Boxes[nextBoxIndex], value)) return true;
                        }
                    }

                    // undo changes made in this recursion layer
                    for (int i = 0; i < Cells.Length; i++)
                    {
                        Cells[i].Value = states[i].Value;
                        Cells[i].PossibleValues = states[i].PossibleValues;
                    }
                }
            }
            return false; // no viable options, go back to previous box or previous character
        }

        /**
         * Output the grid to console
         */
        public void Draw()
        {
            int rowCounter = 0;

            foreach (Group row in Rows)
            {
                StringBuilder rowString = new StringBuilder();
                foreach (Cell cell in row.Cells)
                {
                    if (cell.X % BoxSideLength == 0 && cell.X != 0)
                    {
                        rowString.Remove(rowString.Length-1, 1);
                        rowString.Append("|");
                        rowString.Append(" ");
                    }
                    rowString.Append(cell.Value);
                    rowString.Append(' ', 2);
                }

                rowCounter++;
                if (rowCounter == BoxSideLength)
                {
                    rowCounter = 0;
                    Console.WriteLine(rowString.Append('\n').Append('-', Rows.Length * 3));
                }
                else
                    Console.WriteLine(rowString + "\n");
            }
        }
    }
}

/**
 * Used for persisting a cell's state
 */
struct State
{
    public char Value;
    public string PossibleValues;

    public State(char value, string possibleValues)
    {
        Value = value;
        PossibleValues = possibleValues;
    }
}

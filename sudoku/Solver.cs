using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace sudoku
{
    class Solver
    {
        //Variables for Puzzle
        public static int puzzleSize;
        public static int smallBoxWidth;
        public static int smallBoxHeight;
        public static List<Element> puzzle;

        static void Main(string[] args)
        {
            string fileName;
            fileName = "complete-6.txt";
            //fileName = "complete-5.txt";
            //fileName = "easy1.txt";
            //fileName = "6x6.txt";


            //fileName = "complete-12.txt";
            //fileName = "medium1.txt";


            //Set path to find fileName in folder with program files
            string filePath = Directory.GetCurrentDirectory();
            filePath = filePath.Remove(filePath.IndexOf("\\bin\\Debug"));
            filePath += "\\" + fileName;

            Console.WriteLine("Reading file: " + fileName + "\n");
            List<String> lines = new List<string>();
            try
            {
                lines = File.ReadAllLines(filePath).ToList();
            }
            catch (IOException fileNotFoundError) { Console.WriteLine("Could not find file: \n" + filePath); Console.ReadKey(); return; }
            puzzle = new List<Element>();

            //Iterate over every line read in from file
            for (int lineNum = 0; lineNum < lines.Count; lineNum++)
            {
                string line = lines[lineNum];
                string[] entries = line.Split(' ');
                //Uses input depending on what line it is on
                switch (lineNum)
                {
                    //Line 0: Contains puzzle size (ie. 9x9)
                    case 0:
                        puzzleSize = Int32.Parse(entries[0]);
                        break;
                    //Line 1: Contains smaller box size (ie. 3x3)
                    case 1:
                        smallBoxWidth = Int32.Parse(entries[0]);
                        smallBoxHeight = Int32.Parse(entries[1]);
                        break;
                    //Rest of lines: Contains the puzzle data (values: 1-PuzzleSize, if 0, then its "blank")
                    default:
                        for (int eIndex = 0; eIndex < entries.Length; eIndex++)
                        {
                            int x = eIndex;
                            int y = lineNum - 2;
                            int val = 0;
                            try { val = Int32.Parse(entries[eIndex]); }
                            catch (Exception parseIntError) { Console.WriteLine("Does not parse: " + line); Console.ReadKey(); return; }

                            Element e = new Element(x, y, val);
                            puzzle.Add(e);
                        }
                        break;
                }
            }

            int unsolvedCount = getNumNotSolved();
            Console.WriteLine("# spots empty(0's): " + unsolvedCount + "/" + (puzzleSize * puzzleSize));
            printPuzzle(puzzle, "Before Solved:");
            Console.WriteLine("------------------------------------------------------");

            //Determine which algorithms to use
            bool useDFS = unsolvedCount <= 6;
            bool useModDFS = unsolvedCount <= 12;
            bool useAC3 = true;

            runAlgorithms(useDFS, useModDFS, useAC3);
            
            Console.WriteLine("\nProgram completed successfully, enter to exit");
            Console.ReadKey();
        }

        public static void runAlgorithms(bool runDFS, bool runModDFS, bool runAC3)
        {
            string times = "";
            if (runDFS)
            {
                var DFS_stopwatch = new Stopwatch();
                DFS_stopwatch.Start();

                List<Element> answer = DFS(puzzle, 0, false);
                if (answer != null) printPuzzle(answer, "DFS:");
                else Console.WriteLine("No solution found.");

                DFS_stopwatch.Stop();
                Console.WriteLine("DFS took " + DFS_stopwatch.ElapsedMilliseconds + " ms");
                Console.WriteLine("------------------------------------------------------");
                resetPuzzle(puzzle);
                times += "\n\tDFS: \t\t" + DFS_stopwatch.ElapsedMilliseconds;
            }
            if (runModDFS)
            {
                var modDFS_stopwatch = new Stopwatch();
                modDFS_stopwatch.Start();

                List<Element> modDFSanswer = modDFS(puzzle, 0, false);
                if (modDFSanswer != null) printPuzzle(modDFSanswer, "modDFS:");
                else Console.WriteLine("No solution found.");

                modDFS_stopwatch.Stop();
                Console.WriteLine("modDFS took " + modDFS_stopwatch.ElapsedMilliseconds + " ms");
                Console.WriteLine("------------------------------------------------------");
                resetPuzzle(puzzle);
                times += "\n\tModDFS: \t" + modDFS_stopwatch.ElapsedMilliseconds;
            }
            if (runAC3)
            {
                var AC3_stopwatch = new Stopwatch();
                AC3_stopwatch.Start();

                List<Element> AC3Answer = AC3(puzzle);
                if (AC3Answer != null) printPuzzle(AC3Answer);
                else Console.WriteLine("No solution found.");

                AC3_stopwatch.Stop();
                Console.WriteLine("AC3 took " + AC3_stopwatch.ElapsedMilliseconds + " ms");
                Console.WriteLine("------------------------------------------------------");
                resetPuzzle(puzzle);
                times += "\n\tAC3: \t\t" + AC3_stopwatch.ElapsedMilliseconds;
            }


            Console.WriteLine("\nTIMES: (ms)" + times);
        }

        public static List<Element> DFS(List<Element> myPuzzle, int i, bool printEveryLine = false)
        {
            if (i == myPuzzle.Count)
            {
                // check against answer
                if (printEveryLine)
                    printPuzzle(myPuzzle);
                if (isSolved(myPuzzle))
                    return myPuzzle;

                return null;
            }
            else
            {
                if (!myPuzzle[i].isDefault)
                {
                    for (int j = 1; j <= puzzleSize; j++)
                    {
                        myPuzzle[i].actualValue = j;
                        List<Element> newPuzzle = DFS(myPuzzle, i + 1, printEveryLine);
                        if (newPuzzle != null)
                        {
                            return newPuzzle;
                        }
                    }
                    myPuzzle[i].actualValue = 1;
                } else
                {
                    List<Element> newPuzzle = DFS(myPuzzle, i + 1, printEveryLine);
                    if (newPuzzle != null)
                        return newPuzzle;
                }
            }
            return null;
        }

        public static List<Element> modDFS(List<Element> myPuzzle, int i, bool printEveryLine = false)
        {
            if(i == 0)
            {
                foreach (Element e in myPuzzle)
                {
                    if (e.actualValue == 0)
                        e.updatePossibilites(true);
                }
            }
            

            if (i == myPuzzle.Count)
            {
                // check against answer
                if (printEveryLine)
                    printPuzzle(myPuzzle);
                // check against answer
                if (isSolved(myPuzzle))
                    return myPuzzle;

                return null;
            }
            else
            {
                if (!myPuzzle[i].isDefault)
                {
                    Element e = myPuzzle[i];
                    if (e.possibilities.Count == 0)
                    {
                        List<Element> newPuzzle = modDFS(myPuzzle, i + 1, printEveryLine);
                        if (newPuzzle != null)
                            return newPuzzle;
                    }
                    else
                    {
                        for (int j = 0; j < e.possibilities.Count; j++)
                        {
                            myPuzzle[i].actualValue = e.possibilities[j];
                            List<Element> newPuzzle = modDFS(myPuzzle, i + 1, printEveryLine);
                            if (newPuzzle != null)
                                return newPuzzle;
                        }
                        myPuzzle[i].actualValue = 1;
                    }
                }
                else
                {
                    List<Element> newPuzzle = modDFS(myPuzzle, i + 1, printEveryLine);
                    if (newPuzzle != null)
                        return newPuzzle;
                }
            }
            return null;
        }

        public static List<Element> AC3(List<Element> puzzle)
        {
            // Get all constraints
            // All relationships between each value of every row, column, and box
            Queue<Tuple<Element, Element>> arcs = generateWorklist(puzzle);

            while (arcs.Count > 0)
            {
                Tuple<Element, Element> constraint = arcs.Dequeue();

                if (arc_reduce(constraint.Item1, constraint.Item2))
                {
                    if (constraint.Item1.possibilities.Count == 0)
                    {
                        // No solutions exist to the puzzle; we have exhausted all possibilties since its domain size is 0
                        return null;
                    }
                    else
                    {
                        // Add new arcs to the queue that have been affected by modifying the domain of Item1
                        addNeighbors(constraint.Item1, arcs);
                    }
                }
            }

            // returns the inputted puzzle, each cell's domain reduced only to the possible values.
            return puzzle;
        }

        public static bool arc_reduce(Element x, Element y)
        {
            bool change = false;
            if (x.actualValue == 0)
            {
                if (x.possibilities.Count == 1)
                {
                    x.actualValue = x.possibilities[0];
                    change = true;
                }
                else
                {
                    for (int i = 0; i < x.possibilities.Count; i++)
                    {
                        if (!(x.possibilities[i] != y.actualValue) && y.actualValue != 0)
                        {
                            x.possibilities.Remove(x.possibilities[i]);
                            change = true;
                        }
                    }
                }
            }
            return change;
        }

        public static Queue<Tuple<Element, Element>> generateWorklist(List<Element> puzzle)
        {
            Queue<Tuple<Element, Element>> arcs = new Queue<Tuple<Element, Element>>();

            foreach (Element e in puzzle)
            {
                addNeighbors(e, arcs);
            }
            return arcs;
        }

        public static void addNeighbors(Element e, Queue<Tuple<Element, Element>> Q)
        {
            foreach (Element r in getRow(puzzle, e.row))
            {
                Q.Enqueue(new Tuple<Element, Element>(e, r));
            }
            foreach (Element c in getColumn(puzzle, e.col))
            {
                Q.Enqueue(new Tuple<Element, Element>(e, c));
            }
            foreach (Element b in getBox(puzzle, e.smallBoxNum))
            {
                Q.Enqueue(new Tuple<Element, Element>(e, b));
            }
        }

        public static List<Element> getColumn(List<Element> puzzle, int c)
        {
            List<Element> col = new List<Element>();
            for (int i = 0; i < puzzleSize; i++)
            {
                col.Add(getElementFromCoords(c, i));
            }
            return col;
        }
        public static List<Element> getRow(List<Element> puzzle, int r)
        {
            List<Element> row = new List<Element>();
            for (int i = 0; i < puzzleSize; i++)
            {
                row.Add(getElementFromCoords(i, r));
            }
            return row;
        }
        public static List<Element> getBox(List<Element> puzzle, int n)
        {
            List<Element> box = new List<Element>();
            foreach (Element e in puzzle)
            {
                if (e.smallBoxNum == n)
                {
                    box.Add(e);
                }
            }
            return box;
        }
        public static List<Element> AC3v2(List<Element> myPuzzle, bool debug = false)
        {
            //initial get all possibilities
            foreach (Element e in myPuzzle)
            {
                if(e.actualValue == 0)
                {
                    e.updatePossibilites(true);
                    if (debug)
                        Console.WriteLine("first update: " + e.ToString());
                }
            }

            List<Element> unsolvedPositions = getUnsolvedElements(myPuzzle, debug);

            while (unsolvedPositions.Count > 0)
            {
                bool stuck = true;
                //Go back and update any not solved
                foreach (Element e in unsolvedPositions)
                {
                    if (e.updatePossibilites(true, debug))
                        stuck = false;
                }
                //Update unsolved
                unsolvedPositions = getUnsolvedElements(unsolvedPositions, debug);
                if (stuck)
                {
                    Console.WriteLine("\n\tAC3 Stuck: need to run another algorithm");
                    printPuzzle(myPuzzle, "AC3: when stuck");
                    return modDFS(myPuzzle, 0, debug);
                }
                   
            }
            return myPuzzle;
        }
        public static List<Element> getUnsolvedElements(List<Element> puzzle, bool debug = false)
        {
            List<Element> unsolved = new List<Element>();
            foreach (Element e in puzzle)
            {
                if (e.actualValue == 0)
                {
                    unsolved.Add(e);
                    if (debug)
                        Console.WriteLine("\tunsolved: " + e.ToString());
                }
            }
            return unsolved;
        }  

        //Helper function
        public static void printPuzzle(List<Element> myPuzzle, string title = "")
        {
            string p = title + "\n";
            for (int i = 0; i < myPuzzle.Count; i++)
            {
                Element e = myPuzzle[i];
                var val = e.actualValue + "";
                if (val == "0")
                    val = "?";

                p += val + " ";
                if ((i + 1) % puzzleSize == 0 && i != 0)
                    p += "\n";
            }
            Console.WriteLine(p);
        }
        public static int getNumNotSolved()
        {
            int unsolved = 0;
            foreach (Element e in puzzle)
            {
                if (e.actualValue == 0)
                    unsolved++;
            }
            return unsolved;
        }
        public static bool isSolved(List<Element> myPuzzle)
        {
            foreach (Element e in myPuzzle)
            {
                if (!allowedInRow(e, e.actualValue) || !allowedInCol(e, e.actualValue) || !allowedInSmallBox(e, e.actualValue))
                    return false;
            }
            return true;
        }
        public static Element getElementFromCoords(int x, int y)
        {
            int index = (y * puzzleSize) + x;
            return puzzle[index];
        }
        public static int getSmallBoxNumberFromCoords(int x, int y)
        {
            int smallBoxX = (x / Solver.smallBoxWidth);
            int smallBoxY = (y / Solver.smallBoxHeight);

            int numXBoxes = puzzleSize / smallBoxWidth;

            return (smallBoxY * numXBoxes) + smallBoxX;
        }
        public static bool allowedInRow(Element e, int val)
        {
            for (int i = 0; i < puzzleSize; i++)
            {
                if (getElementFromCoords(i, e.row).actualValue == val && i != e.col)
                    return false;
            }
            return true;
        }
        public static bool allowedInCol(Element e, int val)
        {
            for (int i = 0; i < puzzleSize; i++)
            {
                if (getElementFromCoords(e.col, i).actualValue == val && i != e.row)
                    return false;
            }
            return true;
        }
        public static bool allowedInSmallBox(Element element, int val)
        {
            foreach (Element e in puzzle)
            {
                if (e.smallBoxNum == element.smallBoxNum && e.actualValue == val && e.row != element.row && e.col != element.col)
                    return false;
            }
            return true;
        }
        public static void resetPuzzle(List<Element> puzzle)
        {
            //printPuzzle(puzzle, "Before Reset");
            foreach (Element e in puzzle)
            {
                e.reset();
            }
            //printPuzzle(puzzle,"After Reset");
        }
    }

    class Element
    {
        public int col;
        public int row;
        public int smallBoxNum;

        public List<int> possibilities;
        public int actualValue;
        public int initialVal;
        public bool isDefault = true;

        public Element(int x, int y, int val)
        {
            //Location in Puzzle
            col = x;
            row = y;
            smallBoxNum = Solver.getSmallBoxNumberFromCoords(col, row);

            possibilities = new List<int>();

            //Values
            actualValue = val;
            initialVal = val;
            if (val == 0)
            {
                isDefault = false;
                for (int i = 1; i <= Solver.puzzleSize; i++)
                {
                    possibilities.Add(i);
                }
            }
        }

        //return true if can update
        public bool updatePossibilites(bool updateActualIfOneLeft = false, bool debug = false)
        {
            List<int> newPossibilities = new List<int>();

            foreach (int val in possibilities)
            {
                if (checkValueValid(val))
                {
                    newPossibilities.Add(val);
                    if(debug)
                        Console.WriteLine("Update poss: " + ToString());
                } 
            }
            possibilities = newPossibilities;

            //if there is only 1 possibility left, then set actual value
            if (updateActualIfOneLeft && possibilities.Count == 1)
            {
                actualValue = possibilities[0];
                possibilities.Clear();
                //Console.WriteLine("\tSet value: " + ToString());
                return true;
            }

            if (possibilities.Count == 0 && actualValue == 0)
                return false;

            return false;
        }
        public bool checkValueValid(int val)
        {
            bool allowedRow = Solver.allowedInRow(this, val);
            bool allowedCol = Solver.allowedInCol(this, val);
            bool allowedBox = Solver.allowedInSmallBox(this, val);

            return allowedRow && allowedCol && allowedBox;
        }

        public override string ToString()
        {
            string posString = "";
            foreach (int pos in possibilities)
            {
                posString += (pos + ", ");
            }
            return "Element(" + col + "," + row + ") = " + actualValue + " box: " + smallBoxNum + ",D: "+ isDefault +", P: " + posString;
        }
        public void reset()
        {
            actualValue = initialVal;
            possibilities.Clear();
            if (initialVal == 0)
            {
                for (int i = 1; i <= Solver.puzzleSize; i++)
                {
                    possibilities.Add(i);
                }
            }
        }
    }
}

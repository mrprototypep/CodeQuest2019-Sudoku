using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.CodeDom;
using System.Drawing;
using System.Threading;

namespace Sudoku
{
    public class Program
    {
        public class SudokuTable
        {
            //Checks if the table is complete by seeing if every tile is filled with a number. Note that while this technically doesn't check for the accuracy of the table, the table could only be full if it was accurate.
            public bool IsTableComplete()
            {
                for (int x = 0; x < 9; x++)
                {
                    for (int y = 0; y < 9; y++)
                    {
                        if (table[x, y] == null)
                            return false;
                    }
                }
                return true;
            }

            public int?[,] table; //Table of nullable integers, null being blank. Expected range 1-9 inclusive
            public bool[,] knownCorrect; //Table of bools; true if it's a "known correct" answer i.e. imported from the file.
            public List<int>[,] knownExcluded; //Table of lists of integers, contains the "known excluded" numbers (note that the correct answer could be excluded, it's more of a "known excluded given"

            public SudokuTable() //Initializes everything
            {
                //This is mostly just creating the objects that are used, so the tables and what they contain
                table = new int?[9, 9];
                for (int x = 0; x < 9; x++)
                {
                    for (int y = 0; y < 9; y++)
                        table[x, y] = null;
                }

                knownCorrect = new bool[9, 9];

                knownExcluded = new List<int>[9, 9];
                for (int x = 0; x < 9; x++)
                {
                    for (int y = 0; y < 9; y++)
                        knownExcluded[x,y] = new List<int>();
                }

            }

            public void PopulateLine(int line, string input) //Populate the line in the table, used in the scanning of the file into memory
            {
                for (int i = 0; i < 9; i++)
                {
                    table[i, line] = input[i] == '_' ? null : (int.Parse(input[i].ToString()) as int?); //Inserts either number or null, depending
                    if (table[i, line] != null)
                        knownCorrect[i, line] = true; //If what it's entering isn't null, it's a number. If there's a number at this stage, it's known to be correct.
                }
            }

            //Prints table to console
            public void Output()
            {
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if (table[i, j] == null)
                            Console.Write("_");
                        else
                            Console.Write(table[j, i]);
                    }
                    Console.WriteLine();
                }
            }

            //Checks to see if the number num is contained in the row row (note that 0-indexing means that rows start at 0 and end at 8)
            public bool ContainedInRow(int num, int row)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (table[row, i] == num)
                        return true;
                }
                return false;
            }

            //Checks to see if the number num is contained in the column col
            public bool ContainedInColumn(int num, int col)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (table[i, col] == num)
                        return true;
                }
                return false;
            }

            //Checks to see if the number num is contained in the subgrid at point sg. This exploits the truncation of division with ints, i.e. 3/2 = 1.
            public bool ContainedInSubgrid(int num, Point sg)
            {
                for (int x = sg.x * 3; x < sg.x * 3 + 3; x++)
                {
                    for (int y = sg.y * 3; y < sg.y * 3 + 3; y++)
                    {
                        if (table[x, y] == num)
                            return true;
                    }
                }
                return false;
            }

            //Returns the subgrid that a given point is located in.
            public static Point GetSubgrid(int x, int y)
            {
                return new Point(x / 3, y / 3);
            }

            //Returns null if no valid number exists
            public int? GenerateValidNumber(Point p)
            {
                int num;
                for (num = 1; num <= 9; num++)
                {
                    if (!(knownExcluded[p.x, p.y].Contains(num) || ContainedInRow(num, p.x) || ContainedInColumn(num, p.y) || ContainedInSubgrid(num, GetSubgrid(p.x, p.y))))
                        return num;
                }
                return null;
            }

            //Goes to the next unknown number from a point p. Does not return anything because p is a reference, meaning the passed value is modified.
            public void NextUnknown(ref Point p)
            {
                do
                {
                    try
                    {
                        p.Increment();
                    }
                    catch (Exception ex) //The try-catch here is kinda sloppy so just ignore it.
                    {
                        if (ex.Message.Equals("y too big"))
                            return;
                    }
                } while (knownCorrect[p.x, p.y]); //Continues to increment until it finds one that fits the conditions required.
            }

            //Take a guess, it's the opposite of NextUnknown()
            public void PreviousUnknown(ref Point p)
            {
                do
                {
                    p.Decrement();
                } while (knownCorrect[p.x, p.y]);
            }
        }

        //The point class (which also has a version that already exists in C#, but I forgot what library it belonged to and didn't have internet) is a point on a two-dimensional grid, and has some helper methods
        public class Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            //Increments the value of a point by 1 position. Throws an exception if y exceeds 8 (the array)
            public void Increment()
            {
                x++;
                if (x == 9)
                {
                    y++;
                    x = 0;
                    if (y == 9)
                        throw new Exception("y too big");
                }
            }

            //Guess
            public void Decrement()
            {
                x--;
                if (x == -1)
                {
                    y--;
                    x = 8;
                    if (y == -1)
                        throw new Exception("y too small");
                }
            }
        }

        //Location of the input file
        const string file = "Prob24.txt";

        static List<SudokuTable> tables; //Array containig the tables

        static Form1 show; //This is the GUI, don't try and understand its code. We won't be using one at CodeQuest

        static void Main(string[] args)
        {
            //Setup
            var raw = File.ReadAllLines(file); //Reads the file
            int numTables = int.Parse(raw[0]); //Gets the number on the first line that says how many tables there are

            tables = new List<SudokuTable>(); //Initializes tables list

            int rawIndex = 1;
            for (int i = 0; i < numTables; i++) //Once for each whatever
            {
                var table = new SudokuTable();
                for (int j = 0; j < 9; j++, rawIndex++) //Go through next 9
                {
                    table.PopulateLine(j, raw[rawIndex]); //Writes the sudoku table to the object
                }
                tables.Add(table);
            }

            show = new Form1(); //GUI stuff
            show.Show(); //GUI stuff

            //Processing
            foreach (SudokuTable t in tables)
            {
                //This block of code finds the first unknown point. It takes into account the possibility that the first point, [0, 0], may or may not be known
                Point p = new Point(-1, 0);
                t.NextUnknown(ref p);

                show.UpdateTable(t, true); //GUI stuff

                int iterations = 0; //GUI stuff but not really, but it's only used by the GUI so
                while (!t.IsTableComplete()) //Continue until table is complete. This method is run every iteration, and could probably be optimized. But it's already almost instantaneous so I'm not going to bother
                {
                    t.table[p.x, p.y] = null; //Sets the value at this point to null to prevent it from counting toward the checking for numbers. There's no checking here to make sure the point isn't already known because that's done already with the NextUnknown and PreviousUnknown methods

                    int? num = t.GenerateValidNumber(p); //Gets (guesses, basically) the next potentially valid number. Counts up from 1 to 9. Simple stuff

                    if (num == null) //If the next valid number is null, it means that there are no valid numbers and the solution must be wrong somewhere.
                    {
                        t.knownExcluded[p.x, p.y].Clear(); //Removes any excluded values, as the assumption they can't be right is based off of an invalid solution
                        t.PreviousUnknown(ref p); //Goes to previous position
                    } else
                    {
                        t.table[p.x, p.y] = num; //Inserts number into table
                        t.knownExcluded[p.x, p.y].Add((int)num); //Excludes this because if it needs to go back later (which, trust me, it will) it saves a step
                        t.NextUnknown(ref p); //Goes to next number in table
                    }

                    //LOOK HERE IF YOU WANT THE GUI TO UPDATE MORE FREQUENTLY
                    //This entire block of code exists purely for graphical reasons, along with the iterations variable
                    if (iterations % 10000 == 0) //Set that number that's 10,000 right now to whatever you want. It refreshes the graph every (that many) iterations. Lower numbers make it painfully slow.
                        show.UpdateTable(t);
                    iterations++;
                }

                t.Output(); //Outputs to console

                //GUI stuff in this block
                show.BackColor = Color.LightGreen;
                show.UpdateTable(t);
                Thread.Sleep(50);
                show.BackColor = Color.White;
            }

            Console.ReadLine(); //This is here so the console doesn't close when the program terminates, and wouldn't be there in the competition. It just waits for user input, basically.
        }
    }
}

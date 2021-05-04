using System; using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace console_maze
{
    public enum MazeType { Square, Triangle, Hexagon }
    public enum MazePropertyType { Other1, Other2 }

    public static class Mazes
    {
        public class CellList : List<Cell>
        {
            public override string ToString()
            {
                string r = this.Aggregate(
                    "", (current, cell) => current + $"{cell.id}, "
                );
                return r != "" ? r.Substring(0, r.Length - 2) : r;
            }
            public CellList(int capacity): base(capacity) 
            {
            }
            public CellList()
            {
            }
        }

        private class ListOfCellList : List<CellList>
        {
            private string CellListToStr(int idx)
            {
                CellList cellList = this[idx];
                string r = cellList.Aggregate(
                    "", (current, cell) => current + $"({cell}),"
                );
                return r != "" ? r.Substring(0, r.Length-1) : r;
            }
            public override string ToString()
            {
                string r = "";
                for (int i = 0; i < Count; i++) {
                    r += $"[{i}: {CellListToStr(i)}],";
                }
                return r != "" ? r.Substring(0, r.Length-1) : r;
            }
        }

        public class Cell : IEquatable<Cell>
        {
            public int id { get; }
            public CellList group;
            public readonly CellList joints;
            // When generating maze, a joint become either a wall or link:  
            public readonly CellList walls;
            public readonly CellList links;

            public Cell(int id)
            {
                this.id = id;
                joints = new CellList();
                walls = new CellList();
                links = new CellList();
            }
            public override string ToString()
            {
                return $"{id}: {joints}|{walls}|{links}";
            }
            public override int GetHashCode() => id;
            public override bool Equals(object obj)
            {
                return obj is Cell objAsPart && Equals(objAsPart);
            }
            public bool Equals(Cell other) => other != null && id.Equals(other.id); 
            public static bool operator ==(Cell a, Cell b)
            {
                return a is { } && b is { } && a.id == b.id;
            }
            public static bool operator !=(Cell a, Cell b) => !(a == b);

            public void LinkTo(Cell other, FnLog log=null)
            {
                // move joint here -> cell
                log?.Invoke($"BEFORE: ({id}<->{other.id}) ({this} -> link to {other})");
                HashSet<Cell> selected = new HashSet<Cell>(
                    joints.Where(item => item == other)
                );
                links.AddRange(selected);
                joints.RemoveAll(selected.Contains);
                // move joint cell -> here
                selected = new HashSet<Cell>(
                    other.joints.Where(item => item == this)
                );
                other.links.AddRange(selected);
                other.joints.RemoveAll(selected.Contains);
                log?.Invoke($"AFTER : ({id}<->{other.id}) ({this} -> link to {other})");
            }
        }

        // (1) loop: allocate Cell's, (2) loop: for each Cell, call CbInitLinks():
        public delegate void CbInitLinks(AbstractMaze l, int no);
        public delegate void FnLog(object message);
        public abstract class AbstractMaze : List<Cell>
        {
            protected int w { get; }
            protected int h { get; }
            protected int z { get; }
            protected readonly FnLog log;
            protected CbInitLinks cbInitLinks;
            protected AbstractMaze(int w, int h, int z, FnLog log): base(w * h * z) 
            {
                this.w = w;
                this.h = h;
                this.z = z;
                this.log = log;
                /* ! calling parent base(capacity) only allocate memory for pointers
                 *   -> they point to "null" -> we need to create them: 
                 */
                for (int i = 0; i < w * h * z; i++) {
                    Add(new Cell(i));
                }
            }
            public virtual void Generate()
            {
                if (cbInitLinks == null) {
                    throw new Exception("? init links callback is null ?");
                }
                for (int i = 0; i < Count; i++) {
                    cbInitLinks(this, i);
                }
            }
        }

        public class SquareMaze : AbstractMaze
        {
            public SquareMaze(int w, int h, int z, FnLog log) : base(w, h, z, log)
            {
                // define how to init links for each cell:
                cbInitLinks = InitLinks;
            }
            private void InitLinks(AbstractMaze l, int no)
            {
                Cell c = l[no];
                int x = no % w;
                int y = no / w;
                c.joints.Clear();
                c.walls.Clear();
                c.links.Clear();
                if (y > 0) {
                    c.joints.Add(l[ no-w ]);
                }
                if (x < w-1) {
                    c.joints.Add(l[ no+1 ]);
                }
                if (y < h-1) {
                    c.joints.Add(l[ no+w ]);
                }
                if (x > 0) {
                    c.joints.Add(l[ no-1 ]);
                }
            }
            private void PrintMaze(int lineStart, int lineEnd)
            {
                for (int y = lineStart; y <= lineEnd; y++) {
                    string t = "";
                    string m = "";
                    string b = "";
                    if (y == h) {
                        break;
                    }
                    for (int x = 0; x < w; x++) {
                        Cell c = this[x + y * w];
                        // top
                        if (x + (y-1)*w >= 0 && c.links.Contains(this[ x + (y-1)*w ])) {
                            t += "+     +";
                        } else {
                            t += "+-----+";
                        }
                        // mid
                        if (x > 0 && c.links.Contains(this[x - 1 + y * w])) {
                            m += " ";
                        } else {
                            m += "|";
                        }
                        m += $" {c.id,3} ";
                        if (x < w-1 && c.links.Contains(this[x + 1 + y * w])) {
                            m += " ";
                        } else {
                            m += "|";
                        }
                        // bottom
                        if (x + (y+1)*w < w*h && c.links.Contains(this[ x + (y+1)*w ])) {
                            b += "+     +";
                        } else {
                            b += "+-----+";
                        }
                    }
                    log($"{t}");
                    log($"{m}");
                    log($"{b}");
                    // if (y >= h) {
                    //     continue;
                    // }
                    // t = $"Cells line {y}";
                    // for (int x = 0; x < w - 1; x++) {
                    //     t += $"{this[x + y*w]} / ";
                    // }
                    // log($"{t}");
                }
            }
            public override void Generate()
            {
                base.Generate();
                ListOfCellList groups = new ListOfCellList();
            
                // 1. Initialize the cells of the first row to each exist in their
                //   own set.
                // Generate groups of cells:
                Random rnd = new Random();
                log($"Maze: size ({w} x {h})");
                // log("Doing first line...");
                for (int x = 0; x < w; x++) {
                    groups.Add(new CellList() { this[x] });
                    this[x].group = groups[ groups.Count-1 ];
                }
                // log("First line done:");
                // PrintMaze(0, 1);

                for (int y = 0; y < h-1; y++) {
                    // log($"--------------------------------------------------------------------------------------");
                    // log($"Doing line {y}...");
                    // log($"2.------------------------------------------------------------------------------------");
                    // PrintMaze(y, y+1);
                    // log($"groups: {groups}");
                    // 2. Now, randomly join adjacent cells, but only if they are
                    //    not in the same set. When joining adjacent cells, merge
                    //    the cells of both sets into a single set, indicating that
                    //    all cells in both sets are now connected (there is a path
                    //    that connects any two cells in the set):
                    //      ___________________
                    //     |           |       |
                    //     | 1   1   1 | 4   4 |
                    //     |___________|_______|
                    for (int x = 1; x < w; x++) {
                        if (this[ x-1 + y*w ].group == this[ x + y*w ].group) {
                            continue;
                        }
                        if (rnd.Next(2) <= 0) {
                            continue;
                        }
                        // log($"Joining ({this[ x-1 + y*w ]}) <-> ({this[ x + y*w ]}), " +
                            // $"groups ({this[ x-1 + y*w ].group} <-> {this[ x + y*w ].group})");
                        CellList newG = this[ x-1 + y*w ].group;
                        CellList oldG = this[ x + y*w ].group;
                        // log($"newG = {newG}");
                        // log($"oldG = {oldG}");
                        foreach (Cell c in oldG) {
                            // log($"moving cell {c} -> {newG}");
                            c.group = newG;
                            // log($"now cell is {c}");
                        }
                        this[x + y*w].LinkTo(this[ x-1 + y*w ], log); 
                        newG.AddRange(oldG);
                        // log($"now, newG = {newG}");
                        groups.Remove(oldG);
                        // log($"Now groups: {groups}");
                    }
                
                    // log($"3.------------------------------------------------------------------------------------");
                    // PrintMaze(y, y+1);
                    // 3. For each set, randomly create vertical connections
                    //    downward to the next row. Each remaining set must have at
                    //    least one vertical connection. The cells in the next row
                    //    thus connected must share the set of the cell above them.
                    //     ___________________
                    //    |           |       |
                    //    | 1   1   1 | 4   4 |
                    //    |    ___    |___    |
                    //    |   |   |   |   |   |
                    //    | 1 |   | 1 |   | 4 |
                    //    |___|   |___|   |___|
                    // log($"3. Start: {groups}");
                    ListOfCellList newGroups = new ListOfCellList();
                    foreach (CellList cellList in groups) {
                        // log($"Current cellList: {cellList}");
                        CellList toAdd = new CellList();
                        // do/while to create at least one connexion:
                        do {
                            int idx = rnd.Next(cellList.Count);
                            toAdd.Add(cellList[idx]);
                            cellList.Remove(cellList[idx]);
                            // loop (if possible) to create another random connexion:
                        } while (cellList.Count > 0 && rnd.Next(2) > 0);
                        // make the link:
                        // log($"Cell(s) which will connect to bottom: {toAdd}");
                        newGroups.Add(new CellList());
                        foreach (Cell cell in toAdd) {
                            cell.LinkTo(this[ cell.id+w ], log);
                            newGroups[newGroups.Count-1].Add(this[ cell.id+w ]);
                            this[ cell.id+w ].group = newGroups[newGroups.Count-1];
                        }
                        cellList.AddRange(toAdd);
                        // log($" now, newGroups: {newGroups}");
                    }
                    // log($"3. End  : {groups}");
                    // log($"3b. Override groups with newGroups: {groups} <<->> {newGroups}");
                    groups = newGroups;
                    // log($"3b. groups now: {groups}");

                    // log($"4.------------------------------------------------------------------------------------");
                    // log($"4. Any remaining cells -> create their own sets.");
                    // PrintMaze(y, y+1);
                    // 4. Flesh out the next row by putting any remaining cells into
                    //    their own sets.
                    //     ___________________
                    //    |           |       |
                    //    | 1   1   1 | 4   4 |
                    //    |    ___    |___    |
                    //    |   |   |   |   |   |
                    //    | 1 | 6 | 1 | 7 | 4 |
                    //    |___|___|___|___|___|
                    for (int x = 0; x < w; x++) {
                        if (this[ x+(y+1)*w ].group != null) {
                            continue;
                        }
                        // log($"cell {this[ x+(y+1)*w ]} (x:{x},y:{y+1}) alone -> new group");
                        // log($"BEFORE: {groups}");
                        groups.Add(new CellList { this[ x+(y+1)*w ] });
                        this[ x+(y+1)*w ].group = groups[ groups.Count-1 ]; 
                        // log($"AFTER : {groups}");
                    }

                    // PrintMaze(y, y+1);
                    // 6. Repeat until the last row is reached.
                    // log($"Line {y} done.");
                    // log($"{groups}");
                }

                // 7. For the last row, join all adjacent cells that do not share
                //    a set, and omit the vertical connections, and we're done!
                for (int x = 1, y=h-1; x < w; x++) {
                    if (this[ x-1 + y*w ].group == this[ x + y*w ].group) {
                        continue;
                    }
                    log($"Joining ({this[ x-1 + y*w ]}) <-> ({this[ x + y*w ]})");
                    this[x + y*w].LinkTo(this[ x-1 + y*w ], log); 
                }
                PrintMaze(0, h);
            
            }
        }
    }

}
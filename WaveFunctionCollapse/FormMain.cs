using System.Drawing.Drawing2D;

namespace WaveFunctionCollapse {
    public partial class FormMain : Form {
        enum Modes {
            TilesView,
            Generator
        }

        Modes mode = Modes.Generator;
        readonly List<Tile> tiles = new();

        class Cell {
            public Tile Tile;
            public int Entropy;
            public bool Collapsed;
            public int Index;
        }

        int scale = 2;
        int gridSize = 38;
        Cell[] grid;

        public FormMain() {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                            ControlStyles.UserPaint |
                            ControlStyles.OptimizedDoubleBuffer |
                            ControlStyles.ResizeRedraw, true);

            this.Paint += Render;
            this.WindowState = FormWindowState.Maximized;

            this.BackColor = Color.FromArgb(33, 33, 33);
        }

        private void FormMain_Load(object sender, EventArgs e) {
            foreach(FileInfo file in (new DirectoryInfo("../../samples/circuit").GetFiles("*.png"))) {
                tiles.AddRange(TilesFactory.GenerateTiles(file.FullName));
            }

            grid = new Cell[gridSize * gridSize];
            for(int i = 0; i < grid.Length; i++) {
                Cell c = new() {
                    Index = i,
                    Entropy = tiles.Count
                };
                grid[i] = c;
            }

            Task.Run(() => {
                while(true) {
                    this.Invalidate();
                    Thread.Sleep(30);
                }
            });

            if(mode == Modes.Generator) {
                int f = 0;
                int mf = tiles.Count / 4;

                Task.Run(() => {
                    Random rnd = new();

                    while(true) {
                        Cell[] avCells = (from cell in grid
                                          where !cell.Collapsed
                                          orderby cell.Entropy ascending
                                          select cell).ToArray();

                        if(avCells.Length > 0) {
                            int minEntropy = avCells[0].Entropy;
                            avCells = avCells.Where(c => c.Entropy == minEntropy).ToArray();
                            List<int> triedTiles = new();

                            while(true) {
                                Cell selCell = avCells[rnd.Next(avCells.Length)];
                                List<(Cell cell, int d)> srndCells = GetSurroundingCells(selCell);

                                int ti = rnd.Next(tiles.Count);
                                if(triedTiles.Contains(ti)) continue;
                                triedTiles.Add(ti);

                                Tile t = tiles[ti];

                                if(CanFit(t, srndCells)) {
                                    foreach((Cell cell, int d) sr1 in srndCells) {
                                        sr1.cell.Entropy -= 3;
                                        foreach((Cell cell, int d) sr2 in GetSurroundingCells(sr1.cell)) {
                                            sr2.cell.Entropy -= 2;
                                            foreach((Cell cell, int d) sr3 in GetSurroundingCells(sr2.cell)) {
                                                sr3.cell.Entropy -= 1;
                                            }
                                        }
                                    }

                                    selCell.Tile = t;
                                    selCell.Collapsed = true;

                                    break;
                                } else if(triedTiles.Count == tiles.Count) {
                                    (Cell cell, int d) tc = srndCells[rnd.Next(srndCells.Count)];

                                    tc.cell.Collapsed = false;
                                    tc.cell.Entropy = tiles.Count - GetSurroundingCells(tc.cell).Count;

                                    break;
                                }
                            }
                        } else {
                            break;
                        }

                        if(f == 0) Thread.Sleep(1);
                        f = (f + 1) % mf;
                    }
                });
            }
        }

        private bool CanFit(Tile t, List<(Cell cell, int d)> srndCells) {
            foreach((Cell cell, int d) srndCell in srndCells) {
                if(!srndCell.cell.Collapsed) continue;
                int[] scca = srndCell.cell.Tile.ColorArray;

                switch(srndCell.d) {
                    case 0: // srndCell is at the Top
                        if(t.ColorArray[0] != scca[2]) return false;
                        break;
                    case 1: // srndCells is at the Right
                        if(t.ColorArray[1] != scca[3]) return false;
                        break;
                    case 2: // srndCells is at the Bottom
                        if(t.ColorArray[2] != scca[0]) return false;
                        break;
                    case 3: // srndCells is at the Left
                        if(t.ColorArray[3] != scca[1]) return false;
                        break;
                }
            }

            return true;
        }

        private List<(Cell, int)> GetSurroundingCells(Cell selCell) {
            List<(Cell, int)> cells = new();
            (int x, int y)[] indexes = {
                (0, -1),
                (1, 0),
                (0, 1),
                (-1, 0)
            };

            int y0 = selCell.Index / gridSize;
            int x0 = selCell.Index - y0 * gridSize;

            for(int i = 0; i < indexes.Length; i++) {
                int x = x0 + indexes[i].x;
                int y = y0 + indexes[i].y;

                if(x >= 0 && x < gridSize && y >= 0 && y < gridSize) {
                    Cell cell = grid[x + y * gridSize];
                    cells.Add((cell, i));
                }
            }

            return cells;
        }

        private void Render(object? sender, PaintEventArgs e) {
            Graphics g = e.Graphics;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            int w = tiles[0].Bitmap.Width;
            int h = tiles[0].Bitmap.Height;

            switch(mode) {
                case Modes.Generator:
                    // Draw Collapsed Cells
                    for(int i = 0; i < grid.Length; i++) {
                        if(grid[i].Collapsed) {
                            int y = i / gridSize;
                            int x = i - y * gridSize;

                            g.DrawImage(grid[i].Tile.Bitmap,
                                x * w * scale + 1,
                                y * h * scale + 1,
                                w * scale + scale / 2,
                                h * scale + scale / 2);
                        }
                    }

                    // Draw Grid
                    for(int y = 0; y < gridSize; y++) {
                        for(int x = 0; x < gridSize; x++) {
                            g.DrawRectangle(Pens.Black,
                                x * w * scale,
                                y * h * scale,
                                w * scale,
                                h * scale);
                        }
                    }

                    break;

                case Modes.TilesView:
                    // Show Tiles Rotations
                    string lastName = "";
                    int j = (h - 2) * scale;
                    for(int ti = 0; ti < tiles.Count; ti++) {
                        string name = tiles[ti].FileName.Split('.')[0];
                        if(lastName != name) {
                            lastName = name;
                            int i = (w - 2) * scale;

                            var rotations = from t in tiles where t.FileName.StartsWith(name) select t;
                            foreach(var r in rotations) {
                                g.DrawImage(r.Bitmap, i, j, w * scale, h * scale);
                                i += w * scale + (h - 2) * scale;
                            }

                            j += h * scale + (h - 2) * scale;
                        }
                    }

                    break;
            }
        }
    }
}
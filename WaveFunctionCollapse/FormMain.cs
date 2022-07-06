using System.Drawing.Drawing2D;

namespace WaveFunctionCollapse {
    public partial class FormMain : Form {
        enum Modes {
            TilesView,
            Generator
        }

        Modes mode = Modes.Generator;
        readonly List<Tile> tiles = new();

        int scale = 2;
        int gridWidth = 68;
        int gridHeight = 39;
        Cell[,] grid;

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
            LoadTiles("circuit");
            InitGrid();
            RendererLoop();
            GeneratorLoop();            
        }

        private void GeneratorLoop() {
            if(mode == Modes.Generator) {
                int f = 0;
                int mf = tiles.Count / 8;

                Task.Run(() => {
                    Random rnd = new();

                    while(true) {
                        List<Cell> avCells = new();
                        foreach(var c in grid) if(!c.Collapsed) avCells.Add(c);
                        avCells.Sort((c1, c2) => c1.Entropy - c2.Entropy);

                        if(avCells.Count > 0) {
                            int minEntropy = avCells[0].Entropy;
                            avCells = avCells.Where(c => c.Entropy == minEntropy).ToList();
                            List<int> triedTiles = new();

                            while(true) {
                                Cell selCell = avCells[rnd.Next(avCells.Count)];
                                List<(Cell cell, int d)> srndCells = GetSurroundingCells(selCell);

                                int ti = rnd.Next(tiles.Count);
                                if(triedTiles.Contains(ti)) continue;
                                triedTiles.Add(ti);

                                Tile t = tiles[ti];

                                if(CanFit(t, srndCells)) {
                                    AdjustEntropy(srndCells, 2);

                                    selCell.Tile = t;
                                    selCell.Collapsed = true;

                                    break;
                                } else if(triedTiles.Count == tiles.Count) {
                                    (Cell cell, int d) tc = srndCells[rnd.Next(srndCells.Count)];

                                    tc.cell.Collapsed = false;
                                    tc.cell.Entropy = tiles.Count - GetSurroundingCells(tc.cell).Count + 1;

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

        private void RendererLoop() {
            Task.Run(() => {
                while(true) {
                    this.Invalidate();
                    Thread.Sleep(30);
                }
            });
        }

        private void InitGrid() {
            grid = new Cell[gridWidth, gridHeight];
            for(int y = 0; y < gridHeight; y++) {
                for(int x = 0; x < gridWidth; x++) {
                    grid[x, y] = new Cell() {
                        Position = new Point(x, y),
                        Entropy = tiles.Count
                    };
                }
            }
        }

        private void LoadTiles(string set) {
            foreach(FileInfo file in (new DirectoryInfo($"../../samples/{set}").GetFiles("*.png"))) {
                tiles.AddRange(TilesFactory.GenerateTiles(file.FullName));
            }
        }

        private void AdjustEntropy(List<(Cell cell, int d)> srndCells, int e = 3) {
            if(e == 0) return;

            foreach((Cell cell, int d) sr1 in srndCells) {
                sr1.cell.Entropy -= e;
                AdjustEntropy(GetSurroundingCells(sr1.cell), e - 1);
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

            for(int i = 0; i < indexes.Length; i++) {
                int x = selCell.Position.X + indexes[i].x;
                int y = selCell.Position.Y + indexes[i].y;

                if(x >= 0 && x < gridWidth && y >= 0 && y < gridHeight) {
                    cells.Add((grid[x, y], i));
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
                    for(int y = 0; y < gridHeight; y++) {
                        for(int x = 0; x < gridWidth; x++) {
                            if(grid[x, y].Collapsed) {
                                g.DrawImage(grid[x, y].Tile.Bitmap,
                                    x * w * scale + 1,
                                    y * h * scale + 1,
                                    w * scale + scale / 2,
                                    h * scale + scale / 2);
                            }

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

                            var rotations = from t in tiles where t.FileName.StartsWith(name + ".") select t;
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
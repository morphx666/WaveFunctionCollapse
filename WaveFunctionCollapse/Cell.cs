using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveFunctionCollapse {
    internal class Cell {
        public Tile Tile;
        public int Entropy;
        public bool Collapsed;
        public Point Position;
    }
}

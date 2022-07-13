using System.Security.Cryptography.Xml;

namespace WaveFunctionCollapse {
    internal class TilesFactory {
        static public double Tolerance = 0.35;
        static readonly List<int[]> ColorsLUT = new();

        static RotateFlipType[] rotations = {
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.Rotate90FlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate270FlipNone,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipY
        };

        public static List<Tile> GenerateTiles(string fileName) {
            List<Tile> tiles = new();

            for(int i = 0; i < rotations.Length; i++) {
                using(Bitmap bmp = (Bitmap)Image.FromFile(fileName)) {
                    bmp.RotateFlip(rotations[i]);

                    int[] cl = GetColorsLUT(bmp);
                    if(!RotationExists(tiles, cl)) {
                        FileInfo fi = new(fileName);
                        tiles.Add(new Tile($"{fi.Name} [{rotations[i]}]", (Bitmap)bmp.Clone(), cl));
                    }
                }
            }

            tiles.ForEach(t => {
                t.Rotations = tiles.Count;
                t.PrimaryColor = GetPrimaryColor(t);
            });

            return tiles;
        }

        private static int GetPrimaryColor(Tile t) {
            Dictionary<int, int> dc = new();

            for(int y = 0; y < t.Bitmap.Height; y++) {
                for(int x = 0; x < t.Bitmap.Width; x++) {
                    int c = t.Bitmap.GetPixel(x, y).ToArgb();
                    if(dc.ContainsKey(c)) {
                        dc[c]++;
                    } else {
                        dc.Add(c, 1);
                    }
                }
            }

            return dc.OrderByDescending(d => d.Value).First().Key;
        }

        private static int[] GetColorsLUT(Bitmap bmp) {
            int[] result = new int[4];

            int[] top = new int[bmp.Width];
            int[] right = new int[bmp.Height];
            int[] bottom = new int[bmp.Height];
            int[] left = new int[bmp.Height];

            for(int i = 0; i < bmp.Width; i++) {
                top[i] = bmp.GetPixel(i, 0).ToArgb();
                bottom[i] = bmp.GetPixel(i, bmp.Height - 1).ToArgb();
            }

            for(int j = 0; j < bmp.Height; j++) {
                left[j] = bmp.GetPixel(0, j).ToArgb();
                right[j] = bmp.GetPixel(bmp.Width - 1, j).ToArgb();
            }

            int idx = FindColor(top);
            if(idx == -1) {
                ColorsLUT.Add(top);
                idx = ColorsLUT.Count - 1;
            }
            result[0] = idx;

            idx = FindColor(right);
            if(idx == -1) {
                ColorsLUT.Add(right);
                idx = ColorsLUT.Count - 1;
            }
            result[1] = idx;

            idx = FindColor(bottom);
            if(idx == -1) {
                ColorsLUT.Add(bottom);
                idx = ColorsLUT.Count - 1;
            }
            result[2] = idx;

            idx = FindColor(left);
            if(idx == -1) {
                ColorsLUT.Add(left);
                idx = ColorsLUT.Count - 1;
            }
            result[3] = idx;

            return result;
        }

        private static int FindColor(int[] color) {
            for(int i = 0; i < ColorsLUT.Count; i++) {
                if(ColorsMatch(color, ColorsLUT[i])) return i;
            }
            return -1;
        }

        public static bool ColorsMatch(int[] color1, int[] color2) {
            if(color1.Length != color2.Length) return false;

            double error = 0;
            for(int i = 0; i < color1.Length; i++) {
                Color c1 = Color.FromArgb(color1[i]);
                float h1 = c1.GetHue() + c1.GetBrightness() + c1.GetSaturation();
                Color c2 = Color.FromArgb(color2[i]);
                float h2 = c2.GetHue() + c2.GetBrightness() + c2.GetSaturation();

                error += Math.Abs(h1 - h2);
            }
            return (error / 1000.0) <= Tolerance;
        }

        private static bool RotationExists(List<Tile> tiles, int[] cl) {
            foreach(Tile t in tiles) {
                int c = 0;
                for(int i = 0; i < t.ColorArray.Length; i++) {
                    if(t.ColorArray[i] == cl[i]) c++;
                }
                if(c == t.ColorArray.Length) return true;
            }
            return false;
        }
    }
}
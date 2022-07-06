namespace WaveFunctionCollapse {
    internal class TilesFactory {
        static public double Tolerance = 0.35;
        static readonly List<int[]> ColorsLUT = new();

        public static List<Tile> GenerateTiles(string fileName) {
            List<Tile> tiles = new();
            RotateFlipType[] rotations = {
                RotateFlipType.RotateNoneFlipNone,
                RotateFlipType.Rotate90FlipNone,
                RotateFlipType.Rotate180FlipNone,
                RotateFlipType.Rotate270FlipNone,
                RotateFlipType.RotateNoneFlipX,
                RotateFlipType.RotateNoneFlipY
            };

            for(int i = 0; i < rotations.Length; i++) {
                using(Bitmap bmp = (Bitmap)Image.FromFile(fileName)) {
                    bmp.RotateFlip(rotations[i]);

                    int[] cl = GenerateColorsLUT(bmp);
                    if(!RotationExists(tiles, cl)) {
                        FileInfo fi = new(fileName);
                        tiles.Add(new Tile($"{fi.Name} [{rotations[i]}]", (Bitmap)bmp.Clone(), cl));
                    }
                }
            }

            return tiles;
        }

        private static int[] GenerateColorsLUT(Bitmap bmp) {
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
            double error = 0;
            if(color1.Length != color2.Length) return false;
            for(int i = 0; i < color1.Length; i++) {
                if(color1[i] != color2[i]) {
                    error += Math.Abs(Color.FromArgb(color1[i]).GetHue() - Color.FromArgb(color2[i]).GetHue());
                }
            }
            return (error / 1000.0) < Tolerance;
        }

        private static bool RotationExists(List<Tile> tiles, int[] cl) {
            foreach(Tile t in tiles) {
                for(int i = 0; i < t.ColorArray.Length; i++) {
                    if(!ColorsMatch(t.ColorArray, cl)) return false;
                }
            }
            return false;
        }
    }
}
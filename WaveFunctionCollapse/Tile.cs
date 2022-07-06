namespace WaveFunctionCollapse {
    internal class Tile {
        public Bitmap Bitmap;
        public int[] ColorArray;
        public string FileName;
        public int Rotations;

        public Tile(string fileName, Bitmap bitmap, int[] colorArray) {
            FileName = fileName;
            Bitmap = bitmap;
            ColorArray = colorArray;
        }
    }
}

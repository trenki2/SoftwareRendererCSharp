namespace Renderer
{
    public class VertexCache
    {
        private const int VertexCacheSize = 16;

        private int[] inputIndex = new int[VertexCacheSize];
        private int[] outputIndex = new int[VertexCacheSize];

        public VertexCache()
        {
            clear();
        }

        public void clear()
        {
            for (int i = 0; i < VertexCacheSize; i++)
                inputIndex[i] = -1;
        }

        public void set(int inIndex, int outIndex)
        {
            int cacheIndex = inIndex % VertexCacheSize;
            inputIndex[cacheIndex] = inIndex;
            outputIndex[cacheIndex] = outIndex;
        }

        public int lookup(int inIndex)
        {
            int cacheIndex = inIndex % VertexCacheSize;
            if (inputIndex[cacheIndex] == inIndex)
                return outputIndex[cacheIndex];
            else
                return -1;
        }
    }
}
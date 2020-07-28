namespace Renderer
{
    public struct Constants
    {
        /// Rendering Block size.
        public const int BlockSize = 8;

        /// Maximum affine variables used for interpolation across the triangle.
        public const int MaxAVars = 16;

        /// Maximum perspective variables used for interpolation across the triangle.
        public const int MaxPVars = 16;
    }
}
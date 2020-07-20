namespace Renderer
{
    /// Vertex input structure for the Rasterizer. Output from the VertexProcessor.
    public unsafe struct RasterizerVertex
    {
        public float x; ///< The x component.
		public float y; ///< The y component.
		public float z; ///< The z component.
		public float w; ///< The w component.

        /// Affine variables.
        public fixed float avar[Constants.MaxAVars];

        /// Perspective variables.
        public fixed float pvar[Constants.MaxPVars];
    };
}
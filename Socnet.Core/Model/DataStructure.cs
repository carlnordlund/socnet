namespace Socnet.Core.Model
{
    /// <summary>
    /// Abstract base class for all data structures that can be stored in a <see cref="Dataset"/>.
    /// The name of the concrete class is used as the data type name shown to the user, so
    /// concrete classes must keep their names (Actorset, Matrix, Partition, etc).
    /// </summary>
    public abstract class DataStructure
    {
        /// <summary>
        /// The name of the structure (i.e. the name of the variable it is stored under).
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The data type name of this structure, as shown in the console.
        /// </summary>
        public string DataType => GetType().Name;

        /// <summary>
        /// A short textual description of the size of the structure.
        /// </summary>
        public abstract string Size { get; }

        /// <summary>
        /// Adds lines describing the content of this structure. Lines prefixed with ':' are always
        /// shown in the console, also in silent mode.
        /// </summary>
        /// <param name="content">List of lines to add to.</param>
        public abstract void GetContent(List<string> content);

        /// <summary>
        /// Returns the full view of this structure: a header line followed by its content.
        /// </summary>
        public List<string> View
        {
            get
            {
                List<string> content = [$"Name:{Name}\tDatatype:{DataType}\tSize:{Size}"];
                GetContent(content);
                return content;
            }
        }
    }
}

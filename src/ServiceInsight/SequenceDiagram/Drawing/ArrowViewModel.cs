namespace ServiceInsight.SequenceDiagram.Drawing
{
    using System.Diagnostics;

    [DebuggerDisplay("{Type}->{Title}")]
    public class ArrowViewModel : UmlViewModel
    {
        public int Vector { get; set; }
        public int Length { get; set; }
        public ArrowType Type { get; set; }

    }
}
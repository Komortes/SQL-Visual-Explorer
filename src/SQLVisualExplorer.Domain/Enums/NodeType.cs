namespace SQLVisualExplorer.Domain.Enums;

public enum NodeType
{
    Unknown,
    SeqScan,
    IndexScan,
    IndexOnlyScan,
    BitmapHeapScan,
    NestedLoop,
    HashJoin,
    MergeJoin,
    Sort,
    Aggregate
}

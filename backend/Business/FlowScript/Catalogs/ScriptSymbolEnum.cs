namespace Business.FlowScript.Catalogs
{
    // What the grammar reads that is not a step: the quotes and the comment mark, and the labels of
    // the header and its sections.
    internal enum ScriptSymbolEnum
    {
        QUOTE_OPEN,
        QUOTE_CLOSE,
        COMMENT,
        FLOW,
        ID,
        SIZES,
        AREAS,
        POINTS,
        INPUTS,
        TEMPLATES,
        STEPS,
    }
}

namespace Business.FlowScript.Catalogs
{
    // What the grammar reads that is not a step: the marks around text and comments, and the
    // labels of the header and its sections.
    internal enum ScriptSymbolEnum
    {
        TEXT_START,
        TEXT_END,
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

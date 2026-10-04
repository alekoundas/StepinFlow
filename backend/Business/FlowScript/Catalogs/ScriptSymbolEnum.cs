namespace Business.FlowScript.Catalogs
{
    internal enum ScriptSymbolEnum
    {

        // <[ ]>
        QUOTE_OPEN,
        QUOTE_CLOSE,

        // #
        COMMENT,

        // Flow
        FLOWFIELD_NAME,
        FLOWFIELD_ID,
        FLOWFIELD_SIZES,

        // Flow references
        AREAS,
        POINTS,
        CSV_COLUMNS,
        TEMPLATES,
        STEPS,

        // FlowArea
        INSIDE,
        ON_SCREEN,
        RATIO,
        OFFSET,
        AT,
    }
}

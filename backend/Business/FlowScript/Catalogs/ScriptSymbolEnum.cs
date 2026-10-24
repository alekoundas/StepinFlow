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
        FLOWFIELD_DESCRIPTION,

        // Flow references
        AREAS,
        POINTS,
        CSV_COLUMNS,
        TEMPLATES,
        STEPS,

        // FlowArea
        MAIN,
        INSIDE,
        ON_SCREEN,
        RATIO,
        OFFSET,
        AT,
        WINDOW,
        MONITOR,
        PRIMARY,
        PROCESS,
        TITLE,
        SIZE,
        SCALES_WITH,
        TAB,
        WITH_FRAME,

        // FlowCsvColumn
        DEFAULT,
        SECRET,

        // FlowStepTemplate
        CLICK,
        CAPTURED,

        // FlowStep
        TO,
        OUT_OF,
        POINT,
        NOWHERE,
        MATCH,
        TEMPLATE,
        ACCURACY,
        REQUIRED,
        IN,
        OPENS,
        KEEP,
        TIMEOUT,
        NO_TIMEOUT,
        AND,
        TIMES,
        FOREVER,
        PASSED,
        FAILED,
        MILLISECONDS, // Written onto a number: 800ms, 120dpi, 1920x1080
        DPI,
        SIZE_SEPARATOR,
    }
}

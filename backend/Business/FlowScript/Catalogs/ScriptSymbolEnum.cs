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
        WINDOW,
        MONITOR,
        PRIMARY,
        PROCESS,
        TITLE,
        SIZE,
        SCALES_WITH,

        // FlowCsvColumn
        SECRET,

        // FlowStepTemplate
        CLICK,
        CAPTURED,

        // FlowStep
        TO,
        POINT,
        MATCH,
        TEMPLATE,
        ACCURACY,
        REQUIRED,
        IN,
        KEEP,
        TIMEOUT,
        NO_TIMEOUT,
        AND,
        TIMES,
        FOREVER,
        EACH,
        PASSED,
        FAILED,

        // Written onto a number: 800ms, 120dpi, 1920x1080
        MILLISECONDS,
        DPI,
        SIZE_SEPARATOR,
    }
}

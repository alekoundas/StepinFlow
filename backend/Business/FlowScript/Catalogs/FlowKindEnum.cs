namespace Business.FlowScript.Catalogs
{
    /// <summary>
    /// The words of a flow's <c>Kind:</c> line. An automation or a QA test, either of which can also
    /// be a sub-flow: the database keeps them as <c>IsQaTest</c> and <c>IsSubFlow</c>.
    /// </summary>
    internal enum FlowKindEnum
    {
        AUTOMATION,
        QA_TEST,
        SUB_FLOW,
    }
}

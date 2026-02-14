namespace Core.Enums
{
    // https://github.com/opencv/opencv/blob/d3bc563c6e01c2bc153f23e7393322a95c7d3974/modules/imgproc/include/opencv2/imgproc.hpp#L3672
    public enum TemplateMatchModeEnum
    {
        SqDiff,
        SqDiffNormed,
        CCorr,
        CCorrNormed,
        CCoeff,
        CCoeffNormed
    }
}

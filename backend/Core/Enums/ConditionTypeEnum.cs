namespace Core.Enums
{
    public enum ConditionTypeEnum
    {
        // Text
        EQUALS,
        NOT_EQUALS,
        CONTAINS,
        NOT_CONTAINS,
        MATCHES_REGEX,
        IS_EMPTY,
        IS_NOT_EMPTY,

        // Numeric. Text that will not parse as a number is a failure, not a false result.
        GREATER_THAN,
        LESS_THAN,
        BETWEEN,

        // Length in characters, against a whole number. Anything else is a failure, as above.
        LENGTH_EQUALS,
        LENGTH_GREATER_THAN,
        LENGTH_LESS_THAN,
    }
}

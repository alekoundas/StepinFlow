using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>What the tester decided about one recorded action, in Review.</summary>
    public class ReviewedActionDto
    {
        public int ActionIndex { get; set; }
        public ReviewChoiceEnum Choice { get; set; }

        // The element's rectangle as reviewed, in the main area's pixels. Null for an action with none.
        // Searched by its picture, the template is cut along it; by its words, they are read inside it.
        public int? ElementX { get; set; }
        public int? ElementY { get; set; }
        public int? ElementWidth { get; set; }
        public int? ElementHeight { get; set; }

        public SearchByEnum SearchBy { get; set; }

        /// <summary>For a search by its words: the words it waits to read, or the first number its rule compares with.</summary>
        public string? SearchText { get; set; }

        /// <summary>For a check by its words: how the words read are compared. Null is contains, as a click's search does.</summary>
        public ConditionTypeEnum? ConditionType { get; set; }

        /// <summary>For a check by its words between two numbers: the upper one.</summary>
        public string? ConditionTextEnd { get; set; }

        /// <summary>For a check: a Notify first in its Success branch, with the check's result.</summary>
        public bool NotifyOnSuccess { get; set; }

        /// <summary>For a check: a Notify before the execution ends in its Failure branch, with why it failed.</summary>
        public bool NotifyOnFailure { get; set; }

        /// <summary>For a check's notifications, or a notification marked instead of a click: the bot it sends through.</summary>
        public int? DiscordBotId { get; set; }

        public string? SuccessMessage { get; set; }
        public string? FailureMessage { get; set; }

        /// <summary>For a notification marked instead of a click: what it says.</summary>
        public string? Message { get; set; }

        /// <summary>For a notification marked instead of a click: the words inside the rectangle are read first, and sent with it.</summary>
        public bool IsReadingText { get; set; }

        /// <summary>For a notification that reads the words: a pattern for the part of them it keeps. Empty keeps them all.</summary>
        public string? ReadTextPattern { get; set; }

        /// <summary>For a wait marked instead of a click: how long.</summary>
        public int WaitMilliseconds { get; set; }

        /// <summary>For typed text that becomes an input, or an input's value typed: its name.</summary>
        public string? InputName { get; set; }

        /// <summary>For an input: never written to a file, never shown to a model.</summary>
        public bool IsSecret { get; set; }

        /// <summary>For a command: what it executes. For an application: the program that opens it.</summary>
        public string? CommandText { get; set; }

        /// <summary>
        /// For a stage marked while recording, its name. For any other action, a stage the tester
        /// named as beginning there - where a new page began. Empty is no stage.
        /// </summary>
        public string? StageName { get; set; }
    }
}

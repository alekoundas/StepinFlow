using System.Drawing;
using Core.Enums;

namespace Business.Ai.Vision.Models
{
    /// <summary>The element under a click, in the pixels of the screenshot it was found in.</summary>
    public sealed class LocatedElement
    {
        public Rectangle Bounds { get; init; }
        public ElementFoundByEnum FoundBy { get; init; }

        /// <summary>How sure the model was, from 0 to 1. Null for the box around the click.</summary>
        public float? Score { get; init; }

        /// <summary>Why it is the box around the click. Null when the model found it.</summary>
        public string? Note { get; init; }
    }
}

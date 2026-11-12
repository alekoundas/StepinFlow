namespace Business.Recording.Draft.Models
{
    // What one entry of a recording's draft is, which says where the layout puts it.
    public enum DraftItemKindEnum
    {
        STEP,       // A move, a click, typed text, a key: done where steps go now
        SEARCH,     // A wait for what was pressed, or a check: what follows goes in its Success branch
        STAGE,      // A stage marker, at the top of the flow
        EVERY,      // A Find All Images, with what is done at each hit
    }
}

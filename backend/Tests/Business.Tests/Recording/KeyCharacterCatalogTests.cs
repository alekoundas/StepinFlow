using Business.Recording.Catalogs;
using Core.Enums.Business;

namespace Business.Tests.Recording
{
    public sealed class KeyCharacterCatalogTests
    {
        [Fact]
        public void Every_letter_types_itself_small_and_its_capital()
        {
            foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                KeyCodeEnum key = Enum.Parse<KeyCodeEnum>(letter.ToString());

                KeyCharacterCatalog.GetCharacter(key, isCapital: false).ShouldBe(char.ToLowerInvariant(letter).ToString());
                KeyCharacterCatalog.GetCharacter(key, isCapital: true).ShouldBe(letter.ToString());
            }
        }

        [Fact]
        public void A_number_row_key_and_its_numpad_twin_type_the_same_digit()
        {
            KeyCharacterCatalog.GetCharacter(KeyCodeEnum.Num7, isCapital: false).ShouldBe("7");
            KeyCharacterCatalog.GetCharacter(KeyCodeEnum.Numpad7, isCapital: false).ShouldBe("7");
        }

        // What Shift makes of a digit belongs to the layout, so a capital asked for is the digit.
        [Fact]
        public void A_key_with_no_capital_types_itself_when_one_is_asked_for()
        {
            KeyCharacterCatalog.GetCharacter(KeyCodeEnum.Num7, isCapital: true).ShouldBe("7");
        }

        [Fact]
        public void Only_letters_have_a_capital()
        {
            KeyCharacterCatalog.All.Where(x => x.Capital != null).Count().ShouldBe(26);
        }

        [Fact]
        public void A_key_that_does_something_instead_types_nothing()
        {
            KeyCharacterCatalog.GetCharacter(KeyCodeEnum.Enter, isCapital: false).ShouldBeNull();
        }

        [Fact]
        public void No_key_is_listed_twice()
        {
            KeyCharacterCatalog.All.Select(x => x.KeyCode).ShouldBeUnique();
        }
    }
}

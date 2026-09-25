using Core.Helpers;

namespace Core.Tests.Helpers
{
    public sealed class FlowNameHelperTests
    {
        [Fact]
        public void A_free_name_is_kept_trimmed()
        {
            FlowNameHelper.MakeUnique("  Find login  ", ["Other"]).ShouldBe("Find login");
        }

        [Fact]
        public void A_taken_name_gets_the_first_free_number_whatever_the_case()
        {
            FlowNameHelper.MakeUnique("Find login", ["find LOGIN", "Find login 2"]).ShouldBe("Find login 3");
        }

        [Fact]
        public void Duplicates_are_found_whatever_the_case_and_surrounding_space()
        {
            FlowNameHelper.Duplicates(["Login", " login ", "Cart", "LOGIN"]).ShouldBe(["Login"]);
        }

        [Fact]
        public void Blank_names_are_not_duplicates_of_each_other()
        {
            FlowNameHelper.Duplicates(["", " ", "", "Cart"]).ShouldBeEmpty();
        }
    }
}

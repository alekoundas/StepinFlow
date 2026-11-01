using Core.Helpers;

namespace Core.Tests.Helpers
{
    /// <summary>
    /// Which tab a browser tab area means. The address bar leaves out https:// and www., and the
    /// area's address is often pasted from it with them.
    /// </summary>
    public sealed class TabAddressHelperTests
    {
        [Theory]
        [InlineData("tradingeconomics.com/commodity/crude-oil", "https://tradingeconomics.com/commodity/crude-oil")]
        [InlineData("https://www.shop.example.com/cart", "shop.example.com/cart")]
        [InlineData("Shop.Example.com/Cart", "shop.example.com/cart")]
        [InlineData("shop.example.com/cart?item=4", "shop.example.com/cart")]
        [InlineData("shop.example.com/cart/checkout", "shop.example.com/cart")]
        [InlineData("shop.example.com/cart#top", "shop.example.com/cart")]
        [InlineData("shop.example.com", "shop.example.com/")]
        [InlineData("localhost:5173/login", "http://localhost")]
        public void A_tab_at_the_address_or_a_page_under_it_is_the_one(string tab, string area)
        {
            TabAddressHelper.Matches(tab, area).ShouldBeTrue();
        }

        [Theory]
        [InlineData("shop.example.community", "shop.example.com")]
        [InlineData("shop.example.com/cartoons", "shop.example.com/cart")]
        [InlineData("shop.example.com", "shop.example.com/cart")]
        [InlineData("news.example.com", "")]
        public void Another_address_is_another_tab(string tab, string area)
        {
            TabAddressHelper.Matches(tab, area).ShouldBeFalse();
        }

        [Theory]
        [InlineData("shop.example.com/cart", "https://shop.example.com/cart")]
        [InlineData("  http://localhost:5173 ", "http://localhost:5173")]
        [InlineData("file:///C:/report.html", "file:///C:/report.html")]
        public void An_address_with_no_scheme_opens_over_https(string area, string opened)
        {
            TabAddressHelper.ToOpen(area).ShouldBe(opened);
        }
    }
}

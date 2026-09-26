namespace Business.Tests
{
    /// <summary>
    /// The running test's cancellation token, imported with <c>using static</c> so a call reads
    /// <c>ExecuteAsync(step, cache, Ct)</c> rather than repeating xUnit's ambient context.
    ///
    /// A property and never a field: the token belongs to the test that is running, and a static
    /// field would capture the first one and hand it to every test after it.
    /// </summary>
    public static class TestToken
    {
        public static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }
    }
}

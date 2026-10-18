using Xunit;

// Run database test collections sequentially to limit LocalDB memory and connection pressure.
// Explicit Task.WhenAll requests inside concurrency tests still run together.
// xUnit.net (2026): https://xunit.net/docs/running-tests-in-parallel
// Accessed: 18 October 2026.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

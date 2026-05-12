using Xunit;

// SQLite in-memory connections + EF Core's AsyncLocal-based change tracking don't
// always play nicely under xunit's default cross-class parallelism. The unit
// suite is fast enough single-threaded that the safety is worth the determinism.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

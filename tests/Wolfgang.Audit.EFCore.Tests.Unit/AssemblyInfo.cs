using Xunit;

// SQLite in-memory connections shared across tests don't play nicely under xunit's
// default cross-class parallelism. The unit suite is fast enough single-threaded
// that the safety is worth the determinism.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

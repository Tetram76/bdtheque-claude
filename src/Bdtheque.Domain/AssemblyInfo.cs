using System.Runtime.CompilerServices;

// The operations kept internal so that the application cannot bypass the atomic ones (e.g. adding a
// visual at a rank of its choosing) stay testable on their own.
[assembly: InternalsVisibleTo("Bdtheque.Domain.Tests")]

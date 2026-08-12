This is because I got fed up with EF Core concurrency exceptions that shouldn't happen, that I couldn't figure out. I still want to use EF Core for queries and migrations. But for single-row inserts and updates where I'm getting bogus concurrency exceptions, I wanted an option to insert or update a row directly. I also didn't want raw inline SQL. There are ways to make that safe, but it requires too much effort. I wanted something no more complex than passing the original entity, with no inline SQL exposed.

There are plenty of existing libraries and approaches for this, but I had something very specific in mind for the behavior that I wanted. I opted to use Dapper as the intermediate library as it has nice parameter handling, and lets you abstract the raw SQL itself, presenting a clean, safe method.

The core of this are some extension methods: [DbContextExtensions](PlainCrud.Extensions/DbContextExtensions.cs).

## "Bogus Concurrency Exceptions"?

What do I mean by "bogus concurrency exceptions"? In my case, I'm using MySQL in AWS. For reasons unkown, when it executes `SELECT ROW_COUNT()` after a successful update, it may return 0 instead of the expected 1. EF Core's SaveChanges method mistakes that 0 as an unsuccessful update, believing that an attempt was made to update a stale row. Even with no concurrency token in use, and a clearly successful update, the affected row count is reported as zero. There's no way to disable the EF Core logic here, so I have to bypass it with my own SQL. I've worked around this before with ExecuteUpdateAsync. For general-purpose entity updates, that's not practical because it doesn't handle inserts and requires explicit column references.

# Security Guidelines for Data Layer

## SQL Injection Prevention

### Entity Framework Core Protection

This project uses Entity Framework Core which provides built-in protection against SQL injection attacks through:

1. **Parameterized Queries**: All LINQ queries are automatically converted to parameterized SQL queries
2. **No String Concatenation**: We never concatenate user input directly into SQL strings
3. **Type Safety**: Strong typing prevents injection through type mismatches

### Safe Query Examples

```csharp
// ✅ SAFE - Parameterized query
var user = await _context.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();

// ✅ SAFE - Parameterized with multiple conditions
var jobs = await _context.JobPostings
    .Where(j => j.IsActive && j.Sector == sector)
    .ToListAsync();

// ✅ SAFE - Using FromSqlRaw with parameters
var results = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", email)
    .ToListAsync();
```

### Unsafe Patterns to AVOID

```csharp
// ❌ UNSAFE - String concatenation (DO NOT USE)
var query = $"SELECT * FROM Users WHERE Email = '{email}'";
var user = await _context.Users.FromSqlRaw(query).FirstOrDefaultAsync();

// ❌ UNSAFE - String interpolation in raw SQL (DO NOT USE)
var user = await _context.Users
    .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'")
    .FirstOrDefaultAsync();
```

### Code Review Checklist

When reviewing data access code, ensure:

- [ ] No use of `FromSqlRaw` or `FromSqlInterpolated` with string concatenation
- [ ] All user inputs are passed as parameters, not concatenated
- [ ] LINQ queries are used wherever possible
- [ ] Any raw SQL uses parameterized queries with `{0}`, `{1}` placeholders
- [ ] No dynamic SQL generation from user input

### Additional Security Measures

1. **Least Privilege**: Database user has minimal required permissions
2. **Input Validation**: All DTOs have validation attributes
3. **Sanitization**: User inputs are validated before reaching the database
4. **Audit Logging**: All database operations are logged for security audits

## References

- [EF Core SQL Injection Prevention](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries)
- [OWASP SQL Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)

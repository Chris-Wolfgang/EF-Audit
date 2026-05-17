# Web API example

Demonstrates the on-behalf-of audit pattern. The application's service account
(`svc-orders`) is recorded as `AuditHeader.UserId`; the authenticated human is
recorded as `AuditHeader.OnBehalfOfUserId`.

```
dotnet run --project examples/Wolfgang.Audit.EFCore.Example.WebApi
```

Then create a product as Steve:

```
curl -X POST http://localhost:5000/products \
  -H "Content-Type: application/json" \
  -H "X-User: steve" \
  -d '{"name":"Widget","price":9.99}'
```

And read the audit log:

```
curl http://localhost:5000/audit
```

You'll see the header with `userId = svc-orders` and `onBehalfOfUserId = steve`.

The `X-User` header is a demo shortcut — a real app would resolve the acting
user from `HttpContext.User` after authentication. See
[`HttpContextAuditUserProvider`](./HttpContextAuditUserProvider.cs).

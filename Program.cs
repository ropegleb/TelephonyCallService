using TelephonyCallService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SessionRepository>();
builder.Services.AddHostedService<CleanupService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Normalize double slashes before routing kicks in
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && path.Contains("//"))
        context.Request.Path = "/" + path.TrimStart('/');
    await next();
});

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/from", async (HttpContext ctx, SessionRepository repo) =>
{
    string body;
    using (var reader = new StreamReader(ctx.Request.Body))
        body = await reader.ReadToEndAsync();

    string? contact, from;
    try
    {
        var req = System.Text.Json.JsonSerializer.Deserialize<PostFromRequest>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        contact = req?.Contact;
        from = req?.From;
    }
    catch (System.Text.Json.JsonException)
    {
        (contact, from) = BodyParser.ParseRaw(body);
    }

    if (string.IsNullOrEmpty(contact))
        return Results.BadRequest(new { error = "contact is required" });

    var xi = ContactParser.ExtractXi(contact);
    if (xi is null)
        return Results.BadRequest(new { error = "x-i parameter not found in contact header" });

    repo.Save(xi, from ?? string.Empty);
    return Results.Ok();
})
.Accepts<PostFromRequest>("application/json")
.Produces(200)
.Produces(400)
.WithName("PostFrom")
.WithTags("From");

app.MapGet("/from", (string? contact, SessionRepository repo) =>
{
    if (string.IsNullOrEmpty(contact))
        return Results.BadRequest(new { error = "contact parameter is required" });

    var xi = ContactParser.ExtractXi(contact);
    if (xi is null)
        return Results.BadRequest(new { error = "x-i parameter not found in contact header" });

    var from = repo.GetFrom(xi);
    return Results.Ok(new { from = from ?? string.Empty });
})
.Produces<object>(200)
.Produces(400)
.WithName("GetFrom")
.WithTags("From");

app.Run();

using TodoApi;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;  // חשוב להוסיף את ה-namespace הזה

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// הזרקה של MySqlConnection לשירותים
builder.Services.AddTransient<MySqlConnection>(sp =>
    new MySqlConnection(builder.Configuration.GetConnectionString("ToDoDB")));

// הוסף Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });
});

var app = builder.Build();

app.UseCors("AllowAllOrigins");

// הפעל את Swagger וה-UI שלו
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1");
    c.RoutePrefix = string.Empty; // אם אתה רוצה ש-Swagger ייפתח על דף הבית
});

// קריאה לשליפת כל הפריטים
app.MapGet("/get", async (MySqlConnection db) =>
{
    var items = await db.QueryAsync<Item>("SELECT * FROM practicod.items");
    return Results.Ok(items);
});

// קריאה להוספת פריט חדש
app.MapPost("/items", async (MySqlConnection db, Item newItem) =>
{
    
    var query = "INSERT INTO practicod.items (name, IsComplete) VALUES (@Name, @IsComplete)";
    await db.ExecuteAsync(query, newItem);

    // כדי לקבל את ה-Id החדש שהוקצה באופן אוטומטי בבסיס הנתונים
    newItem.Id = await db.QuerySingleAsync<int>("SELECT LAST_INSERT_ID()");

    return Results.Created($"/items/{newItem.Id}", newItem);
});

// קריאה לעדכון פריט קיים
app.MapPut("/items/{id}", async (int id, bool complete, MySqlConnection db) =>
{
    Item? existingItem = await db.QueryFirstOrDefaultAsync<Item>("SELECT * FROM practicod.items WHERE Id = @Id", new { Id = id });

    if (existingItem == null)
    {
        return Results.NotFound();
    }

    await db.ExecuteAsync("UPDATE practicod.items SET IsComplete = @IsComplete WHERE Id = @Id", new { Id = id, IsComplete = complete });

    return Results.Ok(existingItem);
});

// קריאה למחיקת פריט
app.MapDelete("/items/{id}", async (MySqlConnection db, int id) =>
{
    var existingItem = await db.QueryFirstOrDefaultAsync<Item>("SELECT * FROM practicod.items WHERE id = @Id", new { Id = id });

    if (existingItem == null)
    {
        return Results.NotFound();
    }

    await db.ExecuteAsync("DELETE FROM practicod.items WHERE id = @Id", new { Id = id });

    return Results.NoContent();
});

app.Run();

using System.Data.SQLite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));
SQLiteConnection connection = DatabaseConnector.Db();
SQLiteCommand command = connection.CreateCommand();
command.CommandText = "PRAGMA foreign_keys = ON;" +
    "CREATE TABLE if not Exists `User` " +
    "(`UserID` integer NOT NULL PRIMARY KEY, `Email` text not NULL UNIQUE, `PasswordHash` text not NULL, " +
    "`PasswordSalt` text not NULL); " +
    "CREATE TABLE if not Exists `Session`" +
    " (`SessionID` integer NOT NULL PRIMARY KEY, `SessionCookie` text not null UNIQUE," +
    " `UserID` integer not null, " +
    "`ValidUntil` integer not null, `LoginTime` integer not null, " +
    "FOREIGN KEY (`UserID`) REFERENCES `User`(`UserID`));" +
    "CREATE TABLE if not Exists `Entry` (`EntryID` integer NOT NULL PRIMARY KEY, " +
    "`Title` text, `Content` text, " +
    "`AuthorID` integer NOT NULL, `CreationTime` integer NOT NULL, " +
    "FOREIGN KEY (`AuthorID`) REFERENCES `User`(`UserID`));";
command.ExecuteNonQuery();
command.Dispose();
app.Run();
using BlogEntries.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Data.SQLite;

//Postrequest listaba eltarolni a CreationDate, EntryContent, EntryUser

[ApiController]
[Route("[controller]/[action]")]
public class UserController : Controller
{

    [HttpPost]
    public IActionResult Create([FromForm] string email, [FromForm] string password)
    {
        string salt = PasswordManager.GenerateSalt();
        string hashedPassword = PasswordManager.GeneratePasswordHash(password, salt);

        SQLiteConnection connection = DatabaseConnector.Db();
        string insertSql = "INSERT INTO User (Email, PasswordHash, PasswordSalt) VALUES (@Email, @PasswordHash, @PasswordSalt)";
        using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
        {
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            cmd.Parameters.AddWithValue("@PasswordSalt", salt);
            cmd.ExecuteNonQuery();
        }
        return Ok("User created successfully");
    }

    [HttpPost]
    public IActionResult Login([FromForm] string email, [FromForm] string password)
    {
        SQLiteConnection connection = DatabaseConnector.Db();
        string selectSql = "SELECT UserID, PasswordHash, PasswordSalt FROM User WHERE Email = @Email";
        using (SQLiteCommand cmd = new SQLiteCommand(selectSql, connection))
        {
            cmd.Parameters.AddWithValue("@Email", email);
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string? storedPasswordHash = reader["PasswordHash"].ToString();
                    string? storedSalt = reader["PasswordSalt"].ToString();

                    if (!string.IsNullOrEmpty(storedPasswordHash) && !string.IsNullOrEmpty(storedSalt) &&
                        PasswordManager.Verify(password, storedSalt, storedPasswordHash))
                    {
                        Int64 userID = Convert.ToInt64(reader["UserID"]);
                        string sessionCookie = SessionManager.CreateSession(userID);
                        Response.Cookies.Append("id", sessionCookie);
                        return Ok("Login successful. Session Cookie: " + sessionCookie);
                    }
                }
            }
        }
        return Unauthorized("Invalid email or password");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return new UnauthorizedResult();
        }
        SessionManager.InvalidateSession(sessionId);
        return Ok("Logout successful");

    }

    static public bool IsLoggedIn(string SessionCookie)
    {
        Int64 userID = SessionManager.GetUserID(SessionCookie);
        return userID != -1;
    }

    [HttpGet]
    public IActionResult GetUser()
    {
        var sessionId = Request.Cookies["id"];
        if (string.IsNullOrEmpty(sessionId))
        {
            return new UnauthorizedResult();
        }
        Int64 userID = SessionManager.GetUserID(sessionId);
        if (userID == -1)
        {
            return new UnauthorizedResult();
        }
        return Json(userID);
    }
}
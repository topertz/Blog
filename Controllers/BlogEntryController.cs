using BlogEntries;
using BlogEntries.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Data.SQLite;

[ApiController]
[Route("[controller]/[action]")]
public class BlogEntryController : Controller
{
    [HttpGet]
    public IActionResult GetBlockEntries()
    {
        List<Data> datas = new List<Data>();
        string insertSql = "SELECT * FROM Entry";
        using (SQLiteConnection connection = DatabaseConnector.CreateNewConnection())
        {
            using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
            {
                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Data data = new()
                    {
                        id = reader.GetInt32(0),
                        title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        text = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        authorID = reader.GetInt32(3),
                        date = reader.GetInt32(4)
                    };
                    datas.Add(data);
                }
            }
        }
        return Json(datas);
    }

    [HttpGet]
    public IActionResult GetUserEntries()
    {
        
        var sessionId = Request.Cookies["id"];

        Int64 userID = SessionManager.GetUserID(sessionId);
        bool isValidUser = userID != -1;

        List<Data> datas = new List<Data>();
        string insertSql = @"
        SELECT e.EntryID, e.Title, e.Content, e.AuthorID, e.CreationTime, u.Email 
        FROM Entry e
        JOIN User u ON e.AuthorID = u.UserID
        WHERE e.AuthorID = @UserID OR @IsValidUser = 0";

        using (SQLiteConnection connection = DatabaseConnector.CreateNewConnection())
        {
            using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@IsValidUser", isValidUser ? 1 : 0);

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Data data = new()
                    {
                        id = reader.GetInt32(0),
                        title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        text = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        authorID = reader.GetInt32(3),
                        date = reader.GetInt32(4)
                    };
                    datas.Add(data);
                }
            }
        }
        return Json(datas);
    }

    [HttpPost]
    public IActionResult CreateBlogEntry([FromForm] string? title, [FromForm] string? content)
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
        if (!UserController.IsLoggedIn(sessionId))
        {
            return Unauthorized("User is not logged in.");
        }
        /*if(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            return BadRequest("Title and content is null");
        }*/

        SQLiteConnection connection = DatabaseConnector.Db();
        string insertSql = "INSERT INTO Entry (Title, Content, AuthorID, CreationTime) VALUES (@Title, @Content, @AuthorID, @CreationTime)";
        using (SQLiteCommand cmd = new SQLiteCommand(insertSql, connection))
        {
            cmd.Parameters.AddWithValue("@Title", title ?? "");
            cmd.Parameters.AddWithValue("@Content", content ?? "");
            cmd.Parameters.AddWithValue("@AuthorID", userID);
            cmd.Parameters.AddWithValue("@CreationTime", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            cmd.ExecuteNonQuery();
        }
        return Ok("Blog entry created successfully");
    }

    [HttpPost]
    public IActionResult UpdateBlogEntry([FromForm] string? postTitle, [FromForm] string? postContent, [FromForm] string? newTitle, [FromForm] string? newContent)
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
        if (!UserController.IsLoggedIn(sessionId))
        {
            return Unauthorized("User is not logged in.");
        }

        SQLiteConnection connection = DatabaseConnector.Db();
        string updateSql = @"
            UPDATE Entry 
            SET Title = COALESCE(NULLIF(@NewTitle, ''), Title), 
                Content = COALESCE(NULLIF(@NewContent, ''), Content) 
            WHERE AuthorID = @AuthorID 
            AND (@postTitle IS NULL OR Title = @postTitle)
            AND (@postContent IS NULL OR Content = @postContent)";
        using (SQLiteCommand cmd = new SQLiteCommand(updateSql, connection))
        {
            cmd.Parameters.AddWithValue("@NewTitle", newTitle ?? "");
            cmd.Parameters.AddWithValue("@NewContent", newContent ?? "");
            cmd.Parameters.AddWithValue("@postTitle", postTitle ?? "");
            cmd.Parameters.AddWithValue("@postContent", postContent ?? "");
            cmd.Parameters.AddWithValue("@AuthorID", userID);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("Entry not found or user is not the author.");
            }
        }
        return Ok("Blog entry updated successfully");
    }

    [HttpPost]
    public IActionResult DeleteBlogEntry([FromForm] string? title, [FromForm] string? content)
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
        if (!UserController.IsLoggedIn(sessionId))
        {
            return Unauthorized("User is not logged in.");
        }

        SQLiteConnection connection = DatabaseConnector.Db();
        string deleteSql = @"
            DELETE FROM Entry
            WHERE AuthorID = @AuthorID
            AND(@Title IS NULL OR Title = @Title)
            AND(@Content IS NULL OR Content = @Content)";
        using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, connection))
        {
            cmd.Parameters.AddWithValue("@Title", title ?? "");
            cmd.Parameters.AddWithValue("@Content", content ?? "");
            cmd.Parameters.AddWithValue("@AuthorID", userID);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("Entry not found or user is not the author.");
            }
            cmd.Parameters.Clear();
        }
        return Ok("Blog entry deleted successfully");
    }

    [HttpPost]
    public IActionResult UpdateAllBlogEntries([FromForm] string? newTitle, [FromForm] string? newContent)
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
        if (!UserController.IsLoggedIn(sessionId))
        {
            return Unauthorized("User is not logged in.");
        }

        SQLiteConnection connection = DatabaseConnector.Db();
        string updateSql = @"
            UPDATE Entry 
            SET Title = COALESCE(NULLIF(@NewTitle, ''), Title), 
                Content = COALESCE(NULLIF(@NewContent, ''), Content) 
            WHERE AuthorID = @AuthorID";
        using (SQLiteCommand cmd = new SQLiteCommand(updateSql, connection))
        {
            cmd.Parameters.AddWithValue("@NewTitle", newTitle ?? "");
            cmd.Parameters.AddWithValue("@NewContent", newContent ?? "");
            cmd.Parameters.AddWithValue("@AuthorID", userID);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("No entries found for this user.");
            }
        }
        return Ok("All blog entries updated successfully");
    }

    [HttpPost]
    public IActionResult DeleteAllBlogEntries()
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
        if (!UserController.IsLoggedIn(sessionId))
        {
            return Unauthorized("User is not logged in.");
        }

        SQLiteConnection connection = DatabaseConnector.Db();
        string deleteSql = "DELETE FROM Entry WHERE AuthorID = @AuthorID";
        using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, connection))
        {
            cmd.Parameters.AddWithValue("@AuthorID", userID);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound("No entries found for this user.");
            }
        }
        return Ok("All blog entries deleted successfully");
    }
}
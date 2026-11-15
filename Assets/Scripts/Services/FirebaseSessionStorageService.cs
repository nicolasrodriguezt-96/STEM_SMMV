using System;
using System.IO;
using SQLite;
using UnityEngine;

// ========================================
// MODELO SQLITE
// ========================================

[Table("firebase_session")]
public class FirebaseSessionRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string UserId { get; set; }

    public string RefreshToken { get; set; }
}

// ========================================
// ALMACENAMIENTO DE SESIÓN
// ========================================

public class FirebaseSessionStorageService
{
    private readonly string databasePath;

    public FirebaseSessionStorageService()
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Data"
        );

        Directory.CreateDirectory(directory);

        databasePath = Path.Combine(
            directory,
            "stem_results.db"
        );
    }

    private SQLiteConnection OpenDatabase()
    {
        SQLiteConnection connection =
            new SQLiteConnection(
                databasePath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create
            );

        connection.Execute(
            "PRAGMA journal_mode = WAL;"
        );

        connection.Execute(
            "PRAGMA synchronous = FULL;"
        );

        connection.CreateTable<FirebaseSessionRecord>();

        return connection;
    }

    // ========================================
    // RECUPERAR SESIÓN
    // ========================================

    public FirebaseSessionRecord LoadSession()
    {
        using (SQLiteConnection database =
            OpenDatabase())
        {
            return database.Find<FirebaseSessionRecord>(1);
        }
    }

    // ========================================
    // GUARDAR SESIÓN
    // ========================================

    public void SaveSession(
        string userId,
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "Los datos de la sesión Firebase son inválidos."
            );
        }

        FirebaseSessionRecord session =
            new FirebaseSessionRecord
            {
                Id = 1,
                UserId = userId,
                RefreshToken = refreshToken
            };

        using (SQLiteConnection database =
            OpenDatabase())
        {
            database.RunInTransaction(() =>
            {
                database.InsertOrReplace(session);
            });
        }

        Debug.Log(
            "Sesión Firebase guardada localmente."
        );
    }
}
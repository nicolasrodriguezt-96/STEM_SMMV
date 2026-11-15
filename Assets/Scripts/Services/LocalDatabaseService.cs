using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;

public class LocalDatabaseService : IDisposable
{
    private readonly SQLiteConnection connection;

    public string DatabasePath { get; private set; }

    // ========================================
    // INICIALIZAR BASE DE DATOS
    // ========================================

    public LocalDatabaseService()
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "STEM",
            "Data"
        );

        // Crear carpeta si no existe
        Directory.CreateDirectory(directory);

        DatabasePath = Path.Combine(
            directory,
            "stem_results.db"
        );

        // Abrir o crear base de datos
        connection = new SQLiteConnection(
            DatabasePath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create
        );

        // Mejorar resistencia ante apagados
        connection.Execute(
            "PRAGMA journal_mode = WAL;"
        );

        connection.Execute(
            "PRAGMA synchronous = FULL;"
        );

        // Crear tabla si todavía no existe
        connection.CreateTable<LocalResultRecord>();

        Debug.Log(
            "Base de datos SQLite disponible en: " +
            DatabasePath
        );
    }

    // ========================================
    // GUARDAR RESULTADO
    // ========================================

    public void SaveResult(LocalResultRecord result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(
                nameof(result)
            );
        }

        // Generar ID si no existe
        if (string.IsNullOrWhiteSpace(result.Id))
        {
            result.Id = Guid.NewGuid().ToString();
        }

        // Todo resultado nuevo queda pendiente
        result.SyncStatus = 0;

        result.SyncedAtUtc = null;

        // Guardar mediante una transacción
        connection.RunInTransaction(() =>
        {
            connection.Insert(result);
        });

        Debug.Log(
            "Resultado guardado localmente: " +
            result.Id
        );
    }

    // ========================================
    // OBTENER TODOS LOS RESULTADOS
    // ========================================

    public List<LocalResultRecord> GetAllResults()
    {
        return connection
            .Table<LocalResultRecord>()
            .ToList();
    }

    // ========================================
    // OBTENER RESULTADOS PENDIENTES
    // ========================================

    public List<LocalResultRecord> GetPendingResults(
        int limit = 50)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit)
            );
        }

        return connection.Query<LocalResultRecord>(
            "SELECT * FROM result_executions " +
            "WHERE SyncStatus = 0 " +
            "ORDER BY FinishedAtUtc ASC " +
            "LIMIT ?",
            limit
        );
    }

    // ========================================
    // MARCAR RESULTADO COMO SINCRONIZADO
    // ========================================

    public void MarkAsSynced(string resultId)
    {
        if (string.IsNullOrWhiteSpace(resultId))
        {
            return;
        }

        LocalResultRecord result =
            connection.Find<LocalResultRecord>(
                resultId
            );

        if (result == null)
        {
            return;
        }

        result.SyncStatus = 1;

        result.SyncedAtUtc =
            DateTime.UtcNow.ToString("o");

        connection.RunInTransaction(() =>
        {
            connection.Update(result);
        });

        Debug.Log(
            "Resultado sincronizado: " +
            resultId
        );
    }

    // ========================================
    // CONTAR RESULTADOS
    // ========================================

    public int CountResults()
    {
        return connection
            .Table<LocalResultRecord>()
            .Count();
    }

    // ========================================
    // CERRAR CONEXIÓN
    // ========================================

    public void Dispose()
    {
        if (connection != null)
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
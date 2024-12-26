using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace MangaReader.MangaList;

public class FavoritesMangaDto
{
    [BsonId] public string Url { get; init; } = null;
    public string Title { get; init; } = null;
}
public class Db : IDb
{
    private readonly string dbFile;

    public Db(string dbFile)
    {
        this.dbFile = dbFile;
    }

    private ILiteCollection<FavoritesMangaDto> GetFavoritesMangaCollection(ILiteDatabase db)
    {
        return db.GetCollection<FavoritesMangaDto>("FavoritesMangas");
    }

    public List<FavoritesManga> LoadFavoritesManga()
    {
        using var db= new LiteDatabase(dbFile);
        return this.GetFavoritesMangaCollection(db).FindAll()
            .Select(dto => new FavoritesManga(dto.Url, dto.Title))
            .ToList();
    }
    

    public void InsertFavoritesManga(FavoritesManga manga)
    {
        using var db = new LiteDatabase(dbFile);
        this.GetFavoritesMangaCollection(db)
            .Insert(new FavoritesMangaDto { Url = manga.Url, Title = manga.Title });
    }

    public void DeleteFavoritesManga(string url)
    {
        using var db = new LiteDatabase(dbFile);
        this.GetFavoritesMangaCollection(db).Delete(url);
    }
}
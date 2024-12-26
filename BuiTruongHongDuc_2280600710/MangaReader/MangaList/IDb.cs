using System.Collections.Generic;

namespace MangaReader.MangaList;

public interface IDb
{
    List<FavoritesManga> LoadFavoritesManga();
    void InsertFavoritesManga(FavoritesManga manga);
    void DeleteFavoritesManga(string url);
}
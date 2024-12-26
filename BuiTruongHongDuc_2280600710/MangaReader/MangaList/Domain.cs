using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Web;
using Avalonia.Controls.Chrome;
using MangaReader.DomainCommon;

namespace MangaReader.MangaList;

public class Manga
{
    public Manga(string title, string description, string coverUrl, string lastChapter, string mangaUrl)
    {
        Title = title;
        Description = description;
        CoverUrl = coverUrl;
        LastChapter = lastChapter;
        MangaUrl = mangaUrl;
    }

    public string Title { get; }

    public string Description { get; }
    public string CoverUrl { get; }
    public string LastChapter { get; }
    public string MangaUrl { get; }
}

public class FavoritesManga
{
    public string Url { get; }
    public string Title { get; }

    public FavoritesManga(string url, string title)
    {
        this.Url = url;
        this.Title = title;
    }
}
public class MangaList
{
    public int TotalMangaNumber { get;}
    public int TotalPageNumber { get;}
    public List<Manga> CurrentPage { get;}

    public MangaList(int totalMangaNumber, int totalPageNumber, List<Manga> currentPage)
    {
        TotalMangaNumber = totalMangaNumber;
        TotalPageNumber = totalPageNumber;
        CurrentPage = currentPage;
    }
    
}

public class Domain
{
    private readonly string baseUrl;
    private readonly Http http;
    private readonly IDb db;
    private readonly List<FavoritesManga> favoritesMangas;

    public Domain(string baseUrl, Http http, IDb db)
    {
        this.baseUrl = baseUrl;
        this.http = http;
        this.db = db;
        favoritesMangas = db.LoadFavoritesManga();
        this.SortFavoriesMangas();
    }

    private void SortFavoriesMangas()
    {
        favoritesMangas.Sort((manga1, manga2) => string.Compare(manga1.Title, manga2.Title, StringComparison.OrdinalIgnoreCase));
    }
    private Task<string> DownloadHtml(int page, string filterText)
    {
        if (page < 1) page = 1;
        string url;
        if (filterText == "")
        {
            url = $"{this.baseUrl}/filter?status=0&sort=updatedAt&page={page}";
        }
        else
        {
            var text = HttpUtility.HtmlDecode(filterText);
            url =$"{this.baseUrl}/tim-kiem?keyword={text}&page={page}";
        }
        Console.WriteLine($"Downloading page {page} from {url}");
        return http.GetStringAsync(url);
    }

    // private int ParseTotalMangaNumber(XmlDocument doc)
    // {
    //     //hdcphu@ updated for https://apptruyen247.com
    //     var text = doc.DocumentElement!.FirstChild!.FirstChild!.InnerText.Trim();
    //     vfar number = text.Substring(7);
    //     return int.Parse(number);
    // }

     private int ParseTotalPageNumber(HtmlNode section)
     {
         var a = section.QuerySelector("nav.paging-new li:last-child > a");
         if(a == null)
             return 1;
         var href = a.Attributes["href"].Value;
         var equalIndex = href.LastIndexOf('=');
         var page = href[(equalIndex + 1)..];
         return int.Parse(page);
     }
    private int FindTotalPageNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalPages") + 13);
        s = s.Substring(0, s.IndexOf(","));
        return int.Parse(s);
    }

    private int FindTotalMangaNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalDocs")+12);
        s = s.Substring(0, s.IndexOf("}"));
        return int.Parse(s);
    }
    private List<Manga> ParseMangaList(HtmlNode section)
    {
        List<Manga> mangaList = new List<Manga>();
        var bookWrapperNodes = section.QuerySelector("div.grid").QuerySelectorAll("div.book-wrapper").ToArray();
        
        for (int i = 0; i < bookWrapperNodes.Length; i++)
        {
            var bookNode = bookWrapperNodes[i];
            var a_relative = bookNode.QuerySelector("div > a");
            var div_content = bookNode.QuerySelector("div > div");
            
            var title = Html.Decode(div_content.QuerySelector("a").InnerText.Trim());
            var description = Html.Decode(div_content.QuerySelector("div").InnerText.Trim());
            var div_lastChapter = div_content.QuerySelector("div.flex.flex-col.flex-1.pr-4.justify-end.w-full > a");
            var lastChapter = Html.Decode(div_lastChapter?.InnerText.Trim());
            var coverUrl = baseUrl + Html.Decode(a_relative.QuerySelector("img").Attributes!["src"]!.Value);
            var mangaUrl = baseUrl + Html.Decode(a_relative.Attributes!["href"]!.Value);

            var manga = new Manga(title, description, coverUrl, lastChapter, mangaUrl);
            mangaList.Add(manga);
            Console.WriteLine($"{i} : Title: {title} Description: {description} Chapter Url: {lastChapter}{Environment.NewLine} CoverUrl ={coverUrl},{Environment.NewLine} MangaUrl = {mangaUrl}");
        }
        return mangaList;
    }

    private MangaList Parse(string html)
    {
        try
        {
            var totalPageNumber = FindTotalPageNumber(html);
            var totalMangaNumber = FindTotalMangaNumber(html);

            File.WriteAllText("docbefore.html",html);
            var xmlStartAt= html.IndexOf("<div class=\"grid grid-cols-1");
            html = html.Substring(xmlStartAt);
            html = html.Substring(0, html.IndexOf("<div class=\"mt-6\">"));
            var doc = new HtmlDocument();
            doc.LoadHtml("<html> <head/> <body>" + html + "</body></html>");
            var section = doc.DocumentNode;
            if (section == null)
                return new MangaList(0, 0, new List<Manga>());
            return new MangaList(
                totalPageNumber: totalPageNumber,
                totalMangaNumber: totalMangaNumber,
                currentPage: this.ParseMangaList(section));

        }
        catch (Exception e)
        {
            throw new ParseException();
        }
    }

    public async Task<MangaList> LoadMangaList(int page, string filterText = "")
    {
        var html = await DownloadHtml(page, filterText);
        return this.Parse(html);
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetBytesAsync(url, token);
    }

    public IEnumerable<string> GetFavoritesMangaTitles()
    {
        return favoritesMangas.Select(favoritesManga => favoritesManga.Title);
    }

    public string? GetFavoritesMangaUrl(int index)
    {
        if(index < 0 || index >= favoritesMangas.Count)
            return null;
        return favoritesMangas[index].Url;
    }

    public bool IsFavoritesManga(string mangaUrl)
    {
        return favoritesMangas.Exists(manga => manga.Url == mangaUrl);
    }

    public void AddFavoritesManga(string url, string title)
    {
        if (this.IsFavoritesManga(url)) return;
        var favoritesManga = new FavoritesManga(url, title);
        favoritesMangas.Add(favoritesManga);
        this.SortFavoriesMangas();
        db.InsertFavoritesManga(favoritesManga);
    }

    public void RemoveFavoritesManga(string url)
    {
        favoritesMangas.RemoveAll(manga => manga.Url == url);
        db.DeleteFavoritesManga(url);
    }
}
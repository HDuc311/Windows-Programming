using System.Collections.Generic;

namespace MangaReader.MangaList;

public class Item
{
    public Item(string title, string chapterNumber, string description, bool isFavorites)
    {
        Title = title;
        ChapterNumber = chapterNumber;
        Description = description;
        IsFavorites = isFavorites;
    }
    public string Title { get; }
    public string ChapterNumber { get; }
    public string Description { get; }
    public bool IsFavorites { get; }
    public string ToolTip => this.Title+" - " + this.Description;

}

public interface IView
{
    void SetLoadingVisible(bool value);
    void SetErrorPanelVisible(bool value);
    void SetMainContentVisible(bool value);

    void SetTotalMangaNumber(string text);
    void SetCurrentPageButtonContent(string content);
    void SetCurrentPageButtonEnabled(bool value);
    
    void SetNumericUpDownMaximum(int value);
    void SetNumericUpDownValue(int value);
    int GetNumericUpDownValue();
    void SetListBoxContent(IEnumerable<Item> items);
    void SetCover(int index, byte[]? bytes);
    
    void SetFirstButtonAndPrevButtonEnabled(bool value);
    void SetLastButtonAndNextButtonEnabled(bool value);

    void HideFlyout();

    void SetErrorMessage(string text);
    string? GetFilterText();
    void OpenMangaDetail(string mangaUrl);
    void SetFavoritesManga(IEnumerable<string> mangaTitles);
    void UpdateFavoritesManga(int index, bool value);
}
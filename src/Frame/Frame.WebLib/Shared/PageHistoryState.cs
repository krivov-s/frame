
namespace Frame.WebLib.Shared
{
    public class PageHistoryState
    {
        private readonly List<string> _previousPages = [];

        public void AddPageToHistory(string pageName)
        {
            _previousPages.Add(pageName);
        }
        public void RemoveLastHistoryPage()
        {
            _previousPages.RemoveAt(_previousPages.Count - 1);
        }

        public string GetGoBackPage()
        {
            if (_previousPages.Count > 1)
            {
                // You add a page on initialization, so you need to return the 2nd from the last
                return _previousPages.ElementAt(_previousPages.Count - 2);
            }

            // Can't go back because you didn't navigate enough
            return _previousPages.FirstOrDefault()!;
        }

        public bool CanGoBack()
        {
            return _previousPages.Count > 1;
        }
    }
}

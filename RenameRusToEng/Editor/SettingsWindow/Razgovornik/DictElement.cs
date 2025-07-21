namespace RenameRusToEng
{

    /// <summary>
    /// Элемент представляемого программой Разговорника.
    /// </summary>
    public class DictElement
    {
        public int order;
        public string rus;
        public string eng;
        public DictElement(string a, string b)
        {
            rus = a;
            eng = b;
        }
    }
}
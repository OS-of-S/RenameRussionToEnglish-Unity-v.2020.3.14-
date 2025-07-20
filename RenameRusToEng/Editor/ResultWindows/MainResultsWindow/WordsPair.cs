using UnityEngine;
using UnityEngine.UIElements;

namespace RenameRusToEng
{

    /// <summary>
    /// UI-элемент, репрезентирующий отдельный элемент списка-автодополнения к Разговорнику.
    /// </summary>
    class WordsPair : VisualElement
    {
        const int WordHeight = 20;
        public TextElement rus_word;
        public TextElement eng_word;
        static Color color_gray = ResultsMainWindow.obvodka_color / 3;

        public WordsPair(float WordsListWidth)
        {
            int half_width = (int)(WordsListWidth / 2);
            Box obvodka_1 = new Box()
            {
                style =
                {
                    width = half_width,
                    flexGrow = 1,
                    //backgroundColor = new Color(42, 42, 42, 1),
                    borderTopColor = color_gray,
                    borderBottomColor = color_gray,
                    borderLeftColor = color_gray,
                    borderRightColor = color_gray,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderRightWidth = 1,
                }
            };

            Box obvodka_2 = new Box()
            {
                style =
                {
                    width = half_width,
                    flexGrow = 1,
                    //backgroundColor = new Color(42, 42, 42, 1),
                    borderTopColor = color_gray,
                    borderBottomColor = color_gray,
                    borderLeftColor = color_gray,
                    borderRightColor = color_gray,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                }
            };

            const int otstup = 2;
            rus_word = new TextElement() { style = { borderTopWidth = otstup, borderBottomWidth = otstup, borderRightWidth = otstup, borderLeftWidth = otstup, } };
            eng_word = new TextElement() { style = { borderTopWidth = otstup, borderBottomWidth = otstup, borderRightWidth = otstup, borderLeftWidth = otstup, } };
            ScrollView scroller1 = new ScrollView(ScrollViewMode.Horizontal) { style = { height = WordHeight } };
            ScrollView scroller2 = new ScrollView(ScrollViewMode.Horizontal) { style = { height = WordHeight } };
            //scroller1.Add(rus_word);
            //scroller2.Add(eng_word);
            //obvodka_1.Add(scroller1);
            //obvodka_2.Add(scroller2);
            obvodka_1.Add(rus_word);
            obvodka_2.Add(eng_word);
            VisualElement Row = new VisualElement() { style = { flexDirection = FlexDirection.Row, width = WordsListWidth, flexGrow = 1 } };
            Row.Add(obvodka_1);
            Row.Add(obvodka_2);
            Add(Row);
        }

        public void SetWords(string word_1, string word_2)
        {
            rus_word.text = word_1;
            eng_word.text = word_2;
        }
    }
}
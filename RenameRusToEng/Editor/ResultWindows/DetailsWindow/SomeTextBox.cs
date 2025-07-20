using UnityEngine;
using UnityEngine.UIElements;

namespace RenameRusToEng
{

    /// <summary>
    /// Ёлемент UI, отображающий текстовый блок с рамкой и наименованием.
    /// </summary>
    class SomeTextBox : VisualElement
    {
        const int gap = DetailsWindow.gap;
        const int gap_smaller = 3;
        static Color outline_color = DetailsWindow.outline_color;
        private TextElement info_field;
        public string text
        {
            get => info_field.text;

            set
            {
                info_field.text = value;
            }
        }

        public SomeTextBox(string FieldName, string text)
        {
            style.borderTopWidth = gap;
            style.borderBottomWidth = gap;
            style.borderRightWidth = gap;
            style.borderLeftWidth = gap;

            TextElement label = new TextElement()
            {
                text = FieldName + ": ",
                style =
                        {
                            width = 100
                        }
            };

            Box obvodka = new Box()
            {
                style =
                    {
                        flexGrow = 1,
                        alignSelf = Align.Stretch,
                        //backgroundColor = new Color(42, 42, 42, 1),
                        borderTopColor = outline_color,
                        borderBottomColor = outline_color,
                        borderLeftColor = outline_color,
                        borderRightColor = outline_color,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                    }
            };
            ScrollView scroll_view = new ScrollView() { style = { height = 50, } };
            info_field = new TextElement()
            {
                text = text,
                style = {
                        borderTopWidth = gap_smaller,
                        borderBottomWidth = gap_smaller,
                        borderRightWidth = gap_smaller,
                        borderLeftWidth = gap_smaller,
                    }
            };
            scroll_view.Add(info_field);
            obvodka.Add(scroll_view);

            style.flexDirection = FlexDirection.Row;
            Add(label);
            Add(obvodka);
        }
    }
}